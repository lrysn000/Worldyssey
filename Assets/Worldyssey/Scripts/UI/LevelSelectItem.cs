namespace Worldyssey.UI
{
    /// <summary>
    /// 选关列表中一项的显示数据，由流程层根据关卡目录和存档组装。
    /// </summary>
    public readonly struct LevelSelectItem
    {
        public const int NoBestSteps = -1;

        public LevelSelectItem(string displayName, bool isLocked, int bestSteps)
        {
            DisplayName = displayName;
            IsLocked = isLocked;
            BestSteps = bestSteps;
        }

        public string DisplayName { get; }
        public bool IsLocked { get; }

        /// <summary>最佳步数，未通关为 <see cref="NoBestSteps"/>。</summary>
        public int BestSteps { get; }
    }
}
