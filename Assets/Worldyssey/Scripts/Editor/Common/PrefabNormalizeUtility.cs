using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 整理模型预制体的共用方法：统一成“空根物体 + Model 子物体”、删碰撞体、计算包围盒。
    /// 环境模块和玩法物件的整理工具都用它。
    /// </summary>
    internal static class PrefabNormalizeUtility
    {
        internal const string ModelName = "Model";

        /// <summary>
        /// 保证根物体是不缩放、不旋转的空物体，模型和原有子物体都在 Model 下面。
        /// </summary>
        internal static Transform EnsureModelChild(Transform root)
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

        internal static int RemoveColliders(GameObject root)
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
        internal static bool TryGetBounds(Transform root, out Bounds bounds)
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
    }
}
