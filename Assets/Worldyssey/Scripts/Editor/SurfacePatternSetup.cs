using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给终点和底座的顶面加刻纹图案：终点是四角角标加中心方点，底座是外圈细环加四个朝外的三角刻痕（对应四个可进入的面）。
    /// 图案是平贴在顶面的一片四边形，用卡通材质透明裁剪；底座激活时只有这层图案发光（见 PedestalGlowSet）。
    /// 顺带把底座石头改成浅灰色。改的是 LevelVisualConfig 引用的两个预制体，可以重复运行。
    /// </summary>
    public static class SurfacePatternSetup
    {
        /// <summary>图案子物体的名字，PedestalGlowSet 按这个名字找需要发光的渲染器。</summary>
        internal const string PatternChildName = PedestalGlowSet.PatternChildName;

        private const string MenuPath = "Worldyssey/Scene Setup/Add Goal And Pedestal Patterns";
        private const string TextureFolder = ProjectPaths.Textures + "/Tiles";
        private const string GoalTexturePath = TextureFolder + "/Pattern_Goal.png";
        private const string PedestalTexturePath = TextureFolder + "/Pattern_Pedestal.png";
        private const string GoalMaterialPath = ProjectPaths.Materials + "/M_GoalPattern.mat";
        private const string PedestalMaterialPath = ProjectPaths.Materials + "/M_PedestalPattern.mat";
        private const string ToonShaderName = "Worldyssey/Toon";
        private const string ModelChildName = "Model";

        // 高出顶面一点点，避免与顶面闪烁
        private const float SurfaceLift = 0.004f;

        private static readonly Color GoalPatternColor = new Color(0.55f, 0.38f, 0.10f);
        private static readonly Color PedestalPatternColor = new Color(0.42f, 0.41f, 0.39f);
        private static readonly Color PedestalStoneColor = new Color(0.74f, 0.72f, 0.68f);

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            Shader toon = Shader.Find(ToonShaderName);
            if (config == null || config.GoalPrefab == null || config.PedestalPrefab == null || toon == null)
            {
                Debug.LogError($"[{nameof(SurfacePatternSetup)}] 找不到 LevelVisualConfig、终点或底座预制体、{ToonShaderName} 着色器，未做修改");
                return;
            }

            Material goal = CreateMaterial(GoalMaterialPath, GoalTexturePath, GoalPatternColor, toon);
            Material pedestal = CreateMaterial(PedestalMaterialPath, PedestalTexturePath, PedestalPatternColor, toon);
            if (goal == null || pedestal == null)
            {
                return;
            }

            bool isGoalDone = AddPattern(config.GoalPrefab, goal, false);
            bool isPedestalDone = AddPattern(config.PedestalPrefab, pedestal, true);
            AssetDatabase.SaveAssets();
            if (isGoalDone && isPedestalDone)
            {
                Debug.Log($"[{nameof(SurfacePatternSetup)}] 终点和底座已加上刻纹；颜色在 {GoalMaterialPath}、{PedestalMaterialPath} 里调整");
            }
        }

        private static Material CreateMaterial(string materialPath, string texturePath, Color color, Shader toon)
        {
            if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
            {
                Debug.LogError($"[{nameof(SurfacePatternSetup)}] 找不到图案 {texturePath}");
                return null;
            }

            // 像素图案：点采样、不生成 Mipmap、不压缩，边缘清晰；透明部分用来裁剪
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(toon);
                AssetDatabase.CreateAsset(material, materialPath);
                material.SetColor("_BaseColor", color);
            }

            material.shader = toon;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetFloat("_AlphaClip", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cutoff", 0.5f);
            material.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool AddPattern(GameObject prefab, Material material, bool isPedestal)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform model = root.transform.Find(ModelChildName);
                Renderer surface = model != null ? model.GetComponent<Renderer>() : null;
                if (surface == null)
                {
                    Debug.LogError($"[{nameof(SurfacePatternSetup)}] {prefab.name} 没有名为 {ModelChildName} 的渲染器，未修改", prefab);
                    return false;
                }

                Transform old = root.transform.Find(PatternChildName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                // 预制体根在原点、无旋转缩放，世界包围盒即根节点下的尺寸
                Bounds bounds = surface.bounds;
                var pattern = new GameObject(PatternChildName, typeof(MeshFilter), typeof(MeshRenderer));
                pattern.transform.SetParent(root.transform, false);
                pattern.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y + SurfaceLift, bounds.center.z);

                // 自带四边形朝 -Z，绕 X 转 90° 后朝上
                pattern.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                pattern.transform.localScale = new Vector3(bounds.size.x, bounds.size.z, 1f);
                pattern.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var renderer = pattern.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;

                if (isPedestal)
                {
                    TintStone(surface.sharedMaterial);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void TintStone(Material stone)
        {
            if (stone == null)
            {
                return;
            }

            Undo.RecordObject(stone, "Pedestal Stone Color");
            if (stone.HasProperty("_BaseColor"))
            {
                stone.SetColor("_BaseColor", PedestalStoneColor);
            }

            if (stone.HasProperty("_Color"))
            {
                stone.SetColor("_Color", PedestalStoneColor);
            }

            EditorUtility.SetDirty(stone);
        }
    }
}
