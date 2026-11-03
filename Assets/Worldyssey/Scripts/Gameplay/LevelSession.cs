using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.View;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 场景中的一局游戏：运行时加载关卡，创建 <see cref="PlaySession"/>，把输入命令交给它，再把结果交给表现层。
    /// 流程规则在 <see cref="PlaySession"/>，输入时序在 <see cref="InputCommandScheduler"/>，界面流程在 GameFlow，这里只负责接线。
    /// </summary>
    public class LevelSession : MonoBehaviour
    {
        [Tooltip("可选：启动时自动加载的关卡，用于 Sandbox 等开发场景。由 GameFlow 控制流程时留空")]
        [FormerlySerializedAs("_levelAsset")]
        [SerializeField] private LevelAsset _autoStartLevel;

        [SerializeField] private GameplayConfig _gameplayConfig;
        [SerializeField] private PlayerInputReader _inputReader;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private StepPlayer _stepPlayer;

        private InputCommandScheduler _commandScheduler;
        private PlaySession _session;
        private LevelData _currentLevel;
        private bool _isInputEnabled;
        private bool _isCompletionPending;
        private bool _hasReferences;

        /// <summary>当前对局，没有加载关卡时为 null。</summary>
        public PlaySession Session => _session;

        public bool HasLevel => _session != null;

        public string LevelDisplayName => _currentLevel != null ? _currentLevel.DisplayName : string.Empty;

        /// <summary>玩家当前所在世界的名称。</summary>
        public string CurrentWorldName => _session != null ? _session.Rules.Map.GetWorldName(_session.State.PlayerWorld) : string.Empty;

        /// <summary>步数、状态或通关情况发生变化，或者关卡被加载、结束。</summary>
        public event Action StateChanged;

        /// <summary>按了传送但无法进入，参数为原因。面前没有传送箱子时不触发。</summary>
        public event Action<StepFailure> TeleportFailed;

        /// <summary>关卡完成且最后一步的动画已播完，参数为通关步数。</summary>
        public event Action<int> LevelCompleted;

        private void Awake()
        {
            _hasReferences = CheckReference(_gameplayConfig, nameof(_gameplayConfig))
                & CheckReference(_inputReader, nameof(_inputReader))
                & CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_stepPlayer, nameof(_stepPlayer));
            if (!_hasReferences)
            {
                enabled = false;
                return;
            }

            _commandScheduler = new InputCommandScheduler(_gameplayConfig.HoldRepeatDelay);
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _inputReader.DirectionPressed += HandleDirectionPressed;
            _inputReader.DirectionReleased += HandleDirectionReleased;
            _inputReader.TeleportRequested += HandleTeleportRequested;
            _inputReader.UndoRequested += HandleUndoRequested;
            _inputReader.RestartRequested += HandleRestartRequested;
            SubscribeSession();
        }

        private void Start()
        {
            if (_autoStartLevel != null)
            {
                StartLevel(_autoStartLevel.Level);
            }
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            // 等最后一步的动画播完再通知通关，结算界面不会盖住还在移动的箱子
            if (_isCompletionPending)
            {
                if (!_stepPlayer.IsPlaying)
                {
                    _isCompletionPending = false;
                    LevelCompleted?.Invoke(_session.StepCount);
                }

                return;
            }

            if (!CanAcceptCommands())
            {
                return;
            }

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
            if (!_hasReferences)
            {
                return;
            }

            _inputReader.DirectionPressed -= HandleDirectionPressed;
            _inputReader.DirectionReleased -= HandleDirectionReleased;
            _inputReader.TeleportRequested -= HandleTeleportRequested;
            _inputReader.UndoRequested -= HandleUndoRequested;
            _inputReader.RestartRequested -= HandleRestartRequested;
            UnsubscribeSession();

            // 退订后收不到松开事件，清空按键状态以免重新启用时方向键“卡住”
            _commandScheduler.Reset();
        }

        /// <summary>
        /// 加载并开始一个关卡，替换当前关卡。校验有错误时列出全部问题、不加载并返回 false。
        /// 开始后游戏输入处于启用状态。
        /// </summary>
        public bool StartLevel(LevelData level)
        {
            if (!_hasReferences)
            {
                return false;
            }

            EndLevel();
            if (level == null)
            {
                Debug.LogError($"[{nameof(LevelSession)}] StartLevel 传入了 null", this);
                return false;
            }

            if (!ValidateLevel(level))
            {
                return false;
            }

            try
            {
                _session = new PlaySession(new LevelRules(new LevelMap(level)));
            }
            catch (ArgumentException exception)
            {
                // 校验通过却解析失败说明校验器与 LevelMap 规则不一致，必须暴露出来
                Debug.LogError($"[{nameof(LevelSession)}] 关卡 '{level.Id}' 加载失败：{exception.Message}", this);
                return false;
            }

            _currentLevel = level;
            SubscribeSession();
            _levelView.Build(_session.Rules.Map, _session.State);
            _levelView.UpdateFaceHints(_session.Rules, _session.State);
            _isInputEnabled = true;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 结束当前关卡并清除场景中的模型。没有关卡时什么都不做。
        /// </summary>
        public void EndLevel()
        {
            if (_session == null)
            {
                return;
            }

            _stepPlayer.CompleteImmediately();
            UnsubscribeSession();
            _session = null;
            _currentLevel = null;
            _isCompletionPending = false;
            _isInputEnabled = false;
            _commandScheduler.Reset();
            _levelView.Clear();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// 开关游戏输入（移动、传送、撤销、重开按键）。暂停、结算等界面显示时关闭。
        /// </summary>
        public void SetInputEnabled(bool isEnabled)
        {
            _isInputEnabled = isEnabled;
            if (!isEnabled)
            {
                _commandScheduler.Reset();
            }
        }

        /// <summary>撤销一步，打断正在播放的动画。通关后或输入关闭时无效。供 HUD 按钮和按键调用。</summary>
        public void RequestUndo()
        {
            if (!CanAcceptCommands())
            {
                return;
            }

            InterruptPlayback();
            _session.Undo();
        }

        /// <summary>重开，打断正在播放的动画。输入关闭时无效。供 HUD 按钮和按键调用。</summary>
        /// <remarks>
        /// 通关后仍允许按键重开：正式流程里结算界面会关闭输入，不受影响；
        /// 而没有 GameFlow 的开发场景（如 Sandbox）通关后需要靠它重来。
        /// </remarks>
        public void RequestRestart()
        {
            if (_session == null || !_isInputEnabled)
            {
                return;
            }

            RestartLevel();
        }

        /// <summary>
        /// 无条件回到初始状态，供暂停菜单和结算界面的“重玩”使用。
        /// </summary>
        public void RestartLevel()
        {
            if (_session == null)
            {
                return;
            }

            InterruptPlayback();
            _isCompletionPending = false;
            _session.Restart();
        }

        /// <summary>指定世界当前是否处于冲突，供 HUD 显示。</summary>
        public bool IsWorldConflicted(int world)
        {
            return _session != null && _session.Rules.IsWorldConflicted(_session.State, world);
        }

        private bool CanAcceptCommands()
        {
            return _session != null && _isInputEnabled && !_session.IsSolved;
        }

        private void InterruptPlayback()
        {
            // 旧的缓存命令是基于撤销前的局面按下的，不应在新局面上执行
            _stepPlayer.CompleteImmediately();
            _commandScheduler.ClearBuffer();
        }

        private void SubscribeSession()
        {
            if (_session == null)
            {
                return;
            }

            // 先退订再订阅，避免 OnEnable 与 StartLevel 先后调用时重复订阅
            UnsubscribeSession();
            _session.StepApplied += HandleStepApplied;
            _session.StateRestored += HandleStateRestored;
        }

        private void UnsubscribeSession()
        {
            if (_session == null)
            {
                return;
            }

            _session.StepApplied -= HandleStepApplied;
            _session.StateRestored -= HandleStateRestored;
        }

        private void HandleDirectionPressed(Direction direction)
        {
            if (CanAcceptCommands())
            {
                _commandScheduler.Press(direction, Time.time);
            }
        }

        private void HandleDirectionReleased(Direction direction)
        {
            _commandScheduler.Release(direction);
        }

        private void HandleTeleportRequested()
        {
            if (CanAcceptCommands())
            {
                _commandScheduler.PressTeleport();
            }
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

            if (result.IsLevelSolved)
            {
                // 通关后锁定全部游戏输入，等动画播完在 Update 里发出 LevelCompleted
                _isCompletionPending = true;
                _commandScheduler.Reset();
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
        private bool ValidateLevel(LevelData level)
        {
            LevelValidationReport report = LevelValidator.Validate(level);
            IReadOnlyList<LevelIssue> issues = report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                string message = $"[{nameof(LevelSession)}] 关卡 '{level.Id}'：{issues[i].Message}";
                if (issues[i].IsError)
                {
                    Debug.LogError(message, this);
                }
                else
                {
                    Debug.LogWarning(message, this);
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
