using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 传送箱子四个面上的提示板，按 <see cref="FaceStatus"/> 切换材质。
    /// </summary>
    public class PortalFaceHint : MonoBehaviour
    {
        private const int FaceCount = 4;

        [Tooltip("四个面的提示板，顺序为 Up(+z)、Right(+x)、Down(-z)、Left(-x)，与 Direction 枚举值一致")]
        [SerializeField] private Renderer[] _faceRenderers = new Renderer[FaceCount];

        [Header("材质")]
        [SerializeField] private Material _enterableMaterial;
        [SerializeField] private Material _blockedMaterial;
        [SerializeField] private Material _conflictedMaterial;

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

            _faceRenderers[(int)face].sharedMaterial = SelectMaterial(status);
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
