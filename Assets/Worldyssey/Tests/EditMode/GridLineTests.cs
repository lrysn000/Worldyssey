using System.Collections.Generic;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class GridLineTests
    {
        private readonly List<GridPos> _cells = new List<GridPos>();

        [Test]
        public void MostlyHorizontal_LocksToRow()
        {
            GridLine.Collect(new GridPos(2, 2), new GridPos(5, 3), _cells);

            CollectionAssert.AreEqual(new[] { new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2), new GridPos(5, 2) }, _cells);
        }

        [Test]
        public void MostlyVertical_LocksToColumnAndGoesDown()
        {
            GridLine.Collect(new GridPos(3, 4), new GridPos(2, 1), _cells);

            CollectionAssert.AreEqual(new[] { new GridPos(3, 4), new GridPos(3, 3), new GridPos(3, 2), new GridPos(3, 1) }, _cells);
        }

        [Test]
        public void SameCell_IsSingleCell()
        {
            GridLine.Collect(new GridPos(1, 1), new GridPos(1, 1), _cells);

            CollectionAssert.AreEqual(new[] { new GridPos(1, 1) }, _cells);
            Assert.AreEqual(new GridPos(4, 1), GridLine.LockedEnd(new GridPos(1, 1), new GridPos(4, 2)));
        }
    }
}
