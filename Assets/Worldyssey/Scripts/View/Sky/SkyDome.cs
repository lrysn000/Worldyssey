using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 挂在天穹预制体根节点上：天空球染季节色，太阳圆盘摆到平行光的方向，让天上的太阳和地上的影子对得上。
    /// </summary>
    public class SkyDome : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("天空球本身的渲染器")]
        [SerializeField] private Renderer _domeRenderer;

        [Tooltip("可选：太阳圆盘的旋转轴，圆盘在它的正前方（+z）")]
        [SerializeField] private Transform _sunPivot;

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            if (_domeRenderer == null)
            {
                Debug.LogError($"[{nameof(SkyDome)}] {name} 缺少引用：{nameof(_domeRenderer)}", this);
            }
        }

        /// <summary>
        /// 天空的季节色调，乘在天空贴图上。
        /// </summary>
        public void SetTint(Color tint)
        {
            if (_domeRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _domeRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, tint);
            _domeRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// 把太阳圆盘转到 <paramref name="towardSun"/> 方向（从地面指向太阳）。
        /// </summary>
        public void SetSunDirection(Vector3 towardSun)
        {
            if (_sunPivot != null && towardSun != Vector3.zero)
            {
                _sunPivot.rotation = Quaternion.LookRotation(towardSun);
            }
        }
    }
}
