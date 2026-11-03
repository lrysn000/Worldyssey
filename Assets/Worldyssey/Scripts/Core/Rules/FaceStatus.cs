namespace Worldyssey.Core
{
    /// <summary>
    /// 底座上传送箱子某一面能否进入。表现层据此在箱子对应面上显示提示。
    /// </summary>
    public enum FaceStatus
    {
        /// <summary>可以从这一面传送。</summary>
        Enterable = 0,

        /// <summary>目标世界里这一面外侧的落点被挡，只影响这一面。</summary>
        ExitBlocked = 1,

        /// <summary>目标世界的这个底座上停着别的传送箱子，四面都不能进入。</summary>
        PedestalOccupied = 2,

        /// <summary>目标世界处于冲突，四面都不能进入。</summary>
        WorldConflicted = 3,
    }
}
