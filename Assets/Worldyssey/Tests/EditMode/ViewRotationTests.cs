using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class ViewRotationTests
    {
        [TestCase(Direction.Up, Direction.Up)]
        [TestCase(Direction.Right, Direction.Right)]
        [TestCase(Direction.Down, Direction.Down)]
        [TestCase(Direction.Left, Direction.Left)]
        public void DefaultView_ScreenEqualsGrid(Direction screen, Direction expected)
        {
            Assert.AreEqual(expected, ViewRotation.ScreenToGrid(screen, 0));
        }

        [TestCase(Direction.Up, Direction.Right)]
        [TestCase(Direction.Right, Direction.Down)]
        [TestCase(Direction.Down, Direction.Left)]
        [TestCase(Direction.Left, Direction.Up)]
        public void CameraTurnedClockwiseOnce_ScreenUpIsGridRight(Direction screen, Direction expected)
        {
            Assert.AreEqual(expected, ViewRotation.ScreenToGrid(screen, 1));
        }

        [Test]
        public void HalfTurn_ReversesDirections()
        {
            Assert.AreEqual(Direction.Down, ViewRotation.ScreenToGrid(Direction.Up, 2));
            Assert.AreEqual(Direction.Left, ViewRotation.ScreenToGrid(Direction.Right, 2));
        }

        [TestCase(-1, 3)]
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(-6, 2)]
        public void Normalize_WrapsIntoZeroToThree(int turns, int expected)
        {
            Assert.AreEqual(expected, ViewRotation.Normalize(turns));
        }

        [Test]
        public void CounterClockwiseTurn_EqualsThreeClockwiseTurns()
        {
            Assert.AreEqual(ViewRotation.ScreenToGrid(Direction.Up, 3), ViewRotation.ScreenToGrid(Direction.Up, -1));
        }
    }
}
