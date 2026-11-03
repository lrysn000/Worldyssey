using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelRulesPushTests
    {
        [Test]
        public void PushBoxIntoEmpty_MovesBoxAndPlayer()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "#@$  #",
                "######");

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(new GridPos(2, 1), result.State.PlayerPosition);
            Assert.AreEqual(new GridPos(3, 1), result.State.MappedBoxes[0]);

            // 推箱事件在玩家移动之前，表现层据此让两者同时开始补间
            Assert.AreEqual(2, result.Events.Count);
            var pushed = (MappedBoxPushedEvent)result.Events[0];
            Assert.AreEqual(0, pushed.BoxIndex);
            Assert.AreEqual(new GridPos(2, 1), pushed.From);
            Assert.AreEqual(new GridPos(3, 1), pushed.To);
            Assert.IsInstanceOf<PlayerMovedEvent>(result.Events[1]);
        }

        [Test]
        public void PushBoxIntoWall_Fails()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "####",
                "#@$#",
                "####");
            LevelState start = rules.CreateInitialState();

            StepResult result = rules.TryMove(start, Direction.Right);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.BoxBlocked, result.Failure);
            Assert.AreEqual(start, result.State);
        }

        [Test]
        public void PushBoxIntoAnotherBox_Fails()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "#@$$ #",
                "######");
            LevelState start = rules.CreateInitialState();

            StepResult result = rules.TryMove(start, Direction.Right);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.BoxBlocked, result.Failure);
            Assert.AreEqual(start, result.State);
        }

        [Test]
        public void PushBoxOutOfBounds_Fails()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right, "@$");

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.BoxBlocked, result.Failure);
        }

        [Test]
        public void PushSecondBox_ReportsCorrectIndex()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#######",
                "#$    #",
                "#  @$ #",
                "#######");

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            var pushed = (MappedBoxPushedEvent)result.Events[0];
            Assert.AreEqual(1, pushed.BoxIndex);
            Assert.AreEqual(new GridPos(1, 2), result.State.MappedBoxes[0]);
            Assert.AreEqual(new GridPos(5, 1), result.State.MappedBoxes[1]);
        }

        [Test]
        public void CannotPullBox()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "# @$ #",
                "######");

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Left);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(new GridPos(3, 1), result.State.MappedBoxes[0]);
        }

        [Test]
        public void PushBox_NotFacingIt_OnlyTurnsFirst()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Down,
                "######",
                "#@$  #",
                "######");
            LevelState start = rules.CreateInitialState();

            StepResult first = rules.TryMove(start, Direction.Right);

            Assert.IsFalse(first.IsStepTaken);
            Assert.AreEqual(StepFailure.TurnedToFaceBox, first.Failure);
            Assert.AreEqual(Direction.Right, first.State.PlayerFacing);
            Assert.AreEqual(start.PlayerPosition, first.State.PlayerPosition);
            Assert.AreEqual(start.MappedBoxes[0], first.State.MappedBoxes[0]);
            Assert.AreEqual(1, first.Events.Count);
            Assert.IsInstanceOf<PlayerTurnedEvent>(first.Events[0]);

            StepResult second = rules.TryMove(first.State, Direction.Right);

            Assert.IsTrue(second.IsStepTaken);
            Assert.AreEqual(new GridPos(3, 1), second.State.MappedBoxes[0]);
        }

        [Test]
        public void WalkingTowardBox_ArrivesFacingIt_NextPressPushes()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Down,
                "#######",
                "#@ $  #",
                "#######");

            LevelState arrived = TestLevels.Walk(rules, rules.CreateInitialState(), Direction.Right);
            Assert.AreEqual(new GridPos(2, 1), arrived.PlayerPosition, "第一下转向并移动，到箱子旁边时已面朝它");

            StepResult push = rules.TryMove(arrived, Direction.Right);

            Assert.IsTrue(push.IsStepTaken);
            Assert.AreEqual(new GridPos(4, 1), push.State.MappedBoxes[0]);
        }

        [Test]
        public void BlockedBox_NotFacingIt_ReportsTurnNotBlocked()
        {
            // 没面朝时先转向，不判断箱子能否推动；面朝后再推才报告被挡
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Down,
                "####",
                "#@$#",
                "####");

            StepResult first = rules.TryMove(rules.CreateInitialState(), Direction.Right);
            StepResult second = rules.TryMove(first.State, Direction.Right);

            Assert.AreEqual(StepFailure.TurnedToFaceBox, first.Failure);
            Assert.AreEqual(StepFailure.BoxBlocked, second.Failure);
        }
    }
}
