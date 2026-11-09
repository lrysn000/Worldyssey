using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 教学箭头：画在界面上，指向场景里的某个点（箭头朝下）或界面上的某个元素（箭头朝上，放在元素下方），上下轻轻浮动。
    /// 由 TutorialDirector 每帧告诉它指向哪里。
    /// </summary>
    public class TutorialMarker : MonoBehaviour
    {
        [Tooltip("把场景坐标换算到屏幕的相机。界面画布是 Screen Space - Overlay")]
        [SerializeField] private Camera _camera;

        [Tooltip("箭头图片，默认朝下")]
        [SerializeField] private RectTransform _arrow;

        [Header("浮动")]
        [Range(0f, 40f)]
        [SerializeField] private float _bobHeight = 12f;

        [Range(0.1f, 5f)]
        [SerializeField] private float _bobSpeed = 2.5f;

        [Tooltip("指向界面元素时，箭头离元素下边缘的距离")]
        [Range(0f, 80f)]
        [SerializeField] private float _uiGap = 12f;

        private RectTransform _rect;
        private bool _hasReferences;

        private void Awake()
        {
            // 本物体铺满 HUD、轴心居中，箭头锚点居中，这样本物体的局部坐标就是箭头的 anchoredPosition
            _rect = (RectTransform)transform;
            _hasReferences = _camera != null && _arrow != null;
            if (!_hasReferences)
            {
                Debug.LogError($"[{nameof(TutorialMarker)}] {name} 缺少相机或箭头图片", this);
            }

            Hide();
        }

        /// <summary>指向场景里的一个点，箭头朝下画在它上方。点在相机后面时隐藏。</summary>
        public void PointAtWorld(Vector3 worldPoint)
        {
            if (!_hasReferences)
            {
                return;
            }

            Vector3 screen = _camera.WorldToScreenPoint(worldPoint);
            if (screen.z <= 0f
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out Vector2 local))
            {
                Hide();
                return;
            }

            Place(local, false);
        }

        /// <summary>指向界面上的元素，箭头朝上画在它下方。</summary>
        public void PointAtUi(RectTransform target)
        {
            if (!_hasReferences || target == null)
            {
                Hide();
                return;
            }

            Rect bounds = target.rect;
            Vector3 bottom = target.TransformPoint(new Vector3(bounds.center.x, bounds.yMin, 0f));
            Vector2 local = _rect.InverseTransformPoint(bottom);
            Place(local + Vector2.down * _uiGap, true);
        }

        public void Hide()
        {
            if (_arrow != null && _arrow.gameObject.activeSelf)
            {
                _arrow.gameObject.SetActive(false);
            }
        }

        private void Place(Vector2 anchor, bool isPointingUp)
        {
            if (!_arrow.gameObject.activeSelf)
            {
                _arrow.gameObject.SetActive(true);
            }

            // 箭头尖端对准目标：朝下时整个箭头在目标上方，朝上时在下方；浮动只往远离目标的方向
            float bob = (Mathf.Sin(Time.unscaledTime * _bobSpeed * Mathf.PI * 2f) * 0.5f + 0.5f) * _bobHeight;
            float halfHeight = _arrow.rect.height * 0.5f;
            float offset = halfHeight + bob;
            _arrow.anchoredPosition = anchor + (isPointingUp ? Vector2.down : Vector2.up) * offset;
            _arrow.localEulerAngles = new Vector3(0f, 0f, isPointingUp ? 180f : 0f);
        }
    }
}
