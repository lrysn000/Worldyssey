using Worldyssey.Core;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 教学和卡关提示用到的局面查询：冲突掩码、墙角死箱、锁住某个世界的箱子。
    /// </summary>
    public static class TutorialQueries
    {
        /// <summary>按世界编号排列的冲突位掩码，第 n 位为 1 表示世界 n 处于冲突。</summary>
        public static int GetConflictMask(LevelRules rules, LevelState state)
        {
            int mask = 0;
            int worldCount = rules.Map.WorldCount;
            for (int world = 0; world < worldCount; world++)
            {
                if (rules.IsWorldConflicted(state, world))
                {
                    mask |= 1 << world;
                }
            }

            return mask;
        }

        /// <summary>
        /// 箱子是否卡在共享墙的墙角且不在终点上。只看共享墙：独有墙随世界变化，换个世界也许还能推。
        /// </summary>
        public static bool IsCorneredOffGoal(LevelMap map, GridPos box)
        {
            if (map.IsGoal(box))
            {
                return false;
            }

            bool isVerticalBlocked = IsSolid(map, box + Direction.Up) || IsSolid(map, box + Direction.Down);
            bool isHorizontalBlocked = IsSolid(map, box + Direction.Left) || IsSolid(map, box + Direction.Right);
            return isVerticalBlocked && isHorizontalBlocked;
        }

        /// <summary>
        /// 玩家面前的传送箱子通往哪个世界，面前没有传送箱子时返回 -1。
        /// </summary>
        public static int GetFacedPortalWorld(LevelRules rules, LevelState state)
        {
            int portal = state.FindPortalBoxAt(state.PlayerWorld, state.PlayerPosition + state.PlayerFacing);
            return portal >= 0 ? rules.Map.GetPortalWorld(LevelMap.GetPartnerIndex(portal)) : -1;
        }

        /// <summary>
        /// 找出让 <paramref name="world"/> 冲突的目标箱子编号，没有时返回 -1。
        /// </summary>
        public static int FindConflictingBox(LevelRules rules, LevelState state, int world)
        {
            for (int i = 0; i < state.MappedBoxes.Count; i++)
            {
                GridPos box = state.MappedBoxes[i];
                if (rules.Map.IsUniqueWall(world, box) || state.FindPortalBoxAt(world, box) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsSolid(LevelMap map, GridPos pos)
        {
            return !map.IsInBounds(pos) || map.IsSharedWall(pos);
        }
    }
}
