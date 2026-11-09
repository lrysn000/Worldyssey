using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡可解性检查：判断从开局能否通关，不求最少步数。
    /// 推箱和传送都直接调用 <see cref="LevelRules"/>，和游戏用同一套规则，规则改了检查结果自动跟着变。
    /// </summary>
    /// <remarks>
    /// 只关心能不能解，所以局面可以合并得很粗（见 <see cref="SolvabilitySearch"/>）：
    /// 不记朝向（转身免费），不记玩家站在哪一格、只记他所在的连通区域，只搜索推箱和传送这两类会改变局面的操作。
    /// 比逐步搜索少一到两个数量级的局面。
    /// </remarks>
    public static class LevelSolver
    {
        /// <summary>编辑器里使用的默认局面数上限，超过后返回 <see cref="SolveStatus.LimitReached"/>。</summary>
        public const int DefaultMaxStates = 1000000;

        /// <summary>
        /// 检查关卡是否可解。<paramref name="progress"/> 定期收到已搜索的局面数，返回 false 表示取消。
        /// </summary>
        public static SolveResult Solve(LevelRules rules, int maxStates = DefaultMaxStates, Func<int, bool> progress = null)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return new SolvabilitySearch(rules).Run(maxStates, progress);
        }
    }
}
