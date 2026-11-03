using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.Gameplay;

namespace Worldyssey.UI
{
    /// <summary>
    /// 游戏内 HUD：步数、关卡名、当前世界、冲突中的世界、通关和传送失败提示，以及撤销和重开按钮。
    /// </summary>
    /// <remarks>
    /// 文案暂用英文：TextMeshPro 默认字体没有中文字形，中文字体资源在 M6 做 UI 时统一处理。
    /// </remarks>
    public class HudPanel : MonoBehaviour
    {
        private const string StepCountFormat = "Steps: {0}";
        private const string SolvedText = "Solved!";
        private const string ConflictPrefix = "Conflict: ";
        private const string ConflictSeparator = ", ";
        private const string WorldConflictedHint = "Target world is in conflict";
        private const string ExitBlockedHint = "Exit is blocked on this side";
        private const string PedestalOccupiedHint = "Target pedestal is occupied";

        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private GameplayConfig _gameplayConfig;

        [Header("文本")]
        [SerializeField] private TMP_Text _stepCountText;
        [SerializeField] private TMP_Text _levelNameText;
        [SerializeField] private TMP_Text _worldNameText;
        [SerializeField] private TMP_Text _conflictText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _hintText;

        [Header("按钮")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;

        private readonly StringBuilder _conflictBuilder = new StringBuilder(64);
        private Coroutine _hintRoutine;

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

            if (_undoButton != null)
            {
                _undoButton.onClick.AddListener(HandleUndoClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartClicked);
            }
        }

        private void Start()
        {
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

            if (_undoButton != null)
            {
                _undoButton.onClick.RemoveListener(HandleUndoClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }

            SetHint(string.Empty);
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
            _levelSession.RequestUndo();
        }

        private void HandleRestartClicked()
        {
            _levelSession.RequestRestart();
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
                _stepCountText.text = string.Format(StepCountFormat, session.StepCount);
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
                _statusText.text = session.IsSolved ? SolvedText : string.Empty;
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

                _conflictBuilder.Append(_conflictBuilder.Length == 0 ? ConflictPrefix : ConflictSeparator);
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
        }

        private static string ToHint(StepFailure failure)
        {
            switch (failure)
            {
                case StepFailure.WorldConflicted:
                    return WorldConflictedHint;
                case StepFailure.ExitBlocked:
                    return ExitBlockedHint;
                case StepFailure.PedestalOccupied:
                    return PedestalOccupiedHint;
                default:
                    return string.Empty;
            }
        }

        private void CheckReference(Object reference, string fieldName)
        {
            if (reference == null)
            {
                Debug.LogError($"[{nameof(HudPanel)}] {name} 缺少引用：{fieldName}", this);
            }
        }
    }
}
