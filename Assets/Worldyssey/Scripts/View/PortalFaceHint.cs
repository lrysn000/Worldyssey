using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 传送箱子四个面上的提示板：按 <see cref="FaceStatus"/> 切换材质，可进入的面缓慢呼吸发光。
    /// </summary>
    /// <remarks>
    /// 发光通过 MaterialPropertyBlock 写入 _EmissionColor，不修改共享材质；可进入材质需开启 Emission（由效果设置工具完成）。
    /// </remarks>
    public class PortalFaceHint : MonoBehaviour
    {
        private const int FaceCount = 4;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("四个面的提示板，顺序为 Up(+z)、Right(+x)、Down(-z)、Left(-x)，与 Direction 枚举值一致")]
        [SerializeField] private Renderer[] _faceRenderers = new Renderer[FaceCount];

        [Header("材质")]
        [SerializeField] private Material _enterableMaterial;
        [SerializeField] private Material _blockedMaterial;
        [SerializeField] private Material _conflictedMaterial;

        [Header("发光")]
        [Tooltip("可进入面的发光颜色（HDR，亮度大于 1 才会产生泛光）")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _glowColor = new Color(0.3f, 1f, 0.45f) * 2f;

        [Tooltip("呼吸发光的亮度范围（乘在发光颜色上）")]
        [SerializeField] private Vector2 _glowRange = new Vector2(0.4f, 1.2f);

        [Tooltip("呼吸速度（每秒弧度）")]
        [Min(0f)]
        [SerializeField] private float _pulseSpeed = 3f;

        private readonly bool[] _isEnterable = new bool[FaceCount];
        private MaterialPropertyBlock _propertyBlock;
        private bool _isVisible;
        private bool _isValid;

        private void Awake()
        {
            _isValid = _faceRenderers != null && _faceRenderers.Length == FaceCount
                && _enterableMaterial != null && _blockedMaterial != null && _conflictedMaterial != null;
            if (_isValid)
            {
                for (int i = 0; i < FaceCount; i++)
                {
                    _isValid &= _faceRenderers[i] != null;
                }
            }

            if (!_isValid)
            {
                Debug.LogError($"[{nameof(PortalFaceHint)}] {name} 的面提示板或材质未配置完整，提示将不显示", this);
            }

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            // 只有显示中且有可进入的面时才需要逐帧更新亮度
            if (!_isValid || !_isVisible)
            {
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed);
            Color glow = _glowColor * Mathf.Lerp(_glowRange.x, _glowRange.y, wave);
            for (int i = 0; i < FaceCount; i++)
            {
                if (_isEnterable[i])
                {
                    SetEmission(_faceRenderers[i], glow);
                }
            }
        }

        /// <summary>
        /// 显示或隐藏全部提示板。箱子不在底座上时隐藏。
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (!_isValid)
            {
                return;
            }

            _isVisible = isVisible;
            for (int i = 0; i < FaceCount; i++)
            {
                _faceRenderers[i].enabled = isVisible;
            }
        }

        /// <summary>
        /// 设置某一面的状态。
        /// </summary>
        public void SetStatus(Direction face, FaceStatus status)
        {
            if (!_isValid)
            {
                return;
            }

            int index = (int)face;
            Renderer faceRenderer = _faceRenderers[index];
            faceRenderer.sharedMaterial = SelectMaterial(status);
            _isEnterable[index] = status == FaceStatus.Enterable;
            if (!_isEnterable[index])
            {
                SetEmission(faceRenderer, Color.black);
            }
        }

        private void SetEmission(Renderer faceRenderer, Color color)
        {
            faceRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, color);
            faceRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Material SelectMaterial(FaceStatus status)
        {
            switch (status)
            {
                case FaceStatus.Enterable:
                    return _enterableMaterial;
                case FaceStatus.WorldConflicted:
                    return _conflictedMaterial;
                default:
                    // 出口被挡和底座被占对玩家来说都是“这里过不去”，用同一种提示
                    return _blockedMaterial;
            }
        }
    }
}
