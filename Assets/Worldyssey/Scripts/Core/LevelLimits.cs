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

        public const int MinWidth = 3;
        public const int MinHeight = 3;

        // 尺寸上限按固定相机能完整看清、编辑器网格能放下来定
        public const int MaxWidth = 20;
        public const int MaxHeight = 14;

        /// <summary>
        /// 世界数量是否在允许范围内。
        /// </summary>
        public static bool IsValidWorldCount(int worldCount)
        {
            return worldCount >= MinWorldCount && worldCount <= MaxWorldCount;
        }

        /// <summary>
        /// 关卡尺寸是否在允许范围内。
        /// </summary>
        public static bool IsValidSize(int width, int height)
        {
            return width >= MinWidth && width <= MaxWidth && height >= MinHeight && height <= MaxHeight;
        }
    }
}
