using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器网格中的一格：外框（标记问题）、地形底色和地形图标、实体图标和角标文字。把指针事件转交给 <see cref="EditorGridView"/>。
    /// 被绘制时弹一下，给鼠标操作一个手感反馈。
    /// </summary>
    public class EditorGridCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _terrain;
        [Tooltip("可选：地形图标（墙、独有墙、底座、终点），叠在地形底色上")]
        [SerializeField] private Image _terrainIcon;

        [SerializeField] private Image _entity;

        [Tooltip("可选：其他世界在这一格的独有墙或传送箱子，画在角上的淡色小图标")]
        [SerializeField] private Image _ghost;
        [SerializeField] private TMP_Text _label;

        private EditorGridView _owner;
        private Coroutine _popRoutine;

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

        private void OnDisable()
        {
            // 弹跳中途被隐藏（改尺寸、关面板）时直接复位
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>只读时外框不接收鼠标：不能绘制，鼠标指针也不会显示为可点击。</summary>
        public void SetInteractable(bool isInteractable)
        {
            _frame.raycastTarget = isInteractable;
        }

        public void Bind(EditorGridView owner, Core.GridPos position)
        {
            _owner = owner;
            Position = position;
        }

        /// <summary>设置显示内容，见 <see cref="EditorCellVisual"/>。</summary>
        public void Render(in EditorCellVisual visual)
        {
            _frame.color = visual.Frame;
            _terrain.color = visual.Terrain;
            SetIcon(_terrainIcon, visual.TerrainIcon, visual.TerrainIconColor);
            SetIcon(_ghost, visual.GhostIcon, visual.GhostColor);
            _entity.enabled = visual.Entity.a > 0f;
            _entity.color = visual.Entity;
            if (_entity.sprite != visual.EntityIcon)
            {
                _entity.sprite = visual.EntityIcon;
                _entity.type = Image.Type.Simple;
                _entity.preserveAspect = true;
            }

            _entity.rectTransform.localEulerAngles = new Vector3(0f, 0f, visual.EntityAngle);
            _label.text = visual.Label;
        }

        /// <summary>
        /// 从 <paramref name="startScale"/> 倍缩回原大小。弹跳期间移到同级最后，放大的部分不会被相邻格子盖住；
        /// 网格按列表而不是同级顺序管理格子，所以改顺序没有影响。
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

            transform.SetAsLastSibling();
            _popRoutine = StartCoroutine(PopRoutine(startScale, duration));
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

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_owner != null)
            {
                _owner.HandleCellExit(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_owner != null)
            {
                _owner.HandleCellUp();
            }
        }

        private static void SetIcon(Image image, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = sprite != null;
            image.sprite = sprite;
            image.color = color;
        }

        private IEnumerator PopRoutine(float startScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
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
