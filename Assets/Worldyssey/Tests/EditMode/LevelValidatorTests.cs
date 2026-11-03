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
            AssertSingleError(new LevelDataBuilder { FormatVersion = LevelData.CurrentFormatVersion + 1 }, "版本");
        }

        [Test]
        public void NonPositiveSize_IsError()
        {
            Assert.IsTrue(LevelValidator.Validate(new LevelDataBuilder { Width = 0 }.Build()).HasErrors);
        }

        [Test]
        public void TooManyWorlds_IsError()
        {
            var builder = new LevelDataBuilder();
            for (int i = 0; i < 3; i++)
            {
                builder.Worlds.Add(new WorldData($"Extra {i}", System.Array.Empty<string>()));
            }

            AssertSingleError(builder, "世界数量");
        }

        [Test]
        public void StartWorldOutOfRange_IsError()
        {
            AssertSingleError(new LevelDataBuilder { StartWorld = 2 }, "起始世界");
        }

        [Test]
        public void SharedRowCountMismatch_IsError()
        {
            AssertHasError(new LevelDataBuilder { SharedRows = new[] { "#######", "#    .#", "#######" } }, "行数");
        }

        [Test]
        public void SharedRowLengthMismatch_IsError()
        {
            AssertHasError(new LevelDataBuilder { SharedRows = new[] { "#######", "# P   #", "#   .#", "#######" } }, "长度");
        }

        [Test]
        public void UnknownSharedCharacter_IsErrorWithPosition()
        {
            LevelValidationReport report = LevelValidator.Validate(
                new LevelDataBuilder { SharedRows = new[] { "#######", "# P ? #", "#    .#", "#######" } }.Build());

            LevelIssue issue = report.Issues.Single(i => i.IsError);
            StringAssert.Contains("未知字符", issue.Message);
            Assert.AreEqual(new GridPos(4, 2), issue.Position);
        }

        [Test]
        public void UniqueWallRowMismatch_IsError()
        {
            AssertSingleError(new LevelDataBuilder().WithWallRows(1, "       ", "       "), "独有层行数");
        }

        [Test]
        public void UnknownUniqueWallCharacter_IsError()
        {
            AssertSingleError(new LevelDataBuilder().WithWallRows(1, "       ", "       ", "    #  ", "       "), "未知字符");
        }

        [Test]
        public void NoGoal_IsError()
        {
            var builder = new LevelDataBuilder { SharedRows = new[] { "#######", "# P   #", "#     #", "#######" } };
            builder.MappedBoxes.Clear();

            AssertHasError(builder, "没有终点");
        }

        [Test]
        public void BoxCountDifferentFromGoalCount_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.MappedBoxes.Add(new GridPos(4, 2));

            AssertSingleError(builder, "数量");
        }

        [Test]
        public void UniqueWallOnPedestal_IsError()
        {
            AssertHasError(new LevelDataBuilder().WithWallRows(1, "       ", "  x    ", "       ", "       "), "底座");
        }

        [Test]
        public void PairWithSameWorld_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs[0] = new PortalPairData(0, 1, 1);

            AssertHasError(builder, "同一个世界");
        }

        [Test]
        public void PairWithMissingWorld_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs[0] = new PortalPairData(0, 0, 3);

            AssertHasError(builder, "不存在的世界");
        }

        [Test]
        public void DuplicatePairId_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs.Add(new PortalPairData(0, 1, 0));

            AssertSingleError(builder, "重复");
        }

        [Test]
        public void PortalBoxInWorldOutsidePair_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Worlds.Add(new WorldData("World 2", System.Array.Empty<string>()));
            builder.PortalBoxes[0] = new PortalBoxData(0, 2, 2, 2);

            AssertSingleError(builder, "不属于配对");
        }

        [Test]
        public void SameSidePlacedTwice_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes.Add(new PortalBoxData(0, 0, 4, 2));

            AssertSingleError(builder, "不止一次");
        }

        [Test]
        public void PortalBoxWithUnknownPair_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes.Add(new PortalBoxData(9, 1, 4, 2));

            AssertSingleError(builder, "配对 9");
        }

        [Test]
        public void PlayerOnWall_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Player = new PlayerSpawnData(0, 1, Direction.Right) }, "玩家");
        }

        [Test]
        public void PlayerOutOfBounds_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Player = new PlayerSpawnData(9, 1, Direction.Right) }, "越界");
        }

        [Test]
        public void MappedBoxOnStartWorldUniqueWall_IsError()
        {
            AssertSingleError(new LevelDataBuilder().WithWallRows(0, "       ", "       ", "   x   ", "       "), "独有墙上");
        }

        [Test]
        public void PlayerOverlappingMappedBox_IsError()
        {
            AssertSingleError(new LevelDataBuilder { Player = new PlayerSpawnData(3, 1, Direction.Right) }, "重叠");
        }

        [Test]
        public void DuplicateMappedBoxes_IsError()
        {
            var builder = new LevelDataBuilder { SharedRows = new[] { "#######", "# P  .#", "#    .#", "#######" } };
            builder.MappedBoxes.Add(new GridPos(3, 1));

            AssertSingleError(builder, "重叠");
        }

        [Test]
        public void StartWorldPortalBoxOverlappingMappedBox_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.PortalBoxes[0] = new PortalBoxData(0, 0, 3, 1);

            AssertSingleError(builder, "重叠");
        }

        [Test]
        public void OtherWorldPortalBoxOnItsOwnWall_IsError()
        {
            var builder = new LevelDataBuilder().WithWallRows(1, "       ", "       ", "    x  ", "       ");
            builder.PortalBoxes.Add(new PortalBoxData(0, 1, 4, 1));

            AssertSingleError(builder, "独有墙上");
        }

        [Test]
        public void OtherWorldPortalBoxesOverlapping_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Pairs.Add(new PortalPairData(1, 0, 1));
            builder.PortalBoxes.Add(new PortalBoxData(0, 1, 4, 1));
            builder.PortalBoxes.Add(new PortalBoxData(1, 1, 4, 1));

            AssertSingleError(builder, "重叠");
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
