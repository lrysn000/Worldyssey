namespace Worldyssey.Core
{
    /// <summary>
    /// 玩家在当前世界内移动了一格。
    /// </summary>
    public sealed class PlayerMovedEvent : StepEvent
    {
        public PlayerMovedEvent(GridPos from, GridPos to)
        {
            From = from;
            To = to;
        }

        public GridPos From { get; }
        public GridPos To { get; }
    }
}
