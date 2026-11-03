using System.Linq;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelRulesSolveTests
    {
        [Test]
        public void PushLastBoxOntoGoal_SolvesLevel()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "#@$ .#",
                "######");
            LevelState state = rules.CreateInitialState();

            StepResult first = rules.TryMove(state, Direction.Right);
            Assert.IsFalse(first.IsLevelSolved);

            StepResult second = rules.TryMove(first.State, Direction.Right);

            Assert.IsTrue(second.IsLevelSolved);
            Assert.IsTrue(rules.IsSolved(second.State));
            Assert.IsInstanceOf<LevelSolvedEvent>(second.Events.Last());
        }

        [Test]
        public void SomeGoalsUncovered_NotSolved()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#######",
                "#@$.  #",
                "#  *  #",
                "#######");

            Assert.IsFalse(rules.IsSolved(rules.CreateInitialState()));

            StepResult result = rules.TryMove(rules.CreateInitialState(), Direction.Right);

            Assert.IsTrue(result.IsLevelSolved);
        }

        [Test]
        public void PushBoxOffGoal_NoLongerSolved()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "#@*  #",
                "######");
            LevelState start = rules.CreateInitialState();
            Assert.IsTrue(rules.IsSolved(start));

            StepResult result = rules.TryMove(start, Direction.Right);

            Assert.IsTrue(result.IsStepTaken);
            Assert.IsFalse(result.IsLevelSolved);
            Assert.IsFalse(result.Events.OfType<LevelSolvedEvent>().Any());
        }

        [Test]
        public void PlayerStandingOnGoal_DoesNotCount()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#####",
                "#+$ #",
                "#####");

            Assert.IsFalse(rules.IsSolved(rules.CreateInitialState()));
        }

        [Test]
        public void LevelWithoutGoals_IsNeverSolved()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right, "@ ");

            Assert.IsFalse(rules.IsSolved(rules.CreateInitialState()));
        }
    }
}
