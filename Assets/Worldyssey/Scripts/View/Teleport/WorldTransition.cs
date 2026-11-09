using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.View
{
    /// <summary>
    /// 世界切换的过渡：控制一个全屏遮罩的颜色和透明度。传送时遮罩染成目标世界的代表色。
    /// </summary>
    public class WorldTransition : MonoBehaviour
    {
        [Tooltip("全屏黑色遮罩上的 CanvasGroup")]
        [SerializeField] private CanvasGroup _overlay;

        private Graphic _overlayGraphic;

        private void Awake()
        {
            if (_overlay == null)
            {
                Debug.LogError($"[{nameof(WorldTransition)}] {name} 缺少引用：{nameof(_overlay)}", this);
                return;
            }

            // 遮罩图像与 CanvasGroup 在同一物体上，由生成工具保证
            _overlayGraphic = _overlay.GetComponent<Graphic>();
            SetClear();
        }

        /// <summary>
        /// 设置遮罩颜色（不影响透明度，透明度由 <see cref="FadeTo"/> 控制）。
        /// </summary>
        public void SetColor(Color color)
        {
            if (_overlayGraphic != null)
            {
                color.a = 1f;
                _overlayGraphic.color = color;
            }
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
