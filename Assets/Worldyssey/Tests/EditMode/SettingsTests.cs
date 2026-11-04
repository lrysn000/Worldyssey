using System.IO;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class SettingsTests
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
        public void Defaults_FullMasterAndFullscreenWithoutResolution()
        {
            var data = new SettingsData();

            Assert.AreEqual(1f, data.MasterVolume);
            Assert.AreEqual(SettingsData.DefaultChannelVolume, data.MusicVolume);
            Assert.AreEqual(SettingsData.DefaultChannelVolume, data.SfxVolume);
            Assert.IsTrue(data.IsFullscreen);
            Assert.IsFalse(data.HasResolution);
        }

        [Test]
        public void Volumes_AreClampedToZeroOne()
        {
            var data = new SettingsData();

            data.SetMasterVolume(2f);
            data.SetMusicVolume(-1f);

            Assert.AreEqual(1f, data.MasterVolume);
            Assert.AreEqual(0f, data.MusicVolume);
        }

        [Test]
        public void InvalidResolution_MeansUseSystemResolution()
        {
            var data = new SettingsData();
            data.SetResolution(1920, 1080);
            Assert.IsTrue(data.HasResolution);

            data.SetResolution(0, 1080);

            Assert.IsFalse(data.HasResolution);
            Assert.AreEqual(0, data.ResolutionWidth);
        }

        [Test]
        public void MissingFile_ReturnsDefaultsWithoutError()
        {
            SettingsData data = _service.LoadSettingsOrDefault(out string error);

            Assert.IsNotNull(data);
            Assert.AreEqual(string.Empty, error);
            Assert.AreEqual(1f, data.MasterVolume);
        }

        [Test]
        public void SaveThenLoad_PreservesAllValues()
        {
            var data = new SettingsData();
            data.SetMasterVolume(0.5f);
            data.SetMusicVolume(0.25f);
            data.SetSfxVolume(0.75f);
            data.SetFullscreen(false);
            data.SetResolution(1280, 720);

            _service.SaveSettings(data);
            SettingsData loaded = _service.LoadSettingsOrDefault(out string error);

            Assert.AreEqual(string.Empty, error);
            Assert.AreEqual(0.5f, loaded.MasterVolume);
            Assert.AreEqual(0.25f, loaded.MusicVolume);
            Assert.AreEqual(0.75f, loaded.SfxVolume);
            Assert.IsFalse(loaded.IsFullscreen);
            Assert.AreEqual(1280, loaded.ResolutionWidth);
            Assert.AreEqual(720, loaded.ResolutionHeight);
        }

        [Test]
        public void SettingsAndProgress_AreSeparateFiles()
        {
            _service.SaveSettings(new SettingsData());

            Assert.IsTrue(File.Exists(_service.SettingsFilePath));
            Assert.IsFalse(File.Exists(_service.SaveFilePath), "保存设置不应该创建进度存档");
        }

        [Test]
        public void CorruptFile_FallsBackToDefaultsAndBacksUp()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(_service.SettingsFilePath, "{ not json");

            SettingsData data = _service.LoadSettingsOrDefault(out string error);

            Assert.IsNotNull(data);
            Assert.IsNotEmpty(error);
            Assert.IsTrue(File.Exists(_service.SettingsFilePath + ".corrupt"));
        }
    }
}
