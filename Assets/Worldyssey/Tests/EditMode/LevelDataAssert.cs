using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// 逐字段比较两个 <see cref="LevelData"/>。数据类不实现相等比较，避免和运行时语义混淆，比较只在测试里做。
    /// </summary>
    public static class LevelDataAssert
    {
        public static void AreEqual(LevelData expected, LevelData actual)
        {
            Assert.AreEqual(expected.FormatVersion, actual.FormatVersion, "formatVersion");
            Assert.AreEqual(expected.Id, actual.Id, "id");
            Assert.AreEqual(expected.DisplayName, actual.DisplayName, "displayName");
            Assert.AreEqual(expected.Width, actual.Width, "width");
            Assert.AreEqual(expected.Height, actual.Height, "height");
            Assert.AreEqual(expected.StartWorld, actual.StartWorld, "startWorld");
            CollectionAssert.AreEqual(expected.SharedRows, actual.SharedRows, "sharedRows");

            Assert.AreEqual(expected.Worlds.Count, actual.Worlds.Count, "worlds");
            for (int i = 0; i < expected.Worlds.Count; i++)
            {
                Assert.AreEqual(expected.Worlds[i].Name, actual.Worlds[i].Name, $"worlds[{i}].name");
                CollectionAssert.AreEqual(expected.Worlds[i].WallRows, actual.Worlds[i].WallRows, $"worlds[{i}].wallRows");
            }

            Assert.AreEqual(expected.Player.Position, actual.Player.Position, "player position");
            Assert.AreEqual(expected.Player.Facing, actual.Player.Facing, "player facing");
            CollectionAssert.AreEqual(expected.MappedBoxes, actual.MappedBoxes, "mappedBoxes");

            Assert.AreEqual(expected.PortalPairs.Count, actual.PortalPairs.Count, "portalPairs");
            for (int i = 0; i < expected.PortalPairs.Count; i++)
            {
                Assert.AreEqual(expected.PortalPairs[i].Id, actual.PortalPairs[i].Id, $"portalPairs[{i}].id");
                Assert.AreEqual(expected.PortalPairs[i].WorldA, actual.PortalPairs[i].WorldA, $"portalPairs[{i}].worldA");
                Assert.AreEqual(expected.PortalPairs[i].WorldB, actual.PortalPairs[i].WorldB, $"portalPairs[{i}].worldB");
            }

            Assert.AreEqual(expected.PortalBoxes.Count, actual.PortalBoxes.Count, "portalBoxes");
            for (int i = 0; i < expected.PortalBoxes.Count; i++)
            {
                Assert.AreEqual(expected.PortalBoxes[i].Pair, actual.PortalBoxes[i].Pair, $"portalBoxes[{i}].pair");
                Assert.AreEqual(expected.PortalBoxes[i].World, actual.PortalBoxes[i].World, $"portalBoxes[{i}].world");
                Assert.AreEqual(expected.PortalBoxes[i].Position, actual.PortalBoxes[i].Position, $"portalBoxes[{i}].position");
            }
        }
    }
}
