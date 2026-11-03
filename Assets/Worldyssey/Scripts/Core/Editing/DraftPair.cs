namespace Worldyssey.Core
{
    /// <summary>
    /// 编辑中的一对传送箱子所连接的两个世界。配对编号即它在列表中的下标。
    /// </summary>
    public readonly struct DraftPair
    {
        public DraftPair(int worldA, int worldB)
        {
            WorldA = worldA;
            WorldB = worldB;
        }

        public int WorldA { get; }
        public int WorldB { get; }

        public bool Contains(int world)
        {
            return WorldA == world || WorldB == world;
        }

        /// <summary>该侧（位于 <paramref name="world"/>）指向的世界。</summary>
        public int OtherWorld(int world)
        {
            return world == WorldA ? WorldB : WorldA;
        }
    }
}
