using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private int _index;
        private Action<int> _onClicked;

        private void Awake()
        {
            if (_button == null || _nameText == null || _detailText == null)
            {
                Debug.LogError($"[{nameof(LevelButton)}] {name} 的按钮或文本引用未设置", this);
            }
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

        /// <summary>
        /// 高亮表示当前选中项。
        /// </summary>
        public void SetHighlighted(bool isHighlighted)
        {
            if (_button != null && _button.image != null)
            {
                _button.image.color = isHighlighted ? HighlightedColor : NormalColor;
            }
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_index);
        }
    }
}
