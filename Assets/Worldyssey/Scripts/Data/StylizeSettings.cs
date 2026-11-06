using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 全局风格化（卡通 + 像素）的可调参数：像素粒度、卡通明暗、描边。
    /// 渲染功能每帧把这些值写入着色器全局变量，Play 模式下改 Inspector 立即生效；运行时只读，不修改资源本身。
    /// </summary>
    [CreateAssetMenu(fileName = "StylizeSettings", menuName = "Worldyssey/Rendering/Stylize Settings")]
    public class StylizeSettings : ScriptableObject
    {
        public static readonly int SnapOffsetId = Shader.PropertyToID("_StylizeSnapOffset");
        private static readonly int ShadowTintId = Shader.PropertyToID("_ToonShadowTint");
        private static readonly int ShadowParamsId = Shader.PropertyToID("_ToonShadowParams");
        private static readonly int EdgeParamsId = Shader.PropertyToID("_StylizeEdgeParams");
        private static readonly int LineParamsId = Shader.PropertyToID("_StylizeLineParams");
        private static readonly int NormalBiasId = Shader.PropertyToID("_StylizeNormalEdgeBias");

        [Header("像素化")]
        [SerializeField] private bool _isPixelationEnabled = true;

        [Tooltip("低分辨率画面的高度（像素），宽度按屏幕比例计算。越小像素越粗")]
        [Range(90, 1080)]
        [SerializeField] private int _pixelHeight = 360;

        [Tooltip("相机位置对齐像素网格，静止物体在相机移动时不闪烁")]
        [SerializeField] private bool _isPixelSnapEnabled = true;

        [Header("卡通明暗")]
        [Tooltip("暗面颜色 = 固有色 × 这个颜色。偏冷偏饱和，暗面不会发灰")]
        [SerializeField] private Color _shadowTint = new Color(0.55f, 0.58f, 0.78f, 1f);

        [Tooltip("明暗分界：受光强度（0~1）低于它为暗面")]
        [Range(0f, 1f)]
        [SerializeField] private float _shadowThreshold = 0.2f;

        [Tooltip("明暗交界的过渡宽度，0 为完全硬边")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _shadowSoftness = 0.01f;

        [Tooltip("暗面受环境光（季节天光）影响的程度，0 只用上面的暗面颜色")]
        [Range(0f, 1f)]
        [SerializeField] private float _ambientInfluence = 0.4f;

        [Header("描边")]
        [SerializeField] private bool _isOutlineEnabled = true;

        [Tooltip("外轮廓：相邻像素深度差超过这个值（世界单位）就描边")]
        [Min(0.001f)]
        [SerializeField] private float _depthThreshold = 0.3f;

        [Tooltip("内线：相邻像素法线差（0~2）超过这个值就描边，越小转折处的线越多")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _normalThreshold = 0.4f;

        [Tooltip("法线差的方向偏好，只在转折的一侧画线，避免双线")]
        [SerializeField] private Vector3 _normalEdgeBias = new Vector3(1f, 1f, 1f);

        [Tooltip("外轮廓颜色 = 像素自身颜色 × 这个值")]
        [Range(0f, 1f)]
        [SerializeField] private float _outerLineDarken = 0.45f;

        [Tooltip("内线颜色 = 像素自身颜色 × 这个值（一般比外轮廓浅）")]
        [Range(0f, 1f)]
        [SerializeField] private float _innerLineDarken = 0.7f;

        [Tooltip("描边额外提高的饱和度，让线条和物体同色系而不发灰")]
        [Range(0f, 1f)]
        [SerializeField] private float _lineSaturation = 0.35f;

        public bool IsPixelationEnabled => _isPixelationEnabled;
        public int PixelHeight => _pixelHeight;

        /// <summary>只有开启像素化时对齐才有意义。</summary>
        public bool IsPixelSnapActive => _isPixelationEnabled && _isPixelSnapEnabled;

        public bool IsOutlineEnabled => _isOutlineEnabled;

        /// <summary>把卡通明暗和描边参数写入着色器全局变量。</summary>
        public void ApplyGlobals()
        {
            Shader.SetGlobalColor(ShadowTintId, _shadowTint);
            Shader.SetGlobalVector(ShadowParamsId, new Vector4(_shadowThreshold, _shadowSoftness, _ambientInfluence, 0f));
            Shader.SetGlobalVector(EdgeParamsId, new Vector4(_depthThreshold, _normalThreshold, _isOutlineEnabled ? 1f : 0f, 0f));
            Shader.SetGlobalVector(LineParamsId, new Vector4(_outerLineDarken, _innerLineDarken, _lineSaturation, 0f));
            Shader.SetGlobalVector(NormalBiasId, _normalEdgeBias);
        }
    }
}
