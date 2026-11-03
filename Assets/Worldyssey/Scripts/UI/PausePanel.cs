using System;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 暂停菜单：继续、重开、返回选关。
    /// </summary>
    public class PausePanel : UiPanel
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _levelSelectButton;

        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action LevelSelectRequested;

        private void Awake()
        {
            CheckReference(_resumeButton, nameof(_resumeButton));
            CheckReference(_restartButton, nameof(_restartButton));
            CheckReference(_levelSelectButton, nameof(_levelSelectButton));
        }

        private void OnEnable()
        {
            AddListener(_resumeButton, HandleResumeClicked);
            AddListener(_restartButton, HandleRestartClicked);
            AddListener(_levelSelectButton, HandleLevelSelectClicked);
        }

        private void OnDisable()
        {
            RemoveListener(_resumeButton, HandleResumeClicked);
            RemoveListener(_restartButton, HandleRestartClicked);
            RemoveListener(_levelSelectButton, HandleLevelSelectClicked);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private void HandleResumeClicked()
        {
            ResumeRequested?.Invoke();
        }

        private void HandleRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private void HandleLevelSelectClicked()
        {
            LevelSelectRequested?.Invoke();
        }
    }
}
