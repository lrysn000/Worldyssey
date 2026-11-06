using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把两套像素素材自带的 ttf 字体生成 TextMeshPro 字体资源：按每个字体的设计尺寸取样、点阵渲染（不用 SDF、不做 hinting）、点过滤，
    /// 显示时只要字号是设计尺寸的整数倍，笔画就粗细一致。资源名带设计尺寸（如 "retro gaming Pixel 11"）。
    /// 只包含英文字符（字体本身没有中文）。已生成的跳过，删掉资源后可以重新生成。
    /// </summary>
    public static class PixelFontGenerator
    {
        private const string MenuPath = "Worldyssey/UI/Generate Pixel Fonts";
        private const string OutputFolder = ProjectPaths.Art + "/Fonts";
        private const string AssetSuffix = " Pixel ";

        // 不在下表里的字体按这个尺寸取样
        private const int DefaultNativeSize = 16;
        private const int AtlasPadding = 2;
        private const int AtlasSize = 512;
        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        // 各字体的设计尺寸：在这些字号（及整数倍）下渲染没有任何灰色过渡像素，是逐个字号测出来的。
        // MP16REG 在任何整数字号下都对不齐像素，小字会发虚，不建议用作正文
        private static readonly Dictionary<string, int> NativeSizes = new Dictionary<string, int>
        {
            { "retro gaming", 11 },
            { "Visitor", 10 },
            { "pixelart", 8 },
            { "tight pixel", 16 },
            { "MP16REG", 16 },
        };

        internal static readonly string[] FontFolders =
        {
            ProjectPaths.ThirdParty + "/Pixel UI/Font",
            ProjectPaths.ThirdParty + "/Honeti/PixelArtGUI2/Fonts",
        };

        [MenuItem(MenuPath)]
        public static void GenerateFromMenu()
        {
            List<TMP_FontAsset> fonts = GenerateAll();
            Debug.Log($"[{nameof(PixelFontGenerator)}] 像素字体资源共 {fonts.Count} 个，位于 {OutputFolder}");
        }

        /// <summary>生成（或读取已有的）全部像素字体资源。</summary>
        internal static List<TMP_FontAsset> GenerateAll()
        {
            var result = new List<TMP_FontAsset>();
            AssetFolderUtility.EnsureFolder(OutputFolder);
            foreach (string guid in AssetDatabase.FindAssets("t:Font", FontFolders))
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                if (font != null)
                {
                    TMP_FontAsset asset = GetOrCreate(font, Path.GetFileNameWithoutExtension(fontPath));
                    if (asset != null)
                    {
                        result.Add(asset);
                    }
                }
            }

            return result;
        }

        /// <summary>按 ttf 文件名查找生成的字体资源，例如 "retro gaming"、"MP16REG"。</summary>
        internal static TMP_FontAsset Find(string fontFileName)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath(fontFileName));
        }

        private static string AssetPath(string fontFileName)
        {
            return $"{OutputFolder}/{fontFileName}{AssetSuffix}{NativeSize(fontFileName)}.asset";
        }

        private static int NativeSize(string fontFileName)
        {
            return NativeSizes.TryGetValue(fontFileName, out int size) ? size : DefaultNativeSize;
        }

        private static TMP_FontAsset GetOrCreate(Font font, string fontFileName)
        {
            string path = AssetPath(fontFileName);
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                return existing;
            }

            int nativeSize = NativeSize(fontFileName);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, nativeSize, AtlasPadding, GlyphRenderMode.RASTER,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);
            if (asset == null)
            {
                Debug.LogError($"[{nameof(PixelFontGenerator)}] 无法从 {font.name} 生成字体资源");
                return null;
            }

            asset.name = fontFileName + AssetSuffix + nativeSize;
            asset.TryAddCharacters(Characters, out string missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[{nameof(PixelFontGenerator)}] {font.name} 缺少字符：{missing}");
            }

            // 字符已经全部烘进图集，改为静态：打包时不需要原始字体，运行时也不会再改动图集
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            Texture2D atlas = asset.atlasTexture;
            atlas.name = asset.name + " Atlas";
            atlas.filterMode = FilterMode.Point;
            asset.material.name = asset.name + " Material";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(atlas, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
