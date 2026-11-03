namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡结构上的硬性限制。放在 Core 层，是为了让关卡校验在不加载场景的情况下也能使用。
    /// </summary>
    public static class LevelLimits
    {
        public const int MinWorldCount = 1;

        // 上限来自设计决定：再多世界玩家难以在脑中追踪映射关系，选关和编辑器界面也按 4 个来排版
        public const int MaxWorldCount = 4;

        /// <summary>
        /// 世界数量是否在允许范围内。
        /// </summary>
        public static bool IsValidWorldCount(int worldCount)
        {
            return worldCount >= MinWorldCount && worldCount <= MaxWorldCount;
        }
    }
}
