using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 传送箱子模型：在实体补间之外，负责按目标世界着色和持有面提示。
    /// </summary>
    public class PortalBoxView : EntityView
    {
        // URP Lit 与 Built-in Standard 的主色属性名不同，两个都写，换管线也不用改代码
        private static readonly int UrpBaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BuiltInColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("需要按目标世界着色的箱体渲染器")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private PortalFaceHint _faceHint;

        [Tooltip("可选：平贴在箱子顶面的配对符号")]
        [SerializeField] private SpriteRenderer _pairSymbol;

        [Tooltip("箱体按目标世界颜色自发光的强度；材质需开启 Emission 才生效")]
        [Min(0f)]
        [SerializeField] private float _glowIntensity = 0.6f;

        private MaterialPropertyBlock _propertyBlock;

        public PortalFaceHint FaceHint => _faceHint;

        /// <summary>箱体渲染器，回程箱子合成时用它的网格和材质画碎块。</summary>
        public Renderer BodyRenderer => _bodyRenderer;

        protected override void Awake()
        {
            base.Awake();
            if (_bodyRenderer == null)
            {
                Debug.LogError($"[{nameof(PortalBoxView)}] {name} 缺少引用：{nameof(_bodyRenderer)}", this);
            }

            if (_faceHint == null)
            {
                Debug.LogError($"[{nameof(PortalBoxView)}] {name} 缺少引用：{nameof(_faceHint)}", this);
            }
        }

        /// <summary>
        /// 用目标世界的代表色给箱体着色并微微发光。使用 MaterialPropertyBlock，不修改共享材质。
        /// </summary>
        public void SetTargetWorldColor(Color color)
        {
            if (_bodyRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _bodyRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(UrpBaseColorId, color);
            _propertyBlock.SetColor(BuiltInColorId, color);
            _propertyBlock.SetColor(EmissionColorId, color * _glowIntensity);
            _bodyRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// 显示或隐藏箱体和顶面符号（只开关渲染器，位置、面提示和逻辑不受影响）。合成动画期间隐藏，合成完再显示。
        /// </summary>
        public void SetBodyVisible(bool isVisible)
        {
            if (_bodyRenderer != null)
            {
                _bodyRenderer.enabled = isVisible;
            }

            if (_pairSymbol != null)
            {
                _pairSymbol.enabled = isVisible && _pairSymbol.sprite != null;
            }
        }

        /// <summary>
        /// 在顶面印上配对 <paramref name="pairIndex"/> 的符号。没有符号层或符号配置时隐藏。
        /// </summary>
        public void SetPairSymbol(PairSymbolSet symbols, int pairIndex)
        {
            if (_pairSymbol == null)
            {
                return;
            }

            Sprite symbol = symbols != null ? symbols.GetSymbol(pairIndex) : null;
            _pairSymbol.enabled = symbol != null;
            _pairSymbol.sprite = symbol;
            if (symbol != null)
            {
                _pairSymbol.color = symbols.BoxSymbolColor;
            }
        }
    }
}
