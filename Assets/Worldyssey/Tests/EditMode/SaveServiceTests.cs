using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class SaveServiceTests
    {
        private string _root;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "WorldysseyTests_" + System.Guid.NewGuid().ToString("N"));
            _service = new SaveService(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void MissingFile_ReturnsFreshSaveWithoutError()
        {
            SaveData data = _service.LoadOrCreate(out string error);

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.Records.Count);
            Assert.AreEqual(string.Empty, error);
        }

        [Test]
        public void SaveThenLoad_PreservesRecords()
        {
            var tracker = new ProgressTracker(new SaveData());
            tracker.RecordCompletion("test-01", 17);
            tracker.RecordCompletion(ProgressTracker.CustomKey("mine"), 9);

            _service.Save(tracker.Data);
            SaveData loaded = _service.LoadOrCreate(out string error);

            Assert.AreEqual(string.Empty, error);
            var reloaded = new ProgressTracker(loaded);
            Assert.IsTrue(reloaded.TryGetBestSteps("test-01", out int builtInBest));
            Assert.AreEqual(17, builtInBest);
            Assert.IsTrue(reloaded.TryGetBestSteps(ProgressTracker.CustomKey("mine"), out int customBest));
            Assert.AreEqual(9, customBest);
        }

        [Test]
        public void SaveTwice_OverwritesAndLeavesNoTempFile()
        {
            var tracker = new ProgressTracker(new SaveData());
            tracker.RecordCompletion("a", 20);
            _service.Save(tracker.Data);
            tracker.RecordCompletion("a", 10);
            _service.Save(tracker.Data);

            new ProgressTracker(_service.LoadOrCreate(out _)).TryGetBestSteps("a", out int best);

            Assert.AreEqual(10, best);
            Assert.AreEqual(1, Directory.GetFiles(_root).Length, "只应留下存档文件本身");
        }

        [Test]
        public void CorruptFile_IsBackedUpAndFreshSaveReturned()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(_service.SaveFilePath, "{ this is not json");

            SaveData data = _service.LoadOrCreate(out string error);

            Assert.AreEqual(0, data.Records.Count);
            Assert.IsNotEmpty(error);
            Assert.IsFalse(File.Exists(_service.SaveFilePath));
            Assert.IsTrue(File.Exists(_service.SaveFilePath + ".corrupt"));
        }

        [Test]
        public void NewerVersion_IsBackedUpAndFreshSaveReturned()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(_service.SaveFilePath, "{ \"version\": 99, \"records\": [] }");

            SaveData data = _service.LoadOrCreate(out string error);

            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            StringAssert.Contains("99", error);
            Assert.IsTrue(File.Exists(_service.SaveFilePath + ".corrupt"));
        }

        [Test]
        public void CustomLevels_MissingDirectory_ReturnsEmpty()
        {
            var problems = new List<string>();

            Assert.AreEqual(0, _service.LoadCustomLevels(problems).Count);
            Assert.AreEqual(0, problems.Count);
        }

        [Test]
        public void CustomLevels_ValidFilesLoadedInvalidOnesReported()
        {
            Directory.CreateDirectory(_service.CustomLevelDirectory);
            LevelData valid = new LevelDataBuilder { Id = "mine-1" }.Build();
            File.WriteAllText(Path.Combine(_service.CustomLevelDirectory, "b_valid.json"), LevelJsonSerializer.ToJson(valid));
            File.WriteAllText(Path.Combine(_service.CustomLevelDirectory, "a_broken.json"), "{ nope");
            LevelData invalid = new LevelDataBuilder { Id = "" }.Build();
            File.WriteAllText(Path.Combine(_service.CustomLevelDirectory, "c_invalid.json"), LevelJsonSerializer.ToJson(invalid));
            File.WriteAllText(Path.Combine(_service.CustomLevelDirectory, "notes.txt"), "ignored");
            var problems = new List<string>();

            List<CustomLevelEntry> entries = _service.LoadCustomLevels(problems);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("mine-1", entries[0].Level.Id);
            Assert.AreEqual(2, problems.Count);
            StringAssert.Contains("a_broken.json", problems[0]);
            StringAssert.Contains("c_invalid.json", problems[1]);
        }

        [Test]
        public void SaveCustomLevel_ThenLoadCustomLevels_FindsIt()
        {
            LevelData level = new LevelDataBuilder { Id = "custom-1", DisplayName = "Mine" }.Build();

            string path = _service.SaveCustomLevel(level);
            var problems = new List<string>();
            List<CustomLevelEntry> entries = _service.LoadCustomLevels(problems);

            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual(0, problems.Count);
            Assert.AreEqual(1, entries.Count);
            LevelDataAssert.AreEqual(level, entries[0].Level);
        }

        [Test]
        public void SaveCustomLevel_SameIdOverwrites()
        {
            _service.SaveCustomLevel(new LevelDataBuilder { Id = "same", DisplayName = "First" }.Build());
            _service.SaveCustomLevel(new LevelDataBuilder { Id = "same", DisplayName = "Second" }.Build());

            List<CustomLevelEntry> entries = _service.LoadCustomLevels(new List<string>());

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Second", entries[0].Level.DisplayName);
        }

        [Test]
        public void DeleteCustomLevel_RemovesFile()
        {
            string path = _service.SaveCustomLevel(new LevelDataBuilder { Id = "gone", DisplayName = "Gone" }.Build());

            Assert.IsTrue(_service.DeleteCustomLevel(path));

            Assert.IsFalse(File.Exists(path));
            Assert.AreEqual(0, _service.LoadCustomLevels(new List<string>()).Count);
            Assert.IsFalse(_service.DeleteCustomLevel(path), "再次删除时文件已不存在");
        }

        [Test]
        public void DeleteCustomLevel_RejectsFilesOutsideLevelFolder()
        {
            _service.Save(new SaveData());

            Assert.Throws<System.ArgumentException>(() => _service.DeleteCustomLevel(_service.SaveFilePath));
            Assert.IsTrue(File.Exists(_service.SaveFilePath), "存档文件不应被删除");
        }

        [Test]
        public void ToFileName_ReplacesInvalidCharacters()
        {
            Assert.AreEqual("a_b", SaveService.ToFileName("a/b"));
            Assert.AreEqual("unnamed", SaveService.ToFileName(" "));
        }

        [Test]
        public void GenerateCustomLevelId_ContainsTimestamp()
        {
            Assert.AreEqual("custom-20261103-221500", SaveService.GenerateCustomLevelId(new System.DateTime(2026, 11, 3, 22, 15, 0)));
        }
    }
}
