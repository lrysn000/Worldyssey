namespace Worldyssey.Core
{
    /// <summary>
    /// 一步操作失败的原因。表现层据此显示不同提示。
    /// </summary>
    public enum StepFailure
    {
        None = 0,

        /// <summary>前方是墙或地图边界。</summary>
        Blocked = 1,

        /// <summary>前方的箱子无法被推动。</summary>
        BoxBlocked = 2,

        /// <summary>按传送时面前没有停在底座上的本世界传送箱子。</summary>
        NoPortal = 3,

        /// <summary>目标世界处于冲突。</summary>
        WorldConflicted = 4,

        /// <summary>目标世界里的落点被挡。</summary>
        ExitBlocked = 5,

        /// <summary>目标世界的这个底座上停着别的传送箱子。</summary>
        PedestalOccupied = 6,

        /// <summary>前方是箱子但玩家没有面朝它：这一下只转向，下一下才推。</summary>
        TurnedToFaceBox = 7,
    }
}
