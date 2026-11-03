using System.Collections.Generic;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class PlaySessionTests
    {
        private PlaySession _session;
        private List<StepResult> _appliedResults;
        private int _restoredCount;

        [SetUp]
        public void SetUp()
        {
            // 向右推两次即可通关
            LevelRules rules = TestLevels.RulesFromXsb(Direction.Right,
                "######",
                "#@$ .#",
                "#    #",
                "######");
            _session = new PlaySession(rules);
            _appliedResults = new List<StepResult>();
            _restoredCount = 0;
            _session.StepApplied += result => _appliedResults.Add(result);
            _session.StateRestored += () => _restoredCount++;
        }

        [Test]
        public void NewSession_StartsAtInitialState()
        {
            Assert.AreEqual(0, _session.StepCount);
            Assert.IsFalse(_session.IsSolved);
            Assert.AreEqual(_session.Rules.CreateInitialState(), _session.State);
        }

        [Test]
        public void SuccessfulMove_CountsStepAndRaisesEvent()
        {
            bool taken = _session.TryMove(Direction.Down);

            Assert.IsTrue(taken);
            Assert.AreEqual(1, _session.StepCount);
            Assert.AreEqual(1, _appliedResults.Count);
            Assert.AreSame(_appliedResults[0].State, _session.State);
        }

        [Test]
        public void BlockedMove_DoesNotCountButStillAppliesTurn()
        {
            bool taken = _session.TryMove(Direction.Up);

            Assert.IsFalse(taken);
            Assert.AreEqual(0, _session.StepCount);
            Assert.AreEqual(Direction.Up, _session.State.PlayerFacing);
            Assert.AreEqual(1, _appliedResults.Count);
        }

        [Test]
        public void Undo_RestoresPreviousStateAndStepCount()
        {
            LevelState start = _session.State;
            _session.TryMove(Direction.Right);

            bool undone = _session.Undo();

            Assert.IsTrue(undone);
            Assert.AreEqual(0, _session.StepCount);
            Assert.AreEqual(start, _session.State);
            Assert.AreEqual(1, _restoredCount);
        }

        [Test]
        public void Undo_WithoutHistory_ReturnsFalseAndRaisesNothing()
        {
            Assert.IsFalse(_session.Undo());
            Assert.AreEqual(0, _restoredCount);
        }

        [Test]
        public void Restart_ResetsStateAndClearsHistory()
        {
            _session.TryMove(Direction.Right);
            _session.TryMove(Direction.Down);

            _session.Restart();

            Assert.AreEqual(0, _session.StepCount);
            Assert.AreEqual(_session.Rules.CreateInitialState(), _session.State);
            Assert.IsFalse(_session.Undo());
            Assert.AreEqual(1, _restoredCount);
        }

        [Test]
        public void AfterSolved_MovementIsLocked()
        {
            _session.TryMove(Direction.Right);
            _session.TryMove(Direction.Right);
            Assert.IsTrue(_session.IsSolved);
            LevelState solvedState = _session.State;

            bool taken = _session.TryMove(Direction.Left);

            Assert.IsFalse(taken);
            Assert.AreSame(solvedState, _session.State);
            Assert.AreEqual(2, _session.StepCount);
            Assert.AreEqual(2, _appliedResults.Count);
        }

        [Test]
        public void AfterSolved_UndoUnlocksMovement()
        {
            _session.TryMove(Direction.Right);
            _session.TryMove(Direction.Right);

            _session.Undo();

            Assert.IsFalse(_session.IsSolved);
            Assert.IsTrue(_session.TryMove(Direction.Left));
        }

        [Test]
        public void AfterSolved_RestartUnlocksMovement()
        {
            _session.TryMove(Direction.Right);
            _session.TryMove(Direction.Right);

            _session.Restart();

            Assert.IsFalse(_session.IsSolved);
            Assert.IsTrue(_session.TryMove(Direction.Down));
        }
    }
}
