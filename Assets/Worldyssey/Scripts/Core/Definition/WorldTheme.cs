namespace Worldyssey.Core
{
    /// <summary>
    /// 世界的主题（季节）。每个世界一个，同一关卡内不能重复。
    /// 数值同时是季节资源（季节模块组、调色板、UI 主题、环境）的下标，顺序不能改。
    /// </summary>
    public enum WorldTheme
    {
        /// <summary>旧关卡数据里没有主题字段，按世界编号决定（0 春、1 夏……）。</summary>
        Unset = -1,
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3,
    }
}
