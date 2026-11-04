using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 鼠标悬停时整体平滑放大，移开后恢复。只响应鼠标，不响应键盘选中：点击后按钮会保持选中，跟着选中放大会一直停在大号。
    /// 不可交互的控件（例如锁住的关卡）不放大。以自身轴心缩放，轴心应在中心（由添加工具负责调整）。
    /// </summary>
    [DisallowMultipleComponent]
    public class UiHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UiMotionConfig _config;

        private Selectable _selectable;
        private Vector3 _baseScale = Vector3.one;
        private float _progress;
        private bool _isHovered;
        private Coroutine _routine;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _baseScale = transform.localScale;
            if (_config == null)
            {
                Debug.LogError($"[{nameof(UiHoverScale)}] {name} 缺少引用：{nameof(_config)}", this);
            }
        }

        private void OnDisable()
        {
            // 鼠标还停在上面时面板被隐藏，收不到移出事件；直接复原，下次显示不会停在放大状态
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _isHovered = false;
            _progress = 0f;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.IsInteractable())
            {
                return;
            }

            _isHovered = true;
            StartAnimation();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            StartAnimation();
        }

        private void StartAnimation()
        {
            if (_routine == null && _config != null && isActiveAndEnabled)
            {
                _routine = StartCoroutine(AnimateRoutine());
            }
        }

        private IEnumerator AnimateRoutine()
        {
            // 动画期间才逐帧更新，到位就停；用不受时间缩放影响的时间，暂停时也能正常响应
            while (true)
            {
                float target = _isHovered ? 1f : 0f;
                _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime / _config.HoverDuration);
                Apply();
                if (Mathf.Approximately(_progress, target))
                {
                    break;
                }

                yield return null;
            }

            _routine = null;
        }

        private void Apply()
        {
            float scale = _config != null ? Mathf.Lerp(1f, _config.HoverScale, Mathf.SmoothStep(0f, 1f, _progress)) : 1f;
            transform.localScale = _baseScale * scale;
        }
    }
}
