using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class InputCommandSchedulerTests
    {
        private const float RepeatDelay = 0.2f;

        private InputCommandScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new InputCommandScheduler(RepeatDelay);
        }

        [Test]
        public void PressWhenIdle_ExecutesImmediately()
        {
            _scheduler.Press(Direction.Right, 0f);

            Assert.IsTrue(_scheduler.TryGetNextCommand(0f, false, out InputCommand command));
            Assert.AreEqual(InputCommand.Move(Direction.Right), command);
        }

        [Test]
        public void NothingPressed_ReturnsFalse()
        {
            Assert.IsFalse(_scheduler.TryGetNextCommand(5f, false, out _));
        }

        [Test]
        public void PressWhileBusy_IsBufferedUntilIdle()
        {
            _scheduler.Press(Direction.Up, 0f);
            _scheduler.Release(Direction.Up);

            Assert.IsFalse(_scheduler.TryGetNextCommand(0.05f, true, out _));
            Assert.IsTrue(_scheduler.TryGetNextCommand(0.1f, false, out InputCommand command));
            Assert.AreEqual(InputCommand.Move(Direction.Up), command);
            Assert.IsFalse(_scheduler.TryGetNextCommand(0.11f, false, out _));
        }

        [Test]
        public void TwoPressesWhileBusy_OnlyLastIsKept()
        {
            _scheduler.Press(Direction.Up, 0f);
            _scheduler.Release(Direction.Up);
            _scheduler.Press(Direction.Left, 0.02f);
            _scheduler.Release(Direction.Left);

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.1f, false, out InputCommand command));
            Assert.AreEqual(InputCommand.Move(Direction.Left), command);
            Assert.IsFalse(_scheduler.TryGetNextCommand(0.11f, false, out _));
        }

        [Test]
        public void HoldShorterThanDelay_DoesNotRepeat()
        {
            _scheduler.Press(Direction.Down, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);

            Assert.IsFalse(_scheduler.TryGetNextCommand(0.15f, false, out _));
        }

        [Test]
        public void HoldLongerThanDelay_RepeatsWheneverIdle()
        {
            _scheduler.Press(Direction.Down, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.25f, false, out InputCommand first));
            Assert.AreEqual(InputCommand.Move(Direction.Down), first);
            Assert.IsFalse(_scheduler.TryGetNextCommand(0.3f, true, out _));
            Assert.IsTrue(_scheduler.TryGetNextCommand(0.4f, false, out InputCommand second));
            Assert.AreEqual(InputCommand.Move(Direction.Down), second);
        }

        [Test]
        public void Release_StopsRepeat()
        {
            _scheduler.Press(Direction.Down, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);
            _scheduler.Release(Direction.Down);

            Assert.IsFalse(_scheduler.TryGetNextCommand(1f, false, out _));
        }

        [Test]
        public void MultipleHeld_LastPressedWins_ReleaseFallsBack()
        {
            _scheduler.Press(Direction.Right, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);
            _scheduler.Press(Direction.Up, 0.5f);
            _scheduler.TryGetNextCommand(0.5f, false, out _);

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.8f, false, out InputCommand whileBothHeld));
            Assert.AreEqual(InputCommand.Move(Direction.Up), whileBothHeld);

            _scheduler.Release(Direction.Up);

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.9f, false, out InputCommand afterRelease));
            Assert.AreEqual(InputCommand.Move(Direction.Right), afterRelease);
        }

        [Test]
        public void NewPress_RestartsRepeatTimerForThatDirection()
        {
            _scheduler.Press(Direction.Right, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);
            _scheduler.Release(Direction.Right);
            _scheduler.Press(Direction.Right, 1f);
            _scheduler.TryGetNextCommand(1f, false, out _);

            Assert.IsFalse(_scheduler.TryGetNextCommand(1.1f, false, out _));
            Assert.IsTrue(_scheduler.TryGetNextCommand(1.25f, false, out _));
        }

        [Test]
        public void ClearBuffer_DropsBufferedButKeepsHold()
        {
            _scheduler.Press(Direction.Left, 0f);
            _scheduler.ClearBuffer();

            Assert.IsFalse(_scheduler.TryGetNextCommand(0.1f, false, out _));
            Assert.IsTrue(_scheduler.TryGetNextCommand(0.3f, false, out InputCommand command));
            Assert.AreEqual(InputCommand.Move(Direction.Left), command);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            _scheduler.Press(Direction.Left, 0f);
            _scheduler.Reset();

            Assert.IsFalse(_scheduler.TryGetNextCommand(1f, false, out _));
        }

        [Test]
        public void NegativeDelay_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InputCommandScheduler(-1f));
        }

        [Test]
        public void TeleportWhenIdle_ExecutesImmediately()
        {
            _scheduler.PressTeleport();

            Assert.IsTrue(_scheduler.TryGetNextCommand(0f, false, out InputCommand command));
            Assert.AreEqual(InputCommandType.Teleport, command.Type);
            Assert.IsFalse(_scheduler.TryGetNextCommand(0.01f, false, out _));
        }

        [Test]
        public void TeleportWhileBusy_IsBuffered()
        {
            _scheduler.PressTeleport();

            Assert.IsFalse(_scheduler.TryGetNextCommand(0f, true, out _));
            Assert.IsTrue(_scheduler.TryGetNextCommand(0.2f, false, out InputCommand command));
            Assert.AreEqual(InputCommand.Teleport(), command);
        }

        [Test]
        public void BufferIsShared_LatestOfMoveAndTeleportWins()
        {
            _scheduler.Press(Direction.Up, 0f);
            _scheduler.Release(Direction.Up);
            _scheduler.PressTeleport();

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.1f, false, out InputCommand first));
            Assert.AreEqual(InputCommand.Teleport(), first);
            Assert.IsFalse(_scheduler.TryGetNextCommand(0.11f, false, out _));

            _scheduler.PressTeleport();
            _scheduler.Press(Direction.Left, 0.2f);
            _scheduler.Release(Direction.Left);

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.3f, false, out InputCommand second));
            Assert.AreEqual(InputCommand.Move(Direction.Left), second);
        }

        [Test]
        public void HeldDirectionResumesAfterBufferedTeleport()
        {
            _scheduler.Press(Direction.Right, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);
            _scheduler.PressTeleport();

            Assert.IsTrue(_scheduler.TryGetNextCommand(0.5f, false, out InputCommand first));
            Assert.AreEqual(InputCommand.Teleport(), first);
            Assert.IsTrue(_scheduler.TryGetNextCommand(0.6f, false, out InputCommand second));
            Assert.AreEqual(InputCommand.Move(Direction.Right), second);
        }

        [Test]
        public void LastCommandWasRepeat_DistinguishesPressFromHold()
        {
            _scheduler.Press(Direction.Up, 0f);
            _scheduler.TryGetNextCommand(0f, false, out _);
            Assert.IsFalse(_scheduler.LastCommandWasRepeat, "按下产生的命令");

            _scheduler.TryGetNextCommand(0.5f, false, out _);
            Assert.IsTrue(_scheduler.LastCommandWasRepeat, "按住产生的命令");

            _scheduler.PressTeleport();
            _scheduler.TryGetNextCommand(0.6f, false, out _);
            Assert.IsFalse(_scheduler.LastCommandWasRepeat);
        }
    }
}
