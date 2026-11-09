using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Worldyssey.View;
using Object = UnityEngine.Object;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 生成四个季节的 <see cref="SeasonVisualSet"/>，按文件名把 Prefabs/Environment 下的预制体分类填进去，并连到 LevelVisualConfig。
    /// 季节文件夹（Spring / Summer / Autumn / Winter）里的预制体只给对应季节；Environment 根目录的地块和草作为各季节的默认值。
    /// 只填写空着的字段，手动调过的不会被覆盖，可以在添加新素材后重复运行。
    /// </summary>
    public static class SeasonVisualSetSetup
    {
        private const string MenuPath = "Worldyssey/Art/Create + Link Season Visual Sets";
        private const string EnvironmentFolder = ProjectPaths.EnvironmentPrefabs;
        private const string SetFolder = ProjectPaths.Seasons;
        private const int WinterIndex = 3;

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            AssetFolderUtility.EnsureFolder(SetFolder);
            List<GameObject> shared = LoadPrefabs(EnvironmentFolder, true);
            var sets = new SeasonVisualSet[SeasonNames.Length];
            for (int i = 0; i < SeasonNames.Length; i++)
            {
                string setPath = $"{SetFolder}/SeasonVisualSet_{SeasonNames[i]}.asset";
                var set = AssetDatabase.LoadAssetAtPath<SeasonVisualSet>(setPath);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<SeasonVisualSet>();
                    AssetDatabase.CreateAsset(set, setPath);
                }

                string seasonFolder = $"{EnvironmentFolder}/{SeasonNames[i]}";
                List<GameObject> own = AssetDatabase.IsValidFolder(seasonFolder) ? LoadPrefabs(seasonFolder, false) : new List<GameObject>();

                // 冬天地上是雪，不撒共用的绿草
                Fill(set, own, shared, i != WinterIndex);
                sets[i] = set;
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LevelVisualConfig)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelVisualConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(config);
                SetArrayIfEmpty(serialized, "_seasonSets", sets);
                SetArrayIfEmpty(serialized, "_sharedWallVariants", Filter(shared, "Shared_Wall", true).ToArray());
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = sets[0];
            Debug.Log($"[{nameof(SeasonVisualSetSetup)}] 季节模块组已就绪：{SetFolder}，并已连到 LevelVisualConfig", sets[0]);
        }

        private static void Fill(SeasonVisualSet set, List<GameObject> own, List<GameObject> shared, bool includeSharedGrass)
        {
            var serialized = new SerializedObject(set);
            SetIfEmpty(serialized, "_floorPrefab", FirstFloor(own, false) ?? FirstFloor(shared, false));
            SetIfEmpty(serialized, "_floorCornerPrefab", FirstFloor(own, true) ?? FirstFloor(shared, true));
            SetArrayIfEmpty(serialized, "_uniqueWallPrefabs", Filter(own, "Unique_Wall", true).ToArray());

            List<GameObject> grass = Filter(own, "Grass", true);
            if (includeSharedGrass)
            {
                grass.AddRange(Filter(shared, "Grass", true));
            }

            SetArrayIfEmpty(serialized, "_grassPrefabs", grass.ToArray());
            SetArrayIfEmpty(serialized, "_rimTallPrefabs", Filter(own, "Tree", true).ToArray());

            // 季节文件夹里剩下的（花、灌木等）都算矮装饰
            var low = new List<GameObject>();
            foreach (GameObject prefab in own)
            {
                if (!Contains(prefab.name, "Floor") && !Contains(prefab.name, "Unique_Wall") && !Contains(prefab.name, "Grass") && !Contains(prefab.name, "Tree"))
                {
                    low.Add(prefab);
                }
            }

            SetArrayIfEmpty(serialized, "_rimLowPrefabs", low.ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
        }

        private static GameObject FirstFloor(List<GameObject> prefabs, bool isCorner)
        {
            foreach (GameObject prefab in prefabs)
            {
                if (Contains(prefab.name, "Floor") && Contains(prefab.name, "Rounded") == isCorner)
                {
                    return prefab;
                }
            }

            return null;
        }

        private static List<GameObject> Filter(List<GameObject> prefabs, string keyword, bool isMatch)
        {
            var result = new List<GameObject>();
            foreach (GameObject prefab in prefabs)
            {
                if (Contains(prefab.name, keyword) == isMatch)
                {
                    result.Add(prefab);
                }
            }

            return result;
        }

        private static List<GameObject> LoadPrefabs(string folder, bool isTopLevelOnly)
        {
            var result = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (isTopLevelOnly && Path.GetDirectoryName(path)?.Replace('\\', '/') != folder)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    result.Add(prefab);
                }
            }

            return result;
        }

        private static void SetIfEmpty(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == null && value != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetArrayIfEmpty(SerializedObject serialized, string propertyName, Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (HasAnyReference(property) || values.Length == 0)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        /// <summary>
        /// 列表里只剩丢失的引用（预制体被删了）也算空，这样换了素材后重新运行能把列表重新填上。
        /// </summary>
        private static bool HasAnyReference(SerializedProperty array)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
