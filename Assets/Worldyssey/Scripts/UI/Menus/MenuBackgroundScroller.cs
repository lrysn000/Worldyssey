using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 主菜单背景横向循环滚动：贴图高度铺满屏幕、宽度按原比例截取一段，每帧推进 UV 偏移。
    /// 贴图需要 Wrap Mode = Repeat，并且左右边缘能无缝衔接。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MenuBackgroundScroller : MonoBehaviour
    {
        [Header("滚动")]
        [Tooltip("整张图滚完一圈所需的秒数，越大越慢")]
        [Min(1f)]
        [SerializeField] private float _loopSeconds = 80f;

        [Tooltip("勾选时向左滚（画面内容从右往左移动）")]
        [SerializeField] private bool _isScrollingLeft = true;

        private RawImage _image;
        private RectTransform _rect;
        private float _offset;
        private float _visibleWidth = 1f;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _rect = (RectTransform)transform;
            if (_image.texture == null)
            {
                Debug.LogError($"[{nameof(MenuBackgroundScroller)}] {name} 的 RawImage 没有贴图", this);
            }
        }

        private void OnEnable()
        {
            RefreshVisibleWidth();
        }

        private void OnRectTransformDimensionsChange()
        {
            // 分辨率或窗口变化时才重新计算可见宽度，平时 Update 只做加法
            if (_image != null)
            {
                RefreshVisibleWidth();
            }
        }

        private void Update()
        {
            // 用不受暂停影响的时间，菜单打开时 timeScale 可能为 0
            float delta = Time.unscaledDeltaTime / _loopSeconds;
            _offset = Mathf.Repeat(_offset + (_isScrollingLeft ? delta : -delta), 1f);
            _image.uvRect = new Rect(_offset, 0f, _visibleWidth, 1f);
        }

        private void RefreshVisibleWidth()
        {
            Texture texture = _image.texture;
            Rect size = _rect.rect;
            if (texture == null || size.height <= 0f || texture.height <= 0)
            {
                return;
            }

            // 高度铺满：屏幕宽高比 ÷ 贴图宽高比 = 一屏能看到贴图宽度的几分之几（超宽屏时大于 1，靠 Repeat 补齐）
            float screenAspect = size.width / size.height;
            float textureAspect = (float)texture.width / texture.height;
            _visibleWidth = screenAspect / textureAspect;
            _image.uvRect = new Rect(_offset, 0f, _visibleWidth, 1f);
        }
    }
}
