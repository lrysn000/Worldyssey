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
    }
}
