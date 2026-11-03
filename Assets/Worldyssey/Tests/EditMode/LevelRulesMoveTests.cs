using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelRulesMoveTests
    {
        [Test]
        public void MoveIntoEmpty_MovesAndCountsStep()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#####",
                "#@  #",
                "#####");
            LevelState start = rules.CreateInitialState();

            StepResult result = rules.TryMove(start, Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(StepFailure.None, result.Failure);
            Assert.AreEqual(new GridPos(2, 1), result.State.PlayerPosition);
            Assert.AreEqual(1, result.Events.Count);
            var moved = (PlayerMovedEvent)result.Events[0];
            Assert.AreEqual(new GridPos(1, 1), moved.From);
            Assert.AreEqual(new GridPos(2, 1), moved.To);
        }

        [Test]
        public void MoveInNewDirection_TurnsThenMoves()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Down,
                "#####",
                "#@  #",
                "#####");

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(Direction.Right, result.State.PlayerFacing);
            Assert.AreEqual(2, result.Events.Count);
            Assert.IsInstanceOf<PlayerTurnedEvent>(result.Events[0]);
            Assert.IsInstanceOf<PlayerMovedEvent>(result.Events[1]);
        }

        [Test]
        public void MoveIntoWall_OnlyTurnsAndDoesNotCountStep()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#####",
                "#@  #",
                "#####");
            LevelState start = rules.CreateInitialState();

            StepResult result = rules.TryMove(start, Direction.Up);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.Blocked, result.Failure);
            Assert.AreEqual(start.PlayerPosition, result.State.PlayerPosition);
            Assert.AreEqual(Direction.Up, result.State.PlayerFacing);
            Assert.AreEqual(1, result.Events.Count);
            var turned = (PlayerTurnedEvent)result.Events[0];
            Assert.AreEqual(Direction.Right, turned.From);
            Assert.AreEqual(Direction.Up, turned.To);
        }

        [Test]
        public void MoveIntoWall_AlreadyFacingIt_ProducesNoEvent()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Up,
                "#####",
                "#@  #",
                "#####");
            LevelState start = rules.CreateInitialState();

            StepResult result = rules.TryMove(start, Direction.Up);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.Blocked, result.Failure);
            Assert.AreEqual(0, result.Events.Count);
            Assert.AreEqual(start, result.State);
        }

        [Test]
        public void MoveOutOfBounds_IsTreatedAsWall()
        {
            // 没有外墙的关卡，玩家在最左列
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Left, "@  ");

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Left);

            Assert.IsFalse(result.IsStepTaken);
            Assert.AreEqual(StepFailure.Blocked, result.Failure);
            Assert.AreEqual(new GridPos(0, 0), result.State.PlayerPosition);
        }

        [Test]
        public void TryMove_DoesNotModifyInputState()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "#@$  #",
                "######");
            LevelState start = rules.CreateInitialState();
            LevelState copy = start.Clone();

            StepResult result = rules.TryMove(start, Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.AreEqual(copy, start);
            Assert.AreNotSame(start, result.State);
        }
    }
}
