using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一次校验的全部结果。
    /// </summary>
    public sealed class LevelValidationReport
    {
        private readonly List<LevelIssue> _issues;

        internal LevelValidationReport(List<LevelIssue> issues)
        {
            _issues = issues;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].IsError)
                {
                    ErrorCount++;
                }
                else
                {
                    WarningCount++;
                }
            }
        }

        public IReadOnlyList<LevelIssue> Issues => _issues;
        public int ErrorCount { get; }
        public int WarningCount { get; }

        /// <summary>有错误的关卡不能保存为可游玩状态，也不能加载。</summary>
        public bool HasErrors => ErrorCount > 0;
    }
}
