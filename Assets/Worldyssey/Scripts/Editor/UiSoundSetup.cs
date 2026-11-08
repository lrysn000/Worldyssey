using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Audio;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给场景里每个根 Canvas 挂 <see cref="UiAudio"/>，给场景和 UI 预制体里的每个按钮挂 <see cref="UiSoundButton"/>。
    /// 按名字区分：带 Back、Cancel、Close、Resume 的按钮用返回音，其他用确认音。
    /// 可以重复运行：已有的组件保留，手动改过的音效类型不会被覆盖。需要先运行 Build Audio。
    /// </summary>
    public static class UiSoundSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Build UI Sounds";
        private const string UiPrefabFolder = "Assets/Worldyssey/Prefabs/UI";

        private static readonly string[] BackKeywords = { "back", "cancel", "close", "resume" };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var library = GameUiBuilder.FindAsset<AudioLibrary>(null);
            var sfxPlayer = Object.FindObjectOfType<SfxPlayer>(true);
            if (library == null || sfxPlayer == null)
            {
                Debug.LogError($"[{nameof(UiSoundSetup)}] 找不到 AudioLibrary 或场景里的 SfxPlayer，先运行 Worldyssey/Setup/Build Audio，未做任何修改");
                return;
            }

            int canvasCount = AddCanvasAudio(library, sfxPlayer);
            // 先处理预制体：场景里的预制体实例会随之带上组件，下面就不会重复添加
            int prefabButtons = AddToPrefabs();
            int sceneButtons = AddToButtons(Object.FindObjectsOfType<Button>(true));

            EditorSceneManager.MarkSceneDirty(sfxPlayer.gameObject.scene);
            Debug.Log($"[{nameof(UiSoundSetup)}] {canvasCount} 个 Canvas 已接好界面音效，新加按钮音效：场景 {sceneButtons} 个、预制体 {prefabButtons} 个。记得保存场景");
        }

        private static int AddCanvasAudio(AudioLibrary library, SfxPlayer sfxPlayer)
        {
            int count = 0;
            foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                if (!canvas.TryGetComponent(out UiAudio uiAudio))
                {
                    uiAudio = Undo.AddComponent<UiAudio>(canvas.gameObject);
                }

                GameUiBuilder.SetReference(uiAudio, "_library", library);
                GameUiBuilder.SetReference(uiAudio, "_sfxPlayer", sfxPlayer);
                count++;
            }

            return count;
        }

        private static int AddToButtons(Button[] buttons)
        {
            int added = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].TryGetComponent(out UiSoundButton _))
                {
                    continue;
                }

                UiSoundButton sound = Undo.AddComponent<UiSoundButton>(buttons[i].gameObject);
                SetKind(sound, buttons[i].name);
                added++;
            }

            return added;
        }

        private static int AddToPrefabs()
        {
            int added = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                int addedHere = 0;
                foreach (Button button in contents.GetComponentsInChildren<Button>(true))
                {
                    if (button.TryGetComponent(out UiSoundButton _))
                    {
                        continue;
                    }

                    SetKind(button.gameObject.AddComponent<UiSoundButton>(), button.name);
                    addedHere++;
                }

                if (addedHere > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }

                PrefabUtility.UnloadPrefabContents(contents);
                added += addedHere;
            }

            return added;
        }

        private static void SetKind(UiSoundButton sound, string buttonName)
        {
            var serialized = new SerializedObject(sound);
            serialized.FindProperty("_kind").enumValueIndex = (int)(IsBackButton(buttonName) ? UiSoundKind.Back : UiSoundKind.Click);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsBackButton(string buttonName)
        {
            string lower = buttonName.ToLowerInvariant();
            for (int i = 0; i < BackKeywords.Length; i++)
            {
                if (lower.Contains(BackKeywords[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
