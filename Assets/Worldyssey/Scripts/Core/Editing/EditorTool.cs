namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡编辑器的画笔。顺序即工具栏中的按钮顺序。
    /// </summary>
    public enum EditorTool
    {
        /// <summary>把共享层地形改为空地。</summary>
        Floor = 0,
        SharedWall = 1,
        Pedestal = 2,
        Goal = 3,

        /// <summary>当前世界的独有墙。</summary>
        UniqueWall = 4,

        MappedBox = 5,

        /// <summary>放置玩家，同时把起始世界设为当前世界；点在玩家身上则顺时针转向。</summary>
        Player = 6,

        /// <summary>放置所选配对在当前世界的一侧。</summary>
        PortalBox = 7,

        /// <summary>清除这一格在当前世界的实体和独有墙，地形保留。右键总是橡皮擦。</summary>
        Eraser = 8,
    }
}
