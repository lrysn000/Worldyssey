using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 天空的运行时表现：天穹跟着相机（看起来无限远），云绕岛缓慢漂移并轻微上下浮动。
    /// 云的轨道半径按当前关卡大小计算，换关卡后自动适应。季节色调由 <see cref="SeasonEnvironmentController"/> 通过 <see cref="ApplySeason"/> 设置。
    /// </summary>
    public class SkyView : MonoBehaviour
    {
        private const int CloudSalt = 5;
        private const int CloudWorld = -2;
        private const float BobFrequency = 0.4f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private SkySettings _settings;
        [SerializeField] private Camera _camera;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private LevelVisualConfig _visualConfig;

        private readonly List<Cloud> _clouds = new List<Cloud>();
        private MaterialPropertyBlock _propertyBlock;
        private SkyDome _dome;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_settings, nameof(_settings))
                & CheckReference(_camera, nameof(_camera))
                & CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_visualConfig, nameof(_visualConfig));
            if (!_hasReferences)
            {
                return;
            }

            if (_settings.DomePrefab != null)
            {
                _dome = Instantiate(_settings.DomePrefab, transform);
            }

            SpawnClouds();
        }

        private void LateUpdate()
        {
            if (!_hasReferences)
            {
                return;
            }

            // 天穹始终以相机为中心，相机怎么移动都碰不到它的边
            if (_dome != null)
            {
                _dome.transform.position = _camera.transform.position;
            }

            // 云每帧都在动，只能逐帧更新；云很少，开销可以忽略
            float islandRadius = GetIslandRadius();
            Vector3 center = _levelView.transform.position;
            float deltaTime = Time.deltaTime;
            float time = Time.time;
            for (int i = 0; i < _clouds.Count; i++)
            {
                Cloud cloud = _clouds[i];
                cloud.Angle += cloud.Speed * deltaTime;
                float radians = cloud.Angle * Mathf.Deg2Rad;
                float radius = islandRadius + cloud.ExtraDistance;
                var direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
                float bob = Mathf.Sin(time * BobFrequency + cloud.BobPhase) * _settings.CloudBobAmplitude;
                cloud.Transform.position = center + direction * radius + Vector3.up * (cloud.Height + bob);

                // 长边顺着漂移方向
                cloud.Transform.rotation = Quaternion.LookRotation(new Vector3(-direction.z, 0f, direction.x));
            }
        }

        /// <summary>
        /// 应用季节色调，并把太阳圆盘转到平行光的方向。
        /// </summary>
        public void ApplySeason(SeasonEnvironment environment, Quaternion sunRotation)
        {
            if (_dome != null)
            {
                _dome.SetTint(environment.SkyTint);

                // 平行光沿自身 +z 照射，太阳在相反方向
                _dome.SetSunDirection(sunRotation * Vector3.back);
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < _clouds.Count; i++)
            {
                Renderer[] renderers = _clouds[i].Renderers;
                for (int r = 0; r < renderers.Length; r++)
                {
                    renderers[r].GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor(BaseColorId, environment.CloudTint);
                    renderers[r].SetPropertyBlock(_propertyBlock);
                }
            }
        }

        private void SpawnClouds()
        {
            IReadOnlyList<GameObject> prefabs = _settings.CloudPrefabs;
            if (prefabs.Count == 0)
            {
                return;
            }

            Vector2 heights = _settings.CloudHeightRange;
            Vector2 lengths = _settings.CloudLengthRange;
            Vector2 speeds = _settings.CloudDriftSpeedRange;
            for (int i = 0; i < _settings.CloudCount; i++)
            {
                // 云的分布固定，每次进游戏都一样
                var random = new CellRandom(new GridPos(i, 0), CloudWorld, CloudSalt);
                GameObject prefab = prefabs[random.Range(prefabs.Count)];
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = Instantiate(prefab, transform);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                ScaleToLength(instance.transform, renderers, random.Range(lengths.x, lengths.y));
                _clouds.Add(new Cloud
                {
                    Transform = instance.transform,
                    Renderers = renderers,

                    // 起始角度均匀分布再加一点随机，避免扎堆
                    Angle = 360f * i / _settings.CloudCount + random.Range(-15f, 15f),
                    Speed = random.Range(speeds.x, speeds.y),
                    ExtraDistance = _settings.CloudMinDistance + random.Range(0f, _settings.CloudDistanceSpread),
                    Height = random.Range(heights.x, heights.y),
                    BobPhase = random.Range(0f, Mathf.PI * 2f),
                });
            }
        }

        /// <summary>
        /// 按模型实际大小缩放到目标长度，不同来源的云模型不用手动统一尺寸。
        /// </summary>
        private static void ScaleToLength(Transform cloud, Renderer[] renderers, float targetLength)
        {
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float length = Mathf.Max(bounds.size.x, bounds.size.z);
            if (length > 0f)
            {
                cloud.localScale *= targetLength / length;
            }
        }

        private float GetIslandRadius()
        {
            Bounds bounds = _levelView.LevelBounds;
            if (bounds.size == Vector3.zero)
            {
                return _settings.DefaultIslandRadius;
            }

            // 关卡范围不含外围装饰地带，要加上
            return Mathf.Max(bounds.extents.x, bounds.extents.z) + _visualConfig.RimWidth * _visualConfig.CellSize;
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(SkyView)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }

        private sealed class Cloud
        {
            public Transform Transform;
            public Renderer[] Renderers;
            public float Angle;
            public float Speed;
            public float ExtraDistance;
            public float Height;
            public float BobPhase;
        }
    }
}
