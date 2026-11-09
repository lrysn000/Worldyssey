using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 相机拖动平移的状态：当前平移量、拖动时抓住的地面点、回到中心的过渡。纯计算，由 <see cref="CameraRig"/> 持有并驱动。
    /// </summary>
    public sealed class CameraPanOffset
    {
        private Vector3 _grabPoint;
        private bool _hasGrab;
        private Vector3 _returnStart;
        private float _returnDuration;
        private float _returnElapsed;
        private bool _isReturning;

        /// <summary>相对关卡中心的水平平移量（Y 恒为 0）。</summary>
        public Vector3 Value { get; private set; }

        /// <summary>立即清零，并取消拖动和回中过渡。</summary>
        public void Clear()
        {
            Value = Vector3.zero;
            _hasGrab = false;
            _isReturning = false;
        }

        /// <summary>开始拖动：记下抓住的地面点。新的拖动会打断正在进行的回中。</summary>
        public void Grab(bool hasPoint, Vector3 point)
        {
            _isReturning = false;
            _hasGrab = hasPoint;
            _grabPoint = point;
        }

        /// <summary>
        /// 拖动中：让抓住的点回到鼠标下方的 <paramref name="hit"/>，平移量各轴限制在 ±<paramref name="limit"/> 内。
        /// 没有抓住点时返回 false。
        /// </summary>
        public bool Drag(Vector3 hit, Vector3 limit)
        {
            if (!_hasGrab)
            {
                return false;
            }

            Vector3 pan = Value + (_grabPoint - hit);
            Value = new Vector3(Mathf.Clamp(pan.x, -limit.x, limit.x), 0f, Mathf.Clamp(pan.z, -limit.z, limit.z));
            return true;
        }

        /// <summary>在 <paramref name="duration"/> 秒内平滑回到中心，并结束当前拖动。已经在中心时什么也不做。</summary>
        public void StartReturn(float duration)
        {
            _hasGrab = false;
            if (Value == Vector3.zero)
            {
                return;
            }

            if (duration <= 0f)
            {
                Clear();
                return;
            }

            _returnStart = Value;
            _returnDuration = duration;
            _returnElapsed = 0f;
            _isReturning = true;
        }

        /// <summary>推进回中过渡，返回平移量是否发生了变化（需要重新摆放相机）。</summary>
        public bool StepReturn(float deltaTime)
        {
            if (!_isReturning)
            {
                return false;
            }

            _returnElapsed += deltaTime;
            float t = Mathf.Clamp01(_returnElapsed / _returnDuration);
            Value = Vector3.Lerp(_returnStart, Vector3.zero, Mathf.SmoothStep(0f, 1f, t));
            _isReturning = t < 1f;
            return true;
        }
    }
}
