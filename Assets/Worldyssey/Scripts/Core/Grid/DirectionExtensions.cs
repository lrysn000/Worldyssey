using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// <see cref="Direction"/> 与坐标偏移之间的转换。
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>
        /// 该方向移动一格对应的坐标偏移。
        /// </summary>
        public static GridPos ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new GridPos(0, 1);
                case Direction.Right: return new GridPos(1, 0);
                case Direction.Down: return new GridPos(0, -1);
                case Direction.Left: return new GridPos(-1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, "未知方向");
            }
        }

        /// <summary>
        /// 相反方向。
        /// </summary>
        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Right: return Direction.Left;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, "未知方向");
            }
        }
    }
}
