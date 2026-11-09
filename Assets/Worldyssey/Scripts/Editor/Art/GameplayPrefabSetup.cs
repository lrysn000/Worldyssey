using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using Worldyssey.View;
using Object = UnityEngine.Object;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 接入 Prefabs/Gameplay/Imported 下的正式玩法物件：整理结构、删碰撞体、按类别缩放对齐，
    /// 给需要移动的物件挂 <see cref="EntityView"/>，再替换进所有 LevelVisualConfig。按文件名分类，可以重复运行。
    /// 传送箱子需要箱体渲染器和四块提示板，结构特殊，不在这里处理。
    /// </summary>
    public static class GameplayPrefabSetup
    {
        private const string MenuPath = "Worldyssey/Art/Setup Imported Gameplay Prefabs";
        private const string Folder = ProjectPaths.ImportedGameplayPrefabs;

        private const float BoxFootprint = 0.8f;
        private const float PlayerHeight = 0.9f;
        private const float MarkerFootprint = 0.9f;

        // 终点和底座上要放箱子，箱子底面在 y = 0，太厚会陷进去
        private const float MaxMarkerThickness = 0.05f;

        private enum Category
        {
            Unknown,
            MappedBox,
            Player,
            Goal,
            Pedestal,
            PortalBox,
        }

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var report = new StringBuilder($"[{nameof(GameplayPrefabSetup)}] 处理 {Folder}：\n");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }))
            {
                report.AppendLine(Process(AssetDatabase.GUIDToAssetPath(guid)));
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        private static string Process(string path)
        {
            string prefabName = System.IO.Path.GetFileNameWithoutExtension(path);
            Category category = Classify(prefabName);
            if (category == Category.Unknown)
            {
                return $"  {prefabName}：名字里没有 Box / Player / Goal / Pedestal，跳过";
            }

            if (category == Category.PortalBox)
            {
                return $"  {prefabName}：传送箱子需要连箱体渲染器和提示板，暂不自动接入";
            }

            string result = Normalize(path, category);
            AssignToConfigs(path, category);
            return result;
        }

        private static string Normalize(string path, Category category)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform model = PrefabNormalizeUtility.EnsureModelChild(root.transform);
                int removedColliders = PrefabNormalizeUtility.RemoveColliders(root);
                if (!PrefabNormalizeUtility.TryGetBounds(root.transform, out Bounds bounds))
                {
                    return $"  {root.name}：没有网格，跳过";
                }

                float scale = FitScale(category, bounds.size);
                model.localScale *= scale;
                PrefabNormalizeUtility.TryGetBounds(root.transform, out bounds);

                // 水平居中、底面贴地：原点就是格子中心的地面，推箱挤压以底面为基准，不会陷进地里
                model.localPosition += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
                PrefabNormalizeUtility.TryGetBounds(root.transform, out bounds);

                bool needsEntityView = category == Category.MappedBox || category == Category.Player;
                if (needsEntityView && !root.TryGetComponent(out EntityView _))
                {
                    root.AddComponent<EntityView>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                string warning = (category == Category.Goal || category == Category.Pedestal) && bounds.size.y > MaxMarkerThickness
                    ? $"  ⚠ 厚度 {bounds.size.y:F2} 米，超过 {MaxMarkerThickness} 米，箱子放上去会陷进去"
                    : string.Empty;
                return $"  {root.name}（{category}）：{bounds.size.x:F2} × {bounds.size.y:F2} × {bounds.size.z:F2} 米，缩放 ×{scale:F2}，删除碰撞体 {removedColliders} 个{warning}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 箱子按占地缩放；玩家按高度缩放；终点和底座按占地缩放，厚度不动（太厚只提示，不压扁模型）。
        /// </summary>
        private static float FitScale(Category category, Vector3 size)
        {
            float footprint = Mathf.Max(size.x, size.z);
            switch (category)
            {
                case Category.MappedBox:
                    return footprint > 0f ? BoxFootprint / footprint : 1f;
                case Category.Player:
                    return size.y > 0f ? PlayerHeight / size.y : 1f;
                case Category.Goal:
                case Category.Pedestal:
                    return footprint > 0f ? MarkerFootprint / footprint : 1f;
                default:
                    return 1f;
            }
        }

        private static void AssignToConfigs(string path, Category category)
        {
            Object value = category == Category.MappedBox || category == Category.Player
                ? AssetDatabase.LoadAssetAtPath<EntityView>(path)
                : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string field = category switch
            {
                Category.MappedBox => "_mappedBoxPrefab",
                Category.Player => "_playerPrefab",
                Category.Goal => "_goalPrefab",
                _ => "_pedestalPrefab",
            };

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LevelVisualConfig)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelVisualConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null || value == null)
                {
                    continue;
                }

                GameUiBuilder.SetReference(config, field, value);
                EditorUtility.SetDirty(config);
            }
        }

        private static Category Classify(string prefabName)
        {
            if (Contains(prefabName, "Portal"))
            {
                return Category.PortalBox;
            }

            if (Contains(prefabName, "Box"))
            {
                return Category.MappedBox;
            }

            if (Contains(prefabName, "Player"))
            {
                return Category.Player;
            }

            if (Contains(prefabName, "Goal"))
            {
                return Category.Goal;
            }

            return Contains(prefabName, "Pedestal") ? Category.Pedestal : Category.Unknown;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
