using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Gameplay;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 多语言接入：把翻译表接到 GameSettingsController；给设置面板补一行语言下拉框；
    /// 给场景和界面预制体里内容与翻译表原文完全一致的固定文字挂上 <see cref="LocalizedText"/>。
    /// 含格式占位符的原文和世界名由代码翻译，不自动挂。可以重复运行，已挂的不重复挂。
    /// </summary>
    public static class LocalizationSetup
    {
        private const string MenuPath = "Worldyssey/Content/Localize Scene Texts";
        internal const string TablePath = ProjectPaths.Root + "/Localization/Strings.csv";
        private const string LanguageLabelName = "LanguageLabel";
        private const string ResolutionDropdownName = "ResolutionDropdown";
        private const string ResolutionLabelName = "ResolutionLabel";

        // 语言行放在第 6 行（全屏、分辨率、三条音量之后），分辨率在第 2 行
        private const int RowsBelowResolution = 4;

        private static readonly string[] UiPrefabNames = { "P_LevelCard", "P_LevelButton", "P_EditorGridCell" };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var table = AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath);
            var settings = Object.FindObjectOfType<GameSettingsController>(true);
            if (table == null || settings == null)
            {
                Debug.LogError($"[{nameof(LocalizationSetup)}] 找不到翻译表 {TablePath} 或场景里的 GameSettingsController，先打开游戏场景");
                return;
            }

            GameUiBuilder.SetReference(settings, "_translationTable", table);
            AddLanguageRow(Object.FindObjectOfType<SettingsPanel>(true));

            HashSet<string> sources = ReadStaticSources(table);
            int count = 0;
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                count += Localize(root, sources, true);
            }

            foreach (string prefabName in UiPrefabNames)
            {
                count += LocalizePrefab(ProjectPaths.UiPrefabs + "/" + prefabName + ".prefab", sources);
            }

            EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
            Debug.Log($"[{nameof(LocalizationSetup)}] 新挂上 {count} 个 LocalizedText，记得保存场景。翻译在 {TablePath} 里改", table);
        }

        /// <summary>
        /// 翻译表里可以自动挂组件的原文：去掉含占位符的格式串（由代码填值）和世界名（随关卡变化，由代码翻译）。
        /// </summary>
        private static HashSet<string> ReadStaticSources(TextAsset table)
        {
            var sources = new HashSet<string>();
            List<List<string>> rows = CsvTable.Parse(table.text);
            for (int i = 1; i < rows.Count; i++)
            {
                string source = rows[i][0];
                if (source.Length > 0 && source.IndexOf('{') < 0 && !IsWorldName(source))
                {
                    sources.Add(source);
                }
            }

            return sources;
        }

        private static bool IsWorldName(string source)
        {
            for (int i = 0; i < WorldThemes.Count; i++)
            {
                if (WorldThemes.DisplayName((WorldTheme)i) == source)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Localize(GameObject root, HashSet<string> sources, bool isSceneObject)
        {
            int count = 0;
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string source = text.text != null ? text.text.Trim() : string.Empty;
                if (!sources.Contains(source) || text.GetComponent<LocalizedText>() != null)
                {
                    continue;
                }

                LocalizedText localized = isSceneObject
                    ? Undo.AddComponent<LocalizedText>(text.gameObject)
                    : text.gameObject.AddComponent<LocalizedText>();
                var serialized = new SerializedObject(localized);
                serialized.FindProperty("_source").stringValue = source;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }

            return count;
        }

        private static int LocalizePrefab(string path, HashSet<string> sources)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[{nameof(LocalizationSetup)}] 找不到界面预制体 {path}，跳过");
                return 0;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int count = Localize(root, sources, false);
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                return count;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 复制分辨率那一行（标签 + 下拉框）放到音量下面作为语言行，样式和主题着色都沿用。已有语言行时只重新接引用。
        /// </summary>
        private static void AddLanguageRow(SettingsPanel panel)
        {
            Transform resolution = FindDeep(panel != null ? panel.transform : null, ResolutionDropdownName);
            if (resolution == null)
            {
                Debug.LogWarning($"[{nameof(LocalizationSetup)}] 设置面板里没有分辨率下拉框，没有添加语言行；可用 Build Settings Controls 重建设置面板");
                return;
            }

            Transform body = resolution.parent;
            Transform language = body.Find(SettingsControlsBuilder.LanguageDropdownName);
            if (language == null)
            {
                float offset = -(SettingsControlsBuilder.RowHeight + SettingsControlsBuilder.RowGap) * RowsBelowResolution;
                language = Duplicate(resolution, SettingsControlsBuilder.LanguageDropdownName, offset);
                Transform resolutionLabel = body.Find(ResolutionLabelName);
                if (resolutionLabel != null)
                {
                    Transform label = Duplicate(resolutionLabel, LanguageLabelName, offset);
                    label.GetComponent<TMP_Text>().text = UiText.Language;
                }
            }

            GameUiBuilder.SetReference(panel, "_languageDropdown", language.GetComponent<TMP_Dropdown>());
        }

        private static Transform Duplicate(Transform original, string name, float offsetY)
        {
            GameObject copy = Object.Instantiate(original.gameObject, original.parent);
            Undo.RegisterCreatedObjectUndo(copy, "Add Language Row");
            copy.name = name;
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = ((RectTransform)original).anchoredPosition + new Vector2(0f, offsetY);
            return rect;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
