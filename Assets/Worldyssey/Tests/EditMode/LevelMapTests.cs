using System;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelMapTests
    {
        [Test]
        public void SharedRows_FirstRowIsTopOfGrid()
        {
            // 第 0 行只在最左边有墙，应映射到 y = Height - 1
            var map = new LevelMap(TestLevels.FromXsb(
                "#   ",
                " @  ",
                "    "));

            Assert.IsTrue(map.IsSharedWall(new GridPos(0, 2)));
            Assert.IsFalse(map.IsSharedWall(new GridPos(0, 0)));
            Assert.AreEqual(new GridPos(1, 1), map.PlayerStart);
        }

        [Test]
        public void Goals_AreParsedFromAllGoalMarkers()
        {
            var map = new LevelMap(TestLevels.FromXsb(
                "#####",
                "#.*+#",
                "#####"));

            Assert.AreEqual(3, map.GoalPositions.Count);
            Assert.IsTrue(map.IsGoal(new GridPos(1, 1)));
            Assert.IsTrue(map.IsGoal(new GridPos(2, 1)));
            Assert.IsTrue(map.IsGoal(new GridPos(3, 1)));
            Assert.AreEqual(1, map.MappedBoxStarts.Count);
        }

        [Test]
        public void Queries_OutOfBounds_ReturnFalse()
        {
            var map = new LevelMap(TestLevels.FromXsb("@ "));

            Assert.IsFalse(map.IsInBounds(new GridPos(-1, 0)));
            Assert.IsFalse(map.IsInBounds(new GridPos(2, 0)));
            Assert.IsFalse(map.IsSharedWall(new GridPos(-1, 0)));
            Assert.IsFalse(map.IsGoal(new GridPos(0, 5)));
        }

        [Test]
        public void Pedestal_IsParsed()
        {
            var data = new LevelData(
                "p", "p", 3, 1, 0,
                new[] { " P " },
                new[] { new WorldData("w", new[] { "   " }) },
                new PlayerSpawnData(0, 0, Direction.Right),
                Array.Empty<GridPos>(),
                Array.Empty<PortalPairData>(),
                Array.Empty<PortalBoxData>());

            var map = new LevelMap(data);

            Assert.IsTrue(map.IsPedestal(new GridPos(1, 0)));
            Assert.IsFalse(map.IsSharedWall(new GridPos(1, 0)));
        }

        [Test]
        public void RowCountMismatch_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(3, 2, new[] { "   " })));
        }

        [Test]
        public void RowLengthMismatch_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(3, 1, new[] { "  " })));
        }

        [Test]
        public void UnknownCharacter_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(3, 1, new[] { " ? " })));
        }

        [Test]
        public void NonPositiveSize_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(0, 1, new[] { "" })));
        }

        private static LevelData CreateData(int width, int height, string[] sharedRows)
        {
            return new LevelData(
                "bad", "bad", width, height, 0,
                sharedRows,
                Array.Empty<WorldData>(),
                new PlayerSpawnData(0, 0, Direction.Down),
                Array.Empty<GridPos>(),
                Array.Empty<PortalPairData>(),
                Array.Empty<PortalBoxData>());
        }
    }
}
