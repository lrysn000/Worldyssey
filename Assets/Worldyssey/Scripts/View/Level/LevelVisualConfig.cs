using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 关卡表现配置：各类格子和实体的预制体、格子尺寸。替换美术时只需替换这里的引用。
    /// 地面和独有墙优先使用各季节的 <see cref="SeasonVisualSet"/>，没配置的季节回退到这里的占位预制体。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelVisualConfig", menuName = "Worldyssey/Level Visual Config")]
    public class LevelVisualConfig : ScriptableObject
    {
        [Header("格子")]
        [SerializeField] private GameObject _floorPrefab;
        [SerializeField] private GameObject _sharedWallPrefab;
        [SerializeField] private GameObject _goalPrefab;
        [SerializeField] private GameObject _pedestalPrefab;
        [SerializeField] private GameObject _uniqueWallPrefab;

        [Tooltip("可选：共享墙的变体，按格子坐标固定挑选。为空时用上面的 Shared Wall Prefab")]
        [SerializeField] private GameObject[] _sharedWallVariants = new GameObject[0];

        [Header("实体")]
        [SerializeField] private EntityView _playerPrefab;
        [SerializeField] private EntityView _mappedBoxPrefab;
        [SerializeField] private PortalBoxView _portalBoxPrefab;

        [Tooltip("可选：区分传送箱子配对的符号，印在箱子顶面。留空时不印")]
        [SerializeField] private PairSymbolSet _pairSymbols;

        [Header("世界")]
        [Tooltip("各世界的代表色，与 UI 强调色共用。传送箱子按它指向的世界着色，传送遮罩用目标世界的颜色")]
        [SerializeField] private WorldPalette _worldPalette;

        [Header("季节")]
        [Tooltip("按季节排列：0 春、1 夏、2 秋、3 冬（每个世界用哪个季节由关卡数据决定）。缺少的季节或其中留空的项，用上面的占位预制体")]
        [SerializeField] private SeasonVisualSet[] _seasonSets = new SeasonVisualSet[0];

        [Tooltip("网格外围装饰地带的圈数，0 为不要外围。外围只是装饰，玩家走不上去")]
        [Range(0, 4)]
        [SerializeField] private int _rimWidth = 2;

        [Tooltip("可选：地面格子衬线的材质（Worldyssey/FloorGrid 着色器），线的深浅和粗细在材质上调。留空时不画衬线")]
        [SerializeField] private Material _floorGridMaterial;

        [Header("草的互动")]
        [Tooltip("玩家压草的半径（米）")]
        [Min(0f)]
        [SerializeField] private float _grassPlayerRadius = 0.45f;

        [Tooltip("箱子压草的半径（米）")]
        [Min(0f)]
        [SerializeField] private float _grassBoxRadius = 0.6f;

        [Tooltip("压痕完全回弹的时间（秒）")]
        [Min(0.01f)]
        [SerializeField] private float _grassRecoverDuration = 0.5f;

        [Tooltip("移动多远留下一个压痕点（米），越小痕迹越连续，占用的互动名额也越多")]
        [Min(0.01f)]
        [SerializeField] private float _grassTrailSpacing = 0.2f;

        [Header("特效")]
        [Tooltip("可选：通关时在每个终点喷出的粒子预制体，粒子播完后自行销毁")]
        [SerializeField] private ParticleSystem _celebrationBurstPrefab;

        [Tooltip("通关时终点闪光的颜色（HDR）；终点材质需开启 Emission 才生效")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _goalFlashColor = new Color(1f, 0.85f, 0.3f) * 3f;

        [Header("底座")]
        [Tooltip("底座上停着传送箱子时，按箱子通往的世界颜色发光的强度；0 为不发光。底座材质需支持 _EmissionColor（Toon 着色器支持）")]
        [Range(0f, 3f)]
        [SerializeField] private float _pedestalGlowIntensity = 0.8f;

        [Header("布局")]
        [Tooltip("一格在世界空间中的边长")]
        [Min(0.1f)]
        [SerializeField] private float _cellSize = 1f;

        public GameObject FloorPrefab => _floorPrefab;
        public GameObject SharedWallPrefab => _sharedWallPrefab;
        public GameObject GoalPrefab => _goalPrefab;
        public GameObject PedestalPrefab => _pedestalPrefab;
        public GameObject UniqueWallPrefab => _uniqueWallPrefab;
        public EntityView PlayerPrefab => _playerPrefab;
        public EntityView MappedBoxPrefab => _mappedBoxPrefab;
        public PortalBoxView PortalBoxPrefab => _portalBoxPrefab;
        public float CellSize => _cellSize;
        public IReadOnlyList<GameObject> SharedWallVariants => _sharedWallVariants;
        public int RimWidth => _rimWidth;
        public Material FloorGridMaterial => _floorGridMaterial;
        public PairSymbolSet PairSymbols => _pairSymbols;
        public float GrassPlayerRadius => _grassPlayerRadius;
        public float GrassBoxRadius => _grassBoxRadius;
        public float GrassRecoverDuration => _grassRecoverDuration;
        public float GrassTrailSpacing => _grassTrailSpacing;
        public ParticleSystem CelebrationBurstPrefab => _celebrationBurstPrefab;
        public Color GoalFlashColor => _goalFlashColor;
        public float PedestalGlowIntensity => _pedestalGlowIntensity;

        /// <summary>
        /// 网格坐标转换为关卡根节点下的局部坐标。网格 y 对应世界 z。
        /// </summary>
        public Vector3 GridToLocal(GridPos pos)
        {
            return new Vector3(pos.X * _cellSize, 0f, pos.Y * _cellSize);
        }

        /// <summary>
        /// 季节的代表色，来自 <see cref="WorldPalette"/>。参数是季节编号（<see cref="WorldTheme"/> 的数值），不是世界编号。
        /// 取不到时返回白色并报错，方便发现漏配。
        /// </summary>
        public Color GetSeasonColor(int season)
        {
            if (_worldPalette != null && _worldPalette.TryGetColor(season, out Color color))
            {
                return color;
            }

            Debug.LogError($"[{nameof(LevelVisualConfig)}] {name} 取不到季节 {season} 的颜色，检查 {nameof(_worldPalette)}", this);
            return Color.white;
        }

        /// <summary>
        /// 某个季节的模块组，没有配置时返回 null。参数是季节编号（<see cref="WorldTheme"/> 的数值），不是世界编号。
        /// </summary>
        public SeasonVisualSet GetSeasonSet(int season)
        {
            return _seasonSets != null && season >= 0 && season < _seasonSets.Length ? _seasonSets[season] : null;
        }

        /// <summary>
        /// 朝向转换为绕 y 轴的旋转。Up 对应 +z。
        /// </summary>
        public static Quaternion FacingToRotation(Direction facing)
        {
            return Quaternion.Euler(0f, (int)facing * 90f, 0f);
        }

        /// <summary>
        /// 检查所有引用是否已设置，缺失时逐项报错。
        /// </summary>
        public bool Validate(Object context)
        {
            bool isValid = true;
            isValid &= CheckAssigned(_floorPrefab, nameof(_floorPrefab), context);
            isValid &= CheckAssigned(_sharedWallPrefab, nameof(_sharedWallPrefab), context);
            isValid &= CheckAssigned(_goalPrefab, nameof(_goalPrefab), context);
            isValid &= CheckAssigned(_pedestalPrefab, nameof(_pedestalPrefab), context);
            isValid &= CheckAssigned(_uniqueWallPrefab, nameof(_uniqueWallPrefab), context);
            isValid &= CheckAssigned(_playerPrefab, nameof(_playerPrefab), context);
            isValid &= CheckAssigned(_mappedBoxPrefab, nameof(_mappedBoxPrefab), context);
            isValid &= CheckAssigned(_portalBoxPrefab, nameof(_portalBoxPrefab), context);
            isValid &= CheckAssigned(_worldPalette, nameof(_worldPalette), context);
            return isValid;
        }

        private bool CheckAssigned(Object reference, string fieldName, Object context)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(LevelVisualConfig)}] {name} 缺少引用：{fieldName}", context);
            return false;
        }
    }
}
