namespace Worldyssey.Core
{
    /// <summary>
    /// 编辑操作的结果：需要刷新多少界面，以及是否要给出提示。
    /// </summary>
    public enum EditorFeedback
    {
        /// <summary>没有变化。</summary>
        None = 0,

        /// <summary>只有网格内容变化（绘制格子）。</summary>
        GridChanged = 1,

        /// <summary>结构或选择变化，需要刷新全部控件。</summary>
        AllChanged = 2,

        /// <summary>放传送箱子前没有选中配对。</summary>
        SelectPairFirst = 3,

        /// <summary>当前世界不属于所选配对，或该格已有传送箱子。</summary>
        PortalRejected = 4,

        /// <summary>新增配对需要两个不同的世界。</summary>
        PairRejected = 5,

        NothingToUndo = 6,
    }
}
