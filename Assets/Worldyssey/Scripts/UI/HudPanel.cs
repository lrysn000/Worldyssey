using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.Gameplay;

namespace Worldyssey.UI
{
    /// <summary>
    /// 游戏内 HUD：步数、关卡名、当前世界、冲突中的世界、通关和传送失败提示，以及撤销、重开、暂停按钮。
    /// </summary>
    public class HudPanel : UiPanel
    {
        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private GameplayConfig _gameplayConfig;

        [Header("文本")]
        [SerializeField] private TMP_Text _stepCountText;
        [SerializeField] private TMP_Text _levelNameText;
        [SerializeField] private TMP_Text _worldNameText;
        [SerializeField] private TMP_Text _conflictText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _hintText;

        [Header("文字底板（可选）")]
        [Tooltip("通关文字的底板，有文字时才显示")]
        [SerializeField] private GameObject _statusBackground;

        [Tooltip("传送失败提示的底板，有提示时才显示")]
        [SerializeField] private GameObject _hintBackground;

        [Header("按钮")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;

        [Tooltip("可选：没有暂停流程的开发场景可以不设置")]
        [SerializeField] private Button _pauseButton;

        private readonly StringBuilder _conflictBuilder = new StringBuilder(64);
        private Coroutine _hintRoutine;

        /// <summary>点击了暂停按钮。</summary>
        public event Action PauseRequested;

        private void Awake()
        {
            CheckReference(_levelSession, nameof(_levelSession));
            CheckReference(_gameplayConfig, nameof(_gameplayConfig));
            CheckReference(_stepCountText, nameof(_stepCountText));
            CheckReference(_levelNameText, nameof(_levelNameText));
            CheckReference(_worldNameText, nameof(_worldNameText));
            CheckReference(_conflictText, nameof(_conflictText));
            CheckReference(_statusText, nameof(_statusText));
            CheckReference(_hintText, nameof(_hintText));
            CheckReference(_undoButton, nameof(_undoButton));
            CheckReference(_restartButton, nameof(_restartButton));
        }

        private void OnEnable()
        {
            if (_levelSession != null)
            {
                _levelSession.StateChanged += HandleStateChanged;
                _levelSession.TeleportFailed += HandleTeleportFailed;
            }

            AddListener(_undoButton, HandleUndoClicked);
            AddListener(_restartButton, HandleRestartClicked);
            AddListener(_pauseButton, HandlePauseClicked);

            // 面板可能在关卡加载之后才显示，显示时补一次刷新
            SetHint(string.Empty);
            Refresh();
        }

        private void OnDisable()
        {
            if (_levelSession != null)
            {
                _levelSession.StateChanged -= HandleStateChanged;
                _levelSession.TeleportFailed -= HandleTeleportFailed;
            }

            RemoveListener(_undoButton, HandleUndoClicked);
            RemoveListener(_restartButton, HandleRestartClicked);
            RemoveListener(_pauseButton, HandlePauseClicked);

            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }
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

        private static void ClearUiSelection()
        {
            // 点击后按钮会保持选中，之后按空格或回车会再次触发它；游戏中用键盘操作，清掉选中避免误触
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void HandleStateChanged()
        {
            Refresh();
        }

        private void HandleTeleportFailed(StepFailure failure)
        {
            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
            }

            _hintRoutine = StartCoroutine(ShowHintRoutine(ToHint(failure)));
        }

        private void HandleUndoClicked()
        {
            ClearUiSelection();
            _levelSession.RequestUndo();
        }

        private void HandleRestartClicked()
        {
            ClearUiSelection();
            _levelSession.RequestRestart();
        }

        private void HandlePauseClicked()
        {
            ClearUiSelection();
            PauseRequested?.Invoke();
        }

        private IEnumerator ShowHintRoutine(string message)
        {
            SetHint(message);
            float duration = _gameplayConfig != null ? _gameplayConfig.HintDuration : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetHint(string.Empty);
            _hintRoutine = null;
        }

        private void Refresh()
        {
            if (_levelSession == null || _levelSession.Session == null)
            {
                return;
            }

            PlaySession session = _levelSession.Session;
            if (_stepCountText != null)
            {
                _stepCountText.text = string.Format(UiText.StepCountFormat, session.StepCount);
            }

            if (_levelNameText != null)
            {
                _levelNameText.text = _levelSession.LevelDisplayName;
            }

            if (_worldNameText != null)
            {
                _worldNameText.text = _levelSession.CurrentWorldName;
            }

            if (_conflictText != null)
            {
                _conflictText.text = BuildConflictText(session.Rules.Map);
            }

            if (_statusText != null)
            {
                _statusText.text = session.IsSolved ? UiText.Solved : string.Empty;
            }

            if (_statusBackground != null)
            {
                _statusBackground.SetActive(session.IsSolved);
            }
        }

        private string BuildConflictText(LevelMap map)
        {
            _conflictBuilder.Clear();
            for (int world = 0; world < map.WorldCount; world++)
            {
                if (!_levelSession.IsWorldConflicted(world))
                {
                    continue;
                }

                _conflictBuilder.Append(_conflictBuilder.Length == 0 ? UiText.ConflictPrefix : UiText.ConflictSeparator);
                _conflictBuilder.Append(map.GetWorldName(world));
            }

            return _conflictBuilder.ToString();
        }

        private void SetHint(string message)
        {
            if (_hintText != null)
            {
                _hintText.text = message;
            }

            if (_hintBackground != null)
            {
                _hintBackground.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private static string ToHint(StepFailure failure)
        {
            switch (failure)
            {
                case StepFailure.WorldConflicted:
                    return UiText.WorldConflictedHint;
                case StepFailure.ExitBlocked:
                    return UiText.ExitBlockedHint;
                case StepFailure.PedestalOccupied:
                    return UiText.PedestalOccupiedHint;
                default:
                    return string.Empty;
            }
        }
    }
}
