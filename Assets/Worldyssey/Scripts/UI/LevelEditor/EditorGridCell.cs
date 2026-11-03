using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器网格中的一格：外框（标记问题）、地形底色、实体色块和文字。把指针事件转交给 <see cref="EditorGridView"/>。
    /// </summary>
    public class EditorGridCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _terrain;
        [SerializeField] private Image _entity;
        [SerializeField] private TMP_Text _label;

        private EditorGridView _owner;

        /// <summary>该格对应的网格坐标。</summary>
        public Core.GridPos Position { get; private set; }

        public RectTransform RectTransform => (RectTransform)transform;

        private void Awake()
        {
            if (_frame == null || _terrain == null || _entity == null || _label == null)
            {
                Debug.LogError($"[{nameof(EditorGridCell)}] {name} 的图像或文字引用未设置", this);
            }
        }

        public void Bind(EditorGridView owner, Core.GridPos position)
        {
            _owner = owner;
            Position = position;
        }

        /// <summary>
        /// 设置显示内容。<paramref name="entityColor"/> 的 alpha 为 0 时隐藏实体色块。
        /// </summary>
        public void Render(Color frameColor, Color terrainColor, Color entityColor, string label)
        {
            _frame.color = frameColor;
            _terrain.color = terrainColor;
            _entity.enabled = entityColor.a > 0f;
            _entity.color = entityColor;
            _label.text = label;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_owner != null)
            {
                _owner.HandleCellDown(this, eventData.button);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_owner != null)
            {
                _owner.HandleCellEnter(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_owner != null)
            {
                _owner.HandleCellUp();
            }
        }
    }
}
