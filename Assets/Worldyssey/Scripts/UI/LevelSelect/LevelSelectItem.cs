using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关列表中一项的显示数据，由流程层根据关卡目录和存档组装。
    /// </summary>
    public readonly struct LevelSelectItem
    {
        public const int NoBestSteps = -1;

        public LevelSelectItem(LevelData level, bool isLocked, int bestSteps, bool isNext)
        {
            Level = level;
            IsLocked = isLocked;
            BestSteps = bestSteps;
            IsNext = isNext;
        }

        /// <summary>完整关卡数据，详情栏的小地图和季节图标用到。</summary>
        public LevelData Level { get; }

        public string DisplayName => Level != null ? Level.DisplayName : string.Empty;
        public bool IsLocked { get; }

        /// <summary>最佳步数，未通关为 <see cref="NoBestSteps"/>。</summary>
        public int BestSteps { get; }

        public bool IsCleared => BestSteps != NoBestSteps;

        /// <summary>内置关卡中第一个已解锁但未通关的关卡，玩家下一步该玩的那一关。</summary>
        public bool IsNext { get; }

        /// <summary>
        /// 第 <paramref name="world"/> 个世界的季节。未填写主题的旧关卡按世界编号回退，与游戏内显示一致。
        /// </summary>
        public WorldTheme GetWorldTheme(int world)
        {
            WorldTheme theme = WorldThemes.TryParse(Level.Worlds[world].ThemeText, out WorldTheme parsed) ? parsed : WorldTheme.Unset;
            return WorldThemes.Resolve(theme, world);
        }
    }
}
