using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// 多世界下的阻挡、推传送箱子和冲突判定（需求文档 3.3、3.4）。
    /// </summary>
    public class LevelRulesWorldTests
    {
        private static readonly int[][] OnePair01 = { new[] { 0, 1 } };

        [Test]
        public void UniqueWall_BlocksInItsOwnWorld()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#####", "#@  #", "#####" },
                new[] { new[] { "     ", "  x  ", "     " }, null },
                new int[0][]);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.Blocked, result.Failure);
        }

        [Test]
        public void UniqueWall_DoesNotBlockOtherWorlds()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#####", "#@  #", "#####" },
                new[] { new[] { "     ", "  x  ", "     " }, null },
                new int[0][],
                startWorld: 1);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
        }

        [Test]
        public void PushPortalBox_MovesItAndRaisesEvent()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@   #", "######" },
                new[] { new[] { "      ", "  a   ", "      " }, null },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(new GridPos(3, 1), result.State.PortalBoxes[0].Position);
            var pushed = (PortalBoxPushedEvent)result.Events[0];
            Assert.AreEqual(0, pushed.PortalIndex);
            Assert.AreEqual(new GridPos(2, 1), pushed.From);
            Assert.AreEqual(new GridPos(3, 1), pushed.To);
        }

        [Test]
        public void PushPortalBox_IntoUniqueWall_Fails()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@   #", "######" },
                new[] { new[] { "      ", "  ax  ", "      " }, null },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.BoxBlocked, result.Failure);
        }

        [Test]
        public void OtherWorldPortalBox_DoesNotBlock()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@   #", "######" },
                new[] { null, new[] { "      ", "  a   ", "      " } },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(new GridPos(2, 1), result.State.PlayerPosition);
            Assert.AreEqual(new GridPos(2, 1), result.State.PortalBoxes[1].Position);
        }

        [Test]
        public void MappedBox_IsBlockedByCurrentWorldPortalBox()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@$  #", "######" },
                new[] { new[] { "      ", "   a  ", "      " }, null },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.BoxBlocked, result.Failure);
        }

        [Test]
        public void PushMappedBox_OntoOtherWorldUniqueWall_ConflictsThatWorld()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@$  #", "######" },
                new[] { null, new[] { "      ", "   x  ", "      " } },
                new int[0][]);
            LevelState start = rules.CreateInitialState();
            Assert.IsFalse(rules.IsWorldConflicted(start, 1));

            StepResult result = rules.TryMove(start, Direction.Right);

            Assert.IsTrue(result.IsStepTaken, "推目标箱子只看当前世界，别的世界的独有墙不挡");
            Assert.IsTrue(rules.IsWorldConflicted(result.State, 1));
            Assert.IsFalse(rules.IsWorldConflicted(result.State, 0));
        }

        [Test]
        public void PushMappedBox_OntoOtherWorldPortalBox_ConflictsThatWorld()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@$  #", "######" },
                new[] { null, new[] { "      ", "   a  ", "      " } },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.IsTrue(rules.IsWorldConflicted(result.State, 1));
        }

        [Test]
        public void PushMappedBox_OntoPedestalHoldingOtherWorldPortalBox_ConflictsThatWorld()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@$P #", "######" },
                new[] { null, new[] { "      ", "   a  ", "      " } },
                OnePair01);

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(new GridPos(3, 1), result.State.MappedBoxes[0]);
            Assert.IsTrue(rules.IsWorldConflicted(result.State, 1));
        }

        [Test]
        public void PortalBox_CanBePushedOntoAndOffPedestal()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@ P #", "######" },
                new[] { new[] { "      ", "  a   ", "      " }, null },
                OnePair01);
            LevelState state = rules.CreateInitialState();

            state = rules.TryMove(state, Direction.Right).State;
            Assert.IsTrue(rules.IsPortalOnPedestal(state, 0));

            StepResult offPedestal = rules.TryMove(state, Direction.Right);

            Assert.IsTrue(offPedestal.IsStepTaken);
            Assert.AreEqual(new GridPos(4, 1), offPedestal.State.PortalBoxes[0].Position);
            Assert.IsFalse(rules.IsPortalOnPedestal(offPedestal.State, 0));
        }

        [Test]
        public void InitialConflict_IsAllowedAndDetected()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@ $ #", "######" },
                new[] { null, new[] { "      ", "   x  ", "      " } },
                new int[0][]);

            Assert.IsTrue(rules.IsWorldConflicted(rules.CreateInitialState(), 1));
        }

        [Test]
        public void StateEquality_IncludesPortalBoxes()
        {
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "######", "#@   #", "######" },
                new[] { new[] { "      ", "  a   ", "      " }, null },
                OnePair01);
            LevelState start = rules.CreateInitialState();

            LevelState pushed = rules.TryMove(start, Direction.Right).State;
            LevelState back = rules.TryMove(pushed, Direction.Left).State;

            Assert.AreNotEqual(start, back, "玩家回到原位但传送箱子位置不同，状态应不相等");
            Assert.AreEqual(start, start.Clone());
        }
    }
}
