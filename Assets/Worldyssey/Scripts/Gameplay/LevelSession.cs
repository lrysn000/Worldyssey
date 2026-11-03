using System;
using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.View;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 场景中的一局游戏：从关卡资源创建 <see cref="PlaySession"/>，把输入命令交给它，再把结果交给表现层。
    /// 流程规则在 <see cref="PlaySession"/>，输入时序在 <see cref="InputCommandScheduler"/>，这里只负责接线。
    /// </summary>
    public class LevelSession : MonoBehaviour
    {
        [SerializeField] private LevelAsset _levelAsset;
        [SerializeField] private GameplayConfig _gameplayConfig;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private StepPlayer _stepPlayer;

        private PlaySession _session;
        private InputCommandScheduler _commandScheduler;

        /// <summary>当前对局，关卡加载失败时为 null。</summary>
        public PlaySession Session => _session;

        public string LevelDisplayName => _levelAsset != null ? _levelAsset.Level.DisplayName : string.Empty;

        /// <summary>玩家当前所在世界的名称。</summary>
        public string CurrentWorldName => _session != null ? _session.Rules.Map.GetWorldName(_session.State.PlayerWorld) : string.Empty;

        /// <summary>步数、状态或通关情况发生变化。</summary>
        public event Action StateChanged;

        /// <summary>按了传送但无法进入，参数为原因。面前没有传送箱子时不触发。</summary>
        public event Action<StepFailure> TeleportFailed;

        private void Awake()
        {
            bool hasReferences = CheckReference(_levelAsset, nameof(_levelAsset))
                & CheckReference(_gameplayConfig, nameof(_gameplayConfig))
                & CheckReference(_inputReader, nameof(_inputReader))
                & CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_stepPlayer, nameof(_stepPlayer));
            if (!hasReferences)
            {
                enabled = false;
                return;
            }

            if (!ValidateLevel())
            {
                enabled = false;
                return;
            }

            try
            {
                _session = new PlaySession(new LevelRules(new LevelMap(_levelAsset.Level)));
            }
            catch (ArgumentException exception)
            {
                // 关卡数据非法：报出具体原因并停用本组件，不带着坏数据继续运行
                Debug.LogError($"[{nameof(LevelSession)}] 关卡 {_levelAsset.name} 加载失败：{exception.Message}", this);
                enabled = false;
                return;
            }

            _commandScheduler = new InputCommandScheduler(_gameplayConfig.HoldRepeatDelay);
        }

        private void OnEnable()
        {
            if (_session == null)
            {
                return;
            }

            _inputReader.DirectionPressed += HandleDirectionPressed;
            _inputReader.DirectionReleased += HandleDirectionReleased;
            _inputReader.TeleportRequested += HandleTeleportRequested;
            _inputReader.UndoRequested += HandleUndoRequested;
            _inputReader.RestartRequested += HandleRestartRequested;
            _session.StepApplied += HandleStepApplied;
            _session.StateRestored += HandleStateRestored;
        }

        private void Start()
        {
            if (_session == null)
            {
                return;
            }

            _levelView.Build(_session.Rules.Map, _session.State);
            _levelView.UpdateFaceHints(_session.Rules, _session.State);
            StateChanged?.Invoke();
        }

        private void Update()
        {
            // 按住连续移动依赖时间，必须每帧询问；这里只做几次比较，不分配内存
            if (!_commandScheduler.TryGetNextCommand(Time.time, _stepPlayer.IsPlaying, out InputCommand command))
            {
                return;
            }

            if (command.Type == InputCommandType.Teleport)
            {
                _session.TryTeleport();
            }
            else
            {
                _session.TryMove(command.Direction);
            }
        }

        private void OnDisable()
        {
            if (_session == null)
            {
                return;
            }

            _inputReader.DirectionPressed -= HandleDirectionPressed;
            _inputReader.DirectionReleased -= HandleDirectionReleased;
            _inputReader.TeleportRequested -= HandleTeleportRequested;
            _inputReader.UndoRequested -= HandleUndoRequested;
            _inputReader.RestartRequested -= HandleRestartRequested;
            _session.StepApplied -= HandleStepApplied;
            _session.StateRestored -= HandleStateRestored;

            // 退订后收不到松开事件，清空按键状态以免重新启用时方向键“卡住”
            _commandScheduler.Reset();
        }

        /// <summary>撤销一步，打断正在播放的动画。供 HUD 按钮和按键调用。</summary>
        public void RequestUndo()
        {
            if (_session == null)
            {
                return;
            }

            InterruptPlayback();
            _session.Undo();
        }

        /// <summary>重开，打断正在播放的动画。供 HUD 按钮和按键调用。</summary>
        public void RequestRestart()
        {
            if (_session == null)
            {
                return;
            }

            InterruptPlayback();
            _session.Restart();
        }

        /// <summary>指定世界当前是否处于冲突，供 HUD 显示。</summary>
        public bool IsWorldConflicted(int world)
        {
            return _session != null && _session.Rules.IsWorldConflicted(_session.State, world);
        }

        private void InterruptPlayback()
        {
            // 旧的缓存命令是基于撤销前的局面按下的，不应在新局面上执行
            _stepPlayer.CompleteImmediately();
            _commandScheduler.ClearBuffer();
        }

        private void HandleDirectionPressed(Direction direction)
        {
            _commandScheduler.Press(direction, Time.time);
        }

        private void HandleDirectionReleased(Direction direction)
        {
            _commandScheduler.Release(direction);
        }

        private void HandleTeleportRequested()
        {
            _commandScheduler.PressTeleport();
        }

        private void HandleUndoRequested()
        {
            RequestUndo();
        }

        private void HandleRestartRequested()
        {
            RequestRestart();
        }

        private void HandleStepApplied(StepResult result)
        {
            _stepPlayer.Play(result, _session.Rules);

            if (IsTeleportRejection(result.Failure))
            {
                TeleportFailed?.Invoke(result.Failure);
            }

            // 顶着墙按住时每帧都会尝试移动但什么都没变，此时不通知 UI，避免每帧刷新文本
            if (result.IsStepTaken || result.Events.Count > 0)
            {
                StateChanged?.Invoke();
            }
        }

        private void HandleStateRestored()
        {
            _levelView.Sync(_session.State);
            _levelView.UpdateFaceHints(_session.Rules, _session.State);
            StateChanged?.Invoke();
        }

        /// <summary>
        /// 加载前完整校验：有错误时列出全部问题并拒绝加载；只有警告时记录后照常加载。
        /// </summary>
        private bool ValidateLevel()
        {
            LevelValidationReport report = LevelValidator.Validate(_levelAsset.Level);
            IReadOnlyList<LevelIssue> issues = report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                string message = $"[{nameof(LevelSession)}] 关卡 {_levelAsset.name}：{issues[i].Message}";
                if (issues[i].IsError)
                {
                    Debug.LogError(message, _levelAsset);
                }
                else
                {
                    Debug.LogWarning(message, _levelAsset);
                }
            }

            return !report.HasErrors;
        }

        private static bool IsTeleportRejection(StepFailure failure)
        {
            return failure == StepFailure.WorldConflicted
                || failure == StepFailure.ExitBlocked
                || failure == StepFailure.PedestalOccupied;
        }

        private bool CheckReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(LevelSession)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
