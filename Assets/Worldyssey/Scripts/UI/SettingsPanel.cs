using System;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 设置面板。目前只是空壳：能从主菜单和暂停菜单打开、关闭，并跟随主题；具体设置项以后加入。
    /// </summary>
    public class SettingsPanel : UiPanel
    {
        [SerializeField] private Button _closeButton;

        /// <summary>点击了关闭按钮。</summary>
        public event Action CloseRequested;

        private void Awake()
        {
            CheckReference(_closeButton, nameof(_closeButton));
        }

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
