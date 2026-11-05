using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 通用确认对话框：一段说明文字加确认、取消两个按钮。确认时执行调用方传入的操作，取消或确认后都会关闭。
    /// </summary>
    public class ConfirmDialog : UiPanel
    {
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirmed;

        private void Awake()
        {
            CheckReference(_messageText, nameof(_messageText));
            CheckReference(_confirmButton, nameof(_confirmButton));
            CheckReference(_cancelButton, nameof(_cancelButton));
        }

        private void OnEnable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(HandleCancelClicked);
            }
        }

        private void OnDisable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }

            _onConfirmed = null;
        }

        /// <summary>显示对话框，玩家确认时调用 <paramref name="onConfirmed"/>。</summary>
        public void Show(string message, Action onConfirmed)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            SetVisible(true);
            _onConfirmed = onConfirmed;
        }

        private void HandleConfirmClicked()
        {
            // 先关闭再执行：执行的操作可能会刷新整个页面
            Action onConfirmed = _onConfirmed;
            SetVisible(false);
            onConfirmed?.Invoke();
        }

        private void HandleCancelClicked()
        {
            SetVisible(false);
        }
    }
}
