namespace Worldyssey.Core
{
    /// <summary>
    /// 目标箱子被推动了一格。所有世界中的这个箱子同步移动。
    /// </summary>
    public sealed class MappedBoxPushedEvent : StepEvent
    {
        public MappedBoxPushedEvent(int boxIndex, GridPos from, GridPos to)
        {
            BoxIndex = boxIndex;
            From = from;
            To = to;
        }

        /// <summary>箱子编号，对应 <see cref="LevelState.MappedBoxes"/> 的下标。</summary>
        public int BoxIndex { get; }
        public GridPos From { get; }
        public GridPos To { get; }
    }
}
