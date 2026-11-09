using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 世界指示器里的一个图标位：季节图标加右下角的冲突叉号。
    /// 显示什么、多大、什么颜色都由 <see cref="WorldIndicatorPanel"/> 决定，这里只负责摆放和弹跳动画。
    /// </summary>
    public class WorldIconSlot : MonoBehaviour
    {
        [SerializeField] private Image _icon;

        [Tooltip("冲突叉号，尺寸按锚点随图标缩放")]
        [SerializeField] private Image _conflictMark;

        private RectTransform _rect;
        private Coroutine _popRoutine;

        // 图标位初始可能是隐藏的，Awake 未必先于 Show 执行，所以按需获取
        private RectTransform Rect => _rect != null ? _rect : _rect = (RectTransform)transform;

        private void Awake()
        {
            if (_icon == null)
            {
                Debug.LogError($"[{nameof(WorldIconSlot)}] {name} 缺少引用：{nameof(_icon)}", this);
            }

            if (_conflictMark == null)
            {
                Debug.LogError($"[{nameof(WorldIconSlot)}] {name} 缺少引用：{nameof(_conflictMark)}", this);
            }
        }

        private void OnDisable()
        {
            // 弹跳中途被隐藏时直接复位，下次显示不会停在放大的状态
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 显示图标。<paramref name="x"/> 为图标中心在行内的横向位置，<paramref name="size"/> 为边长。
        /// </summary>
        public void Show(Sprite icon, Color color, float x, float size, bool isConflicted, Color markColor)
        {
            gameObject.SetActive(true);
            Rect.anchoredPosition = new Vector2(x, 0f);
            Rect.sizeDelta = new Vector2(size, size);

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.color = color;
            }

            if (_conflictMark != null)
            {
                _conflictMark.gameObject.SetActive(isConflicted);
                _conflictMark.color = markColor;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 从 <paramref name="startScale"/> 倍缩回原大小，用于切换世界时提示玩家“到了这里”。
        /// </summary>
        public void Pop(float startScale, float duration)
        {
            if (!isActiveAndEnabled || duration <= 0f)
            {
                return;
            }

            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
            }

            _popRoutine = StartCoroutine(PopRoutine(startScale, duration));
        }

        private IEnumerator PopRoutine(float startScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 暂停时 timeScale 可能为 0，界面动画用不受影响的时间
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
                yield return null;
            }

            transform.localScale = Vector3.one;
            _popRoutine = null;
        }
    }
}
