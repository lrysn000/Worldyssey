namespace Worldyssey.Core
{
    /// <summary>
    /// 传送时，目标世界中原本未出现的伙伴箱子在底座上生成。
    /// </summary>
    public sealed class PartnerSpawnedEvent : StepEvent
    {
        public PartnerSpawnedEvent(int portalIndex, GridPos position)
        {
            PortalIndex = portalIndex;
            Position = position;
        }

        /// <summary>对应 <see cref="LevelState.PortalBoxes"/> 的下标。</summary>
        public int PortalIndex { get; }
        public GridPos Position { get; }
    }
}
