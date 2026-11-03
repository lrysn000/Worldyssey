using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class ProgressTrackerTests
    {
        private static readonly string[] Catalog = { "a", "b", "c" };

        private ProgressTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new ProgressTracker(new SaveData());
        }

        [Test]
        public void FirstCompletion_IsRecordedAsBest()
        {
            Assert.IsTrue(_tracker.RecordCompletion("a", 20));

            Assert.IsTrue(_tracker.IsCompleted("a"));
            Assert.IsTrue(_tracker.TryGetBestSteps("a", out int best));
            Assert.AreEqual(20, best);
        }

        [Test]
        public void FewerSteps_ReplacesBest()
        {
            _tracker.RecordCompletion("a", 20);

            Assert.IsTrue(_tracker.RecordCompletion("a", 15));

            _tracker.TryGetBestSteps("a", out int best);
            Assert.AreEqual(15, best);
        }

        [Test]
        public void SameOrMoreSteps_KeepsBest()
        {
            _tracker.RecordCompletion("a", 20);

            Assert.IsFalse(_tracker.RecordCompletion("a", 20));
            Assert.IsFalse(_tracker.RecordCompletion("a", 30));

            _tracker.TryGetBestSteps("a", out int best);
            Assert.AreEqual(20, best);
            Assert.AreEqual(1, _tracker.Data.Records.Count);
        }

        [Test]
        public void NotCompleted_HasNoBest()
        {
            Assert.IsFalse(_tracker.IsCompleted("a"));
            Assert.IsFalse(_tracker.TryGetBestSteps("a", out _));
        }

        [Test]
        public void Unlock_IsLinear()
        {
            Assert.IsTrue(_tracker.IsUnlocked(Catalog, 0));
            Assert.IsFalse(_tracker.IsUnlocked(Catalog, 1));
            Assert.IsFalse(_tracker.IsUnlocked(Catalog, 2));

            _tracker.RecordCompletion("a", 10);

            Assert.IsTrue(_tracker.IsUnlocked(Catalog, 1));
            Assert.IsFalse(_tracker.IsUnlocked(Catalog, 2));
        }

        [Test]
        public void Unlock_OutOfRange_IsFalse()
        {
            Assert.IsFalse(_tracker.IsUnlocked(Catalog, -1));
            Assert.IsFalse(_tracker.IsUnlocked(Catalog, 3));
        }

        [Test]
        public void CustomLevels_DoNotAffectBuiltInUnlock()
        {
            _tracker.RecordCompletion(ProgressTracker.CustomKey("a"), 5);

            Assert.IsFalse(_tracker.IsUnlocked(Catalog, 1));
            Assert.IsFalse(_tracker.IsCompleted(ProgressTracker.BuiltInKey("a")));
            Assert.IsTrue(_tracker.IsCompleted(ProgressTracker.CustomKey("a")));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<System.ArgumentException>(() => _tracker.RecordCompletion("", 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _tracker.RecordCompletion("a", -1));
        }
    }
}
