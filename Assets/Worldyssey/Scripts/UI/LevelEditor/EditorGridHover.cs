using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 鼠标悬停预览：在悬停的格子上用半透明的样子显示“当前画笔画下去会变成什么”，并在坐标文字里显示格子坐标
    /// （画直线时附带长度）。预览层运行时创建，不接收鼠标。
    /// </summary>
    internal sealed class EditorGridHover
    {
        private const float FillAlpha = 0.55f;

        private readonly RectTransform _root;
        private readonly Image _fill;
        private readonly Image _icon;
        private readonly TMP_Text _coordinateText;

        public EditorGridHover(RectTransform gridArea, TMP_Text coordinateText)
        {
            _coordinateText = coordinateText;
            _root = CreateRect("HoverPreview", gridArea);
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _fill = _root.gameObject.AddComponent<Image>();
            _fill.raycastTarget = false;

            RectTransform iconRect = CreateRect("Icon", _root);
            iconRect.anchorMin = new Vector2(0.2f, 0.2f);
            iconRect.anchorMax = new Vector2(0.8f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            _icon = iconRect.gameObject.AddComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            Hide();
        }

        public void Show(EditorGridCell cell, Color color, Sprite icon, int x, int y, int lineLength)
        {
            RectTransform cellRect = cell.RectTransform;
            _root.anchoredPosition = cellRect.anchoredPosition;
            _root.sizeDelta = cellRect.sizeDelta;

            // 放到最上层：格子绘制时会把自己移到最后，预览要盖在它上面
            _root.SetAsLastSibling();
            color.a = FillAlpha;
            _fill.color = color;
            _icon.enabled = icon != null;
            _icon.sprite = icon;
            _icon.color = EditorCellPainter.IconColorOn(color);
            _root.gameObject.SetActive(true);

            if (_coordinateText != null)
            {
                _coordinateText.text = lineLength > 1
                    ? string.Format(Localization.Translate(UiText.EditorLineFormat), x, y, lineLength)
                    : string.Format(Localization.Translate(UiText.EditorCoordinateFormat), x, y);
            }
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
            if (_coordinateText != null)
            {
                _coordinateText.text = string.Empty;
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
