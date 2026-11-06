using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 像素按钮的按下效果：按住时换成“下沉”的底图，按钮里的文字和图标一起下移一格，松开或移出时恢复。
    /// 与按钮自身的颜色过渡、悬停放大互不影响（它们改的是颜色和缩放）。
    /// </summary>
    public class PixelButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _pressedSprite;

        [Tooltip("按下时内容下移的距离（界面单位），一般为一个素材像素")]
        [Min(0f)]
        [SerializeField] private float _contentOffset = 4f;

        private readonly List<RectTransform> _content = new List<RectTransform>();
        private Selectable _selectable;
        private bool _isPressed;

        private void Awake()
        {
            if (_image == null || _normalSprite == null || _pressedSprite == null)
            {
                Debug.LogError($"[{nameof(PixelButtonPress)}] {name} 缺少图像或底图引用", this);
            }

            _selectable = GetComponent<Selectable>();
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is RectTransform child)
                {
                    _content.Add(child);
                }
            }
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            bool isInteractable = _selectable == null || _selectable.IsInteractable();
            if (eventData.button == PointerEventData.InputButton.Left && isInteractable)
            {
                SetPressed(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

        private void SetPressed(bool isPressed)
        {
            if (isPressed == _isPressed || _image == null)
            {
                return;
            }

            _isPressed = isPressed;
            _image.sprite = isPressed ? _pressedSprite : _normalSprite;
            float delta = isPressed ? -_contentOffset : _contentOffset;
            for (int i = 0; i < _content.Count; i++)
            {
                _content[i].anchoredPosition += new Vector2(0f, delta);
            }
        }
    }
}
