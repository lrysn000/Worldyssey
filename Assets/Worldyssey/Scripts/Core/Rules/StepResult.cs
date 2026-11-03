using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一步操作的结果。
    /// </summary>
    /// <remarks>
    /// 失败时 <see cref="State"/> 仍可能与输入不同：撞墙时玩家会转向。
    /// 调用方应始终采用 <see cref="State"/>，但只在 <see cref="IsStepTaken"/> 为 true 时压入撤销快照。
    /// </remarks>
    public sealed class StepResult
    {
        private StepResult(LevelState state, bool isStepTaken, StepFailure failure, bool isLevelSolved, IReadOnlyList<StepEvent> events)
        {
            State = state;
            IsStepTaken = isStepTaken;
            Failure = failure;
            IsLevelSolved = isLevelSolved;
            Events = events;
        }

        /// <summary>操作后的状态。</summary>
        public LevelState State { get; }

        /// <summary>是否成功执行并计为一步。</summary>
        public bool IsStepTaken { get; }

        public StepFailure Failure { get; }

        /// <summary>这一步之后关卡是否完成。</summary>
        public bool IsLevelSolved { get; }

        /// <summary>按播放顺序排列的事件。</summary>
        public IReadOnlyList<StepEvent> Events { get; }

        internal static StepResult Succeeded(LevelState state, bool isLevelSolved, List<StepEvent> events)
        {
            return new StepResult(state, true, StepFailure.None, isLevelSolved, events.ToArray());
        }

        internal static StepResult Failed(LevelState state, StepFailure failure, List<StepEvent> events)
        {
            if (failure == StepFailure.None)
            {
                throw new ArgumentException("失败结果必须带有失败原因", nameof(failure));
            }

            return new StepResult(state, false, failure, false, events.ToArray());
        }
    }
}
