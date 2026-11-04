using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 批量整理 Prefabs/Environment 下的场景模块，让它们符合格子规格：
    /// 结构改为“空根物体 + Model 子物体”，删掉碰撞体，水平居中；地块顶面对齐 y = 0，其余底面对齐 y = 0；
    /// 墙缩小到格子以内，树统一缩放到固定高度；草的模型开启 Read/Write，供运行时合并网格。按文件名分类，可以重复运行。
    /// </summary>
    public static class EnvironmentPrefabNormalizer
    {
        private const string MenuPath = "Worldyssey/Art/Normalize Environment Prefabs";
        private const string EnvironmentFolder = "Assets/Worldyssey/Prefabs/Environment";
        private const string ModelName = "Model";

        private const float UniqueWallFootprint = 0.9f;
        private const float UniqueWallHeight = 0.8f;
        private const float SharedWallFootprint = 0.95f;
        private const float SharedWallHeight = 1.2f;
        private const float TreeHeight = 2f;

        private enum Category
        {
            Floor,
            UniqueWall,
            SharedWall,
            Tree,
            Grass,
            Sky,
            Decor,
        }

        [MenuItem(MenuPath)]
        public static void NormalizeAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnvironmentFolder });
            var report = new StringBuilder($"[{nameof(EnvironmentPrefabNormalizer)}] 整理了 {guids.Length} 个预制体：\n");
            var grassModels = new HashSet<string>();
            foreach (string guid in guids)
            {
                report.AppendLine(Normalize(AssetDatabase.GUIDToAssetPath(guid), grassModels));
            }

            AssetDatabase.SaveAssets();
            report.AppendLine($"开启 Read/Write 的草模型：{EnableReadWrite(grassModels)} 个");
            Debug.Log(report.ToString());
        }

        private static string Normalize(string path, HashSet<string> grassModels)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Category category = Classify(root.name);
                if (category == Category.Sky)
                {
                    // 天穹和云的位置、大小由天空系统在运行时决定，这里只删碰撞体，不动结构
                    int skyColliders = RemoveColliders(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    return $"  {root.name}（Sky）：只删除碰撞体 {skyColliders} 个";
                }

                Transform model = EnsureModelChild(root.transform);
                int removedColliders = RemoveColliders(root);
                if (category == Category.Grass)
                {
                    CollectModelPaths(root, grassModels);
                }

                if (!TryGetBounds(root.transform, out Bounds bounds))
                {
                    return $"  {root.name}：没有网格，跳过";
                }

                float scale = FitScale(category, bounds.size);
                if (!Mathf.Approximately(scale, 1f))
                {
                    model.localScale *= scale;
                    TryGetBounds(root.transform, out bounds);
                }

                // 地块只对齐顶面：圆角地块本身不对称，水平居中会让它偏离格子
                Vector3 shift = category == Category.Floor
                    ? new Vector3(0f, -bounds.max.y, 0f)
                    : new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
                model.localPosition += shift;
                TryGetBounds(root.transform, out bounds);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return $"  {root.name}（{category}）：{bounds.size.x:F2} × {bounds.size.y:F2} × {bounds.size.z:F2} 米，缩放 ×{scale:F2}，删除碰撞体 {removedColliders} 个";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Category Classify(string prefabName)
        {
            if (Contains(prefabName, "Sky") || Contains(prefabName, "Cloud"))
            {
                return Category.Sky;
            }

            if (Contains(prefabName, "Floor"))
            {
                return Category.Floor;
            }

            if (Contains(prefabName, "Unique_Wall"))
            {
                return Category.UniqueWall;
            }

            if (Contains(prefabName, "Shared_Wall"))
            {
                return Category.SharedWall;
            }

            if (Contains(prefabName, "Grass"))
            {
                return Category.Grass;
            }

            return Contains(prefabName, "Tree") ? Category.Tree : Category.Decor;
        }

        /// <summary>
        /// 墙只缩小不放大；树缩放到统一高度；地块和其他装饰保持原大小。
        /// </summary>
        private static float FitScale(Category category, Vector3 size)
        {
            float footprint = Mathf.Max(size.x, size.z);
            switch (category)
            {
                case Category.UniqueWall:
                    return Mathf.Min(1f, UniqueWallFootprint / footprint, UniqueWallHeight / size.y);
                case Category.SharedWall:
                    return Mathf.Min(1f, SharedWallFootprint / footprint, SharedWallHeight / size.y);
                case Category.Tree:
                    return size.y > 0f ? TreeHeight / size.y : 1f;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// 保证根物体是不缩放、不旋转的空物体，模型和原有子物体都在 Model 下面。
        /// </summary>
        private static Transform EnsureModelChild(Transform root)
        {
            Transform existing = root.Find(ModelName);
            bool hasRootMesh = root.TryGetComponent(out MeshFilter _) || root.TryGetComponent(out MeshRenderer _);
            if (existing != null && !hasRootMesh)
            {
                return existing;
            }

            var children = new List<Transform>();
            foreach (Transform child in root)
            {
                children.Add(child);
            }

            var model = new GameObject(ModelName).transform;
            model.SetParent(root, false);
            model.localRotation = root.localRotation;
            model.localScale = root.localScale;
            foreach (Transform child in children)
            {
                child.SetParent(model, false);
            }

            if (root.TryGetComponent(out MeshFilter sourceFilter))
            {
                EditorUtility.CopySerialized(sourceFilter, model.gameObject.AddComponent<MeshFilter>());
            }

            if (root.TryGetComponent(out MeshRenderer sourceRenderer))
            {
                EditorUtility.CopySerialized(sourceRenderer, model.gameObject.AddComponent<MeshRenderer>());
                Object.DestroyImmediate(sourceRenderer);
            }

            if (sourceFilter != null)
            {
                Object.DestroyImmediate(sourceFilter);
            }

            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            return model;
        }

        private static void CollectModelPaths(GameObject root, HashSet<string> paths)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    paths.Add(AssetDatabase.GetAssetPath(filter.sharedMesh));
                }
            }
        }

        /// <summary>
        /// 运行时合并网格要求源网格可读，否则打包后的游戏里合并会失败。返回实际修改的模型数量。
        /// </summary>
        private static int EnableReadWrite(HashSet<string> modelPaths)
        {
            int changed = 0;
            foreach (string modelPath in modelPaths)
            {
                if (AssetImporter.GetAtPath(modelPath) is ModelImporter importer && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            return changed;
        }

        private static int RemoveColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                Object.DestroyImmediate(collider);
            }

            return colliders.Length;
        }

        /// <summary>
        /// 根物体坐标系下所有网格的包围盒。用网格数据计算，不依赖渲染器是否已经更新。
        /// </summary>
        private static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;
                Bounds local = mesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
                    if (!hasBounds)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }

            return hasBounds;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
