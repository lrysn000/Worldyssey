using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Audio;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 创建 <see cref="AudioLibrary"/> 并按文件名规范从 Audio 文件夹自动填入素材，再在场景里建好音频物体并接线。
    /// 可以重复运行：已经有素材的条目保持不变（保留手动调整），只补空着的条目。
    /// </summary>
    public static class AudioSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Build Audio";
        private const string LibraryPath = "Assets/Worldyssey/Config/AudioLibrary.asset";
        private const string AudioFolder = "Assets/Worldyssey/Audio";
        private const string AudioObjectName = "Audio";

        // 春、夏、秋用草地脚步，冬天用雪地脚步
        private static readonly string[] FootstepPrefixes =
        {
            "SFX_Player_Step_Grass", "SFX_Player_Step_Grass", "SFX_Player_Step_Grass", "SFX_Player_Step_Snow",
        };

        private static readonly string[] AmbienceNames = { "AMB_Spring", "AMB_Summer", "AMB_Autumn", "AMB_Winter" };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var levelSession = Object.FindObjectOfType<LevelSession>(true);
            var levelView = levelSession != null ? UiThemeSceneSetup.GetReference<LevelView>(new SerializedObject(levelSession), "_levelView") : null;
            if (levelView == null)
            {
                Debug.LogError($"[{nameof(AudioSetup)}] 当前场景没有 LevelSession，或它没有连 LevelView，未做任何修改");
                return;
            }

            AudioLibrary library = GetOrCreateLibrary();
            FillLibrary(library);
            BuildSceneObjects(library, levelSession, levelView);
        }

        private static AudioLibrary GetOrCreateLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library != null)
            {
                return library;
            }

            library = ScriptableObject.CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        private static void FillLibrary(AudioLibrary library)
        {
            var serialized = new SerializedObject(library);
            SerializedProperty footsteps = serialized.FindProperty("_footsteps");
            footsteps.arraySize = AudioLibrary.SeasonCount;
            for (int i = 0; i < AudioLibrary.SeasonCount; i++)
            {
                // 脚步很频繁，音量压低、音高浮动大一些
                FillCue(footsteps.GetArrayElementAtIndex(i), FootstepPrefixes[i], 0.5f, 0.1f);
            }

            FillCue(serialized.FindProperty("_boxPush"), "SFX_Box_Push", 0.8f, 0.06f);
            FillCue(serialized.FindProperty("_boxOnGoal"), "SFX_Box_OnGoal", 0.9f, 0f);
            FillCue(serialized.FindProperty("_boxOnPedestal"), "SFX_Box_OnPedestal", 0.9f, 0f);
            FillCue(serialized.FindProperty("_blocked"), "SFX_Player_Blocked", 0.7f, 0.05f);
            FillCue(serialized.FindProperty("_teleport"), "SFX_World_Enter", 1f, 0f);
            FillCue(serialized.FindProperty("_undo"), "SFX_Level_Undo", 0.8f, 0f);
            FillCue(serialized.FindProperty("_restart"), "SFX_Level_Restart", 0.8f, 0f);
            FillCue(serialized.FindProperty("_levelComplete"), "SFX_Level_Complete", 1f, 0f);
            FillCue(serialized.FindProperty("_uiClick"), "SFX_UI_Click", 0.8f, 0f);
            FillCue(serialized.FindProperty("_uiBack"), "SFX_UI_Back", 0.8f, 0f);

            FillClip(serialized.FindProperty("_menuMusic"), "BGM_Menu");
            FillClip(serialized.FindProperty("_levelMusic"), "BGM_Level");
            SerializedProperty ambience = serialized.FindProperty("_ambience");
            ambience.arraySize = AudioLibrary.SeasonCount;
            for (int i = 0; i < AudioLibrary.SeasonCount; i++)
            {
                FillClip(ambience.GetArrayElementAtIndex(i), AmbienceNames[i]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static void FillCue(SerializedProperty cue, string prefix, float volume, float pitchJitter)
        {
            SerializedProperty clips = cue.FindPropertyRelative("_clips");
            if (clips.arraySize > 0)
            {
                return;
            }

            List<AudioClip> found = LoadClips(prefix);
            if (found.Count == 0)
            {
                Debug.LogWarning($"[{nameof(AudioSetup)}] {AudioFolder} 里没有 {prefix} 开头的音频，这个条目先空着");
                return;
            }

            clips.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                clips.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            cue.FindPropertyRelative("_volume").floatValue = volume;
            cue.FindPropertyRelative("_pitchRange").vector2Value = new Vector2(1f - pitchJitter, 1f + pitchJitter);
        }

        private static void FillClip(SerializedProperty property, string name)
        {
            if (property.objectReferenceValue != null)
            {
                return;
            }

            List<AudioClip> found = LoadClips(name);
            if (found.Count == 0)
            {
                Debug.LogWarning($"[{nameof(AudioSetup)}] {AudioFolder} 里没有 {name}，这一项先空着");
                return;
            }

            property.objectReferenceValue = found[0];
        }

        /// <summary>文件名等于 <paramref name="prefix"/> 或以 “prefix_” 开头的音频，按文件名排序（变体序号顺序）。</summary>
        private static List<AudioClip> LoadClips(string prefix)
        {
            var result = new List<AudioClip>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName == prefix || fileName.StartsWith(prefix + "_"))
                {
                    paths.Add(path);
                }
            }

            paths.Sort((a, b) => string.CompareOrdinal(Path.GetFileName(a), Path.GetFileName(b)));
            for (int i = 0; i < paths.Count; i++)
            {
                result.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]));
            }

            return result;
        }

        private static void BuildSceneObjects(AudioLibrary library, LevelSession levelSession, LevelView levelView)
        {
            GameObject root = GameObject.Find(AudioObjectName);
            if (root == null)
            {
                root = new GameObject(AudioObjectName);
                Undo.RegisterCreatedObjectUndo(root, UiStyleKit.UndoName);
            }

            SfxPlayer sfx = GetOrAdd<SfxPlayer>(root);
            MusicPlayer music = GetOrAdd<MusicPlayer>(root);
            GameplayAudio gameplay = GetOrAdd<GameplayAudio>(root);

            var settings = Object.FindObjectOfType<GameSettingsController>(true);
            if (settings == null)
            {
                Debug.LogWarning($"[{nameof(AudioSetup)}] 场景里没有 GameSettingsController，音乐、音效会按满音量播放");
            }

            GameUiBuilder.SetReference(sfx, "_settings", settings);
            GameUiBuilder.SetReference(music, "_settings", settings);
            GameUiBuilder.SetReference(music, "_library", library);
            GameUiBuilder.SetReference(gameplay, "_library", library);
            GameUiBuilder.SetReference(gameplay, "_levelSession", levelSession);
            GameUiBuilder.SetReference(gameplay, "_levelView", levelView);
            GameUiBuilder.SetReference(gameplay, "_sfxPlayer", sfx);
            GameUiBuilder.SetReference(gameplay, "_musicPlayer", music);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeObject = library;
            Debug.Log($"[{nameof(AudioSetup)}] 音频已接好，配置在 {LibraryPath}，记得保存场景", root);
        }

        private static T GetOrAdd<T>(GameObject host) where T : Component
        {
            return host.TryGetComponent(out T component) ? component : Undo.AddComponent<T>(host);
        }
    }
}
