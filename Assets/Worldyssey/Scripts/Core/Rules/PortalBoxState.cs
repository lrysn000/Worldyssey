using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一个传送箱子（配对的一侧）的状态。每对有 A、B 两侧，在 <see cref="LevelState.PortalBoxes"/> 中
    /// 固定排在下标 配对编号*2（A 侧）和 配对编号*2+1（B 侧），全程不变。
    /// </summary>
    public readonly struct PortalBoxState : IEquatable<PortalBoxState>
    {
        public PortalBoxState(int pairIndex, int world, bool isPresent, GridPos position)
        {
            PairIndex = pairIndex;
            World = world;
            IsPresent = isPresent;

            // 未出现时坐标无意义，统一置零，保证相等比较只看有意义的字段
            Position = isPresent ? position : default;
        }

        /// <summary>所属配对在关卡配对列表中的下标。</summary>
        public int PairIndex { get; }

        /// <summary>所在世界。这一侧永远只存在于这个世界。</summary>
        public int World { get; }

        public bool IsPresent { get; }
        public GridPos Position { get; }

        /// <summary>
        /// 返回出现在 <paramref name="position"/> 的同一侧。
        /// </summary>
        public PortalBoxState PlacedAt(GridPos position)
        {
            return new PortalBoxState(PairIndex, World, true, position);
        }

        public bool Equals(PortalBoxState other)
        {
            return PairIndex == other.PairIndex
                && World == other.World
                && IsPresent == other.IsPresent
                && Position == other.Position;
        }

        public override bool Equals(object obj)
        {
            return obj is PortalBoxState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PairIndex;
                hash = (hash * 397) ^ World;
                hash = (hash * 397) ^ (IsPresent ? 1 : 0);
                hash = (hash * 397) ^ Position.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return IsPresent ? $"Pair{PairIndex}@W{World}{Position}" : $"Pair{PairIndex}@W{World}(absent)";
        }
    }
}
