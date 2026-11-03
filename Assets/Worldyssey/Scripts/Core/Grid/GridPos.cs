using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 网格坐标。(0, 0) 在左下角，x 向右，y 向上。
    /// </summary>
    /// <remarks>
    /// 字段名不加下划线，是因为它们直接决定关卡 JSON 的键名（"x"、"y"）。
    /// 不用 readonly struct，是因为 Unity 序列化无法写入 readonly 字段。
    /// </remarks>
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        [SerializeField] private int x;
        [SerializeField] private int y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int X => x;
        public int Y => y;

        public static GridPos operator +(GridPos a, GridPos b)
        {
            return new GridPos(a.x + b.x, a.y + b.y);
        }

        public static GridPos operator +(GridPos pos, Direction direction)
        {
            return pos + direction.ToOffset();
        }

        public static bool operator ==(GridPos a, GridPos b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GridPos a, GridPos b)
        {
            return !a.Equals(b);
        }

        public bool Equals(GridPos other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}
