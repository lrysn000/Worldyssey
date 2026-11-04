using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把 Prefabs/Environment 下预制体引用的第三方包材质，换成 Materials 文件夹里的项目材质。
    /// 季节文件夹（Spring / Summer / Autumn / Winter）里的预制体优先用“原名_季节”的材质，例如 Bg_Palette256 → Bg_Palette256_Spring；
    /// 没有季节版本时用同名材质，都没有则保持不变。项目材质可以随意修改，素材包更新时也不会被覆盖。可以重复运行。
    /// </summary>
    public static class EnvironmentMaterialRemapper
    {
        private const string MenuPath = "Worldyssey/Art/Use Project Materials In Environment Prefabs";
        private const string EnvironmentFolder = ProjectPaths.EnvironmentPrefabs;
        private const string ProjectMaterialFolder = ProjectPaths.Materials;
        private const string PackagesFolder = ProjectPaths.ThirdParty + "/";

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        [MenuItem(MenuPath)]
        public static void Remap()
        {
            Dictionary<string, Material> projectMaterials = LoadProjectMaterials();
            var report = new StringBuilder();
            int changedPrefabs = 0;
            int changedSlots = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnvironmentFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                int slots = RemapPrefab(path, GetSeason(path), projectMaterials);
                if (slots > 0)
                {
                    changedPrefabs++;
                    changedSlots += slots;
                    report.AppendLine($"  {path}：替换 {slots} 处");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(EnvironmentMaterialRemapper)}] 修改了 {changedPrefabs} 个预制体、{changedSlots} 处材质引用\n{report}");
        }

        /// <summary>
        /// 按名称索引项目材质。同名材质有多个时无法判断用哪个，报错并跳过这个名字。
        /// </summary>
        private static Dictionary<string, Material> LoadProjectMaterials()
        {
            var byName = new Dictionary<string, Material>();
            var duplicates = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { ProjectMaterialFolder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null)
                {
                    continue;
                }

                if (byName.ContainsKey(material.name))
                {
                    duplicates.Add(material.name);
                    continue;
                }

                byName.Add(material.name, material);
            }

            foreach (string name in duplicates)
            {
                Debug.LogError($"[{nameof(EnvironmentMaterialRemapper)}] {ProjectMaterialFolder} 下有多个名为 {name} 的材质，已跳过这个名字");
                byName.Remove(name);
            }

            return byName;
        }

        /// <summary>
        /// 预制体所在的季节文件夹名，不在季节文件夹里时返回 null。
        /// </summary>
        private static string GetSeason(string prefabPath)
        {
            foreach (string season in SeasonNames)
            {
                if (prefabPath.StartsWith($"{EnvironmentFolder}/{season}/"))
                {
                    return season;
                }
            }

            return null;
        }

        private static bool TryFindReplacement(Material current, string season, Dictionary<string, Material> projectMaterials, out Material replacement)
        {
            if (season != null && projectMaterials.TryGetValue($"{current.name}_{season}", out replacement))
            {
                return true;
            }

            return projectMaterials.TryGetValue(current.name, out replacement);
        }

        private static int RemapPrefab(string path, string season, Dictionary<string, Material> projectMaterials)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int changed = 0;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    bool isRendererChanged = false;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material current = materials[i];
                        if (current == null || !AssetDatabase.GetAssetPath(current).StartsWith(PackagesFolder)
                            || !TryFindReplacement(current, season, projectMaterials, out Material replacement))
                        {
                            continue;
                        }

                        materials[i] = replacement;
                        isRendererChanged = true;
                        changed++;
                    }

                    if (isRendererChanged)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
