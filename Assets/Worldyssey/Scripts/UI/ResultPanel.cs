using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 通关结算：本次步数、最佳步数、下一关、重玩、返回选关。
    /// </summary>
    public class ResultPanel : UiPanel
    {
        [SerializeField] private TMP_Text _stepsText;
        [SerializeField] private TMP_Text _bestText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _levelSelectButton;

        public event Action NextRequested;
        public event Action ReplayRequested;
        public event Action LevelSelectRequested;

        private void Awake()
        {
            CheckReference(_stepsText, nameof(_stepsText));
            CheckReference(_bestText, nameof(_bestText));
            CheckReference(_nextButton, nameof(_nextButton));
            CheckReference(_replayButton, nameof(_replayButton));
            CheckReference(_levelSelectButton, nameof(_levelSelectButton));
        }

        private void OnEnable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(HandleNextClicked);
            }

            if (_replayButton != null)
            {
                _replayButton.onClick.AddListener(HandleReplayClicked);
            }

            if (_levelSelectButton != null)
            {
                _levelSelectButton.onClick.AddListener(HandleLevelSelectClicked);
            }
        }

        private void OnDisable()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(HandleNextClicked);
            }

            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveListener(HandleReplayClicked);
            }

            if (_levelSelectButton != null)
            {
                _levelSelectButton.onClick.RemoveListener(HandleLevelSelectClicked);
            }
        }

        /// <summary>
        /// 填充结算内容。<paramref name="hasNextLevel"/> 为 false 时隐藏“下一关”（最后一关或自建关卡）。
        /// </summary>
        public void ShowResult(int steps, int bestSteps, bool isNewBest, bool hasNextLevel)
        {
            if (_stepsText != null)
            {
                _stepsText.text = string.Format(UiText.ResultStepsFormat, steps);
            }

            if (_bestText != null)
            {
                _bestText.text = isNewBest ? UiText.NewBest : string.Format(UiText.ResultBestFormat, bestSteps);
            }

            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(hasNextLevel);
            }

            SetVisible(true);
        }

        private void HandleNextClicked()
        {
            NextRequested?.Invoke();
        }

        private void HandleReplayClicked()
        {
            ReplayRequested?.Invoke();
        }

        private void HandleLevelSelectClicked()
        {
            LevelSelectRequested?.Invoke();
        }
    }
}
