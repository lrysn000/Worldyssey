using UnityEditor;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 生成 <see cref="UiSkin"/> 并补全空着的字段：面板底和按钮用自制的灰阶像素素材（Art/Textures/UI/Frames），
    /// 滑条和小图标沿用两套素材包，字体用生成的像素字体（标题 retro gaming，正文 MP16REG），季节角饰在 Ornaments。
    /// 已填的字段不覆盖；只有素材方案升级时（默认值版本变化）才替换面板底和按钮这两个旧默认值。
    /// 同时设置自制素材的导入方式（点过滤、不压缩、九宫格边框）。
    /// </summary>
    internal static class PixelUiSkinDefaults
    {
        private const int DefaultsVersion = 4;
        private const string SkinPath = ProjectPaths.Config + "/UiSkin.asset";
        private const string Frames = ProjectPaths.Textures + "/UI/Frames";
        private const string Ornaments = ProjectPaths.Textures + "/UI/Ornaments";
        private const string Honeti = ProjectPaths.ThirdParty + "/Honeti/PixelArtGUI2/Textures";
        // 标题和正文都用 retro gaming：小字也清楚，整套界面字体统一；标题靠更大的整数倍字号区分
        private const string DefaultTitleFont = "retro gaming";
        private const string DefaultBodyFont = "retro gaming";
        private const float PixelsPerUnit = 16f;
        private static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };

        internal static UiSkin GetOrCreateSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            if (skin == null)
            {
                AssetFolderUtility.EnsureFolder(ProjectPaths.Config);
                skin = ScriptableObject.CreateInstance<UiSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            PixelFontGenerator.GenerateAll();
            var serialized = new SerializedObject(skin);
            SerializedProperty version = serialized.FindProperty("_defaultsVersion");
            // 版本 2 起面板底和按钮换成自制素材；只在从更早的版本升级时覆盖，之后你在 UiSkin 里改的不受影响
            bool isUpgrade = version.intValue < 2;

            // 九宫格边框：x 左、y 下、z 右、w 上（素材像素）
            Set(serialized, "_panel", ImportFrame("Frame_Fill", new Vector4(5, 5, 5, 5)), isUpgrade);
            Set(serialized, "_button", ImportFrame("Button_Normal", new Vector4(3, 3, 3, 2)), isUpgrade);
            Set(serialized, "_buttonPressed", ImportFrame("Button_Pressed", new Vector4(3, 2, 3, 3)), false);
            Set(serialized, "_frame", ImportFrame("Frame_Panel", new Vector4(5, 5, 5, 5)), false);
            Set(serialized, "_titleBar", ImportFrame("Frame_TitleBar", new Vector4(3, 3, 3, 3)), false);
            Set(serialized, "_inset", ImportFrame("Frame_Inset", new Vector4(3, 3, 3, 3)), false);
            Set(serialized, "_pattern", Import(Frames + "/Pattern_Dots.png", Vector4.zero, TextureWrapMode.Repeat), false);
            Set(serialized, "_selectCorner", ImportFrame("Select_Corner", Vector4.zero), false);
            Set(serialized, "_pipOn", ImportFrame("Pip_On", Vector4.zero), false);
            Set(serialized, "_pipOff", ImportFrame("Pip_Off", Vector4.zero), false);
            Set(serialized, "_neutralOrnament", Import(Ornaments + "/Ornament_Neutral.png", Vector4.zero, TextureWrapMode.Clamp), false);
            SerializedProperty seasonOrnaments = serialized.FindProperty("_seasonOrnaments");
            seasonOrnaments.arraySize = Seasons.Length;
            for (int i = 0; i < Seasons.Length; i++)
            {
                SerializedProperty element = seasonOrnaments.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    element.objectReferenceValue = Import($"{Ornaments}/Ornament_{Seasons[i]}.png", Vector4.zero, TextureWrapMode.Clamp);
                }
            }

            Set(serialized, "_sliderTrack", LoadSprite(Honeti + "/Panels/UniversalPanel2.png"), false);
            Set(serialized, "_sliderHandle", ImportFrame("Button_Normal", new Vector4(3, 3, 3, 2)), isUpgrade);
            Set(serialized, "_checkIcon", LoadSprite(Honeti + "/Icons/8/Check.png"), false);
            Set(serialized, "_crossIcon", LoadSprite(Honeti + "/Icons/8/X.png"), false);
            Set(serialized, "_arrowDownIcon", LoadSprite(Honeti + "/Icons/8/ArrowDown.png"), false);
            Set(serialized, "_titleFont", PixelFontGenerator.Find(DefaultTitleFont), false);
            Set(serialized, "_bodyFont", PixelFontGenerator.Find(DefaultBodyFont), false);
            // 版本 4：字体按设计尺寸重新生成（新资源），正文从 MP16REG 换成 retro gaming；底纹改为固定的淡浅灰
            if (version.intValue < 4)
            {
                serialized.FindProperty("_titleFont").objectReferenceValue = PixelFontGenerator.Find(DefaultTitleFont);
                serialized.FindProperty("_bodyFont").objectReferenceValue = PixelFontGenerator.Find(DefaultBodyFont);
                serialized.FindProperty("_patternColor").colorValue = new Color(0.8f, 0.8f, 0.8f, 0.35f);
            }

            version.intValue = DefaultsVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        /// <summary>字段为空，或 <paramref name="isForced"/>（默认值升级）时写入。</summary>
        private static void Set(SerializedObject serialized, string field, Object value, bool isForced)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if ((property.objectReferenceValue == null || isForced) && value != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static Sprite ImportFrame(string fileName, Vector4 border)
        {
            return Import($"{Frames}/{fileName}.png", border, TextureWrapMode.Clamp);
        }

        /// <summary>自制像素素材：Sprite、点过滤、不压缩、16 像素 = 1 单位，并设置九宫格边框。</summary>
        private static Sprite Import(string path, Vector4 border, TextureWrapMode wrapMode)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogWarning($"[{nameof(PixelUiSkinDefaults)}] 找不到素材 {path}，UiSkin 里对应字段留空");
                return null;
            }

            bool isReady = importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point
                && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled
                && importer.spriteBorder == border && importer.wrapMode == wrapMode
                && Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit);
            if (!isReady)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.spriteBorder = border;
                importer.wrapMode = wrapMode;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[{nameof(PixelUiSkinDefaults)}] 找不到素材 {path}，UiSkin 里对应字段留空");
            }

            return sprite;
        }
    }
}
