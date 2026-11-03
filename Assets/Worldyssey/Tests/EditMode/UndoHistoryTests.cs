using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// 按 LevelSession 将来的用法组合规则和撤销历史：成功一步才压快照，步数等于历史长度。
    /// </summary>
    public class UndoHistoryTests
    {
        private LevelRules _rules;
        private UndoHistory _history;
        private LevelState _state;

        [SetUp]
        public void SetUp()
        {
            _rules = TestLevels.RulesFromXsb(Direction.Right,
                "#######",
                "#@$  .#",
                "#######");
            _history = new UndoHistory();
            _state = _rules.CreateInitialState();
        }

        [Test]
        public void Undo_RestoresPreviousState()
        {
            LevelState before = _state;
            Step(Direction.Right);

            Assert.IsTrue(_history.TryUndo(out LevelState restored));

            Assert.AreEqual(before, restored);
            Assert.AreEqual(0, _history.Count);
        }

        [Test]
        public void StepCount_FollowsSuccessfulStepsOnly()
        {
            Step(Direction.Right);
            Step(Direction.Up); // 撞墙，只转向
            Step(Direction.Right);

            Assert.AreEqual(2, _history.Count);
        }

        [Test]
        public void UndoRepeatedly_ReturnsToStartThenFails()
        {
            LevelState start = _state;
            Step(Direction.Right);
            Step(Direction.Right);

            Assert.IsTrue(_history.TryUndo(out _));
            Assert.IsTrue(_history.TryUndo(out LevelState restored));
            Assert.AreEqual(start, restored);

            Assert.IsFalse(_history.TryUndo(out LevelState none));
            Assert.IsNull(none);
        }

        [Test]
        public void Restart_ClearsHistoryAndResetsState()
        {
            Step(Direction.Right);
            Step(Direction.Right);

            _history.Clear();
            _state = _rules.CreateInitialState();

            Assert.AreEqual(0, _history.Count);
            Assert.AreEqual(new GridPos(1, 1), _state.PlayerPosition);
            Assert.AreEqual(new GridPos(2, 1), _state.MappedBoxes[0]);
            Assert.IsFalse(_history.TryUndo(out _));
        }

        [Test]
        public void Snapshots_AreNotAffectedByLaterSteps()
        {
            Step(Direction.Right);
            LevelState afterFirst = _state;
            LevelState afterFirstCopy = afterFirst.Clone();
            Step(Direction.Right);
            Step(Direction.Right);

            _history.TryUndo(out _);
            _history.TryUndo(out LevelState restored);

            Assert.AreSame(afterFirst, restored);
            Assert.AreEqual(afterFirstCopy, restored);
        }

        [Test]
        public void Push_Null_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => _history.Push(null));
        }

        private void Step(Direction direction)
        {
            StepResult result = _rules.TryMove(_state, direction);
            if (result.IsStepTaken)
            {
                _history.Push(_state);
            }

            _state = result.State;
        }
    }
}
