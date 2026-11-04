using System.IO;
using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给四个季节调色板材质各复制一张独立的调色板贴图，之后在图片编辑器里改某个季节的色块不会影响其他季节。
    /// 材质已经用上 Textures/Palettes 下的贴图时跳过，可以重复运行。复制会保留原贴图的导入设置（点采样、不压缩等）。
    /// </summary>
    public static class SeasonPaletteTextureSetup
    {
        private const string MenuPath = "Worldyssey/Art/Create Season Palette Textures";
        private const string MaterialFolder = "Assets/Worldyssey/Materials";
        private const string TextureFolder = "Assets/Worldyssey/Textures/Palettes";
        private const string MaterialPrefix = "Bg_Palette256_";

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };
        private static readonly string[] TextureProperties = { "_BaseMap", "_MainTex" };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            AssetFolderUtility.EnsureFolder(TextureFolder);
            foreach (string season in SeasonNames)
            {
                string materialPath = $"{MaterialFolder}/{MaterialPrefix}{season}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                Texture current = material != null ? GetTexture(material) : null;
                if (current == null)
                {
                    Debug.LogWarning($"[{nameof(SeasonPaletteTextureSetup)}] {materialPath} 不存在或没有贴图，跳过");
                    continue;
                }

                string currentPath = AssetDatabase.GetAssetPath(current);
                if (currentPath.StartsWith(TextureFolder + "/"))
                {
                    Debug.Log($"[{nameof(SeasonPaletteTextureSetup)}] {material.name} 已经在用季节贴图 {currentPath}，跳过", material);
                    continue;
                }

                string targetPath = $"{TextureFolder}/{Path.GetFileNameWithoutExtension(currentPath)}_{season}{Path.GetExtension(currentPath)}";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath) == null && !AssetDatabase.CopyAsset(currentPath, targetPath))
                {
                    Debug.LogError($"[{nameof(SeasonPaletteTextureSetup)}] 复制 {currentPath} 到 {targetPath} 失败", material);
                    continue;
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
                foreach (string property in TextureProperties)
                {
                    if (material.HasProperty(property))
                    {
                        material.SetTexture(property, texture);
                    }
                }

                EditorUtility.SetDirty(material);
                Debug.Log($"[{nameof(SeasonPaletteTextureSetup)}] {material.name} 改用 {targetPath}", material);
            }

            AssetDatabase.SaveAssets();
        }

        private static Texture GetTexture(Material material)
        {
            foreach (string property in TextureProperties)
            {
                if (material.HasProperty(property) && material.GetTexture(property) != null)
                {
                    return material.GetTexture(property);
                }
            }

            return null;
        }
    }
}
