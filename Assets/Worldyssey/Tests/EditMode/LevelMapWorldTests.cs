using System;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelMapWorldTests
    {
        private static readonly int[][] OnePair01 = { new[] { 0, 1 } };

        [Test]
        public void UniqueWalls_AreParsedPerWorld()
        {
            var map = new LevelMap(TestLevels.MultiWorld(
                new[] { "@  " },
                new[] { new[] { "  x" }, new[] { " x " } },
                new int[0][]));

            Assert.AreEqual(2, map.WorldCount);
            Assert.IsTrue(map.IsUniqueWall(0, new GridPos(2, 0)));
            Assert.IsFalse(map.IsUniqueWall(0, new GridPos(1, 0)));
            Assert.IsTrue(map.IsUniqueWall(1, new GridPos(1, 0)));
            Assert.IsFalse(map.IsUniqueWall(1, new GridPos(2, 0)));
            Assert.IsFalse(map.IsUniqueWall(0, new GridPos(9, 0)));
        }

        [Test]
        public void PortalBoxes_SideIsDeterminedByWorld()
        {
            var map = new LevelMap(TestLevels.MultiWorld(
                new[] { "@   " },
                new[] { null, new[] { "  a " } },
                OnePair01));

            Assert.AreEqual(1, map.PortalPairCount);
            Assert.AreEqual(2, map.PortalBoxStarts.Count);
            Assert.AreEqual(0, map.GetPortalWorld(0));
            Assert.AreEqual(1, map.GetPortalWorld(1));
            Assert.IsFalse(map.PortalBoxStarts[0].IsPresent);
            Assert.IsTrue(map.PortalBoxStarts[1].IsPresent);
            Assert.AreEqual(new GridPos(2, 0), map.PortalBoxStarts[1].Position);
            Assert.AreEqual(1, LevelMap.GetPartnerIndex(0));
            Assert.AreEqual(0, LevelMap.GetPartnerIndex(1));
        }

        [Test]
        public void EmptyWallRows_MeansNoUniqueWalls()
        {
            LevelData data = CreateData(new[] { new WorldData("w", Array.Empty<string>()) }, 0, null, null);

            var map = new LevelMap(data);

            Assert.IsFalse(map.IsUniqueWall(0, new GridPos(1, 0)));
        }

        [Test]
        public void InvalidWorldCount_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(Array.Empty<WorldData>(), 0, null, null)));
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(Worlds(5), 0, null, null)));
        }

        [Test]
        public void StartWorldOutOfRange_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(Worlds(2), 2, null, null)));
        }

        [Test]
        public void PairWithSameWorldTwice_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                Worlds(2), 0, new[] { new PortalPairData(0, 1, 1) }, null)));
        }

        [Test]
        public void PairReferencingMissingWorld_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                Worlds(2), 0, new[] { new PortalPairData(0, 0, 3) }, null)));
        }

        [Test]
        public void DuplicatePairId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                Worlds(2), 0, new[] { new PortalPairData(7, 0, 1), new PortalPairData(7, 1, 0) }, null)));
        }

        [Test]
        public void PortalBoxInWorldOutsideItsPair_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                Worlds(3), 0, new[] { new PortalPairData(0, 0, 1) }, new[] { new PortalBoxData(0, 2, 1, 0) })));
        }

        [Test]
        public void SameSidePlacedTwice_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                Worlds(2), 0, new[] { new PortalPairData(0, 0, 1) },
                new[] { new PortalBoxData(0, 0, 1, 0), new PortalBoxData(0, 0, 2, 0) })));
        }

        [Test]
        public void PortalBoxWithUnknownPair_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                Worlds(2), 0, new[] { new PortalPairData(0, 0, 1) }, new[] { new PortalBoxData(5, 0, 1, 0) })));
        }

        [Test]
        public void UnknownWallCharacter_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LevelMap(CreateData(
                new[] { new WorldData("w", new[] { " # " }) }, 0, null, null)));
        }

        private static WorldData[] Worlds(int count)
        {
            var worlds = new WorldData[count];
            for (int i = 0; i < count; i++)
            {
                worlds[i] = new WorldData($"w{i}", new[] { "   " });
            }

            return worlds;
        }

        private static LevelData CreateData(WorldData[] worlds, int startWorld, PortalPairData[] pairs, PortalBoxData[] boxes)
        {
            return new LevelData(
                "t", "t", 3, 1, startWorld,
                new[] { "   " },
                worlds,
                new PlayerSpawnData(0, 0, Direction.Right),
                Array.Empty<GridPos>(),
                pairs ?? Array.Empty<PortalPairData>(),
                boxes ?? Array.Empty<PortalBoxData>());
        }
    }
}
