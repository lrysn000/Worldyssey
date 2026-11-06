using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class LevelEditorModelTests
    {
        private LevelEditorModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new LevelEditorModel(LevelDraft.CreateNew("m", "Model"));
        }

        [Test]
        public void LineStroke_FollowsMouseAndShrinksBack()
        {
            _model.SelectTool(EditorTool.MappedBox);

            _model.BeginStroke(new GridPos(2, 2), false, true);
            _model.ContinueStroke(new GridPos(5, 3));
            Assert.AreEqual(4, _model.Draft.MappedBoxes.Count, "锁定到横向，从 x=2 到 x=5");

            _model.ContinueStroke(new GridPos(3, 2));
            _model.EndStroke();

            Assert.AreEqual(2, _model.Draft.MappedBoxes.Count, "终点移回来后多画的格子消失");
            Assert.IsFalse(_model.IsDrawingLine);
            Assert.AreEqual(1, _model.UndoCount, "一整条线是一步撤销");
            _model.Undo();
            Assert.AreEqual(0, _model.Draft.MappedBoxes.Count);
        }

        [Test]
        public void LineStroke_WithPlayerTool_PlacesOnce()
        {
            _model.SelectTool(EditorTool.Player);

            _model.BeginStroke(new GridPos(2, 2), false, true);
            _model.ContinueStroke(new GridPos(4, 2));
            _model.EndStroke();

            Assert.AreEqual(new GridPos(2, 2), _model.Draft.PlayerPosition);
        }

        [Test]
        public void Stroke_AcrossCells_IsOneUndoStep()
        {
            _model.SelectTool(EditorTool.MappedBox);

            Assert.AreEqual(EditorFeedback.GridChanged, _model.BeginStroke(new GridPos(2, 2), false));
            _model.ContinueStroke(new GridPos(3, 2));
            _model.ContinueStroke(new GridPos(4, 2));
            _model.EndStroke();

            Assert.AreEqual(3, _model.Draft.MappedBoxes.Count);
            Assert.AreEqual(1, _model.UndoCount);
            Assert.AreEqual(EditorFeedback.AllChanged, _model.Undo());
            Assert.AreEqual(0, _model.Draft.MappedBoxes.Count);
        }

        [Test]
        public void StrokeWithoutChange_LeavesNoUndoStep()
        {
            _model.SelectTool(EditorTool.SharedWall);

            Assert.AreEqual(EditorFeedback.None, _model.BeginStroke(new GridPos(0, 0), false), "边界本来就是墙");
            _model.EndStroke();

            Assert.AreEqual(0, _model.UndoCount);
            Assert.AreEqual(EditorFeedback.NothingToUndo, _model.Undo());
        }

        [Test]
        public void RightButtonStroke_Erases()
        {
            _model.SelectTool(EditorTool.MappedBox);
            _model.BeginStroke(new GridPos(2, 2), false);
            _model.EndStroke();

            _model.BeginStroke(new GridPos(2, 2), true);
            _model.EndStroke();

            Assert.AreEqual(0, _model.Draft.MappedBoxes.Count);
        }

        [Test]
        public void PlayerTool_OnlyActsOnFirstCellOfStroke()
        {
            _model.SelectTool(EditorTool.Player);

            _model.BeginStroke(new GridPos(3, 3), false);
            Assert.AreEqual(EditorFeedback.None, _model.ContinueStroke(new GridPos(4, 3)));
            _model.EndStroke();

            Assert.AreEqual(new GridPos(3, 3), _model.Draft.PlayerPosition);
        }

        [Test]
        public void PortalTool_WithoutPair_AsksToSelectPair()
        {
            _model.SelectTool(EditorTool.PortalBox);

            Assert.AreEqual(EditorFeedback.SelectPairFirst, _model.BeginStroke(new GridPos(3, 3), false));
            _model.EndStroke();
            Assert.AreEqual(0, _model.UndoCount);
        }

        [Test]
        public void AddPair_ConnectsCurrentAndTargetWorldAndSelectsIt()
        {
            Assert.AreEqual(EditorFeedback.PairRejected, _model.AddPair(), "只有一个世界时不能建配对");

            _model.AddWorld();
            Assert.AreEqual(1, _model.CurrentWorld, "新世界自动成为当前世界");
            Assert.AreEqual(0, _model.TargetWorld);

            Assert.AreEqual(EditorFeedback.AllChanged, _model.AddPair());

            Assert.AreEqual(0, _model.SelectedPair);
            Assert.AreEqual(EditorTool.PortalBox, _model.Tool);
            Assert.AreEqual(new DraftPair(1, 0), _model.Draft.Pairs[0]);
        }

        [Test]
        public void PortalPlacement_InWorldOutsidePair_IsRejected()
        {
            _model.AddWorld();
            _model.AddWorld();
            _model.SelectWorld(0);
            _model.CycleTargetWorld();
            Assert.AreEqual(2, _model.TargetWorld);
            _model.AddPair();

            _model.SelectWorld(1);

            Assert.AreEqual(EditorFeedback.PortalRejected, _model.BeginStroke(new GridPos(3, 3), false));
            _model.EndStroke();
        }

        [Test]
        public void TargetWorld_NeverEqualsCurrentWorld()
        {
            _model.AddWorld();
            _model.AddWorld();

            for (int i = 0; i < 5; i++)
            {
                _model.CycleTargetWorld();
                Assert.AreNotEqual(_model.CurrentWorld, _model.TargetWorld);
            }

            _model.SelectWorld(_model.TargetWorld);
            Assert.AreNotEqual(_model.CurrentWorld, _model.TargetWorld);
        }

        [Test]
        public void RemoveCurrentWorld_ClearsPairSelectionAndCanBeUndone()
        {
            _model.AddWorld();
            _model.AddPair();

            Assert.AreEqual(EditorFeedback.AllChanged, _model.RemoveCurrentWorld());

            Assert.AreEqual(1, _model.Draft.WorldCount);
            Assert.AreEqual(LevelEditorModel.NoPair, _model.SelectedPair);

            _model.Undo();
            Assert.AreEqual(2, _model.Draft.WorldCount);
            Assert.AreEqual(1, _model.Draft.Pairs.Count);
        }

        [Test]
        public void Resize_BeyondLimit_IsNoChange()
        {
            _model.Resize(LevelLimits.MaxWidth, 0);

            Assert.AreEqual(EditorFeedback.None, _model.Resize(1, 0));
            Assert.AreEqual(LevelLimits.MaxWidth, _model.Draft.Width);
        }

        [Test]
        public void Report_TracksEdits()
        {
            Assert.IsTrue(_model.Report.HasErrors, "新关卡没有终点");

            _model.SelectTool(EditorTool.Goal);
            _model.BeginStroke(new GridPos(5, 3), false);
            _model.EndStroke();
            _model.SelectTool(EditorTool.MappedBox);
            _model.BeginStroke(new GridPos(4, 3), false);
            _model.EndStroke();

            Assert.IsFalse(_model.Report.HasErrors);
        }

        [Test]
        public void Load_ResetsHistoryAndSelection()
        {
            _model.AddWorld();
            _model.AddPair();

            _model.Load(LevelDraft.CreateNew("other", "Other"));

            Assert.AreEqual(0, _model.UndoCount);
            Assert.AreEqual(LevelEditorModel.NoPair, _model.SelectedPair);
            Assert.AreEqual(0, _model.CurrentWorld);
            Assert.AreEqual("other", _model.Draft.Id);
        }
    }
}
