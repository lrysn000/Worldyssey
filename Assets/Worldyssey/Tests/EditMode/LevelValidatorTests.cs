using System.Linq;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelValidatorTests
    {
        [Test]
        public void DefaultBuilderLevel_IsValid()
        {
            LevelValidationReport report = LevelValidator.Validate(new LevelDataBuilder().Build());

            Assert.AreEqual(0, report.ErrorCount, Describe(report));
            Assert.AreEqual(0, report.WarningCount, Describe(report));
        }

        [Test]
        public void ValidLevel_CanBeLoadedByLevelMap()
        {
            // 校验通过的关卡必须能被运行时解析，两者的规则不能出现分歧
            Assert.DoesNotThrow(() => new LevelMap(new LevelDataBuilder().Build()));
        }

        [Test]
        public void NullData_IsError()
        {
            Assert.IsTrue(LevelValidator.Validate(null).HasErrors);
        }

        [Test]
        public void EmptyId_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Id = " " }, "id");
        }

        [Test]
        public void EmptyDisplayName_IsWarningOnly()
        {
            LevelValidationReport report = LevelValidator.Validate(new LevelDataBuilder { DisplayName = "" }.Build());

            Assert.IsFalse(report.HasErrors);
            Assert.AreEqual(1, report.WarningCount);
        }

        [Test]
        public void NewerFormatVersion_IsError()
        {
            AssertSingleError(new LevelDataBuilder { FormatVersion = LevelData.CurrentFormatVersion + 1 }, "version");
        }

        [Test]
        public void NonPositiveSize_IsError()
        {
            Assert.IsTrue(LevelValidator.Validate(new LevelDataBuilder { Width = 0 }.Build()).HasErrors);
        }

        [Test]
        public void SizeBeyondLimit_IsError()
        {
            var builder = new LevelDataBuilder { Width = LevelLimits.MaxWidth + 1 };
            builder.SharedRows = new[]
            {
                new string('#', builder.Width),
                "# P" + new string(' ', builder.Width - 4) + "#",
                "#" + new string(' ', builder.Width - 3) + ".#",
                new string('#', builder.Width),
            };

            AssertSingleError(builder, "outside the allowed range");
        }

        [Test]
        public void TooManyWorlds_IsError()
        {
            var builder = new LevelDataBuilder();
            for (int i = 0; i < 3; i++)
            {
                builder.Worlds.Add(new WorldData($"Extra {i}", System.Array.Empty<string>()));
            }

            AssertSingleError(builder, "World count");
        }

        [Test]
        public void StartWorldOutOfRange_IsError()
        {
            AssertSingleError(new LevelDataBuilder { StartWorld = 2 }, "Start world");
        }

        [Test]
        public void SharedRowCountMismatch_IsError()
        {
            AssertHasError(new LevelDataBuilder { SharedRows = new[] { "#######", "#    .#", "#######" } }, "row count");
        }

        [Test]
        public void SharedRowLengthMismatch_IsError()
        {
            AssertHasError(new LevelDataBuilder { SharedRows = new[] { "#######", "# P   #", "#   .#", "#######" } }, "length");
        }

        [Test]
        public void UnknownSharedCharacter_IsErrorWithPosition()
        {
            LevelValidationReport report = LevelValidator.Validate(
                new LevelDataBuilder { SharedRows = new[] { "#######", "# P ? #", "#    .#", "#######" } }.Build());

            LevelIssue issue = report.Issues.Single(i => i.IsError);
            StringAssert.Contains("Unknown character", issue.Message);
            Assert.AreEqual(new GridPos(4, 2), issue.Position);
        }

        [Test]
        public void UniqueWallRowMismatch_IsError()
        {
            AssertSingleError(new LevelDataBuilder().WithWallRows(1, "       ", "       "), "unique layer row count");
        }

        [Test]
        public void UnknownUniqueWallCharacter_IsError()
        {
            AssertSingleError(new LevelDataBuilder().WithWallRows(1, "       ", "       ", "    #  ", "       "), "Unknown character");
        }

        [Test]
        public void NoGoal_IsError()
        {
            var builder = new LevelDataBuilder { SharedRows = new[] { "#######", "# P   #", "#     #", "#######" } };
            builder.MappedBoxes.Clear();

            AssertHasError(builder, "no goal");
        }

        [Test]
        public void BoxCountDifferentFromGoalCount_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.MappedBoxes.Add(new GridPos(4, 2));

            AssertSingleError(builder, "count");
        }

        [Test]
        public void UniqueWallOnPedestal_IsError()
        {
            AssertHasError(new LevelDataBuilder().WithWallRows(1, "       ", "  x    ", "       ", "       "), "pedestal");
        }

        [Test]
        public void PairWithSameWorld_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs[0] = new PortalPairData(0, 1, 1);

            AssertHasError(builder, "to itself");
        }

        [Test]
        public void PairWithMissingWorld_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs[0] = new PortalPairData(0, 0, 3);

            AssertHasError(builder, "does not exist");
        }

        [Test]
        public void TooManyPairs_IsError()
        {
            var builder = new LevelDataBuilder();
            for (int i = builder.Pairs.Count; i <= LevelLimits.MaxPairCount; i++)
            {
                builder.Pairs.Add(new PortalPairData(i, 0, 1));
            }

            AssertHasError(builder, "portal pairs");
        }

        [Test]
        public void DuplicatePairId_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs.Add(new PortalPairData(0, 1, 0));

            AssertSingleError(builder, "duplicated");
        }

        [Test]
        public void PortalBoxInWorldOutsidePair_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Worlds.Add(new WorldData("World 2", System.Array.Empty<string>()));
            builder.PortalBoxes[0] = new PortalBoxData(0, 2, 2, 2);

            AssertSingleError(builder, "not part of pair");
        }

        [Test]
        public void SameSidePlacedTwice_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes.Add(new PortalBoxData(0, 0, 4, 2));

            AssertSingleError(builder, "more than one");
        }

        [Test]
        public void PortalBoxWithUnknownPair_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes.Add(new PortalBoxData(9, 1, 4, 2));

            AssertSingleError(builder, "pair 9");
        }

        [Test]
        public void PlayerOnWall_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Player = new PlayerSpawnData(0, 1, Direction.Right) }, "Player");
        }

        [Test]
        public void PlayerOutOfBounds_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Player = new PlayerSpawnData(9, 1, Direction.Right) }, "out of bounds");
        }

        [Test]
        public void MappedBoxOnStartWorldUniqueWall_IsError()
        {
            AssertSingleError(new LevelDataBuilder().WithWallRows(0, "       ", "       ", "   x   ", "       "), "unique wall of");
        }

        [Test]
        public void PlayerOverlappingMappedBox_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Player = new PlayerSpawnData(3, 1, Direction.Right) }, "overlaps");
        }

        [Test]
        public void DuplicateMappedBoxes_IsError()
        {
            var builder = new LevelDataBuilder { SharedRows = new[] { "#######", "# P  .#", "#    .#", "#######" } };
            builder.MappedBoxes.Add(new GridPos(3, 1));

            AssertSingleError(builder, "overlaps");
        }

        [Test]
        public void StartWorldPortalBoxOverlappingMappedBox_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes[0] = new PortalBoxData(0, 0, 3, 1);

            AssertSingleError(builder, "overlaps");
        }

        [Test]
        public void OtherWorldPortalBoxOnItsOwnWall_IsError()
        {
            var builder = new LevelDataBuilder().WithWallRows(1, "       ", "       ", "    x  ", "       ");
            builder.PortalBoxes.Add(new PortalBoxData(0, 1, 4, 1));

            AssertSingleError(builder, "unique wall of");
        }

        [Test]
        public void OtherWorldPortalBoxesOverlapping_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs.Add(new PortalPairData(1, 0, 1));
            builder.PortalBoxes.Add(new PortalBoxData(0, 1, 4, 1));
            builder.PortalBoxes.Add(new PortalBoxData(1, 1, 4, 1));

            AssertSingleError(builder, "overlaps");
        }

        [Test]
        public void InitialConflictWithUniqueWall_IsWarningOnly()
        {
            LevelValidationReport report = LevelValidator.Validate(
                new LevelDataBuilder().WithWallRows(1, "       ", "       ", "   x   ", "       ").Build());

            Assert.IsFalse(report.HasErrors, Describe(report));
            LevelIssue warning = report.Issues.Single();
            Assert.AreEqual(LevelIssueSeverity.Warning, warning.Severity);
            Assert.AreEqual(1, warning.World);
            Assert.AreEqual(new GridPos(3, 1), warning.Position);
        }

        [Test]
        public void InitialConflictWithOtherWorldPortalBox_IsWarningOnly()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes.Add(new PortalBoxData(0, 1, 3, 1));

            LevelValidationReport report = LevelValidator.Validate(builder.Build());

            Assert.IsFalse(report.HasErrors, Describe(report));
            Assert.AreEqual(1, report.WarningCount);
        }

        [Test]
        public void MultipleProblems_AreAllReported()
        {
            var builder = new LevelDataBuilder
            {
                Id = "",
                StartWorld = 5,
                Player = new PlayerSpawnData(-1, 0, Direction.Up),
            };
            builder.Pairs[0] = new PortalPairData(0, 1, 1);
            builder.MappedBoxes.Add(new GridPos(4, 2));

            LevelValidationReport report = LevelValidator.Validate(builder.Build());

            // id、起始世界、玩家越界、箱子数量、配对同世界、传送箱子引用无效配对
            Assert.GreaterOrEqual(report.ErrorCount, 6, Describe(report));
        }

        private static void AssertSingleError(LevelDataBuilder builder, string expectedFragment)
        {
            LevelValidationReport report = LevelValidator.Validate(builder.Build());
            Assert.AreEqual(1, report.ErrorCount, Describe(report));
            StringAssert.Contains(expectedFragment, report.Issues.First(i => i.IsError).Message);
        }

        private static void AssertHasError(LevelDataBuilder builder, string expectedFragment)
        {
            LevelValidationReport report = LevelValidator.Validate(builder.Build());
            Assert.IsTrue(report.Issues.Any(i => i.IsError && i.Message.Contains(expectedFragment)), Describe(report));
        }

        private static string Describe(LevelValidationReport report)
        {
            return string.Join("\n", report.Issues.Select(i => i.ToString()));
        }
    }
}
