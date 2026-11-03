using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelJsonSerializerTests
    {
        [Test]
        public void RoundTrip_PreservesAllContent()
        {
            var builder = new LevelDataBuilder().WithWallRows(1, "       ", "       ", "   x   ", "       ");
            builder.Pairs.Add(new PortalPairData(7, 1, 0));
            builder.PortalBoxes.Add(new PortalBoxData(7, 1, 4, 2));
            LevelData original = builder.Build();

            string json = LevelJsonSerializer.ToJson(original);
            bool isParsed = LevelJsonSerializer.TryFromJson(json, out LevelData restored, out string error);

            Assert.IsTrue(isParsed, error);
            LevelDataAssert.AreEqual(original, restored);
            Assert.AreEqual(json, LevelJsonSerializer.ToJson(restored), "再次序列化应得到完全相同的文本");
        }

        [Test]
        public void RoundTrip_KeepsRuntimeBehaviour()
        {
            LevelData original = TestLevels.MultiWorld(
                new[] { "#######", "#@P   #", "#######" },
                new[] { new[] { "       ", "  a    ", "       " }, new[] { "       ", "     x ", "       " } },
                new[] { new[] { 0, 1 } });

            LevelJsonSerializer.TryFromJson(LevelJsonSerializer.ToJson(original), out LevelData restored, out _);

            var originalRules = new LevelRules(new LevelMap(original));
            var restoredRules = new LevelRules(new LevelMap(restored));
            Assert.AreEqual(originalRules.CreateInitialState(), restoredRules.CreateInitialState());
            Assert.AreEqual(
                originalRules.TryTeleport(originalRules.CreateInitialState()).State,
                restoredRules.TryTeleport(restoredRules.CreateInitialState()).State);
        }

        [Test]
        public void Json_UsesReadableRowsAndNumericFacing()
        {
            string json = LevelJsonSerializer.ToJson(new LevelDataBuilder().Build());

            StringAssert.Contains("\"# P   #\"", json);
            StringAssert.Contains("\"facing\": 1", json);
            StringAssert.Contains("\"formatVersion\": 1", json);
        }

        [Test]
        public void EmptyText_Fails()
        {
            Assert.IsFalse(LevelJsonSerializer.TryFromJson("  ", out LevelData level, out string error));
            Assert.IsNull(level);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void MalformedJson_Fails()
        {
            Assert.IsFalse(LevelJsonSerializer.TryFromJson("{ \"id\": ", out LevelData level, out string error));
            Assert.IsNull(level);
            StringAssert.Contains("格式错误", error);
        }

        [Test]
        public void NewerFormatVersion_Fails()
        {
            string json = LevelJsonSerializer.ToJson(new LevelDataBuilder().Build())
                .Replace("\"formatVersion\": 1", "\"formatVersion\": 99");

            Assert.IsFalse(LevelJsonSerializer.TryFromJson(json, out _, out string error));
            StringAssert.Contains("99", error);
        }

        [Test]
        public void MissingFields_ParseButFailValidation()
        {
            Assert.IsTrue(LevelJsonSerializer.TryFromJson("{ \"id\": \"partial\" }", out LevelData level, out string error), error);

            LevelValidationReport report = LevelValidator.Validate(level);

            Assert.IsTrue(report.HasErrors);
        }
    }
}
