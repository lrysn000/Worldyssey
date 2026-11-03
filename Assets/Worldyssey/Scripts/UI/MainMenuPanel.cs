using System;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 主菜单：开始游戏、关卡编辑器、退出。设置入口在 M8 加入。
    /// </summary>
    public class MainMenuPanel : UiPanel
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        [Tooltip("可选：场景中没有关卡编辑器时留空，按钮会被隐藏")]
        [SerializeField] private Button _levelEditorButton;

        public event Action StartRequested;
        public event Action QuitRequested;
        public event Action LevelEditorRequested;

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

            if (_levelEditorButton != null)
            {
                _levelEditorButton.onClick.AddListener(HandleLevelEditorClicked);
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

            if (_levelEditorButton != null)
            {
                _levelEditorButton.onClick.RemoveListener(HandleLevelEditorClicked);
            }
        }

        /// <summary>
        /// 场景中有没有关卡编辑器，决定是否显示入口按钮。
        /// </summary>
        public void SetLevelEditorAvailable(bool isAvailable)
        {
            if (_levelEditorButton != null)
            {
                _levelEditorButton.gameObject.SetActive(isAvailable);
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

        private void HandleLevelEditorClicked()
        {
            LevelEditorRequested?.Invoke();
        }
    }
}
