using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 求解时判断两个状态是否“同一局面”：目标箱子彼此没有区别，只要占据的格子集合相同就算相同。
    /// <see cref="LevelState"/> 自己的相等比较按编号逐个比（表现层要知道哪个箱子动了），直接用会把交换箱子的局面重复搜索。
    /// </summary>
    internal sealed class UnorderedBoxComparer : IEqualityComparer<LevelState>
    {
        public static readonly UnorderedBoxComparer Instance = new UnorderedBoxComparer();

        public bool Equals(LevelState a, LevelState b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null
                || a.PlayerWorld != b.PlayerWorld
                || a.PlayerPosition != b.PlayerPosition
                || a.PlayerFacing != b.PlayerFacing)
            {
                return false;
            }

            IReadOnlyList<PortalBoxState> portalsA = a.PortalBoxes;
            IReadOnlyList<PortalBoxState> portalsB = b.PortalBoxes;
            if (portalsA.Count != portalsB.Count)
            {
                return false;
            }

            for (int i = 0; i < portalsA.Count; i++)
            {
                if (!portalsA[i].Equals(portalsB[i]))
                {
                    return false;
                }
            }

            // 箱子不会重叠，所以 a 的每个箱子都能在 b 里找到，两个集合就相同
            IReadOnlyList<GridPos> boxesA = a.MappedBoxes;
            IReadOnlyList<GridPos> boxesB = b.MappedBoxes;
            if (boxesA.Count != boxesB.Count)
            {
                return false;
            }

            for (int i = 0; i < boxesA.Count; i++)
            {
                if (b.FindMappedBoxAt(boxesA[i]) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(LevelState state)
        {
            unchecked
            {
                int hash = state.PlayerWorld;
                hash = (hash * 397) ^ state.PlayerPosition.GetHashCode();
                hash = (hash * 397) ^ (int)state.PlayerFacing;

                IReadOnlyList<PortalBoxState> portals = state.PortalBoxes;
                for (int i = 0; i < portals.Count; i++)
                {
                    hash = (hash * 397) ^ portals[i].GetHashCode();
                }

                // 箱子部分与顺序无关：各自打散后相加
                int boxHash = 0;
                IReadOnlyList<GridPos> boxes = state.MappedBoxes;
                for (int i = 0; i < boxes.Count; i++)
                {
                    int h = boxes[i].GetHashCode() * -1640531535;
                    boxHash += h ^ (h >> 15);
                }

                return (hash * 397) ^ boxHash;
            }
        }
    }
}
