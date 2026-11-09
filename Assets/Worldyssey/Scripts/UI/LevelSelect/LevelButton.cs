using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关列表中的一个按钮：关卡名和状态（锁定 / 未通关 / 最佳步数）。
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color HighlightedColor = new Color(0.30f, 0.50f, 0.85f, 1f);

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _detailText;

        [Tooltip("可选：左侧的小图标，例如配对符号。不设置图标时隐藏")]
        [SerializeField] private Image _icon;

        [Tooltip("可选：底图上的 ThemedGraphic。设置后高亮通过切换主题角色实现，不再写死颜色")]
        [SerializeField] private ThemedGraphic _themedImage;

        private int _index;
        private Action<int> _onClicked;
        private ThemedGraphic _themedName;
        private ThemedGraphic _themedDetail;
        private ThemedGraphic _themedIcon;
        private UiColorRole _nameRole;
        private UiColorRole _detailRole;

        private void Awake()
        {
            if (_button == null || _nameText == null || _detailText == null)
            {
                Debug.LogError($"[{nameof(LevelButton)}] {name} 的按钮或文本引用未设置", this);
                return;
            }

            // 记下文字原本的颜色角色：高亮时底色变深，文字要换成浅色，取消高亮再换回来
            _themedName = _nameText.GetComponent<ThemedGraphic>();
            _themedDetail = _detailText.GetComponent<ThemedGraphic>();
            _themedIcon = _icon != null ? _icon.GetComponent<ThemedGraphic>() : null;
            _nameRole = _themedName != null ? _themedName.Role : UiColorRole.Text;
            _detailRole = _themedDetail != null ? _themedDetail.Role : UiColorRole.Text;
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }

        /// <summary>
        /// 填充显示内容。点击时以 <paramref name="index"/> 回调 <paramref name="onClicked"/>。
        /// </summary>
        public void Setup(LevelSelectItem item, int index, Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;

            if (_nameText != null)
            {
                _nameText.text = item.DisplayName;
            }

            if (_detailText != null)
            {
                _detailText.text = item.IsLocked
                    ? UiText.Locked
                    : item.BestSteps == LevelSelectItem.NoBestSteps
                        ? UiText.NotCleared
                        : string.Format(UiText.BestStepsFormat, item.BestSteps);
            }

            if (_button != null)
            {
                _button.interactable = !item.IsLocked;
            }
        }

        /// <summary>
        /// 直接指定标题和说明，用于选关以外的列表（编辑器的配对列表、打开关卡列表）。
        /// </summary>
        public void Setup(string title, string detail, int index, Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;
            if (_nameText != null)
            {
                _nameText.text = title;
            }

            if (_detailText != null)
            {
                _detailText.text = detail;
            }

            if (_button != null)
            {
                _button.interactable = true;
            }
        }

        /// <summary>设置左侧小图标，null 时隐藏。没有图标层时忽略。</summary>
        public void SetIcon(Sprite icon)
        {
            if (_icon != null)
            {
                _icon.enabled = icon != null;
                _icon.sprite = icon;
            }
        }

        /// <summary>
        /// 高亮表示当前选中项。
        /// </summary>
        public void SetHighlighted(bool isHighlighted)
        {
            if (_themedImage != null)
            {
                _themedImage.SetRole(isHighlighted ? UiColorRole.Accent : UiColorRole.InnerFrame);
                SetTextRole(_themedName, isHighlighted ? UiColorRole.HeaderText : _nameRole);
                SetTextRole(_themedDetail, isHighlighted ? UiColorRole.HeaderText : _detailRole);
                SetTextRole(_themedIcon, isHighlighted ? UiColorRole.HeaderText : _nameRole);
                return;
            }

            if (_button != null && _button.image != null)
            {
                _button.image.color = isHighlighted ? HighlightedColor : NormalColor;
            }
        }

        private static void SetTextRole(ThemedGraphic text, UiColorRole role)
        {
            if (text != null)
            {
                text.SetRole(role);
            }
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_index);
        }
    }
}
