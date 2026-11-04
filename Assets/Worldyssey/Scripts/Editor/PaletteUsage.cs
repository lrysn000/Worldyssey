using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一张调色板贴图的色块使用情况：自动识别色块大小，记录每个色块被哪些模型用到。
    /// </summary>
    internal sealed class PaletteUsage
    {
        public readonly Texture2D Texture;
        public readonly Color32[] Pixels;
        public readonly int Width;
        public readonly int Height;
        public readonly int CellSize;
        public readonly Dictionary<Vector2Int, HashSet<PrefabLabel>> Cells = new Dictionary<Vector2Int, HashSet<PrefabLabel>>();

        private PaletteUsage(Texture2D texture, Color32[] pixels, int width, int height, int cellSize)
        {
            Texture = texture;
            Pixels = pixels;
            Width = width;
            Height = height;
            CellSize = cellSize;
        }

        public int Columns => Width / CellSize;
        public int Rows => Height / CellSize;

        /// <summary>
        /// 直接读取图片文件得到像素，不依赖贴图的 Read/Write 和压缩设置。
        /// </summary>
        public static PaletteUsage Create(Texture2D texture)
        {
            var copy = new Texture2D(2, 2);
            copy.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)));
            Color32[] pixels = copy.GetPixels32();
            var usage = new PaletteUsage(texture, pixels, copy.width, copy.height, DetectCellSize(pixels, copy.width, copy.height));
            Object.DestroyImmediate(copy);
            return usage;
        }

        /// <summary>
        /// 记录一个子网格用到的色块（按三角形中心的 UV 取色块）。网格不可读时返回 false。
        /// </summary>
        public bool AddSubMesh(Mesh mesh, int subMesh, PrefabLabel label)
        {
            Vector2[] uvs = mesh.uv;
            if (uvs.Length == 0)
            {
                return mesh.vertexCount == 0;
            }

            int[] triangles = mesh.GetTriangles(subMesh);
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector2 uv = (uvs[triangles[i]] + uvs[triangles[i + 1]] + uvs[triangles[i + 2]]) / 3f;

                // 贴图是重复模式，UV 超出 0~1 时取小数部分
                uv = new Vector2(uv.x - Mathf.Floor(uv.x), uv.y - Mathf.Floor(uv.y));
                var cell = new Vector2Int(
                    Mathf.Min(Columns - 1, (int)(uv.x * Width) / CellSize),
                    Mathf.Min(Rows - 1, (int)(uv.y * Height) / CellSize));
                if (!Cells.TryGetValue(cell, out HashSet<PrefabLabel> labels))
                {
                    labels = new HashSet<PrefabLabel>();
                    Cells.Add(cell, labels);
                }

                labels.Add(label);
            }

            return true;
        }

        /// <summary>
        /// 色块中心的颜色。色块坐标以左下角为原点（与 UV 一致）。
        /// </summary>
        public Color32 GetCellColor(Vector2Int cell)
        {
            int x = cell.x * CellSize + CellSize / 2;
            int y = cell.y * CellSize + CellSize / 2;
            return Pixels[y * Width + x];
        }

        /// <summary>
        /// 色块大小 = 所有同色连续段长度的最大公约数（横向和纵向）。
        /// </summary>
        private static int DetectCellSize(Color32[] pixels, int width, int height)
        {
            int divisor = 0;
            for (int y = 0; y < height; y++)
            {
                int run = 1;
                for (int x = 1; x < width; x++)
                {
                    if (Same(pixels[y * width + x], pixels[y * width + x - 1]))
                    {
                        run++;
                        continue;
                    }

                    divisor = Gcd(divisor, run);
                    run = 1;
                }

                divisor = Gcd(divisor, run);
            }

            for (int x = 0; x < width; x++)
            {
                int run = 1;
                for (int y = 1; y < height; y++)
                {
                    if (Same(pixels[y * width + x], pixels[(y - 1) * width + x]))
                    {
                        run++;
                        continue;
                    }

                    divisor = Gcd(divisor, run);
                    run = 1;
                }

                divisor = Gcd(divisor, run);
            }

            return Mathf.Max(1, divisor);
        }

        private static bool Same(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                (a, b) = (b, a % b);
            }

            return a;
        }
    }
}
