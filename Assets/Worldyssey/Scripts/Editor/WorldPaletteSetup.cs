using UnityEditor;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 生成季节调色板，并连到所有 LevelVisualConfig 和四份季节主题上，让 3D 与 UI 共用同一份季节色。
    /// 已经连好的引用不会被覆盖，可以重复运行。
    /// </summary>
    public static class WorldPaletteSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Link World Palette (3D + UI)";
        private const string PaletteFolder = ProjectPaths.Config;
        private const string PalettePath = PaletteFolder + "/WorldPalette.asset";

        [MenuItem(MenuPath)]
        public static void Link()
        {
            AssetFolderUtility.EnsureFolder(PaletteFolder);
            var palette = AssetDatabase.LoadAssetAtPath<WorldPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<WorldPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            int visualCount = 0;
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LevelVisualConfig)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelVisualConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null && SetIfEmpty(config, "_worldPalette", palette))
                {
                    visualCount++;
                }
            }

            int themeCount = 0;
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (themeSet != null)
            {
                for (int world = 0; world < LevelLimits.MaxWorldCount; world++)
                {
                    // 越界或没配置时 GetWorldTheme 会回退为中性主题，中性主题不连调色板
                    UiTheme theme = themeSet.GetSeasonTheme(world);
                    if (theme == null || theme == themeSet.NeutralTheme || !SetIfEmpty(theme, "_worldPalette", palette))
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(theme);
                    serialized.FindProperty("_paletteWorld").intValue = world;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    themeCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = palette;
            Debug.Log($"[{nameof(WorldPaletteSetup)}] 调色板 {PalettePath} 已连到 {visualCount} 个表现配置、{themeCount} 套季节主题", palette);
        }

        /// <summary>
        /// 引用为空时写入并返回 true；已有引用时保持不变，返回 false。
        /// </summary>
        private static bool SetIfEmpty(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue != null)
            {
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }
    }
}
