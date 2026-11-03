using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class GridPosTests
    {
        [TestCase(Direction.Up, 3, 5)]
        [TestCase(Direction.Down, 3, 3)]
        [TestCase(Direction.Left, 2, 4)]
        [TestCase(Direction.Right, 4, 4)]
        public void AddDirection_MovesOneCell(Direction direction, int expectedX, int expectedY)
        {
            GridPos result = new GridPos(3, 4) + direction;

            Assert.AreEqual(new GridPos(expectedX, expectedY), result);
        }

        [TestCase(Direction.Up, Direction.Down)]
        [TestCase(Direction.Down, Direction.Up)]
        [TestCase(Direction.Left, Direction.Right)]
        [TestCase(Direction.Right, Direction.Left)]
        public void Opposite_ReturnsReverseDirection(Direction direction, Direction expected)
        {
            Assert.AreEqual(expected, direction.Opposite());
        }

        [Test]
        public void Equality_ComparesByValue()
        {
            var a = new GridPos(1, 2);
            var b = new GridPos(1, 2);
            var c = new GridPos(2, 1);

            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
