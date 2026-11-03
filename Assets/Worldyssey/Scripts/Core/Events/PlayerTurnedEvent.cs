namespace Worldyssey.Core
{
    /// <summary>
    /// 玩家改变了朝向。
    /// </summary>
    public sealed class PlayerTurnedEvent : StepEvent
    {
        public PlayerTurnedEvent(Direction from, Direction to)
        {
            From = from;
            To = to;
        }

        public Direction From { get; }
        public Direction To { get; }
    }
}
