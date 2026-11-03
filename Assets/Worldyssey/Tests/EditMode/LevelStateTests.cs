using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelStateTests
    {
        [Test]
        public void Clone_IsEqualButNotSameInstance()
        {
            var state = new LevelState(0, new GridPos(1, 1), Direction.Up, new[] { new GridPos(2, 2) });

            LevelState clone = state.Clone();

            Assert.AreNotSame(state, clone);
            Assert.AreEqual(state, clone);
            Assert.AreEqual(state.GetHashCode(), clone.GetHashCode());
        }

        [Test]
        public void Constructor_CopiesBoxCollection()
        {
            var boxes = new[] { new GridPos(2, 2) };
            var state = new LevelState(0, new GridPos(1, 1), Direction.Up, boxes);

            boxes[0] = new GridPos(9, 9);

            Assert.AreEqual(new GridPos(2, 2), state.MappedBoxes[0]);
        }

        [Test]
        public void Equals_DetectsEachDifference()
        {
            var baseline = new LevelState(0, new GridPos(1, 1), Direction.Up, new[] { new GridPos(2, 2) });

            Assert.AreNotEqual(baseline, new LevelState(1, new GridPos(1, 1), Direction.Up, new[] { new GridPos(2, 2) }));
            Assert.AreNotEqual(baseline, new LevelState(0, new GridPos(1, 2), Direction.Up, new[] { new GridPos(2, 2) }));
            Assert.AreNotEqual(baseline, new LevelState(0, new GridPos(1, 1), Direction.Left, new[] { new GridPos(2, 2) }));
            Assert.AreNotEqual(baseline, new LevelState(0, new GridPos(1, 1), Direction.Up, new[] { new GridPos(3, 2) }));
        }

        [Test]
        public void FindMappedBoxAt_ReturnsIndexOrMinusOne()
        {
            var state = new LevelState(0, new GridPos(0, 0), Direction.Up, new[] { new GridPos(2, 2), new GridPos(3, 3) });

            Assert.AreEqual(1, state.FindMappedBoxAt(new GridPos(3, 3)));
            Assert.AreEqual(-1, state.FindMappedBoxAt(new GridPos(4, 4)));
        }
    }
}
