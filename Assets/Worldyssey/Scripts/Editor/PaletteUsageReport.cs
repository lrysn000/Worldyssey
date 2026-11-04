using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 分析 Prefabs/Environment 下的模型各用到了调色板贴图的哪些色块，生成网页报告并打开。
    /// 改调色板之前先看：只被草用到的色块可以放心改；草和石头共用的色块一改两边都变，需要拆材质。
    /// 每张调色板贴图（例如四个季节各一张）单独一份报告，只统计用到这张贴图的模型。
    /// </summary>
    public static class PaletteUsageReport
    {
        private const string MenuPath = "Worldyssey/Art/Palette Usage Report";
        private const string EnvironmentFolder = "Assets/Worldyssey/Prefabs/Environment";
        private const string PaletteKeyword = "Palette";

        // 报告放在项目根目录下、Assets 之外，不会被 Unity 导入
        private const string ReportFolderName = "PaletteReports";

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };
        private static readonly string[] TextureProperties = { "_BaseMap", "_MainTex" };

        [MenuItem(MenuPath)]
        public static void Generate()
        {
            var usages = new Dictionary<Texture2D, PaletteUsage>();
            var unreadable = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnvironmentFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    CollectPrefab(prefab, Label(path, prefab.name), usages, unreadable);
                }
            }

            if (usages.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PaletteUsageReport)}] {EnvironmentFolder} 下没有模型使用名字带 {PaletteKeyword} 的贴图");
                return;
            }

            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, ReportFolderName);
            Directory.CreateDirectory(folder);
            foreach (PaletteUsage usage in usages.Values)
            {
                string file = Path.Combine(folder, $"{usage.Texture.name}.html");
                File.WriteAllText(file, PaletteReportHtml.Build(usage, unreadable), Encoding.UTF8);
                Application.OpenURL("file:///" + file.Replace('\\', '/'));
                Debug.Log($"[{nameof(PaletteUsageReport)}] 已生成 {file}");
            }
        }

        private static void CollectPrefab(GameObject prefab, PrefabLabel label, Dictionary<Texture2D, PaletteUsage> usages, List<string> unreadable)
        {
            foreach (MeshRenderer meshRenderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!meshRenderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                {
                    continue;
                }

                Mesh mesh = filter.sharedMesh;
                Material[] materials = meshRenderer.sharedMaterials;
                for (int subMesh = 0; subMesh < mesh.subMeshCount && materials.Length > 0; subMesh++)
                {
                    Material material = materials[Mathf.Min(subMesh, materials.Length - 1)];
                    Texture2D texture = material != null ? GetTexture(material) : null;
                    if (texture == null || texture.name.IndexOf(PaletteKeyword, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!usages.TryGetValue(texture, out PaletteUsage usage))
                    {
                        usage = PaletteUsage.Create(texture);
                        usages.Add(texture, usage);
                    }

                    if (!usage.AddSubMesh(mesh, subMesh, label) && !unreadable.Contains(label.Name))
                    {
                        unreadable.Add(label.Name);
                    }
                }
            }
        }

        private static Texture2D GetTexture(Material material)
        {
            foreach (string property in TextureProperties)
            {
                if (material.HasProperty(property) && material.GetTexture(property) is Texture2D texture)
                {
                    return texture;
                }
            }

            return null;
        }

        private static PrefabLabel Label(string path, string prefabName)
        {
            string season = "共用";
            foreach (string name in SeasonNames)
            {
                if (path.StartsWith($"{EnvironmentFolder}/{name}/"))
                {
                    season = name;
                }
            }

            return new PrefabLabel($"{season}/{prefabName}", Categorize(prefabName));
        }

        private static string Categorize(string prefabName)
        {
            if (Contains(prefabName, "Grass"))
            {
                return "草";
            }

            if (Contains(prefabName, "Floor"))
            {
                return "地面";
            }

            if (Contains(prefabName, "Wall"))
            {
                return "墙";
            }

            return Contains(prefabName, "Tree") ? "树" : "装饰";
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
