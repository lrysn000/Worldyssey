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
    
        [Test]
        public void Teleport_CanBeUndoneButDoesNotCountAsStep()
        {
            // 玩家在底座左边面朝右，世界 0 的传送箱子已在底座上，通往世界 1
            LevelRules rules = TestLevels.RulesMultiWorld(
                new[] { "#######", "#@P   #", "#     #", "#######" },
                new[] { new[] { "       ", "  a    ", "       ", "       " }, null },
                new[] { new[] { 0, 1 } });
            var session = new PlaySession(rules);

            Assert.IsTrue(session.TryTeleport());
            Assert.AreEqual(1, session.State.PlayerWorld);
            Assert.AreEqual(0, session.StepCount, "传送不计步");

            Assert.IsTrue(session.TryMove(Direction.Down));
            Assert.AreEqual(1, session.StepCount);

            Assert.IsTrue(session.Undo());
            Assert.AreEqual(0, session.StepCount);
            Assert.AreEqual(1, session.State.PlayerWorld);

            Assert.IsTrue(session.Undo(), "传送仍可撤销");
            Assert.AreEqual(0, session.State.PlayerWorld);
            Assert.AreEqual(0, session.StepCount);
        }
    }
}
