namespace Worldyssey.Core
{
    /// <summary>
    /// 当前世界的一个传送箱子被推动了一格。
    /// </summary>
    public sealed class PortalBoxPushedEvent : StepEvent
    {
        public PortalBoxPushedEvent(int portalIndex, GridPos from, GridPos to)
        {
            PortalIndex = portalIndex;
            From = from;
            To = to;
        }

        /// <summary>对应 <see cref="LevelState.PortalBoxes"/> 的下标。</summary>
        public int PortalIndex { get; }
        public GridPos From { get; }
        public GridPos To { get; }
    }
}
