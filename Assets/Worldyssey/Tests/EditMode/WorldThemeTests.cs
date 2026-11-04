using System;
using System.Linq;
using NUnit.Framework;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    public class WorldThemeTests
    {
        [Test]
        public void NewDraft_FirstWorldIsSpring()
        {
            LevelDraft draft = LevelDraft.CreateNew("d", "D");

            Assert.AreEqual(WorldTheme.Spring, draft.GetWorldTheme(0));
            Assert.AreEqual("Spring", draft.GetWorldName(0));
        }

        [Test]
        public void AddWorld_TakesFirstUnusedTheme()
        {
            LevelDraft draft = LevelDraft.CreateNew("d", "D");
            draft.SetWorldTheme(0, WorldTheme.Summer);

            draft.AddWorld();
            draft.AddWorld();

            Assert.AreEqual(WorldTheme.Spring, draft.GetWorldTheme(1));
            Assert.AreEqual(WorldTheme.Autumn, draft.GetWorldTheme(2));
        }

        [Test]
        public void SetWorldTheme_UsedByOtherWorld_Swaps()
        {
            LevelDraft draft = LevelDraft.CreateNew("d", "D");
            draft.AddWorld();

            Assert.IsTrue(draft.SetWorldTheme(0, WorldTheme.Summer));

            Assert.AreEqual(WorldTheme.Summer, draft.GetWorldTheme(0));
            Assert.AreEqual(WorldTheme.Spring, draft.GetWorldTheme(1));
        }

        [Test]
        public void SetWorldTheme_Unused_OnlyChangesThatWorld()
        {
            LevelDraft draft = LevelDraft.CreateNew("d", "D");
            draft.AddWorld();

            Assert.IsTrue(draft.SetWorldTheme(1, WorldTheme.Winter));

            Assert.AreEqual(WorldTheme.Spring, draft.GetWorldTheme(0));
            Assert.AreEqual(WorldTheme.Winter, draft.GetWorldTheme(1));
        }

        [Test]
        public void SetWorldTheme_SameThemeOrInvalid_ReturnsFalse()
        {
            LevelDraft draft = LevelDraft.CreateNew("d", "D");

            Assert.IsFalse(draft.SetWorldTheme(0, WorldTheme.Spring));
            Assert.IsFalse(draft.SetWorldTheme(0, WorldTheme.Unset));
            Assert.IsFalse(draft.SetWorldTheme(5, WorldTheme.Winter));
        }

        [Test]
        public void RoundTrip_PreservesThemesAndNames()
        {
            LevelDraft draft = LevelDraft.CreateNew("d", "D");
            draft.AddWorld();
            draft.SetWorldTheme(0, WorldTheme.Winter);
            draft.SetWorldTheme(1, WorldTheme.Autumn);

            LevelData data = draft.ToLevelData();
            Assert.IsTrue(LevelJsonSerializer.TryFromJson(LevelJsonSerializer.ToJson(data), out LevelData loaded, out string error), error);
            var map = new LevelMap(loaded);

            Assert.AreEqual(WorldTheme.Winter, map.GetWorldTheme(0));
            Assert.AreEqual(WorldTheme.Autumn, map.GetWorldTheme(1));
            Assert.AreEqual("Winter", map.GetWorldName(0));
            Assert.AreEqual(WorldTheme.Winter, LevelDraft.FromLevelData(loaded).GetWorldTheme(0));
        }

        [Test]
        public void MissingTheme_FallsBackToWorldIndex()
        {
            var map = new LevelMap(new LevelDataBuilder().Build());

            Assert.AreEqual(WorldTheme.Spring, map.GetWorldTheme(0));
            Assert.AreEqual(WorldTheme.Summer, map.GetWorldTheme(1));
        }

        [Test]
        public void DuplicateTheme_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Worlds[0] = new WorldData("a", Array.Empty<string>(), "Winter");
            builder.Worlds[1] = new WorldData("b", Array.Empty<string>(), "Winter");

            LevelValidationReport report = LevelValidator.Validate(builder.Build());

            Assert.AreEqual(1, report.ErrorCount, Describe(report));
            StringAssert.Contains("both use theme", report.Issues.First(i => i.IsError).Message);
        }

        [Test]
        public void ExplicitThemeCollidingWithFallback_IsError()
        {
            // 世界 1 没写主题，回退为夏天，与世界 0 显式写的夏天冲突
            var builder = new LevelDataBuilder();
            builder.Worlds[0] = new WorldData("a", Array.Empty<string>(), "Summer");

            LevelValidationReport report = LevelValidator.Validate(builder.Build());

            Assert.IsTrue(report.Issues.Any(i => i.IsError && i.Message.Contains("both use theme")), Describe(report));
        }

        [Test]
        public void UnknownTheme_IsError()
        {
            var builder = new LevelDataBuilder();
            builder.Worlds[0] = new WorldData("a", Array.Empty<string>(), "Monsoon");

            LevelValidationReport report = LevelValidator.Validate(builder.Build());

            Assert.IsTrue(report.Issues.Any(i => i.IsError && i.Message.Contains("unknown theme")), Describe(report));
        }

        [Test]
        public void EditorModel_SetTheme_CanBeUndone()
        {
            var model = new LevelEditorModel(LevelDraft.CreateNew("m", "M"));
            model.AddWorld();
            model.SelectWorld(0);

            model.SetCurrentWorldTheme(WorldTheme.Summer);
            Assert.AreEqual(WorldTheme.Summer, model.Draft.GetWorldTheme(0));
            Assert.AreEqual(WorldTheme.Spring, model.Draft.GetWorldTheme(1));

            model.Undo();
            Assert.AreEqual(WorldTheme.Spring, model.Draft.GetWorldTheme(0));
            Assert.AreEqual(WorldTheme.Summer, model.Draft.GetWorldTheme(1));
        }

        private static string Describe(LevelValidationReport report)
        {
            return string.Join("\n", report.Issues.Select(i => i.Message));
        }
    }
}
