namespace Worldyssey.Core
{
    /// <summary>
    /// 传送时，目标世界中已出现在别处的伙伴箱子瞬移到底座上，原位置空出。
    /// </summary>
    public sealed class PartnerMovedEvent : StepEvent
    {
        public PartnerMovedEvent(int portalIndex, GridPos from, GridPos to)
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
