using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键配置会动的草：生成四季草材质（Worldyssey/Grass 着色器，纯色渐变、不用贴图），连到各季节模块组，
    /// 并在当前场景加入互动位置收集器。已有的材质和引用不会覆盖，可以重复运行。
    /// </summary>
    public static class GrassMaterialSetup
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Setup Animated Grass (Current Scene)";
        private const string ShaderName = "Worldyssey/Grass";
        private const string MaterialFolder = ProjectPaths.Materials + "/Grass";
        private const string SeasonFolder = ProjectPaths.Seasons;

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        // 春嫩绿，夏深绿、风最大，秋枯黄，冬覆霜、草茎较硬摆得少
        private static readonly Color[] RootColors =
        {
            new Color(0.32f, 0.52f, 0.2f), new Color(0.16f, 0.42f, 0.18f), new Color(0.52f, 0.36f, 0.14f), new Color(0.5f, 0.56f, 0.6f),
        };

        private static readonly Color[] TipColors =
        {
            new Color(0.66f, 0.88f, 0.42f), new Color(0.38f, 0.7f, 0.28f), new Color(0.92f, 0.72f, 0.32f), new Color(0.93f, 0.96f, 0.98f),
        };

        private static readonly float[] WindStrengths = { 0.07f, 0.09f, 0.08f, 0.035f };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[{nameof(GrassMaterialSetup)}] 找不到着色器 {ShaderName}，检查 Shaders/Grass.shader 是否编译通过，未做任何修改");
                return;
            }

            AssetFolderUtility.EnsureFolder(MaterialFolder);
            for (int i = 0; i < SeasonNames.Length; i++)
            {
                Material material = GetOrCreateMaterial(shader, i);
                var set = AssetDatabase.LoadAssetAtPath<SeasonVisualSet>($"{SeasonFolder}/SeasonVisualSet_{SeasonNames[i]}.asset");
                if (set == null)
                {
                    Debug.LogWarning($"[{nameof(GrassMaterialSetup)}] 没有 {SeasonNames[i]} 的季节模块组，草材质未连上");
                    continue;
                }

                var serialized = new SerializedObject(set);
                SerializedProperty property = serialized.FindProperty("_grassMaterial");
                if (property.objectReferenceValue == null)
                {
                    property.objectReferenceValue = material;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(set);
                }
            }

            AssetDatabase.SaveAssets();
            SetupScene();
        }

        private static Material GetOrCreateMaterial(Shader shader, int season)
        {
            string path = $"{MaterialFolder}/M_Grass_{SeasonNames[season]}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(shader) { enableInstancing = false };
            material.SetColor("_RootColor", RootColors[season]);
            material.SetColor("_TipColor", TipColors[season]);
            material.SetFloat("_WindStrength", WindStrengths[season]);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetupScene()
        {
            var levelView = Object.FindObjectOfType<LevelView>();
            LevelVisualConfig config = levelView != null
                ? new SerializedObject(levelView).FindProperty("_visualConfig").objectReferenceValue as LevelVisualConfig
                : null;
            if (levelView == null || config == null)
            {
                Debug.LogError($"[{nameof(GrassMaterialSetup)}] 当前场景没有设置好表现配置的 LevelView，材质已生成但场景未修改");
                return;
            }

            if (!levelView.TryGetComponent(out GrassInteractorFeeder feeder))
            {
                feeder = Undo.AddComponent<GrassInteractorFeeder>(levelView.gameObject);
            }

            GameUiBuilder.SetReference(feeder, "_levelView", levelView);
            GameUiBuilder.SetReference(feeder, "_visualConfig", config);
            EditorSceneManager.MarkSceneDirty(levelView.gameObject.scene);
            Selection.activeObject = feeder;
            Debug.Log($"[{nameof(GrassMaterialSetup)}] 会动的草已就绪，记得保存场景", feeder);
        }
    }
}
