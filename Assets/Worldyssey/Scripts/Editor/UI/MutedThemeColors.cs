using UnityEditor;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把中性和四季界面主题改成低饱和的配色（与像素装饰预览一致），并把功能色（确认、取消、禁用）一并调暗。
    /// 季节主题的强调色不再取 3D 用的世界调色板（那套颜色偏亮），改用主题自己的强调色；想恢复可以重新运行 Link World Palette。
    /// 按主题资源名称里的 Neutral / Spring / Summer / Autumn / Winter 识别。
    /// </summary>
    public static class MutedThemeColors
    {
        private const string MenuPath = "Worldyssey/Art/UI/Apply Muted Theme Colors";

        // 标题色、面板底、内框、强调色、正文
        private static readonly (string Key, Color32 Header, Color32 Background, Color32 Inner, Color32 Accent, Color32 Text)[] Palettes =
        {
            ("Neutral", new Color32(112, 104, 96, 255), new Color32(238, 233, 226, 255), new Color32(250, 247, 242, 255), new Color32(128, 140, 150, 255), new Color32(70, 62, 56, 255)),
            ("Spring", new Color32(172, 112, 122, 255), new Color32(246, 234, 230, 255), new Color32(252, 246, 244, 255), new Color32(196, 138, 146, 255), new Color32(96, 58, 66, 255)),
            ("Summer", new Color32(74, 134, 124, 255), new Color32(230, 240, 232, 255), new Color32(246, 250, 246, 255), new Color32(96, 158, 146, 255), new Color32(40, 80, 74, 255)),
            ("Autumn", new Color32(138, 92, 70, 255), new Color32(242, 228, 214, 255), new Color32(250, 242, 234, 255), new Color32(184, 120, 78, 255), new Color32(86, 56, 44, 255)),
            ("Winter", new Color32(94, 116, 148, 255), new Color32(226, 232, 240, 255), new Color32(244, 247, 251, 255), new Color32(118, 142, 176, 255), new Color32(46, 58, 80, 255)),
        };

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            int updated = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(UiTheme)))
            {
                var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var palette in Palettes)
                {
                    if (theme != null && theme.name.Contains(palette.Key))
                    {
                        ApplyPalette(theme, palette.Header, palette.Background, palette.Inner, palette.Accent, palette.Text);
                        updated++;
                        break;
                    }
                }
            }

            ApplyFunctional();
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(MutedThemeColors)}] 已更新 {updated} 套界面主题的配色");
        }

        private static void ApplyPalette(UiTheme theme, Color header, Color background, Color inner, Color accent, Color text)
        {
            var serialized = new SerializedObject(theme);
            serialized.FindProperty("_header").colorValue = header;
            serialized.FindProperty("_panelBackground").colorValue = background;
            serialized.FindProperty("_innerFrame").colorValue = inner;
            serialized.FindProperty("_accent").colorValue = accent;
            serialized.FindProperty("_text").colorValue = text;
            serialized.FindProperty("_headerText").colorValue = Color.white;
            serialized.FindProperty("_icon").colorValue = header;
            serialized.FindProperty("_worldPalette").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static void ApplyFunctional()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(UiFunctionalPalette)))
            {
                var palette = AssetDatabase.LoadAssetAtPath<UiFunctionalPalette>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(palette);
                serialized.FindProperty("_confirm").colorValue = new Color32(112, 160, 100, 255);
                serialized.FindProperty("_cancel").colorValue = new Color32(190, 100, 90, 255);
                serialized.FindProperty("_disabled").colorValue = new Color32(170, 168, 164, 255);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(palette);
            }
        }
    }
}
