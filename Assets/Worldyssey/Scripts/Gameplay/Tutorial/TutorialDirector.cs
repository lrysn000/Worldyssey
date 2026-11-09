using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.UI;
using Worldyssey.View;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 关卡教学：按 <see cref="TutorialLibrary"/> 里的步骤逐条显示提示条和箭头，玩家做到了就进入下一条；
    /// 另外在卡关时（想进被锁住的世界、箱子推进墙角、反复撤销）给一次性提示。
    /// 只在首次通关前生效，由 GameFlow 在开始关卡时调用 <see cref="Begin"/>。
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private TutorialLibrary _library;
        [SerializeField] private TutorialHintPanel _hintPanel;
        [SerializeField] private TutorialMarker _marker;

        [Tooltip("可选：指向世界图标的步骤需要它")]
        [SerializeField] private WorldIndicatorPanel _worldIndicator;

        private readonly TutorialStepSequence _sequence = new TutorialStepSequence();
        private TutorialTargetPointer _pointer;
        private StuckHintWatcher _stuckHints;
        private bool _isActive;
        private int _conflictMask;

        // 卡关提示临时盖住当前教学步骤，到时间后恢复
        private bool _isStuckHintShowing;
        private float _stuckHintUntil;
        private TutorialTarget _stuckTarget;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelSession, nameof(_levelSession))
                & CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_library, nameof(_library))
                & CheckReference(_hintPanel, nameof(_hintPanel))
                & CheckReference(_marker, nameof(_marker));
            if (_hasReferences)
            {
                _pointer = new TutorialTargetPointer(_levelSession, _levelView, _worldIndicator, _marker);
                _stuckHints = new StuckHintWatcher(_library);
            }
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _levelSession.StepApplied += HandleStepApplied;
            _levelSession.TeleportFailed += HandleTeleportFailed;
            _levelSession.Undone += HandleUndone;
            _levelSession.Restarted += HandleRestarted;
            _levelSession.StateChanged += HandleStateChanged;
            _levelSession.LevelCompleted += HandleLevelCompleted;
            Localization.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _levelSession.StepApplied -= HandleStepApplied;
            _levelSession.TeleportFailed -= HandleTeleportFailed;
            _levelSession.Undone -= HandleUndone;
            _levelSession.Restarted -= HandleRestarted;
            _levelSession.StateChanged -= HandleStateChanged;
            _levelSession.LevelCompleted -= HandleLevelCompleted;
            Localization.LanguageChanged -= HandleLanguageChanged;
            Stop();
        }

        private void Update()
        {
            if (!_isActive)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (_isStuckHintShowing && now >= _stuckHintUntil)
            {
                _isStuckHintShowing = false;
                ShowCurrentText();
            }

            // 入场转场、暂停期间不出新提示，等玩家能操作了再出
            if (_sequence.Tick(now, _levelSession.IsInputEnabled))
            {
                ShowCurrentText();
            }
        }

        private void LateUpdate()
        {
            if (!_isActive)
            {
                return;
            }

            TutorialStep step = _sequence.DisplayedStep;
            TutorialTarget target = _isStuckHintShowing ? _stuckTarget
                : step != null ? step.Target
                : TutorialTarget.None;
            _pointer.Point(target, _library.MarkerHeight);
        }

        /// <summary>
        /// 关卡开始时调用。<paramref name="isFirstPlay"/> 为 false（已通关、编辑器试玩）时教学和卡关提示都不出现。
        /// </summary>
        public void Begin(string levelId, bool isFirstPlay)
        {
            Stop();
            if (!_hasReferences || !isFirstPlay || !_levelSession.HasLevel)
            {
                return;
            }

            _isActive = true;
            _stuckHints.Reset();
            _conflictMask = GetConflictMask();
            LevelTutorial tutorial = _library.TryGetTutorial(levelId, out LevelTutorial found) ? found : null;
            _sequence.Begin(tutorial?.Steps, _library.StepGapSeconds, Time.unscaledTime);
        }

        /// <summary>关掉提示和箭头，清空进度。</summary>
        public void Stop()
        {
            _isActive = false;
            _sequence.Clear();
            _isStuckHintShowing = false;
            if (_hintPanel != null)
            {
                _hintPanel.Hide();
            }

            if (_marker != null)
            {
                _marker.Hide();
            }
        }

        private void HandleSignal(TutorialSignal signal)
        {
            if (_sequence.Receive(signal, Time.unscaledTime))
            {
                ShowCurrentText();
            }
        }

        private void ShowCurrentText()
        {
            if (_isStuckHintShowing)
            {
                return;
            }

            TutorialStep step = _sequence.DisplayedStep;
            if (step != null)
            {
                _hintPanel.Show(Localization.Translate(step.Text));
            }
            else
            {
                _hintPanel.Hide();
            }
        }

        private void ShowStuckHint(string message, TutorialTarget target)
        {
            _isStuckHintShowing = true;
            _stuckHintUntil = Time.unscaledTime + _library.StuckHintSeconds;
            _stuckTarget = target;
            _hintPanel.Show(message);
        }

        private int GetConflictMask()
        {
            PlaySession session = _levelSession.Session;
            return session != null ? TutorialQueries.GetConflictMask(session.Rules, session.State) : 0;
        }

        private void HandleStepApplied(StepResult result)
        {
            if (!_isActive)
            {
                return;
            }

            int conflicts = GetConflictMask();
            LevelMap map = _levelSession.Session.Rules.Map;
            TutorialSignal signal = TutorialSignal.FromStep(result, map, _conflictMask, conflicts);
            _conflictMask = conflicts;
            HandleSignal(signal);
            if (_stuckHints.TryGetCornerHint(result, map, out string message, out TutorialTarget target))
            {
                ShowStuckHint(message, target);
            }
        }

        private void HandleTeleportFailed(StepFailure failure)
        {
            if (!_isActive || failure != StepFailure.WorldConflicted)
            {
                return;
            }

            if (_stuckHints.TryGetLockedWorldHint(_levelSession.Session, out string message, out TutorialTarget target))
            {
                ShowStuckHint(message, target);
            }
        }

        private void HandleUndone()
        {
            if (!_isActive)
            {
                return;
            }

            _conflictMask = GetConflictMask();
            HandleSignal(TutorialSignal.Direct(TutorialTrigger.Undone));
            if (_stuckHints.TryGetUndoHint(Time.unscaledTime, out string message))
            {
                ShowStuckHint(message, TutorialTarget.None);
            }
        }

        private void HandleRestarted()
        {
            if (!_isActive)
            {
                return;
            }

            _conflictMask = GetConflictMask();
            HandleSignal(TutorialSignal.Direct(TutorialTrigger.Restarted));
        }

        private void HandleStateChanged()
        {
            if (_isActive && !_levelSession.HasLevel)
            {
                Stop();
            }
        }

        private void HandleLanguageChanged()
        {
            if (!_isActive)
            {
                return;
            }

            // 卡关提示是按旧语言拼好的文字，直接结束它，回到按新语言显示的当前教学步骤
            _isStuckHintShowing = false;
            ShowCurrentText();
        }

        private void HandleLevelCompleted(int steps)
        {
            Stop();
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(TutorialDirector)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
