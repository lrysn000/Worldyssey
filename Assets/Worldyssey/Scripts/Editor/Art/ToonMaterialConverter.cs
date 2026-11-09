using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把项目材质（Art/Materials 下）中不透明的 URP Lit / Simple Lit 材质换成 Worldyssey/Toon，保留贴图、颜色、透明裁剪、自发光和剔除设置。
    /// 第三方包里的材质不动；半透明材质跳过（卡通材质只做不透明）。可以重复运行，已转换的不受影响。
    /// </summary>
    public static class ToonMaterialConverter
    {
        private const string MenuPath = "Worldyssey/Art/Convert Materials To Toon";
        private const string ToonShaderName = "Worldyssey/Toon";
        private static readonly string[] SourceShaders = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit" };

        [MenuItem(MenuPath)]
        public static void Convert()
        {
            Shader toon = Shader.Find(ToonShaderName);
            if (toon == null)
            {
                Debug.LogError($"[{nameof(ToonMaterialConverter)}] 找不到着色器 {ToonShaderName}，未做任何修改");
                return;
            }

            if (!EditorUtility.DisplayDialog("Convert Materials To Toon",
                    $"将把 {ProjectPaths.Materials} 下不透明的 Lit / Simple Lit 材质改为 {ToonShaderName}。\n可以用 Ctrl+Z 撤销。继续吗？", "转换", "取消"))
            {
                return;
            }

            int converted = 0;
            int skipped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { ProjectPaths.Materials }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null || !IsSource(material.shader))
                {
                    continue;
                }

                if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                {
                    skipped++;
                    continue;
                }

                ConvertMaterial(material, toon);
                converted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(ToonMaterialConverter)}] 已转换 {converted} 个材质，跳过 {skipped} 个半透明材质");
        }

        private static bool IsSource(Shader shader)
        {
            return shader != null && System.Array.IndexOf(SourceShaders, shader.name) >= 0;
        }

        private static void ConvertMaterial(Material material, Shader toon)
        {
            Texture baseMap = material.GetTexture("_BaseMap");
            Vector2 scale = material.GetTextureScale("_BaseMap");
            Vector2 offset = material.GetTextureOffset("_BaseMap");
            Color baseColor = material.GetColor("_BaseColor");
            bool isAlphaClip = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;
            float cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            Color emission = material.IsKeywordEnabled("_EMISSION") && material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            float cull = material.HasProperty("_Cull") ? material.GetFloat("_Cull") : 2f;

            Undo.RecordObject(material, "Convert Materials To Toon");
            material.shader = toon;
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureOffset("_BaseMap", offset);
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_AlphaClip", isAlphaClip ? 1f : 0f);
            material.SetFloat("_Cutoff", cutoff);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Cull", cull);
            if (isAlphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            EditorUtility.SetDirty(material);
        }
    }
}
