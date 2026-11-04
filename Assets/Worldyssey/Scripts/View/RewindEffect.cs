using UnityEngine;
using UnityEngine.Rendering;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 撤销时的“回溯”反馈：画面短暂褪色。连续撤销时保持褪色，停手后再恢复。
    /// </summary>
    /// <remarks>
    /// 褪色效果放在一个独立的后处理 Volume 里，这里只调它的权重，不在运行时修改配置资源本身。
    /// 只在褪色进行中启用本组件，平时不逐帧运行。
    /// </remarks>
    public class RewindEffect : MonoBehaviour
    {
        [Tooltip("只包含褪色设置的后处理 Volume，权重为 0 时不生效")]
        [SerializeField] private Volume _volume;
        [SerializeField] private GameplayConfig _gameplayConfig;

        private float _holdUntil;
        private bool _isValid;

        private void Awake()
        {
            _isValid = _volume != null && _gameplayConfig != null;
            if (!_isValid)
            {
                Debug.LogError($"[{nameof(RewindEffect)}] {name} 缺少 Volume 或 GameplayConfig 引用", this);
                return;
            }

            _volume.weight = 0f;
            enabled = false;
        }

        private void Update()
        {
            bool isHolding = Time.time < _holdUntil;
            float duration = isHolding ? _gameplayConfig.RewindFadeInDuration : _gameplayConfig.RewindFadeOutDuration;
            float step = duration > 0f ? Time.deltaTime / duration : 1f;
            _volume.weight = Mathf.MoveTowards(_volume.weight, isHolding ? 1f : 0f, step);

            if (!isHolding && _volume.weight <= 0f)
            {
                enabled = false;
            }
        }

        /// <summary>
        /// 触发一次回溯褪色。褪色中再次触发会延长保持时间。
        /// </summary>
        public void Trigger()
        {
            if (!_isValid)
            {
                return;
            }

            _holdUntil = Time.time + _gameplayConfig.RewindHoldDuration;
            enabled = true;
        }

        /// <summary>
        /// 立即恢复正常画面，用于进入或退出关卡。
        /// </summary>
        public void ResetImmediately()
        {
            if (!_isValid)
            {
                return;
            }

            _holdUntil = 0f;
            _volume.weight = 0f;
            enabled = false;
        }
    }
}
