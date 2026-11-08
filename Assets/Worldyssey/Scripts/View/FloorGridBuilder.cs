using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.View
{
    /// <summary>
    /// 在地面上铺一张覆盖可玩区域的平面，由 FloorGrid 着色器画出每一格的衬线，让玩家分清地块。
    /// 整块区域只有一个四边形，线在片元里算，所以线宽能按像素化后的像素保持一致。
    /// </summary>
    public static class FloorGridBuilder
    {
        private const string GridName = "FloorGrid";

        // 平面比可玩区域每边多出半格，边界线的外半边也有地方画
        private const float EdgeMargin = 0.5f;

        // 抬离地面一点点，配合着色器里的深度偏移避免与地面闪烁
        private const float SurfaceLift = 0.005f;

        /// <summary>
        /// 在 <paramref name="root"/> 下生成格子衬线。<paramref name="material"/> 为空时不生成（未启用）。
        /// <paramref name="floorPrefab"/> 用来确定地面顶部高度。
        /// </summary>
        public static void Build(LevelVisualConfig config, LevelMap map, GameObject floorPrefab, Material material, Transform root)
        {
            if (material == null)
            {
                return;
            }

            float cell = config.CellSize;
            float height = GetTopHeight(floorPrefab) + SurfaceLift;

            // 格子中心在 GridToLocal(pos)，所以格线在 (整数 - 0.5) * 边长 处
            float minX = (-0.5f - EdgeMargin) * cell;
            float minZ = (-0.5f - EdgeMargin) * cell;
            float maxX = (map.Width - 0.5f + EdgeMargin) * cell;
            float maxZ = (map.Height - 0.5f + EdgeMargin) * cell;
            var size = new Vector2(map.Width, map.Height);

            var mesh = new Mesh { name = GridName };
            mesh.vertices = new[]
            {
                new Vector3(minX, height, minZ), new Vector3(minX, height, maxZ),
                new Vector3(maxX, height, maxZ), new Vector3(maxX, height, minZ),
            };
            mesh.uv = new[]
            {
                new Vector2(-EdgeMargin, -EdgeMargin), new Vector2(-EdgeMargin, map.Height + EdgeMargin),
                new Vector2(map.Width + EdgeMargin, map.Height + EdgeMargin), new Vector2(map.Width + EdgeMargin, -EdgeMargin),
            };
            mesh.uv2 = new[] { size, size, size, size };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();

            var grid = new GameObject(GridName);
            grid.transform.SetParent(root, false);
            grid.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = grid.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            grid.AddComponent<RuntimeMeshOwner>().Initialize(mesh);
        }

        /// <summary>地面预制体所有网格的最高点（相对预制体根节点），取不到时为 0。</summary>
        private static float GetTopHeight(GameObject floorPrefab)
        {
            if (floorPrefab == null)
            {
                return 0f;
            }

            float top = float.NegativeInfinity;
            Matrix4x4 toRoot = floorPrefab.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in floorPrefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toPrefab = toRoot * filter.transform.localToWorldMatrix;
                Bounds bounds = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    top = Mathf.Max(top, toPrefab.MultiplyPoint3x4(local).y);
                }
            }

            return float.IsNegativeInfinity(top) ? 0f : top;
        }
    }
}
