namespace Worldyssey.Core
{
    /// <summary>
    /// 可解性检查的结论。
    /// </summary>
    public enum SolveStatus
    {
        /// <summary>找到了一种通关方法。</summary>
        Solvable = 0,

        /// <summary>搜完了所有可达局面，关卡无解。</summary>
        Unsolvable = 1,

        /// <summary>局面数超过上限，没有结论。</summary>
        LimitReached = 2,

        /// <summary>调用方中途取消。</summary>
        Cancelled = 3,
    }
}
