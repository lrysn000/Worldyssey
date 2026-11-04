using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Worldyssey.View
{
    /// <summary>
    /// 把一个世界里所有草合并成每种材质一个网格。草数量多、每簇很小，逐个生成物体会让物体数和 Draw Call 都很高。
    /// 直接读预制体的网格计算位置，不实例化中间物体。源网格未开启 Read/Write 时退回逐个实例化，打包后也能正常显示。
    /// </summary>
    public sealed class GrassMeshBatch
    {
        private const string BatchName = "GrassBatch";

        private readonly Transform _root;
        private readonly Dictionary<GameObject, PrefabParts> _partsCache = new Dictionary<GameObject, PrefabParts>();
        private readonly Dictionary<Material, List<CombineInstance>> _instances = new Dictionary<Material, List<CombineInstance>>();
        private readonly Dictionary<Material, ShadowCastingMode> _shadowModes = new Dictionary<Material, ShadowCastingMode>();

        public GrassMeshBatch(Transform root)
        {
            _root = root;
        }

        /// <summary>
        /// 加入一簇草。位置、旋转相对于构造时传入的根节点。
        /// </summary>
        public void Add(GameObject prefab, Vector3 localPosition, Quaternion localRotation, float scale)
        {
            PrefabParts parts = GetParts(prefab);
            if (!parts.IsCombinable)
            {
                GameObject tuft = Object.Instantiate(prefab, _root);
                tuft.transform.localPosition = localPosition;
                tuft.transform.localRotation = localRotation;
                tuft.transform.localScale = Vector3.one * scale;
                return;
            }

            Matrix4x4 placement = Matrix4x4.TRS(localPosition, localRotation, Vector3.one * scale);
            foreach (Part part in parts.Parts)
            {
                for (int subMesh = 0; subMesh < part.Mesh.subMeshCount; subMesh++)
                {
                    Material material = part.Materials[Mathf.Min(subMesh, part.Materials.Length - 1)];
                    if (material == null)
                    {
                        continue;
                    }

                    if (!_instances.TryGetValue(material, out List<CombineInstance> list))
                    {
                        list = new List<CombineInstance>();
                        _instances.Add(material, list);
                        _shadowModes.Add(material, part.ShadowMode);
                    }

                    list.Add(new CombineInstance { mesh = part.Mesh, subMeshIndex = subMesh, transform = placement * part.Matrix });
                }
            }
        }

        /// <summary>
        /// 生成合并后的网格物体。调用后可以继续 Add 再 Build，但一般每个世界只调用一次。
        /// </summary>
        public void Build()
        {
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in _instances)
            {
                // 草多时顶点数会超过 16 位索引的 65535 上限
                var mesh = new Mesh { name = BatchName, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);

                var batch = new GameObject(BatchName);
                batch.transform.SetParent(_root, false);
                batch.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = batch.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = pair.Key;
                meshRenderer.shadowCastingMode = _shadowModes[pair.Key];
                batch.AddComponent<RuntimeMeshOwner>().Initialize(mesh);
            }

            _instances.Clear();
            _shadowModes.Clear();
        }

        private PrefabParts GetParts(GameObject prefab)
        {
            if (_partsCache.TryGetValue(prefab, out PrefabParts cached))
            {
                return cached;
            }

            var parts = new PrefabParts();
            Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    continue;
                }

                if (!filter.sharedMesh.isReadable)
                {
                    parts.IsCombinable = false;
                    Debug.LogWarning($"[{nameof(GrassMeshBatch)}] {prefab.name} 的网格未开启 Read/Write，改为逐个生成。"
                        + "运行 Worldyssey/Art/Normalize Environment Prefabs 可自动开启", prefab);
                }

                parts.Parts.Add(new Part(filter.sharedMesh, toRoot * filter.transform.localToWorldMatrix,
                    meshRenderer.sharedMaterials, meshRenderer.shadowCastingMode));
            }

            _partsCache.Add(prefab, parts);
            return parts;
        }

        private sealed class PrefabParts
        {
            public readonly List<Part> Parts = new List<Part>(1);
            public bool IsCombinable = true;
        }

        private readonly struct Part
        {
            public readonly Mesh Mesh;
            public readonly Matrix4x4 Matrix;
            public readonly Material[] Materials;
            public readonly ShadowCastingMode ShadowMode;

            public Part(Mesh mesh, Matrix4x4 matrix, Material[] materials, ShadowCastingMode shadowMode)
            {
                Mesh = mesh;
                Matrix = matrix;
                Materials = materials;
                ShadowMode = shadowMode;
            }
        }
    }
}
