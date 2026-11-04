using UnityEngine;
using UnityEngine.Rendering;

namespace Worldyssey.View
{
    /// <summary>
    /// 一个季节的环境：太阳、环境光、背景色与雾、调色、天气粒子。由 <see cref="SeasonEnvironmentController"/> 在切换世界时应用。
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonEnvironment", menuName = "Worldyssey/Season Environment")]
    public class SeasonEnvironment : ScriptableObject
    {
        [Header("太阳")]
        [SerializeField] private Color _sunColor = new Color(1f, 0.96f, 0.9f, 1f);

        [Min(0f)]
        [SerializeField] private float _sunIntensity = 1.1f;

        [Tooltip("太阳高度角：越小影子越长")]
        [Range(5f, 90f)]
        [SerializeField] private float _sunPitch = 50f;

        [Tooltip("太阳水平方向")]
        [Range(-180f, 180f)]
        [SerializeField] private float _sunYaw = -30f;

        [Header("环境光（三色）")]
        [SerializeField] private Color _ambientSky = new Color(0.75f, 0.8f, 0.9f, 1f);
        [SerializeField] private Color _ambientEquator = new Color(0.65f, 0.65f, 0.65f, 1f);
        [SerializeField] private Color _ambientGround = new Color(0.4f, 0.38f, 0.36f, 1f);

        [Header("背景与雾")]
        [Tooltip("相机背景色，同时是雾的颜色：岛的下方渐隐进这片颜色里")]
        [SerializeField] private Color _backgroundColor = new Color(0.95f, 0.9f, 0.92f, 1f);

        [Tooltip("雾的颜色。透明（A = 0）时跟随背景色。有天穹时调成接近天空下沿的颜色，岛的下方才能自然融进天空")]
        [SerializeField] private Color _fogColor = Color.clear;

        [SerializeField] private bool _isFogEnabled = true;

        [Tooltip("雾从“相机到关卡中心的距离”再往外多少米开始，0 表示刚过关卡中心就开始变淡")]
        [SerializeField] private float _fogStartOffset = 2f;

        [Tooltip("从开始变淡到完全融入背景的距离（米）")]
        [Min(0.1f)]
        [SerializeField] private float _fogRange = 14f;

        [Header("天空")]
        [Tooltip("天穹的色调，乘在天空贴图上；白色为原样")]
        [SerializeField] private Color _skyTint = Color.white;

        [Tooltip("云的色调；白色为原样")]
        [SerializeField] private Color _cloudTint = Color.white;

        [Header("调色与粒子")]
        [Tooltip("可选：季节调色的后处理配置（色温、饱和度等）")]
        [SerializeField] private VolumeProfile _colorProfile;

        [Tooltip("可选：天气粒子，例如花瓣、萤火虫、落叶、雪")]
        [SerializeField] private ParticleSystem _particlesPrefab;

        public Color SunColor => _sunColor;
        public float SunIntensity => _sunIntensity;
        public Quaternion SunRotation => Quaternion.Euler(_sunPitch, _sunYaw, 0f);
        public Color AmbientSky => _ambientSky;
        public Color AmbientEquator => _ambientEquator;
        public Color AmbientGround => _ambientGround;
        public Color BackgroundColor => _backgroundColor;
        public Color FogColor => _fogColor.a > 0f ? _fogColor : _backgroundColor;
        public bool IsFogEnabled => _isFogEnabled;
        public Color SkyTint => _skyTint;
        public Color CloudTint => _cloudTint;
        public float FogStartOffset => _fogStartOffset;
        public float FogRange => _fogRange;
        public VolumeProfile ColorProfile => _colorProfile;
        public ParticleSystem ParticlesPrefab => _particlesPrefab;
    }
}
