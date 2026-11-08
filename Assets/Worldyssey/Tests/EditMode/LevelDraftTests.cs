using System.Linq;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelDraftTests
    {
        private LevelDraft _draft;

        [SetUp]
        public void SetUp()
        {
            _draft = LevelDraft.CreateNew("draft", "Draft");
        }

        [Test]
        public void CreateNew_HasBorderWallsOneWorldAndPlayer()
        {
            Assert.AreEqual(LevelDraft.DefaultWidth, _draft.Width);
            Assert.AreEqual(LevelDraft.DefaultHeight, _draft.Height);
            Assert.AreEqual(1, _draft.WorldCount);
            Assert.AreEqual(SharedCell.Wall, _draft.GetSharedCell(new GridPos(0, 0)));
            Assert.AreEqual(SharedCell.Empty, _draft.GetSharedCell(new GridPos(1, 1)));
            Assert.AreEqual(new GridPos(1, 1), _draft.PlayerPosition);
        }

        [Test]
        public void NewDraft_OnlyFailsValidationForMissingGoal()
        {
            LevelValidationReport report = LevelValidator.Validate(_draft.ToLevelData());

            Assert.AreEqual(1, report.ErrorCount, string.Join("\n", report.Issues.Select(i => i.Message)));
            StringAssert.Contains("goal", report.Issues[0].Message);
        }

        [Test]
        public void TerrainTools_SetSharedCell()
        {
            var pos = new GridPos(3, 3);

            Assert.IsTrue(_draft.Apply(EditorTool.Pedestal, 0, pos, -1));
            Assert.AreEqual(SharedCell.Pedestal, _draft.GetSharedCell(pos));
            Assert.IsFalse(_draft.Apply(EditorTool.Pedestal, 0, pos, -1), "重复绘制同一种地形不算变化");
            Assert.IsTrue(_draft.Apply(EditorTool.Floor, 0, pos, -1));
            Assert.AreEqual(SharedCell.Empty, _draft.GetSharedCell(pos));
        }

        [Test]
        public void UniqueWall_OnlyAffectsCurrentWorld()
        {
            _draft.AddWorld();
            var pos = new GridPos(4, 4);

            _draft.Apply(EditorTool.UniqueWall, 1, pos, -1);

            Assert.IsTrue(_draft.IsUniqueWall(1, pos));
            Assert.IsFalse(_draft.IsUniqueWall(0, pos));
        }

        [Test]
        public void MappedBox_PlacedOncePerCell()
        {
            var pos = new GridPos(3, 2);

            Assert.IsTrue(_draft.Apply(EditorTool.MappedBox, 0, pos, -1));
            Assert.IsFalse(_draft.Apply(EditorTool.MappedBox, 0, pos, -1));
            Assert.AreEqual(1, _draft.MappedBoxes.Count);
        }

        [Test]
        public void Player_PlacingSetsStartWorld_ClickingAgainRotates()
        {
            _draft.AddWorld();
            var pos = new GridPos(2, 2);

            _draft.Apply(EditorTool.Player, 1, pos, -1);
            Assert.AreEqual(1, _draft.StartWorld);
            Assert.AreEqual(pos, _draft.PlayerPosition);
            Direction before = _draft.PlayerFacing;

            _draft.Apply(EditorTool.Player, 1, pos, -1);

            Assert.AreEqual((Direction)(((int)before + 1) % 4), _draft.PlayerFacing);
        }

        [Test]
        public void Portal_EachSidePlacedOnceAndMovesOnSecondPlacement()
        {
            _draft.AddWorld();
            int pair = _draft.AddPair(0, 1);

            Assert.IsTrue(_draft.Apply(EditorTool.PortalBox, 0, new GridPos(2, 2), pair));
            Assert.IsTrue(_draft.Apply(EditorTool.PortalBox, 0, new GridPos(4, 4), pair));

            Assert.AreEqual(1, _draft.Portals.Count);
            Assert.AreEqual(new GridPos(4, 4), _draft.Portals[0].Position);

            Assert.IsTrue(_draft.Apply(EditorTool.PortalBox, 1, new GridPos(2, 2), pair));
            Assert.AreEqual(2, _draft.Portals.Count, "另一侧在另一个世界单独存在");
        }

        [Test]
        public void Portal_RejectedForWorldOutsidePairOrOccupiedCell()
        {
            _draft.AddWorld();
            _draft.AddWorld();
            int pair01 = _draft.AddPair(0, 1);
            int pair02 = _draft.AddPair(0, 2);
            var pos = new GridPos(3, 3);

            Assert.IsFalse(_draft.Apply(EditorTool.PortalBox, 2, pos, pair01), "世界 2 不属于配对 0-1");
            Assert.IsTrue(_draft.Apply(EditorTool.PortalBox, 0, pos, pair01));
            Assert.IsFalse(_draft.Apply(EditorTool.PortalBox, 0, pos, pair02), "同一格已有别的传送箱子");
            Assert.IsFalse(_draft.Apply(EditorTool.PortalBox, 0, pos, 9), "不存在的配对");
        }

        [Test]
        public void Eraser_ClearsEverythingToFloorButKeepsPlayer()
        {
            _draft.AddWorld();
            int pair = _draft.AddPair(0, 1);
            var pos = new GridPos(3, 3);
            _draft.Apply(EditorTool.Goal, 0, pos, -1);
            _draft.Apply(EditorTool.MappedBox, 0, pos, -1);
            _draft.Apply(EditorTool.UniqueWall, 1, pos, -1);
            _draft.Apply(EditorTool.PortalBox, 1, pos, pair);

            Assert.IsTrue(_draft.Apply(EditorTool.Eraser, 1, pos, -1));

            Assert.AreEqual(-1, _draft.FindMappedBox(pos));
            Assert.AreEqual(-1, _draft.FindPortal(1, pos));
            Assert.IsFalse(_draft.IsUniqueWall(1, pos));
            Assert.AreEqual(SharedCell.Empty, _draft.GetSharedCell(pos), "地形也被清成地面");

            GridPos player = _draft.PlayerPosition;
            _draft.Apply(EditorTool.Eraser, 0, player, -1);
            Assert.AreEqual(player, _draft.PlayerPosition, "玩家不会被擦除");
        }

        [Test]
        public void AddWorld_StopsAtLimit()
        {
            for (int i = 1; i < LevelLimits.MaxWorldCount; i++)
            {
                Assert.AreEqual(i, _draft.AddWorld());
            }

            Assert.AreEqual(-1, _draft.AddWorld());
            Assert.AreEqual(LevelLimits.MaxWorldCount, _draft.WorldCount);
        }

        [Test]
        public void RemoveWorld_DropsItsPairsAndShiftsLaterIndices()
        {
            _draft.AddWorld();
            _draft.AddWorld();
            _draft.AddPair(0, 1);
            int pair02 = _draft.AddPair(0, 2);
            _draft.Apply(EditorTool.PortalBox, 2, new GridPos(4, 4), pair02);
            _draft.Apply(EditorTool.UniqueWall, 2, new GridPos(5, 5), -1);
            _draft.Apply(EditorTool.Player, 2, new GridPos(2, 2), -1);

            Assert.IsTrue(_draft.RemoveWorld(1));

            Assert.AreEqual(2, _draft.WorldCount);
            Assert.AreEqual(1, _draft.Pairs.Count, "引用世界 1 的配对被删除");
            Assert.AreEqual(new DraftPair(0, 1), _draft.Pairs[0], "原世界 2 变为世界 1");
            Assert.AreEqual(0, _draft.Portals[0].Pair, "配对编号前移");
            Assert.AreEqual(1, _draft.Portals[0].World);
            Assert.IsTrue(_draft.IsUniqueWall(1, new GridPos(5, 5)));
            Assert.AreEqual(1, _draft.StartWorld);
            Assert.AreEqual(WorldTheme.Autumn, _draft.GetWorldTheme(1), "删除世界不改变其他世界的主题");
            Assert.AreEqual("Autumn", _draft.GetWorldName(1), "世界名称跟随主题");
        }

        [Test]
        public void RemoveWorld_ContainingPlayerMovesStartToFirstWorld()
        {
            _draft.AddWorld();
            _draft.Apply(EditorTool.Player, 1, new GridPos(2, 2), -1);

            _draft.RemoveWorld(1);

            Assert.AreEqual(0, _draft.StartWorld);
        }

        [Test]
        public void RemoveWorld_LastWorldIsRejected()
        {
            Assert.IsFalse(_draft.RemoveWorld(0));
        }

        [Test]
        public void AddPair_RejectsSameOrMissingWorld()
        {
            _draft.AddWorld();

            Assert.AreEqual(-1, _draft.AddPair(1, 1));
            Assert.AreEqual(-1, _draft.AddPair(0, 5));
            Assert.AreEqual(0, _draft.AddPair(1, 0));
        }

        [Test]
        public void AddPair_StopsAtLimit()
        {
            _draft.AddWorld();
            for (int i = 0; i < LevelLimits.MaxPairCount; i++)
            {
                Assert.AreEqual(i, _draft.AddPair(0, 1));
            }

            Assert.AreEqual(-1, _draft.AddPair(0, 1));
            Assert.AreEqual(LevelLimits.MaxPairCount, _draft.Pairs.Count);
        }

        [Test]
        public void RemovePair_RemovesItsPortalsAndRenumbersLater()
        {
            _draft.AddWorld();
            int first = _draft.AddPair(0, 1);
            int second = _draft.AddPair(0, 1);
            _draft.Apply(EditorTool.PortalBox, 0, new GridPos(2, 2), first);
            _draft.Apply(EditorTool.PortalBox, 0, new GridPos(4, 4), second);

            Assert.IsTrue(_draft.RemovePair(first));

            Assert.AreEqual(1, _draft.Pairs.Count);
            Assert.AreEqual(1, _draft.Portals.Count);
            Assert.AreEqual(0, _draft.Portals[0].Pair);
            Assert.AreEqual(new GridPos(4, 4), _draft.Portals[0].Position);
        }

        [Test]
        public void Resize_KeepsBottomLeftAndDropsOutOfBounds()
        {
            _draft.Apply(EditorTool.MappedBox, 0, new GridPos(7, 5), -1);
            _draft.Apply(EditorTool.MappedBox, 0, new GridPos(2, 2), -1);
            _draft.Apply(EditorTool.Player, 0, new GridPos(7, 5), -1);

            Assert.IsTrue(_draft.Resize(5, 4));

            Assert.AreEqual(5, _draft.Width);
            Assert.AreEqual(4, _draft.Height);
            Assert.AreEqual(1, _draft.MappedBoxes.Count);
            Assert.AreEqual(new GridPos(2, 2), _draft.MappedBoxes[0]);
            Assert.AreEqual(new GridPos(4, 3), _draft.PlayerPosition, "玩家被挪回边界内");
            Assert.AreEqual(SharedCell.Wall, _draft.GetSharedCell(new GridPos(0, 0)), "左下角内容保留");
        }

        [Test]
        public void Resize_IsClampedToLimits()
        {
            _draft.Resize(100, 1);

            Assert.AreEqual(LevelLimits.MaxWidth, _draft.Width);
            Assert.AreEqual(LevelLimits.MinHeight, _draft.Height);
            Assert.IsFalse(_draft.Resize(100, 1), "已经是限制值，不算变化");
        }

        [Test]
        public void RoundTrip_ThroughLevelData_PreservesContent()
        {
            LevelData original = TestLevels.MultiWorld(
                new[] { "#########", "#  P  . #", "# $     #", "#@      #", "#########" },
                new[]
                {
                    new[] { "         ", "   a     ", "         ", "    x    ", "         " },
                    new[] { "         ", "      b  ", "         ", "         ", "         " },
                    new[] { "         ", "         ", "  x      ", "         ", "         " },
                },
                new[] { new[] { 0, 1 }, new[] { 1, 2 } },
                Direction.Up,
                startWorld: 0);

            LevelData restored = LevelDraft.FromLevelData(original).ToLevelData();

            // 全空的独有层会被省略，写法不同但含义相同，所以比较运行时解析结果
            var originalMap = new LevelMap(original);
            var restoredMap = new LevelMap(restored);
            Assert.AreEqual(
                new LevelRules(originalMap).CreateInitialState(),
                new LevelRules(restoredMap).CreateInitialState());
            for (int world = 0; world < originalMap.WorldCount; world++)
            {
                for (int y = 0; y < originalMap.Height; y++)
                {
                    for (int x = 0; x < originalMap.Width; x++)
                    {
                        var pos = new GridPos(x, y);
                        Assert.AreEqual(originalMap.IsUniqueWall(world, pos), restoredMap.IsUniqueWall(world, pos), $"世界 {world} {pos}");
                        Assert.AreEqual(originalMap.IsSharedWall(pos), restoredMap.IsSharedWall(pos));
                        Assert.AreEqual(originalMap.IsPedestal(pos), restoredMap.IsPedestal(pos));
                        Assert.AreEqual(originalMap.IsGoal(pos), restoredMap.IsGoal(pos));
                    }
                }
            }

            // 第二次往返后文本完全一致
            LevelData again = LevelDraft.FromLevelData(restored).ToLevelData();
            LevelDataAssert.AreEqual(restored, again);
        }

        [Test]
        public void FromLevelData_InvalidStructure_Throws()
        {
            var broken = new LevelDataBuilder { SharedRows = new[] { "#" } }.Build();

            Assert.Throws<System.ArgumentException>(() => LevelDraft.FromLevelData(broken));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            LevelDraft copy = _draft.Clone();

            _draft.Apply(EditorTool.MappedBox, 0, new GridPos(3, 3), -1);
            _draft.AddWorld();

            Assert.AreEqual(0, copy.MappedBoxes.Count);
            Assert.AreEqual(1, copy.WorldCount);
        }

        [Test]
        public void DraftHistory_UndoRestoresSnapshotsInReverseOrder()
        {
            var history = new DraftHistory();
            history.Push(_draft);
            _draft.Apply(EditorTool.MappedBox, 0, new GridPos(3, 3), -1);
            history.Push(_draft);
            _draft.Apply(EditorTool.MappedBox, 0, new GridPos(4, 4), -1);

            Assert.IsTrue(history.TryUndo(out LevelDraft previous));
            Assert.AreEqual(1, previous.MappedBoxes.Count);
            Assert.IsTrue(history.TryUndo(out LevelDraft first));
            Assert.AreEqual(0, first.MappedBoxes.Count);
            Assert.IsFalse(history.TryUndo(out _));
        }

        [Test]
        public void DraftHistory_DropsOldestBeyondCapacity()
        {
            var history = new DraftHistory(2);
            for (int i = 0; i < 3; i++)
            {
                _draft.Apply(EditorTool.MappedBox, 0, new GridPos(2 + i, 2), -1);
                history.Push(_draft);
            }

            Assert.AreEqual(2, history.Count);
            history.TryUndo(out _);
            history.TryUndo(out LevelDraft oldestKept);
            Assert.AreEqual(2, oldestKept.MappedBoxes.Count);
        }
    }
}
