using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Worldyssey.Data;
using Worldyssey.Rendering;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 接入全局风格化：生成 <see cref="StylizeSettings"/>，给当前 URP Renderer 加上 <see cref="StylizeRendererFeature"/>，
    /// 把场景相机切换为正交并连到 <see cref="CameraRig"/>。可以重复运行，已有的只更新引用。
    /// 材质转换是单独的菜单（Convert Materials To Toon），因为它会批量改材质。
    /// </summary>
    public static class StylizeSetup
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Setup Stylized Rendering";
        private const string SettingsPath = ProjectPaths.Config + "/StylizeSettings.asset";
        private const string ShaderName = "Hidden/Worldyssey/Stylize";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            StylizeSettings settings = GetOrCreateSettings();
            ScriptableRendererData rendererData = FindRendererData();
            if (rendererData == null)
            {
                Debug.LogError($"[{nameof(StylizeSetup)}] 找不到当前 URP 管线的 Renderer，未做任何修改");
                return;
            }

            StylizeRendererFeature feature = GetOrAddFeature(rendererData);
            var featureObject = new SerializedObject(feature);
            featureObject.FindProperty("_settings").objectReferenceValue = settings;
            featureObject.FindProperty("_shader").objectReferenceValue = Shader.Find(ShaderName);
            featureObject.ApplyModifiedPropertiesWithoutUndo();
            feature.Create();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();

            SetupCamera(settings);
            Selection.activeObject = settings;
            Debug.Log($"[{nameof(StylizeSetup)}] 风格化渲染已接入（{rendererData.name}），参数在 {SettingsPath}，记得保存场景", settings);
        }

        private static StylizeSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<StylizeSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            AssetFolderUtility.EnsureFolder(ProjectPaths.Config);
            settings = ScriptableObject.CreateInstance<StylizeSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        /// <summary>当前管线资源的默认 Renderer（列表第一个）。列表是私有字段，只能通过序列化读取。</summary>
        private static ScriptableRendererData FindRendererData()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))
            {
                return null;
            }

            SerializedProperty list = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            return list != null && list.arraySize > 0 ? list.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData : null;
        }

        /// <summary>
        /// 与 URP 自带的 Renderer Inspector 添加功能的方式一致：作为子资源加入 Renderer，同时登记到功能列表和本地 ID 映射。
        /// </summary>
        private static StylizeRendererFeature GetOrAddFeature(ScriptableRendererData rendererData)
        {
            foreach (ScriptableRendererFeature existing in rendererData.rendererFeatures)
            {
                if (existing is StylizeRendererFeature stylize)
                {
                    return stylize;
                }
            }

            var feature = ScriptableObject.CreateInstance<StylizeRendererFeature>();
            feature.name = "Stylize";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

            var serialized = new SerializedObject(rendererData);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedProperties();
            return feature;
        }

        private static void SetupCamera(StylizeSettings settings)
        {
            var rig = Object.FindObjectOfType<CameraRig>(true);
            if (rig == null)
            {
                Debug.LogWarning($"[{nameof(StylizeSetup)}] 当前场景没有 {nameof(CameraRig)}，相机未设置（打开 Main 场景后再运行一次）");
                return;
            }

            GameUiBuilder.SetReference(rig, "_stylizeSettings", settings);
            var camera = UiThemeSceneSetup.GetReference<Camera>(new SerializedObject(rig), "_camera");
            if (camera != null && !camera.orthographic)
            {
                Undo.RecordObject(camera, "Setup Stylized Rendering");
                camera.orthographic = true;
            }

            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        }
    }
}
