using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 天空配置：天穹预制体和绕岛漂移的云。四季共用，季节色调在 <see cref="SeasonEnvironment"/> 里。
    /// </summary>
    [CreateAssetMenu(fileName = "SkySettings", menuName = "Worldyssey/Sky Settings")]
    public class SkySettings : ScriptableObject
    {
        [Header("天穹")]
        [SerializeField] private SkyDome _domePrefab;

        [Header("云")]
        [SerializeField] private GameObject[] _cloudPrefabs = new GameObject[0];

        [Min(0)]
        [SerializeField] private int _cloudCount = 8;

        [Tooltip("云离岛边缘（含外围装饰地带）的最近距离（米），避免飘到相机和关卡之间")]
        [Min(0f)]
        [SerializeField] private float _cloudMinDistance = 4f;

        [Tooltip("在最近距离之外再随机远多少（米）")]
        [Min(0f)]
        [SerializeField] private float _cloudDistanceSpread = 8f;

        [Tooltip("云的高度范围（米），岛面为 0。负数在岛的下方，更有浮空感")]
        [SerializeField] private Vector2 _cloudHeightRange = new Vector2(-7f, 3f);

        [Tooltip("云的长度范围（米），按模型实际大小换算缩放")]
        [SerializeField] private Vector2 _cloudLengthRange = new Vector2(4f, 9f);

        [Tooltip("绕岛漂移的角速度范围（度 / 秒）")]
        [SerializeField] private Vector2 _cloudDriftSpeedRange = new Vector2(0.8f, 2f);

        [Tooltip("上下浮动的幅度（米）")]
        [Min(0f)]
        [SerializeField] private float _cloudBobAmplitude = 0.3f;

        [Tooltip("没有关卡时（菜单界面）假定的岛半径（米）")]
        [Min(1f)]
        [SerializeField] private float _defaultIslandRadius = 6f;

        public SkyDome DomePrefab => _domePrefab;
        public IReadOnlyList<GameObject> CloudPrefabs => _cloudPrefabs;
        public int CloudCount => _cloudCount;
        public float CloudMinDistance => _cloudMinDistance;
        public float CloudDistanceSpread => _cloudDistanceSpread;
        public Vector2 CloudHeightRange => _cloudHeightRange;
        public Vector2 CloudLengthRange => _cloudLengthRange;
        public Vector2 CloudDriftSpeedRange => _cloudDriftSpeedRange;
        public float CloudBobAmplitude => _cloudBobAmplitude;
        public float DefaultIslandRadius => _defaultIslandRadius;
    }
}
