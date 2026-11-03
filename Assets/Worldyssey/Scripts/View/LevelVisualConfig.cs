using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 关卡表现配置：各类格子和实体的预制体、格子尺寸。替换美术时只需替换这里的引用。
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

        [Header("实体")]
        [SerializeField] private EntityView _playerPrefab;
        [SerializeField] private EntityView _mappedBoxPrefab;
        [SerializeField] private PortalBoxView _portalBoxPrefab;

        [Header("世界")]
        [Tooltip("每个世界的代表色，下标即世界编号。传送箱子按它指向的世界着色")]
        [SerializeField] private Color[] _worldColors =
        {
            new Color(0.95f, 0.55f, 0.25f),
            new Color(0.25f, 0.75f, 0.85f),
            new Color(0.50f, 0.85f, 0.35f),
            new Color(0.90f, 0.40f, 0.70f),
        };

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

        /// <summary>
        /// 网格坐标转换为关卡根节点下的局部坐标。网格 y 对应世界 z。
        /// </summary>
        public Vector3 GridToLocal(GridPos pos)
        {
            return new Vector3(pos.X * _cellSize, 0f, pos.Y * _cellSize);
        }

        /// <summary>
        /// 世界的代表色。配置的颜色不够时返回白色并报错，方便发现漏配。
        /// </summary>
        public Color GetWorldColor(int world)
        {
            if (_worldColors != null && world >= 0 && world < _worldColors.Length)
            {
                return _worldColors[world];
            }

            Debug.LogError($"[{nameof(LevelVisualConfig)}] {name} 没有配置世界 {world} 的颜色", this);
            return Color.white;
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
