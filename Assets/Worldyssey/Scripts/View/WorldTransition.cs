using System.Collections;
using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 世界切换的过渡：控制一个全屏遮罩的透明度。M8 再替换为正式效果。
    /// </summary>
    public class WorldTransition : MonoBehaviour
    {
        [Tooltip("全屏黑色遮罩上的 CanvasGroup")]
        [SerializeField] private CanvasGroup _overlay;

        private void Awake()
        {
            if (_overlay == null)
            {
                Debug.LogError($"[{nameof(WorldTransition)}] {name} 缺少引用：{nameof(_overlay)}", this);
                return;
            }

            SetClear();
        }

        /// <summary>
        /// 立即恢复为完全透明，用于打断过渡。
        /// </summary>
        public void SetClear()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.alpha = 0f;

            // 遮罩只负责视觉，不能挡住 HUD 按钮的点击
            _overlay.blocksRaycasts = false;
            _overlay.interactable = false;
        }

        /// <summary>
        /// 在 <paramref name="duration"/> 秒内把遮罩透明度从当前值过渡到 <paramref name="targetAlpha"/>。
        /// 由调用方的协程 yield。
        /// </summary>
        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (_overlay == null)
            {
                yield break;
            }

            float startAlpha = _overlay.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _overlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _overlay.alpha = targetAlpha;
        }
    }
}
