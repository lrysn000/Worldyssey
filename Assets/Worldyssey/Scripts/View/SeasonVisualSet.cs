using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 一个季节（世界）的地形模块：地面、四角圆角地面、独有墙变体、地面上撒的草、网格外围装饰地带上的花和树。
    /// 某一项留空时，<see cref="LevelVisualConfig"/> 里的占位预制体顶上，没做完的季节也能正常游玩。
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonVisualSet", menuName = "Worldyssey/Season Visual Set")]
    public class SeasonVisualSet : ScriptableObject
    {
        [Header("地面")]
        [Tooltip("普通地块，顶面在 y = 0")]
        [SerializeField] private GameObject _floorPrefab;

        [Tooltip("可选：网格四个角用的圆角地块。模型的圆角必须朝右上（+x, +z），其余三个角由代码旋转")]
        [SerializeField] private GameObject _floorCornerPrefab;

        [Header("独有墙")]
        [Tooltip("按格子坐标固定挑选一个，并随机旋转 90° 的倍数，所以四个方向都要好看")]
        [SerializeField] private GameObject[] _uniqueWallPrefabs = new GameObject[0];

        [Header("草")]
        [Tooltip("可选：草专用材质（Worldyssey/Grass 着色器），会摆动、被压扁。留空则用草预制体自带的材质")]
        [SerializeField] private Material _grassMaterial;

        [Tooltip("撒在空地上的草；终点、底座、墙所在的格子不撒")]
        [SerializeField] private GameObject[] _grassPrefabs = new GameObject[0];

        [Tooltip("每格最少几簇")]
        [Min(0)]
        [SerializeField] private int _grassPerCellMin = 1;

        [Tooltip("每格最多几簇")]
        [Min(0)]
        [SerializeField] private int _grassPerCellMax = 3;

        [Tooltip("草离格子边缘至少多远（格子边长的比例），避免伸到相邻格")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _grassEdgeMargin = 0.12f;

        [Tooltip("草的随机缩放范围")]
        [SerializeField] private Vector2 _grassScaleRange = new Vector2(0.6f, 1f);

        [Header("外围装饰地带")]
        [Tooltip("矮装饰（花、矮灌木），外围每一圈都可以放")]
        [SerializeField] private GameObject[] _rimLowPrefabs = new GameObject[0];

        [Tooltip("高装饰（树），只种在最外圈，相机转到任何方向都不会挡住网格")]
        [SerializeField] private GameObject[] _rimTallPrefabs = new GameObject[0];

        [Tooltip("外围每格放一个矮装饰的概率")]
        [Range(0f, 1f)]
        [SerializeField] private float _rimLowChance = 0.5f;

        [Tooltip("最外圈每格种一棵树的概率（种了树的格子不再放矮装饰）")]
        [Range(0f, 1f)]
        [SerializeField] private float _rimTallChance = 0.6f;

        [Tooltip("外围装饰的随机缩放范围")]
        [SerializeField] private Vector2 _rimScaleRange = new Vector2(0.8f, 1.2f);

        [Header("环境")]
        [Tooltip("可选：灯光、背景、雾、调色、天气粒子")]
        [SerializeField] private SeasonEnvironment _environment;

        public GameObject FloorPrefab => _floorPrefab;
        public GameObject FloorCornerPrefab => _floorCornerPrefab;
        public IReadOnlyList<GameObject> UniqueWallPrefabs => _uniqueWallPrefabs;
        public IReadOnlyList<GameObject> GrassPrefabs => _grassPrefabs;
        public Material GrassMaterial => _grassMaterial;
        public int GrassPerCellMin => _grassPerCellMin;
        public int GrassPerCellMax => Mathf.Max(_grassPerCellMin, _grassPerCellMax);
        public float GrassEdgeMargin => _grassEdgeMargin;
        public Vector2 GrassScaleRange => _grassScaleRange;
        public IReadOnlyList<GameObject> RimLowPrefabs => _rimLowPrefabs;
        public IReadOnlyList<GameObject> RimTallPrefabs => _rimTallPrefabs;
        public float RimLowChance => _rimLowChance;
        public float RimTallChance => _rimTallChance;
        public Vector2 RimScaleRange => _rimScaleRange;
        public SeasonEnvironment Environment => _environment;
    }
}
