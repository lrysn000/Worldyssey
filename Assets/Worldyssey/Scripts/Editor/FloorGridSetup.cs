using UnityEditor;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 创建地面格子衬线的材质并填到 <see cref="LevelVisualConfig"/>。可以重复运行：材质已存在时只重新关联，不改动已调好的颜色和线宽。
    /// </summary>
    public static class FloorGridSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Add Floor Grid Lines";
        private const string ShaderName = "Worldyssey/FloorGrid";
        private const string MaterialPath = ProjectPaths.Materials + "/M_FloorGrid.mat";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            Shader shader = Shader.Find(ShaderName);
            if (config == null || shader == null)
            {
                Debug.LogError($"[{nameof(FloorGridSetup)}] 找不到 LevelVisualConfig 或着色器 {ShaderName}，未做任何修改");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("_floorGridMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Selection.activeObject = material;
            Debug.Log($"[{nameof(FloorGridSetup)}] 地面衬线已启用，材质 {MaterialPath}：Multiply Color 越深线越明显，Line Width 是像素化后的像素数", material);
        }
    }
}
