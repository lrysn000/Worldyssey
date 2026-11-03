namespace Worldyssey.Core
{
    /// <summary>
    /// 玩家从一个世界传送到另一个世界，坐标和朝向不变。总是传送这一步的第一个事件，
    /// 之后的伙伴事件都发生在目标世界。
    /// </summary>
    public sealed class TeleportedEvent : StepEvent
    {
        public TeleportedEvent(int fromWorld, int toWorld)
        {
            FromWorld = fromWorld;
            ToWorld = toWorld;
        }

        public int FromWorld { get; }
        public int ToWorld { get; }
    }
}
