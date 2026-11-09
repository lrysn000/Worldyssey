namespace Worldyssey.Core
{
    /// <summary>
    /// <see cref="LevelSolver"/> 的结果。
    /// </summary>
    public sealed class SolveResult
    {
        internal SolveResult(SolveStatus status, int exploredStates)
        {
            Status = status;
            ExploredStates = exploredStates;
        }

        public SolveStatus Status { get; }

        /// <summary>确认可解。</summary>
        public bool IsSolvable => Status == SolveStatus.Solvable;

        /// <summary>确认无解。超过上限或取消时为 false，因为那只是没有结论。</summary>
        public bool IsUnsolvable => Status == SolveStatus.Unsolvable;

        /// <summary>搜索过的不同局面数，反映关卡状态空间的大小。</summary>
        public int ExploredStates { get; }
    }
}
