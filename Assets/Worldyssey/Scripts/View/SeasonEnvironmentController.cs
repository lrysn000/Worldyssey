using UnityEngine;
using UnityEngine.Rendering;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 切换世界时应用对应季节的环境：太阳、环境光、背景色和雾、调色、天气粒子。
    /// 与 UI 换色一样监听 <see cref="LevelView.ShownWorldChanged"/>，传送时在画面被颜色铺满的那一刻瞬间切换。
    /// 调色用每个季节一个 Volume、只改权重的方式，不在运行时修改配置资源。
    /// </summary>
    public class SeasonEnvironmentController : MonoBehaviour
    {
        private const int NoWorld = -1;
        private const int DefaultWorld = 0;

        [SerializeField] private LevelView _levelView;
        [SerializeField] private LevelVisualConfig _visualConfig;

        [Tooltip("场景里的平行光（太阳）")]
        [SerializeField] private Light _sun;

        [Tooltip("游戏相机，背景色和雾距离都按它计算")]
        [SerializeField] private Camera _camera;

        [Tooltip("可选：天穹和云")]
        [SerializeField] private SkyView _sky;

        [Tooltip("季节调色 Volume 的优先级：高于全局后处理（0），低于撤销褪色（10）")]
        [SerializeField] private float _volumePriority = 1f;

        private readonly Volume[] _volumes = new Volume[LevelLimits.MaxWorldCount];
        private readonly ParticleSystem[] _particles = new ParticleSystem[LevelLimits.MaxWorldCount];
        private SeasonEnvironment _current;
        private int _currentWorld = NoWorld;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_visualConfig, nameof(_visualConfig))
                & CheckReference(_sun, nameof(_sun))
                & CheckReference(_camera, nameof(_camera));
            if (!_hasReferences)
            {
                return;
            }

            // 四季的 Volume 和粒子一次建好，切换时只开关，避免运行中实例化
            for (int world = 0; world < LevelLimits.MaxWorldCount; world++)
            {
                SeasonEnvironment environment = GetEnvironment(world);
                if (environment == null)
                {
                    continue;
                }

                if (environment.ColorProfile != null)
                {
                    var volumeObject = new GameObject($"SeasonVolume_{world}");
                    volumeObject.transform.SetParent(transform, false);
                    Volume volume = volumeObject.AddComponent<Volume>();
                    volume.isGlobal = true;
                    volume.priority = _volumePriority;
                    volume.sharedProfile = environment.ColorProfile;
                    volume.weight = 0f;
                    _volumes[world] = volume;
                }

                if (environment.ParticlesPrefab != null)
                {
                    ParticleSystem particles = Instantiate(environment.ParticlesPrefab, transform);
                    particles.name = $"SeasonParticles_{world}";
                    particles.gameObject.SetActive(false);
                    _particles[world] = particles;
                }
            }
        }

        private void OnEnable()
        {
            if (_hasReferences)
            {
                _levelView.ShownWorldChanged += HandleShownWorldChanged;
            }
        }

        private void Start()
        {
            // 菜单界面还没有关卡，先用第一个季节的环境，避免显示默认的灰色天空
            if (_hasReferences && _currentWorld == NoWorld)
            {
                Apply(DefaultWorld);
            }
        }

        private void LateUpdate()
        {
            // 雾的距离要跟着相机：关卡大小不同、相机旋转和传送推拉都会改变相机到关卡的距离，所以每帧算一次（只有几次浮点运算）
            if (_current == null || !_current.IsFogEnabled)
            {
                return;
            }

            Bounds bounds = _levelView.LevelBounds;
            if (bounds.size == Vector3.zero)
            {
                return;
            }

            float centerDistance = Vector3.Distance(_camera.transform.position, bounds.center);
            float fogStart = centerDistance + _current.FogStartOffset;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogStart + _current.FogRange;
        }

        private void OnDisable()
        {
            if (_hasReferences)
            {
                _levelView.ShownWorldChanged -= HandleShownWorldChanged;
            }
        }

        private void HandleShownWorldChanged(int world)
        {
            Apply(world);
        }

        private void Apply(int world)
        {
            SeasonEnvironment environment = GetEnvironment(world);
            if (environment == null)
            {
                // 这个季节还没配置环境，保持上一个季节的样子
                return;
            }

            _currentWorld = world;
            _current = environment;
            _sun.color = environment.SunColor;
            _sun.intensity = environment.SunIntensity;
            _sun.transform.rotation = environment.SunRotation;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = environment.AmbientSky;
            RenderSettings.ambientEquatorColor = environment.AmbientEquator;
            RenderSettings.ambientGroundColor = environment.AmbientGround;

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = environment.BackgroundColor;
            RenderSettings.fog = environment.IsFogEnabled;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = environment.FogColor;
            if (_sky != null)
            {
                _sky.ApplySeason(environment, environment.SunRotation);
            }

            for (int i = 0; i < LevelLimits.MaxWorldCount; i++)
            {
                if (_volumes[i] != null)
                {
                    _volumes[i].weight = i == world ? 1f : 0f;
                }

                if (_particles[i] != null)
                {
                    _particles[i].gameObject.SetActive(i == world);
                }
            }
        }

        private SeasonEnvironment GetEnvironment(int world)
        {
            SeasonVisualSet season = _visualConfig.GetSeasonSet(world);
            return season != null ? season.Environment : null;
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(SeasonEnvironmentController)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
