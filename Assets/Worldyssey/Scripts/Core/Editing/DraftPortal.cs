namespace Worldyssey.Core
{
    /// <summary>
    /// 编辑中已摆放的一个传送箱子。
    /// </summary>
    public readonly struct DraftPortal
    {
        public DraftPortal(int pair, int world, GridPos position)
        {
            Pair = pair;
            World = world;
            Position = position;
        }

        public int Pair { get; }
        public int World { get; }
        public GridPos Position { get; }
    }
}
