namespace Worldyssey.Data
{
    /// <summary>
    /// 教学箭头指向的对象类型。
    /// </summary>
    /// <remarks>值会被序列化到资源里，新增类型只能加在末尾，不能调整已有的数值。</remarks>
    public enum TutorialTargetKind
    {
        None = 0,
        Player = 1,
        /// <summary>目标箱子，Index 为箱子编号。</summary>
        MappedBox = 2,
        /// <summary>传送箱子，Index 为 配对编号×2+侧（A 侧 0，B 侧 1）。</summary>
        PortalBox = 3,
        /// <summary>终点，Index 为关卡里第几个终点。</summary>
        Goal = 4,
        /// <summary>任意格子，用 Cell 坐标（左下角为原点，与关卡数据一致）。</summary>
        Cell = 5,
        /// <summary>顶部世界面板里的图标，Index 为世界编号。图标没显示时不画箭头。</summary>
        WorldIcon = 6,
    }
}
