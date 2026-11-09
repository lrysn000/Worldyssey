using System.Collections.Generic;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关列表的统计与默认选中规则。
    /// </summary>
    public static class LevelSelectSummary
    {
        /// <summary>
        /// 默认选中：下一个该玩的关卡；没有（全部通关或自建列表）时选最后一个已解锁的；列表为空时返回 -1。
        /// </summary>
        public static int FindDefaultSelection(IReadOnlyList<LevelSelectItem> items)
        {
            int lastUnlocked = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsNext)
                {
                    return i;
                }

                if (!items[i].IsLocked)
                {
                    lastUnlocked = i;
                }
            }

            return lastUnlocked >= 0 ? lastUnlocked : items.Count > 0 ? 0 : -1;
        }

        public static int CountCleared(IReadOnlyList<LevelSelectItem> items)
        {
            int cleared = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsCleared)
                {
                    cleared++;
                }
            }

            return cleared;
        }
    }
}
