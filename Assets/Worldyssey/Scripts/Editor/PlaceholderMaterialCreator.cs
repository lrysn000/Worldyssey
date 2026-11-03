using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键生成占位材质的编辑器菜单。正式美术替换前，所有网格物体都用这些纯色材质区分类型。
    /// </summary>
    public static class PlaceholderMaterialCreator
    {
        private const string MenuPath = "Worldyssey/Setup/Create Placeholder Materials";
        private const string TargetFolder = "Assets/Worldyssey/Materials/Placeholder";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const string BuiltInLitShaderName = "Standard";

        private readonly struct MaterialSpec
        {
            public readonly string Name;
            public readonly Color Color;

            public MaterialSpec(string name, Color color)
            {
                Name = name;
                Color = color;
            }
        }

        // 颜色只求类型之间一眼能分清，M8 会整体替换，不必细调
        private static readonly MaterialSpec[] Specs =
        {
            new MaterialSpec("M_Floor", new Color(0.75f, 0.75f, 0.72f)),
            new MaterialSpec("M_SharedWall", new Color(0.25f, 0.27f, 0.30f)),
            new MaterialSpec("M_UniqueWall", new Color(0.45f, 0.50f, 0.62f)),
            new MaterialSpec("M_Pedestal", new Color(0.30f, 0.72f, 0.75f)),
            new MaterialSpec("M_Goal", new Color(0.95f, 0.80f, 0.25f)),
            new MaterialSpec("M_Player", new Color(0.20f, 0.50f, 0.95f)),
            new MaterialSpec("M_MappedBox", new Color(0.88f, 0.45f, 0.20f)),
            new MaterialSpec("M_PortalBox", new Color(0.65f, 0.35f, 0.90f)),
            new MaterialSpec("M_HintEnterable", new Color(0.30f, 0.90f, 0.40f)),
            new MaterialSpec("M_HintBlocked", new Color(0.90f, 0.25f, 0.25f)),
            new MaterialSpec("M_HintConflicted", new Color(0.45f, 0.45f, 0.48f)),
        };

        [MenuItem(MenuPath)]
        public static void CreatePlaceholderMaterials()
        {
            Shader shader = FindLitShader();
            if (shader == null)
            {
                Debug.LogError($"[{nameof(PlaceholderMaterialCreator)}] 找不到可用的 Lit 着色器，未生成任何材质");
                return;
            }

            EnsureFolder(TargetFolder);

            int createdCount = 0;
            int skippedCount = 0;
            foreach (MaterialSpec spec in Specs)
            {
                string path = $"{TargetFolder}/{spec.Name}.mat";

                // 已存在的不覆盖：用户可能已经手动调过颜色，或者场景里已有引用
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                {
                    skippedCount++;
                    continue;
                }

                // Material.color 写入的是着色器里标了 [MainColor] 的属性，URP 和 Built-in 都适用
                var material = new Material(shader) { color = spec.Color };
                AssetDatabase.CreateAsset(material, path);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(PlaceholderMaterialCreator)}] 生成 {createdCount} 个，跳过已存在 {skippedCount} 个，着色器：{shader.name}，目录：{TargetFolder}");
        }

        private static Shader FindLitShader()
        {
            // 根据当前渲染管线选着色器，这样以后换管线也不用改这个工具
            string shaderName = GraphicsSettings.currentRenderPipeline != null
                ? UrpLitShaderName
                : BuiltInLitShaderName;
            return Shader.Find(shaderName);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int lastSlash = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, lastSlash);
            string child = folderPath.Substring(lastSlash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
