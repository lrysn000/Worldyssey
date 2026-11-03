using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 规则入口。只持有不变的 <see cref="LevelMap"/>，不持有游戏状态：输入状态和操作，输出新状态和事件。
    /// 同一套规则既给游戏流程用，也给以后的求解器用。
    /// </summary>
    /// <remarks>
    /// 核心约定（需求文档第 3.3 节）：所有阻挡都只看玩家所在的当前世界；
    /// 目标箱子在别的世界与该世界的独有墙或传送箱子重叠时，那个世界处于冲突，不能传送进入。
    /// </remarks>
    public sealed class LevelRules
    {
        private static readonly Direction[] AllDirections = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

        private readonly LevelMap _map;

        public LevelRules(LevelMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public LevelMap Map => _map;

        /// <summary>四个方向，按 Up、Right、Down、Left 排列。</summary>
        public static IReadOnlyList<Direction> Directions => AllDirections;

        /// <summary>
        /// 关卡初始状态，用于开局和重开。
        /// </summary>
        public LevelState CreateInitialState()
        {
            return new LevelState(_map.StartWorld, _map.PlayerStart, _map.PlayerStartFacing, _map.MappedBoxStarts, _map.PortalBoxStarts);
        }

        /// <summary>
        /// 尝试向某个方向移动或推箱。不会修改传入的 <paramref name="state"/>。
        /// </summary>
        public StepResult TryMove(LevelState state, Direction direction)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var events = new List<StepEvent>();
            LevelState next = state.Clone();
            int world = state.PlayerWorld;

            // 无论能否移动都先转向：被挡时玩家也要面朝那个方向，这是传送时“面朝底座”的前提
            if (state.PlayerFacing != direction)
            {
                next.SetPlayerFacing(direction);
                events.Add(new PlayerTurnedEvent(state.PlayerFacing, direction));
            }

            GridPos from = state.PlayerPosition;
            GridPos target = from + direction;

            if (IsTerrainBlocked(world, target))
            {
                return StepResult.Failed(next, StepFailure.Blocked, events);
            }

            GridPos boxTarget = target + direction;
            int mappedIndex = state.FindMappedBoxAt(target);
            int portalIndex = state.FindPortalBoxAt(world, target);
            if (mappedIndex >= 0 || portalIndex >= 0)
            {
                // 推箱前必须已经面朝它：没面朝时这一下只转向，给玩家先对准方向的机会（例如正对底座上的传送箱子再按传送）
                if (state.PlayerFacing != direction)
                {
                    return StepResult.Failed(next, StepFailure.TurnedToFaceBox, events);
                }

                if (IsBlocked(state, world, boxTarget))
                {
                    return StepResult.Failed(next, StepFailure.BoxBlocked, events);
                }

                if (mappedIndex >= 0)
                {
                    next.SetMappedBox(mappedIndex, boxTarget);
                    events.Add(new MappedBoxPushedEvent(mappedIndex, target, boxTarget));
                }
                else
                {
                    next.SetPortalBox(portalIndex, state.PortalBoxes[portalIndex].PlacedAt(boxTarget));
                    events.Add(new PortalBoxPushedEvent(portalIndex, target, boxTarget));
                }
            }

            next.SetPlayerPosition(target);
            events.Add(new PlayerMovedEvent(from, target));

            bool isSolved = IsSolved(next);
            if (isSolved)
            {
                events.Add(new LevelSolvedEvent());
            }

            return StepResult.Succeeded(next, isSolved, events);
        }

        /// <summary>
        /// 尝试通过面前底座上的传送箱子传送。不会修改传入的 <paramref name="state"/>。
        /// </summary>
        public StepResult TryTeleport(LevelState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var events = new List<StepEvent>();
            GridPos pedestal = state.PlayerPosition + state.PlayerFacing;
            int portalIndex = _map.IsPedestal(pedestal) ? state.FindPortalBoxAt(state.PlayerWorld, pedestal) : -1;
            if (portalIndex < 0)
            {
                return StepResult.Failed(state.Clone(), StepFailure.NoPortal, events);
            }

            FaceStatus status = EvaluateEntry(state, portalIndex, state.PlayerPosition);
            if (status != FaceStatus.Enterable)
            {
                return StepResult.Failed(state.Clone(), ToFailure(status), events);
            }

            int partnerIndex = LevelMap.GetPartnerIndex(portalIndex);
            int targetWorld = _map.GetPortalWorld(partnerIndex);
            LevelState next = state.Clone();

            next.SetPlayerWorld(targetWorld);
            events.Add(new TeleportedEvent(state.PlayerWorld, targetWorld));

            // 配对规则：伙伴未出现则生成，在别处则瞬移过来，已在底座上则不变；因此每对在每个世界最多一个
            PortalBoxState partner = state.PortalBoxes[partnerIndex];
            if (!partner.IsPresent)
            {
                next.SetPortalBox(partnerIndex, partner.PlacedAt(pedestal));
                events.Add(new PartnerSpawnedEvent(partnerIndex, pedestal));
            }
            else if (partner.Position != pedestal)
            {
                next.SetPortalBox(partnerIndex, partner.PlacedAt(pedestal));
                events.Add(new PartnerMovedEvent(partnerIndex, partner.Position, pedestal));
            }

            bool isSolved = IsSolved(next);
            if (isSolved)
            {
                events.Add(new LevelSolvedEvent());
            }

            return StepResult.Succeeded(next, isSolved, events);
        }

        /// <summary>
        /// 该传送箱子是否已出现并停在底座上。只有这样的箱子才有面提示、才能传送。
        /// </summary>
        public bool IsPortalOnPedestal(LevelState state, int portalIndex)
        {
            PortalBoxState box = state.PortalBoxes[portalIndex];
            return box.IsPresent && _map.IsPedestal(box.Position);
        }

        /// <summary>
        /// 从 <paramref name="face"/> 一侧（玩家站在底座的这一侧、面朝底座）能否经由该传送箱子进入目标世界。
        /// 箱子必须停在底座上。
        /// </summary>
        public FaceStatus GetFaceStatus(LevelState state, int portalIndex, Direction face)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!IsPortalOnPedestal(state, portalIndex))
            {
                throw new InvalidOperationException($"传送箱子 {portalIndex} 不在底座上，没有面状态");
            }

            GridPos standPos = state.PortalBoxes[portalIndex].Position + face;
            return EvaluateEntry(state, portalIndex, standPos);
        }

        /// <summary>
        /// 指定世界是否处于冲突：有目标箱子与该世界的独有墙或已出现的传送箱子在同一格。
        /// </summary>
        public bool IsWorldConflicted(LevelState state, int world)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            IReadOnlyList<GridPos> boxes = state.MappedBoxes;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (_map.IsUniqueWall(world, boxes[i]) || state.FindPortalBoxAt(world, boxes[i]) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 每个终点上都有目标箱子时关卡完成。传送箱子和冲突都不影响判定。
        /// </summary>
        public bool IsSolved(LevelState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            IReadOnlyList<GridPos> goals = _map.GoalPositions;

            // 没有终点的关卡视为未完成，避免数据出错时开局即通关；正常关卡由校验器保证至少一个终点
            if (goals.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < goals.Count; i++)
            {
                if (state.FindMappedBoxAt(goals[i]) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 站在 <paramref name="standPos"/>、经由 <paramref name="portalIndex"/> 传送时目标世界能否进入。
        /// 检查顺序决定提示：冲突和底座被占影响四个面，所以排在只影响一面的出口被挡之前。
        /// </summary>
        private FaceStatus EvaluateEntry(LevelState state, int portalIndex, GridPos standPos)
        {
            int partnerIndex = LevelMap.GetPartnerIndex(portalIndex);
            int targetWorld = _map.GetPortalWorld(partnerIndex);
            GridPos pedestal = state.PortalBoxes[portalIndex].Position;

            if (IsWorldConflicted(state, targetWorld))
            {
                return FaceStatus.WorldConflicted;
            }

            int occupant = state.FindPortalBoxAt(targetWorld, pedestal);
            if (occupant >= 0 && occupant != partnerIndex)
            {
                return FaceStatus.PedestalOccupied;
            }

            // 伙伴若正停在落点上，传送时它会先瞬移到底座，所以不算挡住
            int portalAtLanding = state.FindPortalBoxAt(targetWorld, standPos);
            bool isLandingBlocked = IsTerrainBlocked(targetWorld, standPos)
                || state.FindMappedBoxAt(standPos) >= 0
                || (portalAtLanding >= 0 && portalAtLanding != partnerIndex);
            return isLandingBlocked ? FaceStatus.ExitBlocked : FaceStatus.Enterable;
        }

        private static StepFailure ToFailure(FaceStatus status)
        {
            switch (status)
            {
                case FaceStatus.ExitBlocked: return StepFailure.ExitBlocked;
                case FaceStatus.PedestalOccupied: return StepFailure.PedestalOccupied;
                case FaceStatus.WorldConflicted: return StepFailure.WorldConflicted;
                default: throw new ArgumentOutOfRangeException(nameof(status), status, "可进入状态没有对应的失败原因");
            }
        }

        /// <summary>
        /// 地形阻挡：越界、共享墙、该世界的独有墙。
        /// </summary>
        private bool IsTerrainBlocked(int world, GridPos pos)
        {
            return !_map.IsInBounds(pos) || _map.IsSharedWall(pos) || _map.IsUniqueWall(world, pos);
        }

        /// <summary>
        /// 当前世界阻挡：地形、任何目标箱子、该世界已出现的传送箱子。
        /// </summary>
        private bool IsBlocked(LevelState state, int world, GridPos pos)
        {
            return IsTerrainBlocked(world, pos)
                || state.FindMappedBoxAt(pos) >= 0
                || state.FindPortalBoxAt(world, pos) >= 0;
        }
    }
}
