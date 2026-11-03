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

        [SerializeField] private LevelVisualConfig _visualConfig;

        private readonly List<EntityView> _mappedBoxViews = new List<EntityView>();
        private readonly List<PortalBoxView> _portalBoxViews = new List<PortalBoxView>();
        private readonly List<Transform> _worldTileRoots = new List<Transform>();
        private Transform _sharedTilesRoot;
        private Transform _entitiesRoot;
        private EntityView _playerView;
        private int _shownWorld = -1;

        /// <summary>玩家模型，关卡未生成时为 null。</summary>
        public EntityView PlayerView => _playerView;

        /// <summary>目标箱子数量。</summary>
        public int MappedBoxViewCount => _mappedBoxViews.Count;

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
            }

            _entitiesRoot = CreateRoot(EntitiesRootName, centerOffset);

            BuildTiles(map);
            BuildEntities(map, state);
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
                        SpawnTile(_visualConfig.GoalPrefab, _sharedTilesRoot, pos);
                    }

                    if (map.IsPedestal(pos))
                    {
                        SpawnTile(_visualConfig.PedestalPrefab, _sharedTilesRoot, pos);
                    }

                    for (int world = 0; world < map.WorldCount; world++)
                    {
                        if (map.IsUniqueWall(world, pos))
                        {
                            SpawnTile(_visualConfig.UniqueWallPrefab, _worldTileRoots[world], pos);
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

        private void SpawnTile(GameObject prefab, Transform parent, GridPos pos)
        {
            GameObject tile = Instantiate(prefab, parent);
            tile.transform.localPosition = _visualConfig.GridToLocal(pos);
        }

        private Transform CreateRoot(string rootName, Vector3 localPosition)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(transform, false);
            root.localPosition = localPosition;
            return root;
        }

        private void Clear()
        {
            _mappedBoxViews.Clear();
            _portalBoxViews.Clear();
            _playerView = null;
            _shownWorld = -1;

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
    }
}
