using System.Linq;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// 传送、配对与面状态（需求文档 3.5、3.6，以及第 7 节 M4 测试清单）。
    /// 基础布局：玩家在 (1,1) 面朝右，底座在 (2,1)，世界 0 的配对 0（A 侧）停在底座上，指向世界 1。
    /// </summary>
    public class LevelRulesTeleportTests
    {
        private static readonly int[][] OnePair01 = { new[] { 0, 1 } };
        private static readonly int[][] TwoPairs01 = { new[] { 0, 1 }, new[] { 0, 1 } };
        private static readonly string[] BasicShared = { "#######", "#@P   #", "#######" };
        private static readonly string[] PortalOnPedestal = { "       ", "  a    ", "       " };

        [Test]
        public void Teleport_PartnerAbsent_SpawnsOnPedestal()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(BasicShared, new[] { PortalOnPedestal, null }, OnePair01);
            LevelState start = rules.CreateInitialState();
            Assert.IsFalse(start.PortalBoxes[1].IsPresent);

            StepResult result = rules.TryTeleport(start);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(1, result.State.PlayerWorld);
            Assert.AreEqual(new GridPos(1, 1), result.State.PlayerPosition);
            Assert.AreEqual(Direction.Left, result.State.PlayerFacing, "传送后背对底座");
            Assert.IsTrue(result.State.PortalBoxes[1].IsPresent);
            Assert.AreEqual(new GridPos(2, 1), result.State.PortalBoxes[1].Position);
            Assert.AreEqual(start.PortalBoxes[0], result.State.PortalBoxes[0]);

            var teleported = (TeleportedEvent)result.Events[0];
            Assert.AreEqual(0, teleported.FromWorld);
            Assert.AreEqual(1, teleported.ToWorld);
            var spawned = (PartnerSpawnedEvent)result.Events[1];
            Assert.AreEqual(1, spawned.PortalIndex);
            Assert.AreEqual(new GridPos(2, 1), spawned.Position);
        }

        [Test]
        public void Teleport_RoundTrip_ReturnsToSameCellWithoutPartnerEvent()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(BasicShared, new[] { PortalOnPedestal, null }, OnePair01);
            LevelState start = rules.CreateInitialState();

            LevelState there = rules.TryTeleport(start).State;
            Assert.AreEqual(StepFailure.NoPortal, rules.TryTeleport(there).Failure, "背对底座时不能直接传回去");

            // 朝箱子按方向键只转身，不会把伙伴箱子推下底座
            StepResult turn = rules.TryMove(there, Direction.Right);
            Assert.AreEqual(StepFailure.TurnedToFaceBox, turn.Failure);
            Assert.AreEqual(new GridPos(2, 1), turn.State.PortalBoxes[1].Position);

            StepResult back = rules.TryTeleport(turn.State);

            Assert.IsTrue(back.IsStepTaken);
            Assert.AreEqual(0, back.State.PlayerWorld);
            Assert.AreEqual(new GridPos(1, 1), back.State.PlayerPosition);
            Assert.AreEqual(Direction.Left, back.State.PlayerFacing);
            Assert.AreEqual(1, back.Events.Count, "伙伴已在底座上，只有传送事件");
            Assert.IsInstanceOf<TeleportedEvent>(back.Events[0]);
        }

        [Test]
        public void Teleport_PartnerElsewhere_MovesToPedestalAndVacatesOldCell()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", "     a ", "       " } },
                OnePair01);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.IsTrue(result.IsStepTaken);
            var moved = (PartnerMovedEvent)result.Events[1];
            Assert.AreEqual(1, moved.PortalIndex);
            Assert.AreEqual(new GridPos(5, 1), moved.From);
            Assert.AreEqual(new GridPos(2, 1), moved.To);
            Assert.AreEqual(-1, result.State.FindPortalBoxAt(1, new GridPos(5, 1)));
            Assert.AreEqual(1, result.State.FindPortalBoxAt(1, new GridPos(2, 1)));
        }

        [Test]
        public void Teleport_PartnerOnLandingCell_IsAllowed()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", " a     ", "       " } },
                OnePair01);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(new GridPos(1, 1), result.State.PlayerPosition);
            Assert.AreEqual(new GridPos(2, 1), result.State.PortalBoxes[1].Position);
        }

        [Test]
        public void NotFacingPedestal_FailsWithNoPortal()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(BasicShared, new[] { PortalOnPedestal, null }, OnePair01, Direction.Up);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.NoPortal, result.Failure);
        }

        [Test]
        public void EmptyPedestal_FailsWithNoPortal()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(BasicShared, new string[][] { null, null }, OnePair01);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.AreEqual(StepFailure.NoPortal, result.Failure);
        }

        [Test]
        public void PortalBoxNotOnPedestal_FailsWithNoPortal()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#######", "#@    #", "#######" },
                new[] { PortalOnPedestal, null },
                OnePair01);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.AreEqual(StepFailure.NoPortal, result.Failure);
        }

        [Test]
        public void TargetWorldConflicted_FailsUntilConflictIsResolved()
        {
            // 目标箱子 (4,2) 压着世界 1 的独有墙，世界 1 一开始就处于冲突
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#######", "#@P $ #", "#     #", "#######" },
                new[] { new[] { "       ", "  a    ", "       ", "       " }, new[] { "       ", "    x  ", "       ", "       " } },
                OnePair01);
            LevelState start = rules.CreateInitialState();

            StepResult blocked = rules.TryTeleport(start);
            Assert.IsFalse(blocked.IsStepTaken);
            Assert.AreEqual(StepFailure.WorldConflicted, blocked.Failure);
            Assert.AreEqual(start, blocked.State);

            // 在世界 0 把箱子推开（先转向对准箱子再推），再绕到底座下方面朝上
            LevelState resolved = TestLevels.Walk(rules, start,
                Direction.Down, Direction.Right, Direction.Right, Direction.Up, Direction.Right, Direction.Right,
                Direction.Down, Direction.Left, Direction.Left, Direction.Up);
            Assert.AreEqual(new GridPos(5, 2), resolved.MappedBoxes[0]);
            Assert.AreEqual(new GridPos(2, 1), resolved.PlayerPosition);
            Assert.AreEqual(Direction.Up, resolved.PlayerFacing);
            Assert.IsFalse(rules.IsWorldConflicted(resolved, 1));

            StepResult result = rules.TryTeleport(resolved);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(1, result.State.PlayerWorld);
        }

        [Test]
        public void LandingBlockedByTargetUniqueWall_FailsWithExitBlocked()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", " x     ", "       " } },
                OnePair01);
            LevelState start = rules.CreateInitialState();

            StepResult result = rules.TryTeleport(start);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.ExitBlocked, result.Failure);
            Assert.AreEqual(start, result.State);
        }

        [Test]
        public void LandingBlockedByOtherPortalBox_FailsWithExitBlocked()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", " b     ", "       " } },
                TwoPairs01);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.AreEqual(StepFailure.ExitBlocked, result.Failure);
        }

        [Test]
        public void TargetPedestalHoldsOtherPortalBox_FailsWithPedestalOccupied()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", "  b    ", "       " } },
                TwoPairs01);

            StepResult result = rules.TryTeleport(rules.CreateInitialState());

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.PedestalOccupied, result.Failure);
        }

        [Test]
        public void FaceStatus_ReportsEachFaceIndependently()
        {
            // 世界 1 在 (1,1) 有独有墙：左面出口被挡；右面空地可进入；上下两面外侧是共享墙
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", " x     ", "       " } },
                OnePair01);
            LevelState state = rules.CreateInitialState();

            Assert.AreEqual(FaceStatus.ExitBlocked, rules.GetFaceStatus(state, 0, Direction.Left));
            Assert.AreEqual(FaceStatus.Enterable, rules.GetFaceStatus(state, 0, Direction.Right));
            Assert.AreEqual(FaceStatus.ExitBlocked, rules.GetFaceStatus(state, 0, Direction.Up));
            Assert.AreEqual(FaceStatus.ExitBlocked, rules.GetFaceStatus(state, 0, Direction.Down));
        }

        [Test]
        public void FaceStatus_PedestalOccupied_AffectsAllFaces()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", "  b    ", "       " } },
                TwoPairs01);
            LevelState state = rules.CreateInitialState();

            foreach (Direction face in LevelRules.Directions)
            {
                Assert.AreEqual(FaceStatus.PedestalOccupied, rules.GetFaceStatus(state, 0, face));
            }
        }

        [Test]
        public void FaceStatus_UpdatesAfterPushingMappedBoxInCurrentWorld()
        {
            // 底座 (3,3)，世界 1 在 (5,2) 有独有墙。在世界 0 把箱子从 (2,2) 往右推：
            // 推到 (3,2) 挡住下面的出口；推到 (4,2) 又空出来；推到 (5,2) 压上世界 1 的墙，四面都冲突
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "########", "#  P   #", "#@$    #", "#      #", "########" },
                new[]
                {
                    new[] { "        ", "   a    ", "        ", "        ", "        " },
                    new[] { "        ", "        ", "     x  ", "        ", "        " },
                },
                OnePair01);
            LevelState state = rules.CreateInitialState();
            Assert.AreEqual(FaceStatus.Enterable, rules.GetFaceStatus(state, 0, Direction.Down));

            state = rules.TryMove(state, Direction.Right).State;
            Assert.AreEqual(FaceStatus.ExitBlocked, rules.GetFaceStatus(state, 0, Direction.Down));
            Assert.AreEqual(FaceStatus.Enterable, rules.GetFaceStatus(state, 0, Direction.Left));

            state = rules.TryMove(state, Direction.Right).State;
            Assert.AreEqual(FaceStatus.Enterable, rules.GetFaceStatus(state, 0, Direction.Down));

            state = rules.TryMove(state, Direction.Right).State;
            Assert.IsTrue(rules.IsWorldConflicted(state, 1));
            foreach (Direction face in LevelRules.Directions)
            {
                Assert.AreEqual(FaceStatus.WorldConflicted, rules.GetFaceStatus(state, 0, face));
            }
        }

        [Test]
        public void GetFaceStatus_PortalNotOnPedestal_Throws()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#######", "#@    #", "#######" },
                new[] { PortalOnPedestal, null },
                OnePair01);

            Assert.Throws<System.InvalidOperationException>(() => rules.GetFaceStatus(rules.CreateInitialState(), 0, Direction.Left));
        }

        [Test]
        public void Solved_IgnoresPortalBoxOnGoalAndConflictedWorlds()
        {
            // 世界 1 的传送箱子停在终点上；在世界 0 把目标箱子推上去，世界 1 进入冲突，但关卡照样完成
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@$. #", "######" },
                new[] { null, new[] { "      ", "   a  ", "      " } },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsLevelSolved);
            Assert.IsTrue(rules.IsWorldConflicted(result.State, 1));
            Assert.IsInstanceOf<LevelSolvedEvent>(result.Events.Last());
        }

        [Test]
        public void UndoTeleport_RestoresWorldPositionAndPartner()
        {
            var session = new PlaySession(TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", "     a ", "       " } },
                OnePair01));
            LevelState before = session.State;

            Assert.IsTrue(session.TryTeleport());
            Assert.AreEqual(1, session.StepCount);
            Assert.AreEqual(1, session.State.PlayerWorld);

            Assert.IsTrue(session.Undo());

            Assert.AreEqual(before, session.State);
            Assert.AreEqual(0, session.State.PlayerWorld);
            Assert.AreEqual(new GridPos(5, 1), session.State.PortalBoxes[1].Position);
            Assert.AreEqual(0, session.StepCount);
        }

        [Test]
        public void UndoTeleport_RestoresAbsentPartner()
        {
            var session = new PlaySession(TestLevels.RulesMultiWorld(BasicShared, new[] { PortalOnPedestal, null }, OnePair01));

            session.TryTeleport();
            session.Undo();

            Assert.IsFalse(session.State.PortalBoxes[1].IsPresent);
        }

        [Test]
        public void FailedTeleport_DoesNotCountStep()
        {
            var session = new PlaySession(TestLevels.RulesMultiWorld(
                BasicShared,
                new[] { PortalOnPedestal, new[] { "       ", " x     ", "       " } },
                OnePair01));
            StepResult received = null;
            session.StepApplied += result => received = result;

            Assert.IsFalse(session.TryTeleport());

            Assert.AreEqual(0, session.StepCount);
            Assert.AreEqual(StepFailure.ExitBlocked, received.Failure);
        }

        [Test]
        public void AfterSolved_TeleportIsLocked()
        {
            var session = new PlaySession(TestLevels.RulesMultiWorld(
                new[] { "#######", "# P@$.#", "#######" },
                new[] { new[] { "       ", "  a    ", "       " }, null },
                OnePair01));

            Assert.IsTrue(session.TryMove(Direction.Right));
            Assert.IsTrue(session.IsSolved);
            session.TryMove(Direction.Left);

            Assert.IsFalse(session.TryTeleport());
        }

        [Test]
        public void PortalBoxOnPedestal_SideApproach_TurnsThenTeleportsFromThatSide()
        {
            // 底座在玩家上方、玩家朝右：按上只转向，不会把传送箱子推走，随后可以从下方进入
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#######", "#  P  #", "#  @  #", "#######" },
                new[] { new[] { "       ", "   a   ", "       ", "       " }, null },
                OnePair01,
                Direction.Right);

            StepResult turn = rules.TryMove(rules.CreateInitialState(), Direction.Up);

            Assert.IsFalse(turn.IsStepTaken);
            Assert.AreEqual(StepFailure.TurnedToFaceBox, turn.Failure);
            Assert.AreEqual(new GridPos(3, 2), turn.State.PortalBoxes[0].Position, "传送箱子没有被推走");

            StepResult teleport = rules.TryTeleport(turn.State);

            Assert.IsTrue(teleport.IsStepTaken);
            Assert.AreEqual(1, teleport.State.PlayerWorld);
            Assert.AreEqual(new GridPos(3, 1), teleport.State.PlayerPosition);
        }

        [Test]
        public void PortalBoxOffPedestal_AlsoNeedsFacingBeforePush()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#######", "#@    #", "#######" },
                new[] { new[] { "       ", "  a    ", "       " }, null },
                OnePair01,
                Direction.Down);

            StepResult first = rules.TryMove(rules.CreateInitialState(), Direction.Right);
            StepResult second = rules.TryMove(first.State, Direction.Right);

            Assert.AreEqual(StepFailure.TurnedToFaceBox, first.Failure);
            Assert.IsTrue(second.IsStepTaken);
            Assert.AreEqual(new GridPos(3, 1), second.State.PortalBoxes[0].Position);
        }
    }
}
