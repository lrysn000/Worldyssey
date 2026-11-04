using UnityEditor;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键生成界面主题资源：功能色、中性和四季共 5 套主题、主题集合，并填好默认色板。
    /// 已存在的资源不会被覆盖，调过的颜色不会丢。
    /// </summary>
    public static class UiThemeAssetCreator
    {
        private const string MenuPath = "Worldyssey/UI/Create Theme Assets";
        private const string ThemeFolder = ProjectPaths.Ui + "/Themes";
        private const string PalettePath = ThemeFolder + "/UiFunctionalPalette.asset";
        private const string ThemeSetPath = ThemeFolder + "/UiThemeSet.asset";

        /// <summary>
        /// 一套主题的默认颜色，顺序：标题栏、面板底、内框、强调、正文、标题文字、图标。
        /// </summary>
        private readonly struct ThemeColors
        {
            public readonly string Name;
            public readonly Color[] Colors;

            public ThemeColors(string name, params Color[] colors)
            {
                Name = name;
                Colors = colors;
            }
        }

        private static readonly string[] ColorFields =
        {
            "_header", "_panelBackground", "_innerFrame", "_accent", "_text", "_headerText", "_icon",
        };

        // 浅色基底：面板底接近白色，季节感靠标题栏和强调色表达。冬季标题栏很浅，标题文字改用深蓝灰保证对比度
        private static readonly ThemeColors Neutral = new ThemeColors("UiTheme_Neutral",
            Rgb(0.33f, 0.39f, 0.47f), Rgb(0.93f, 0.94f, 0.95f), Color.white, Rgb(0.38f, 0.55f, 0.75f),
            Rgb(0.20f, 0.23f, 0.28f), Color.white, Rgb(0.30f, 0.34f, 0.40f));

        private static readonly ThemeColors[] Seasons =
        {
            new ThemeColors("UiTheme_Spring",
                Rgb(0.84f, 0.42f, 0.55f), Rgb(0.99f, 0.93f, 0.95f), Color.white, Rgb(0.90f, 0.48f, 0.60f),
                Rgb(0.30f, 0.20f, 0.24f), Color.white, Rgb(0.78f, 0.38f, 0.50f)),
            new ThemeColors("UiTheme_Summer",
                Rgb(0.13f, 0.58f, 0.60f), Rgb(0.90f, 0.97f, 0.97f), Color.white, Rgb(0.15f, 0.64f, 0.66f),
                Rgb(0.12f, 0.26f, 0.28f), Color.white, Rgb(0.14f, 0.50f, 0.54f)),
            new ThemeColors("UiTheme_Autumn",
                Rgb(0.80f, 0.45f, 0.13f), Rgb(0.99f, 0.95f, 0.88f), Color.white, Rgb(0.86f, 0.50f, 0.16f),
                Rgb(0.32f, 0.20f, 0.10f), Color.white, Rgb(0.70f, 0.38f, 0.12f)),
            new ThemeColors("UiTheme_Winter",
                Rgb(0.66f, 0.82f, 0.93f), Rgb(0.95f, 0.98f, 1.00f), Color.white, Rgb(0.36f, 0.60f, 0.82f),
                Rgb(0.16f, 0.24f, 0.36f), Rgb(0.14f, 0.24f, 0.38f), Rgb(0.24f, 0.42f, 0.62f)),
        };

        [MenuItem(MenuPath)]
        public static void CreateThemeAssets()
        {
            AssetFolderUtility.EnsureFolder(ThemeFolder);
            UiFunctionalPalette palette = LoadOrCreate<UiFunctionalPalette>(PalettePath, out _);

            UiTheme neutral = LoadOrCreateTheme(Neutral, palette);
            var seasons = new UiTheme[Seasons.Length];
            for (int i = 0; i < Seasons.Length; i++)
            {
                seasons[i] = LoadOrCreateTheme(Seasons[i], palette);
            }

            UiThemeSet themeSet = LoadOrCreate<UiThemeSet>(ThemeSetPath, out bool isNewSet);
            if (isNewSet)
            {
                var serialized = new SerializedObject(themeSet);
                serialized.FindProperty("_neutralTheme").objectReferenceValue = neutral;
                SerializedProperty worlds = serialized.FindProperty("_worldThemes");
                worlds.arraySize = seasons.Length;
                for (int i = 0; i < seasons.Length; i++)
                {
                    worlds.GetArrayElementAtIndex(i).objectReferenceValue = seasons[i];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(themeSet);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = themeSet;
            Debug.Log($"[{nameof(UiThemeAssetCreator)}] 主题资源已就绪：{ThemeFolder}", themeSet);
        }

        private static UiTheme LoadOrCreateTheme(ThemeColors colors, UiFunctionalPalette palette)
        {
            UiTheme theme = LoadOrCreate<UiTheme>($"{ThemeFolder}/{colors.Name}.asset", out bool isNew);
            if (!isNew)
            {
                return theme;
            }

            var serialized = new SerializedObject(theme);
            for (int i = 0; i < ColorFields.Length; i++)
            {
                serialized.FindProperty(ColorFields[i]).colorValue = colors.Colors[i];
            }

            serialized.FindProperty("_functionalPalette").objectReferenceValue = palette;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static T LoadOrCreate<T>(string path, out bool isNew) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                isNew = false;
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            isNew = true;
            return asset;
        }

        private static Color Rgb(float r, float g, float b)
        {
            return new Color(r, g, b, 1f);
        }
    }
}
