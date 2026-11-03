using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一局游戏的流程状态：当前状态、撤销历史、是否通关。
    /// 放在 Core 层而不是 MonoBehaviour 里，是为了让“成功一步才压快照”“通关后锁定移动”这类流程规则也能被单元测试覆盖。
    /// </summary>
    public sealed class PlaySession
    {
        private readonly LevelRules _rules;
        private readonly UndoHistory _history = new UndoHistory();

        public PlaySession(LevelRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            State = _rules.CreateInitialState();
            IsSolved = _rules.IsSolved(State);
        }

        public LevelRules Rules => _rules;
        public LevelState State { get; private set; }

        /// <summary>当前步数，即撤销历史长度。</summary>
        public int StepCount => _history.Count;

        /// <summary>当前状态是否已通关。通关后不能再移动，但可以撤销和重开。</summary>
        public bool IsSolved { get; private set; }

        /// <summary>
        /// 执行了一次移动尝试，包括只转向的失败尝试，表现层据此播放转向或失败反馈。
        /// </summary>
        public event Action<StepResult> StepApplied;

        /// <summary>
        /// 状态被整体替换（撤销或重开），表现层应直接同步到 <see cref="State"/>。
        /// </summary>
        public event Action StateRestored;

        /// <summary>
        /// 尝试移动。已通关时忽略并返回 false；否则返回这一步是否计步。
        /// </summary>
        public bool TryMove(Direction direction)
        {
            if (IsSolved)
            {
                return false;
            }

            return Apply(_rules.TryMove(State, direction));
        }

        /// <summary>
        /// 尝试通过面前的传送箱子传送。已通关时忽略并返回 false；否则返回是否传送成功。
        /// 失败时同样触发 <see cref="StepApplied"/>，表现层据此提示原因。
        /// </summary>
        public bool TryTeleport()
        {
            if (IsSolved)
            {
                return false;
            }

            return Apply(_rules.TryTeleport(State));
        }

        /// <summary>
        /// 撤销一步。没有可撤销的步骤时返回 false。
        /// </summary>
        public bool Undo()
        {
            if (!_history.TryUndo(out LevelState previous))
            {
                return false;
            }

            State = previous;
            IsSolved = _rules.IsSolved(State);
            StateRestored?.Invoke();
            return true;
        }

        /// <summary>
        /// 回到初始状态并清空历史。重开本身不能撤销。
        /// </summary>
        public void Restart()
        {
            _history.Clear();
            State = _rules.CreateInitialState();
            IsSolved = _rules.IsSolved(State);
            StateRestored?.Invoke();
        }

        private bool Apply(StepResult result)
        {
            if (result.IsStepTaken)
            {
                _history.Push(State);
            }

            // 失败时也要采用新状态：玩家可能已经转向
            State = result.State;
            IsSolved = result.IsLevelSolved;
            StepApplied?.Invoke(result);
            return result.IsStepTaken;
        }
    }
}
