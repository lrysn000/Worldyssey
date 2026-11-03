using System;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 主菜单：开始游戏、退出。设置和关卡编辑器入口在对应功能完成后加入。
    /// </summary>
    public class MainMenuPanel : UiPanel
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        public event Action StartRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            CheckReference(_startButton, nameof(_startButton));
            CheckReference(_quitButton, nameof(_quitButton));
        }

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(HandleStartClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(HandleQuitClicked);
            }
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(HandleQuitClicked);
            }
        }

        private void HandleStartClicked()
        {
            StartRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            QuitRequested?.Invoke();
        }
    }
}
