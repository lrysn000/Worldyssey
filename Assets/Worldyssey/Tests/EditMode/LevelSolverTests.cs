using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelSolverTests
    {
        [Test]
        public void StraightPush_IsSolvable()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Left,
                "######",
                "#@$ .#",
                "######");

            Assert.AreEqual(SolveStatus.Solvable, LevelSolver.Solve(rules).Status);
        }

        [Test]
        public void WalkAroundBeforePushing_IsSolvable()
        {
            // 要先绕到箱子右边，把它往左推
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#######",
                "#.  $ #",
                "#    @#",
                "#######");

            Assert.IsTrue(LevelSolver.Solve(rules).IsSolvable);
        }

        [Test]
        public void BoxAgainstWallOffGoal_IsUnsolvable()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#####",
                "#.@$#",
                "#####");

            SolveResult result = LevelSolver.Solve(rules);

            Assert.AreEqual(SolveStatus.Unsolvable, result.Status);
            Assert.IsTrue(result.IsUnsolvable);
        }

        [Test]
        public void BoxStartsInCorner_IsUnsolvable()
        {
            // 箱子开局就卡在左上角，哪个方向都推不动
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#######",
                "#$   .#",
                "#  @  #",
                "#######");

            Assert.AreEqual(SolveStatus.Unsolvable, LevelSolver.Solve(rules).Status);
        }

        [Test]
        public void AlreadySolved_IsSolvable()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "#####",
                "#@* #",
                "#####");

            Assert.IsTrue(LevelSolver.Solve(rules).IsSolvable);
        }

        [Test]
        public void BlockedInStartWorld_SolvableThroughTeleport()
        {
            // 世界 0 里箱子左边是独有墙，推不动；传送到世界 1 才能推
            string[] shared =
            {
                "########",
                "#@P    #",
                "#   $ .#",
                "########",
            };
            string[] world0 =
            {
                "        ",
                "  a     ",
                "   x    ",
                "        ",
            };
            LevelRules withPortal = TestLevels.RulesMultiWorld(shared, new[] { world0, null }, new[] { new[] { 0, 1 } });

            Assert.IsTrue(LevelSolver.Solve(withPortal).IsSolvable);
        }

        [Test]
        public void BlockedWithoutUsablePortal_IsUnsolvable()
        {
            // 同样的布局，但两个世界都有那堵独有墙，传送过去也推不动
            string[] shared =
            {
                "########",
                "#@P    #",
                "#   $ .#",
                "########",
            };
            string[] world0 =
            {
                "        ",
                "  a     ",
                "   x    ",
                "        ",
            };
            string[] world1 =
            {
                "        ",
                "        ",
                "   x    ",
                "        ",
            };
            LevelRules rules = TestLevels.RulesMultiWorld(shared, new[] { world0, world1 }, new[] { new[] { 0, 1 } });

            Assert.AreEqual(SolveStatus.Unsolvable, LevelSolver.Solve(rules).Status);
        }

        [Test]
        public void StateLimit_StopsWithoutConclusion()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "##########",
                "#@       #",
                "#  $  $  #",
                "#        #",
                "#  .  .  #",
                "##########");

            SolveResult result = LevelSolver.Solve(rules, 2);

            Assert.AreEqual(SolveStatus.LimitReached, result.Status);
            Assert.IsFalse(result.IsUnsolvable, "超过上限不等于无解");
        }

        [Test]
        public void Cancel_StopsSearch()
        {
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "##########",
                "#@       #",
                "#  $  $  #",
                "#        #",
                "#  .  .  #",
                "##########");

            SolveResult result = LevelSolver.Solve(rules, LevelSolver.DefaultMaxStates, explored => false);

            Assert.AreEqual(SolveStatus.Cancelled, result.Status);
        }
    }
}
