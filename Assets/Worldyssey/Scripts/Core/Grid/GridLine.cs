using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 编辑器画直线用的格子序列：从起点沿横向或纵向（取位移较大的方向）走到终点所在的行或列，
    /// 不画斜线——推箱子关卡里的墙和地面几乎都是横平竖直的。
    /// </summary>
    public static class GridLine
    {
        /// <summary>
        /// 清空 <paramref name="result"/> 并填入从 <paramref name="start"/> 出发、锁定到主方向的直线格子（含两端）。
        /// 横纵位移相等时取横向。
        /// </summary>
        public static void Collect(GridPos start, GridPos end, List<GridPos> result)
        {
            result.Clear();
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            bool isHorizontal = System.Math.Abs(dx) >= System.Math.Abs(dy);
            int length = isHorizontal ? System.Math.Abs(dx) : System.Math.Abs(dy);
            int step = (isHorizontal ? dx : dy) >= 0 ? 1 : -1;
            for (int i = 0; i <= length; i++)
            {
                result.Add(isHorizontal ? new GridPos(start.X + i * step, start.Y) : new GridPos(start.X, start.Y + i * step));
            }
        }

        /// <summary>直线终点（锁定方向后），用于界面显示长度。</summary>
        public static GridPos LockedEnd(GridPos start, GridPos end)
        {
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            return System.Math.Abs(dx) >= System.Math.Abs(dy) ? new GridPos(end.X, start.Y) : new GridPos(start.X, end.Y);
        }
    }
}
