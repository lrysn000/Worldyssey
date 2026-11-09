using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// <see cref="LevelSolver"/> 的搜索过程。一个“局面”= 当前世界 + 目标箱子位置 + 传送箱子状态 + 玩家所在的连通区域。
    /// </summary>
    /// <remarks>
    /// 为什么可以这样合并：
    /// - 朝向：推箱和传送前都需要面朝箱子，而面朝箱子按方向键只转身、不计步、一定成功，所以朝向不影响能做什么。
    /// - 玩家位置：箱子不动时，玩家在同一片空地里可以任意走动，所以区域内任意一格都等价，用区域里编号最小的格子代表。
    /// 剪枝：目标箱子推进两侧都是共享墙（或地图边缘）的非终点墙角就再也推不出来；共享墙每个世界都有，所以与当前世界无关。
    /// </remarks>
    internal sealed class SolvabilitySearch
    {
        // 每搜索这么多个局面回调一次进度
        private const int ProgressInterval = 5000;

        private static readonly Direction[] Directions = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        private readonly LevelRules _rules;
        private readonly LevelMap _map;
        private readonly bool[] _deadCells;
        private readonly bool[] _reached;
        private readonly List<GridPos> _region = new List<GridPos>();
        private readonly Queue<GridPos> _frontier = new Queue<GridPos>();

        public SolvabilitySearch(LevelRules rules)
        {
            _rules = rules;
            _map = rules.Map;
            _deadCells = FindDeadCells(_map);
            _reached = new bool[_map.Width * _map.Height];
        }

        public SolveResult Run(int maxStates, Func<int, bool> progress)
        {
            LevelState start = Normalize(_rules.CreateInitialState());
            var visited = new HashSet<LevelState>(UnorderedBoxComparer.Instance) { start };

            // 广度优先：实测比深度优先少搜很多局面（深度优先容易在一个分支里把箱子推得很远再回头）
            var pending = new Queue<LevelState>();
            pending.Enqueue(start);
            int expanded = 0;

            while (pending.Count > 0)
            {
                LevelState state = pending.Dequeue();
                if (_rules.IsSolved(state))
                {
                    return new SolveResult(SolveStatus.Solvable, visited.Count);
                }

                // 第一次展开就回调一次，调用方能尽早取消；之后每隔一段回调
                expanded++;
                bool isProgressDue = expanded == 1 || expanded % ProgressInterval == 0;
                if (progress != null && isProgressDue && !progress(visited.Count))
                {
                    return new SolveResult(SolveStatus.Cancelled, visited.Count);
                }

                if (visited.Count > maxStates)
                {
                    return new SolveResult(SolveStatus.LimitReached, visited.Count);
                }

                Expand(state, visited, pending);
            }

            return new SolveResult(SolveStatus.Unsolvable, visited.Count);
        }

        /// <summary>
        /// 从玩家能走到的每一格出发，尝试推旁边的箱子，以及经由旁边底座上的传送箱子传送。
        /// </summary>
        private void Expand(LevelState state, HashSet<LevelState> visited, Queue<LevelState> pending)
        {
            int world = state.PlayerWorld;
            CollectRegion(state);

            // 先复制一份：下面加入新局面时会重新计算区域，覆盖 _region
            GridPos[] region = _region.ToArray();
            for (int i = 0; i < region.Length; i++)
            {
                GridPos cell = region[i];
                for (int d = 0; d < Directions.Length; d++)
                {
                    Direction direction = Directions[d];
                    GridPos ahead = cell + direction;
                    int portalIndex = state.FindPortalBoxAt(world, ahead);
                    if (portalIndex < 0 && state.FindMappedBoxAt(ahead) < 0)
                    {
                        continue;
                    }

                    // 站在这一格、面朝箱子：之后按方向键是推，按传送键是传送
                    var standing = new LevelState(world, cell, direction, state.MappedBoxes, state.PortalBoxes);
                    TryAdd(_rules.TryMove(standing, direction), visited, pending);
                    if (portalIndex >= 0 && _map.IsPedestal(ahead))
                    {
                        TryAdd(_rules.TryTeleport(standing), visited, pending);
                    }
                }
            }
        }

        private void TryAdd(StepResult result, HashSet<LevelState> visited, Queue<LevelState> pending)
        {
            if (!result.IsStepTaken || HasDeadBox(result.State))
            {
                return;
            }

            LevelState next = Normalize(result.State);
            if (visited.Add(next))
            {
                pending.Enqueue(next);
            }
        }

        /// <summary>把玩家放到所在区域编号最小的格子、朝向固定，使同一局面只有一种表示。</summary>
        private LevelState Normalize(LevelState state)
        {
            CollectRegion(state);
            GridPos representative = state.PlayerPosition;
            int best = ToIndex(representative);
            for (int i = 0; i < _region.Count; i++)
            {
                int index = ToIndex(_region[i]);
                if (index < best)
                {
                    best = index;
                    representative = _region[i];
                }
            }

            return new LevelState(state.PlayerWorld, representative, Direction.Up, state.MappedBoxes, state.PortalBoxes);
        }

        /// <summary>玩家在当前世界不推箱能走到的所有格子，结果放在 _region。</summary>
        private void CollectRegion(LevelState state)
        {
            Array.Clear(_reached, 0, _reached.Length);
            _region.Clear();
            _frontier.Clear();

            GridPos start = state.PlayerPosition;
            _reached[ToIndex(start)] = true;
            _frontier.Enqueue(start);
            while (_frontier.Count > 0)
            {
                GridPos cell = _frontier.Dequeue();
                _region.Add(cell);
                for (int d = 0; d < Directions.Length; d++)
                {
                    GridPos next = cell + Directions[d];
                    if (IsWalkable(state, next) && !_reached[ToIndex(next)])
                    {
                        _reached[ToIndex(next)] = true;
                        _frontier.Enqueue(next);
                    }
                }
            }
        }

        /// <summary>与 LevelRules.TryMove 的判断一致：地形、任何目标箱子、当前世界的传送箱子都会挡路。</summary>
        private bool IsWalkable(LevelState state, GridPos pos)
        {
            int world = state.PlayerWorld;
            return _map.IsInBounds(pos)
                && !_map.IsSharedWall(pos)
                && !_map.IsUniqueWall(world, pos)
                && state.FindMappedBoxAt(pos) < 0
                && state.FindPortalBoxAt(world, pos) < 0;
        }

        private bool HasDeadBox(LevelState state)
        {
            var boxes = state.MappedBoxes;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (_map.IsInBounds(boxes[i]) && _deadCells[ToIndex(boxes[i])])
                {
                    return true;
                }
            }

            return false;
        }

        private int ToIndex(GridPos pos)
        {
            return pos.Y * _map.Width + pos.X;
        }

        /// <summary>
        /// 不是终点、且横向和纵向各有一侧是共享墙或地图边缘的格子。目标箱子进去后两个方向都推不动。
        /// </summary>
        private static bool[] FindDeadCells(LevelMap map)
        {
            var dead = new bool[map.Width * map.Height];
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    if (map.IsGoal(pos) || map.IsSharedWall(pos))
                    {
                        continue;
                    }

                    bool isVerticalBlocked = IsSolid(map, pos + Direction.Up) || IsSolid(map, pos + Direction.Down);
                    bool isHorizontalBlocked = IsSolid(map, pos + Direction.Left) || IsSolid(map, pos + Direction.Right);
                    dead[y * map.Width + x] = isVerticalBlocked && isHorizontalBlocked;
                }
            }

            return dead;
        }

        private static bool IsSolid(LevelMap map, GridPos pos)
        {
            return !map.IsInBounds(pos) || map.IsSharedWall(pos);
        }
    }
}
