using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 根据关卡地形生成静态格子，生成玩家和箱子模型，并把它们同步到给定状态。
    /// 共享格子和目标箱子在所有世界都显示；独有墙和传送箱子只显示玩家所在世界的。
    /// 关卡整体居中在本物体的位置。
    /// </summary>
    public class LevelView : MonoBehaviour
    {
        private const string SharedTilesRootName = "SharedTiles";
        private const string WorldTilesRootPrefix = "WorldTiles_";
        private const string EntitiesRootName = "Entities";

        private const float GoalFlashDuration = 0.6f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private LevelVisualConfig _visualConfig;

        private readonly List<EntityView> _mappedBoxViews = new List<EntityView>();
        private readonly List<PortalBoxView> _portalBoxViews = new List<PortalBoxView>();
        private readonly List<Transform> _worldTileRoots = new List<Transform>();
        private readonly List<List<PlacedTile>> _uniqueWallTiles = new List<List<PlacedTile>>();
        private readonly List<PlacedTile> _goalTiles = new List<PlacedTile>();
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _revealRoutine;
        private Coroutine _goalFlashRoutine;
        private Transform _sharedTilesRoot;
        private Transform _entitiesRoot;
        private EntityView _playerView;
        private int _shownWorld = -1;

        /// <summary>玩家模型，关卡未生成时为 null。</summary>
        public EntityView PlayerView => _playerView;

        /// <summary>目标箱子数量。</summary>
        public int MappedBoxViewCount => _mappedBoxViews.Count;

        /// <summary>一格的边长（世界单位），未配置时为 1。</summary>
        public float CellSize => _visualConfig != null ? _visualConfig.CellSize : 1f;

        /// <summary>当前关卡在世界空间中的范围（含一格高度），供相机取景。未生成关卡时大小为零。</summary>
        public Bounds LevelBounds { get; private set; }

        /// <summary>显示的世界变了。传送时在画面被颜色充满的那一刻触发，撤销和重开时随同步立即触发。</summary>
        public event Action<int> ShownWorldChanged;

        private void Awake()
        {
            if (_visualConfig == null)
            {
                Debug.LogError($"[{nameof(LevelView)}] {name} 未设置 Visual Config", this);
            }
        }

        /// <summary>
        /// 按箱子编号取模型，编号与 <see cref="LevelState.MappedBoxes"/> 下标一致。
        /// </summary>
        public EntityView GetMappedBoxView(int boxIndex)
        {
            return _mappedBoxViews[boxIndex];
        }

        /// <summary>
        /// 按下标取传送箱子模型，下标与 <see cref="LevelState.PortalBoxes"/> 一致。
        /// </summary>
        public PortalBoxView GetPortalBoxView(int portalIndex)
        {
            return _portalBoxViews[portalIndex];
        }

        /// <summary>
        /// 网格方向在实体根节点下对应的单位向量，用于回弹、挤压等效果。
        /// </summary>
        public Vector3 DirectionToLocal(Direction direction)
        {
            return _visualConfig.GridToLocal(direction.ToOffset()) - _visualConfig.GridToLocal(new GridPos(0, 0));
        }

        /// <summary>
        /// 网格坐标转换为世界坐标（格子底面中心）。
        /// </summary>
        public Vector3 GridToWorld(GridPos pos)
        {
            Vector3 local = _visualConfig.GridToLocal(pos);
            return _entitiesRoot != null ? _entitiesRoot.TransformPoint(local) : transform.TransformPoint(local);
        }

        /// <summary>某个世界的代表色，用于世界切换时的遮罩颜色。</summary>
        public Color GetWorldColor(int world)
        {
            return _visualConfig != null ? _visualConfig.GetWorldColor(world) : Color.black;
        }

        /// <summary>
        /// 新世界从 <paramref name="origin"/>（传送底座）向外展开：独有墙按距离依次从地面升起，传送箱子依次放大出现。
        /// 整个展开在 <paramref name="duration"/> 秒内完成。
        /// </summary>
        public void PlayWorldReveal(int world, GridPos origin, float duration)
        {
            CompleteReveal();
            if (world < 0 || world >= _uniqueWallTiles.Count || duration <= 0f)
            {
                return;
            }

            // 一半时间用于按距离错开起始，一半用于每个物体自身的升起，保证最远的也在时限内完成
            float maxDistance = 1f;
            List<PlacedTile> walls = _uniqueWallTiles[world];
            for (int i = 0; i < walls.Count; i++)
            {
                maxDistance = Mathf.Max(maxDistance, Distance(origin, walls[i].Position));
            }

            for (int i = 0; i < _portalBoxViews.Count; i++)
            {
                if (_portalBoxViews[i].gameObject.activeSelf)
                {
                    maxDistance = Mathf.Max(maxDistance, Distance(origin, LocalToGrid(_portalBoxViews[i].transform.localPosition)));
                }
            }

            float delayPerUnit = duration * 0.5f / maxDistance;
            float riseDuration = duration * 0.5f;
            for (int i = 0; i < _portalBoxViews.Count; i++)
            {
                PortalBoxView view = _portalBoxViews[i];
                if (view.gameObject.activeSelf)
                {
                    float distance = Distance(origin, LocalToGrid(view.transform.localPosition));
                    view.PlayAppear(distance * delayPerUnit, riseDuration);
                }
            }

            _revealRoutine = StartCoroutine(RevealRoutine(walls, origin, delayPerUnit, riseDuration));
        }

        /// <summary>
        /// 立即结束正在进行的展开，所有独有墙回到原位。
        /// </summary>
        public void CompleteReveal()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            for (int world = 0; world < _uniqueWallTiles.Count; world++)
            {
                List<PlacedTile> walls = _uniqueWallTiles[world];
                for (int i = 0; i < walls.Count; i++)
                {
                    walls[i].Transform.localPosition = _visualConfig.GridToLocal(walls[i].Position);
                }
            }
        }

        /// <summary>
        /// 通关庆祝：每个终点闪光，并在终点上喷出粒子（若配置了粒子预制体）。
        /// </summary>
        public void PlayGoalCelebration()
        {
            if (_goalFlashRoutine != null)
            {
                StopCoroutine(_goalFlashRoutine);
            }

            _goalFlashRoutine = StartCoroutine(GoalFlashRoutine());

            ParticleSystem burstPrefab = _visualConfig.CelebrationBurstPrefab;
            if (burstPrefab == null)
            {
                return;
            }

            // 只在通关时生成少量粒子，频率很低，不做对象池；粒子停止后由 stopAction 自行销毁
            for (int i = 0; i < _goalTiles.Count; i++)
            {
                ParticleSystem burst = Instantiate(burstPrefab, _sharedTilesRoot);
                burst.transform.localPosition = _visualConfig.GridToLocal(_goalTiles[i].Position);
                burst.Play();
            }
        }

        /// <summary>
        /// 立即停止所有实体的表现效果并复原。
        /// </summary>
        public void StopAllEffects()
        {
            if (_playerView != null)
            {
                _playerView.StopEffects();
            }

            for (int i = 0; i < _mappedBoxViews.Count; i++)
            {
                _mappedBoxViews[i].StopEffects();
            }

            for (int i = 0; i < _portalBoxViews.Count; i++)
            {
                _portalBoxViews[i].StopEffects();
            }
        }

        /// <summary>
        /// 网格坐标转换为实体根节点下的局部坐标。
        /// </summary>
        public Vector3 GridToLocal(GridPos pos)
        {
            return _visualConfig.GridToLocal(pos);
        }

        /// <summary>
        /// 清空并重新生成整个关卡的模型，然后同步到 <paramref name="state"/>。
        /// </summary>
        public void Build(LevelMap map, LevelState state)
        {
            if (_visualConfig == null || !_visualConfig.Validate(this))
            {
                return;
            }

            Clear();

            // 所有根节点共用同一偏移，让关卡中心落在本物体原点，相机只需对准本物体
            float cellSize = _visualConfig.CellSize;
            var centerOffset = new Vector3(-(map.Width - 1) * cellSize * 0.5f, 0f, -(map.Height - 1) * cellSize * 0.5f);
            _sharedTilesRoot = CreateRoot(SharedTilesRootName, centerOffset);
            for (int world = 0; world < map.WorldCount; world++)
            {
                _worldTileRoots.Add(CreateRoot(WorldTilesRootPrefix + world, centerOffset));
                _uniqueWallTiles.Add(new List<PlacedTile>());
            }

            _entitiesRoot = CreateRoot(EntitiesRootName, centerOffset);

            BuildTiles(map);
            BuildEntities(map, state);

            // 关卡居中在本物体原点；高度取一格，让墙和箱子的顶部也在取景范围内
            var size = new Vector3(map.Width * cellSize, cellSize, map.Height * cellSize);
            LevelBounds = new Bounds(transform.position + Vector3.up * (cellSize * 0.5f), size);
            Sync(state);
        }

        /// <summary>
        /// 把所有实体瞬间同步到 <paramref name="state"/>，并切换到玩家所在世界的显示。
        /// </summary>
        public void Sync(LevelState state)
        {
            if (_playerView == null)
            {
                return;
            }

            ShowWorld(state.PlayerWorld);

            _playerView.SnapTo(
                _visualConfig.GridToLocal(state.PlayerPosition),
                LevelVisualConfig.FacingToRotation(state.PlayerFacing));

            for (int i = 0; i < _mappedBoxViews.Count; i++)
            {
                _mappedBoxViews[i].SnapTo(_visualConfig.GridToLocal(state.MappedBoxes[i]), Quaternion.identity);
            }

            for (int i = 0; i < _portalBoxViews.Count; i++)
            {
                PortalBoxState box = state.PortalBoxes[i];
                bool isVisible = box.IsPresent && box.World == state.PlayerWorld;
                PortalBoxView view = _portalBoxViews[i];
                if (isVisible)
                {
                    view.gameObject.SetActive(true);
                    view.SnapTo(_visualConfig.GridToLocal(box.Position), Quaternion.identity);
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 根据规则刷新当前世界中、停在底座上的传送箱子的四面提示；其余箱子的提示隐藏。
        /// </summary>
        public void UpdateFaceHints(LevelRules rules, LevelState state)
        {
            IReadOnlyList<Direction> directions = LevelRules.Directions;
            for (int i = 0; i < _portalBoxViews.Count; i++)
            {
                PortalFaceHint hint = _portalBoxViews[i].FaceHint;
                if (hint == null)
                {
                    continue;
                }

                PortalBoxState box = state.PortalBoxes[i];
                bool isShown = box.World == state.PlayerWorld && rules.IsPortalOnPedestal(state, i);
                hint.SetVisible(isShown);
                if (!isShown)
                {
                    continue;
                }

                for (int d = 0; d < directions.Count; d++)
                {
                    hint.SetStatus(directions[d], rules.GetFaceStatus(state, i, directions[d]));
                }
            }
        }

        private void ShowWorld(int world)
        {
            if (world == _shownWorld)
            {
                return;
            }

            for (int i = 0; i < _worldTileRoots.Count; i++)
            {
                _worldTileRoots[i].gameObject.SetActive(i == world);
            }

            _shownWorld = world;
            ShownWorldChanged?.Invoke(world);
        }

        private void BuildTiles(LevelMap map)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    if (map.IsSharedWall(pos))
                    {
                        SpawnTile(_visualConfig.SharedWallPrefab, _sharedTilesRoot, pos);
                        continue;
                    }

                    SpawnTile(_visualConfig.FloorPrefab, _sharedTilesRoot, pos);
                    if (map.IsGoal(pos))
                    {
                        GameObject goal = SpawnTile(_visualConfig.GoalPrefab, _sharedTilesRoot, pos);
                        _goalTiles.Add(new PlacedTile(goal.transform, pos, goal.GetComponentInChildren<Renderer>()));
                    }

                    if (map.IsPedestal(pos))
                    {
                        SpawnTile(_visualConfig.PedestalPrefab, _sharedTilesRoot, pos);
                    }

                    for (int world = 0; world < map.WorldCount; world++)
                    {
                        if (map.IsUniqueWall(world, pos))
                        {
                            GameObject wall = SpawnTile(_visualConfig.UniqueWallPrefab, _worldTileRoots[world], pos);
                            _uniqueWallTiles[world].Add(new PlacedTile(wall.transform, pos, null));
                        }
                    }
                }
            }
        }

        private void BuildEntities(LevelMap map, LevelState state)
        {
            _playerView = Instantiate(_visualConfig.PlayerPrefab, _entitiesRoot);
            _playerView.name = "Player";

            for (int i = 0; i < state.MappedBoxes.Count; i++)
            {
                EntityView boxView = Instantiate(_visualConfig.MappedBoxPrefab, _entitiesRoot);
                boxView.name = $"MappedBox_{i}";
                _mappedBoxViews.Add(boxView);
            }

            // 未出现的一侧也预先生成，传送时只需激活，避免运行中实例化
            for (int i = 0; i < state.PortalBoxes.Count; i++)
            {
                PortalBoxView portalView = Instantiate(_visualConfig.PortalBoxPrefab, _entitiesRoot);
                portalView.name = $"PortalBox_{i}";
                int targetWorld = map.GetPortalWorld(LevelMap.GetPartnerIndex(i));
                portalView.SetTargetWorldColor(_visualConfig.GetWorldColor(targetWorld));
                _portalBoxViews.Add(portalView);
            }
        }

        private GameObject SpawnTile(GameObject prefab, Transform parent, GridPos pos)
        {
            GameObject tile = Instantiate(prefab, parent);
            tile.transform.localPosition = _visualConfig.GridToLocal(pos);
            return tile;
        }

        private GridPos LocalToGrid(Vector3 localPosition)
        {
            float cellSize = _visualConfig.CellSize;
            return new GridPos(Mathf.RoundToInt(localPosition.x / cellSize), Mathf.RoundToInt(localPosition.z / cellSize));
        }

        private static float Distance(GridPos a, GridPos b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private IEnumerator RevealRoutine(List<PlacedTile> walls, GridPos origin, float delayPerUnit, float riseDuration)
        {
            float depth = _visualConfig.CellSize;
            float total = riseDuration * 2f;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < walls.Count; i++)
                {
                    float start = Distance(origin, walls[i].Position) * delayPerUnit;
                    float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - start) / riseDuration));
                    walls[i].Transform.localPosition = _visualConfig.GridToLocal(walls[i].Position) + Vector3.down * (depth * (1f - progress));
                }

                yield return null;
            }

            _revealRoutine = null;
            CompleteReveal();
        }

        private IEnumerator GoalFlashRoutine()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            Color flash = _visualConfig.GoalFlashColor;
            float elapsed = 0f;
            while (elapsed < GoalFlashDuration)
            {
                elapsed += Time.deltaTime;
                SetGoalEmission(flash * Mathf.Sin(Mathf.Clamp01(elapsed / GoalFlashDuration) * Mathf.PI));
                yield return null;
            }

            SetGoalEmission(Color.black);
            _goalFlashRoutine = null;
        }

        private void SetGoalEmission(Color color)
        {
            for (int i = 0; i < _goalTiles.Count; i++)
            {
                Renderer goalRenderer = _goalTiles[i].Renderer;
                if (goalRenderer == null)
                {
                    continue;
                }

                goalRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, color);
                goalRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private Transform CreateRoot(string rootName, Vector3 localPosition)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(transform, false);
            root.localPosition = localPosition;
            return root;
        }

        /// <summary>
        /// 销毁当前关卡的全部模型。退出关卡回到菜单时调用。
        /// </summary>
        public void Clear()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            if (_goalFlashRoutine != null)
            {
                StopCoroutine(_goalFlashRoutine);
                _goalFlashRoutine = null;
            }

            _uniqueWallTiles.Clear();
            _goalTiles.Clear();
            _mappedBoxViews.Clear();
            _portalBoxViews.Clear();
            _playerView = null;
            _shownWorld = -1;
            LevelBounds = default;

            for (int i = 0; i < _worldTileRoots.Count; i++)
            {
                Destroy(_worldTileRoots[i].gameObject);
            }

            _worldTileRoots.Clear();
            if (_sharedTilesRoot != null)
            {
                Destroy(_sharedTilesRoot.gameObject);
            }

            if (_entitiesRoot != null)
            {
                Destroy(_entitiesRoot.gameObject);
            }
        }

        /// <summary>
        /// 生成后需要再次访问的格子：独有墙（展开动画）和终点（通关闪光）。
        /// </summary>
        private readonly struct PlacedTile
        {
            public readonly Transform Transform;
            public readonly GridPos Position;
            public readonly Renderer Renderer;

            public PlacedTile(Transform transform, GridPos position, Renderer renderer)
            {
                Transform = transform;
                Position = position;
                Renderer = renderer;
            }
        }
    }
}
