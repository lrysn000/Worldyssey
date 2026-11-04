using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键配置天空：生成天穹、太阳、云三个材质（专用着色器），改好 Environment 下的天穹和云预制体，
    /// 生成天空配置，给四季环境填上默认的天空和云色调，并在当前场景接入。可以重复运行，已有的不会覆盖。
    /// </summary>
    public static class SkySetup
    {
        private const string MenuPath = "Worldyssey/Setup/Setup Sky (Current Scene)";
        private const string EnvironmentFolder = ProjectPaths.EnvironmentPrefabs;
        private const string MaterialFolder = ProjectPaths.Materials + "/Sky";
        private const string SettingsPath = ProjectPaths.Config + "/SkySettings.asset";
        private const string SeasonFolder = ProjectPaths.Seasons;
        private const string SkyShaderName = "Worldyssey/SkyDome";
        private const string CloudShaderName = "Worldyssey/Cloud";
        private const string SunPivotName = "Sun";

        // 太阳圆盘和天空球用同一个着色器，队列晚一位，保证画在天空上面
        private const int SunRenderQueue = (int)RenderQueue.Background + 1;

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };
        private static readonly Color[] SkyTints =
        {
            new Color(1f, 0.9f, 0.95f), new Color(0.9f, 1f, 1f), new Color(1f, 0.9f, 0.78f), new Color(0.88f, 0.93f, 1f),
        };

        private static readonly Color[] CloudTints =
        {
            new Color(1f, 0.95f, 0.97f), Color.white, new Color(1f, 0.93f, 0.85f), new Color(0.92f, 0.95f, 1f),
        };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            string domePath = FindPrefab("SkyDome");
            List<string> cloudPaths = FindPrefabs("Cloud");
            Shader skyShader = Shader.Find(SkyShaderName);
            Shader cloudShader = Shader.Find(CloudShaderName);
            if (domePath == null || skyShader == null || cloudShader == null)
            {
                Debug.LogError($"[{nameof(SkySetup)}] 需要 {EnvironmentFolder} 下的 SkyDome 预制体，以及着色器 {SkyShaderName}、{CloudShaderName}，未做任何修改");
                return;
            }

            Texture texture = GetSkyTexture(domePath);
            AssetFolderUtility.EnsureFolder(MaterialFolder);
            Material domeMaterial = GetOrCreateMaterial("M_SkyDome", skyShader, texture, -1);
            Material sunMaterial = GetOrCreateMaterial("M_SkySun", skyShader, texture, SunRenderQueue);
            Material cloudMaterial = GetOrCreateMaterial("M_Cloud", cloudShader, texture, -1);

            SetupDomePrefab(domePath, domeMaterial, sunMaterial);
            var clouds = new List<GameObject>();
            foreach (string path in cloudPaths)
            {
                clouds.Add(SetupCloudPrefab(path, cloudMaterial));
            }

            SkySettings settings = GetOrCreateSettings(AssetDatabase.LoadAssetAtPath<SkyDome>(domePath), clouds);
            FillSeasonTints();
            AssetDatabase.SaveAssets();
            SetupScene(settings);
        }

        private static void SetupDomePrefab(string path, Material domeMaterial, Material sunMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveCollidersAndShadows(root);
                if (!root.TryGetComponent(out SkyDome dome))
                {
                    dome = root.AddComponent<SkyDome>();
                }

                Renderer domeRenderer = root.GetComponent<Renderer>();
                if (domeRenderer != null)
                {
                    domeRenderer.sharedMaterial = domeMaterial;
                }

                Transform sunPivot = root.transform.Find(SunPivotName);
                if (sunPivot != null)
                {
                    foreach (Renderer sunRenderer in sunPivot.GetComponentsInChildren<Renderer>(true))
                    {
                        sunRenderer.sharedMaterial = sunMaterial;
                    }
                }

                var serialized = new SerializedObject(dome);
                serialized.FindProperty("_domeRenderer").objectReferenceValue = domeRenderer;
                serialized.FindProperty("_sunPivot").objectReferenceValue = sunPivot;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject SetupCloudPrefab(string path, Material cloudMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveCollidersAndShadows(root);
                foreach (Renderer cloudRenderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = new Material[cloudRenderer.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = cloudMaterial;
                    }

                    cloudRenderer.sharedMaterials = materials;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static SkySettings GetOrCreateSettings(SkyDome dome, List<GameObject> clouds)
        {
            var settings = AssetDatabase.LoadAssetAtPath<SkySettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SkySettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty domeProperty = serialized.FindProperty("_domePrefab");
            if (domeProperty.objectReferenceValue == null)
            {
                domeProperty.objectReferenceValue = dome;
            }

            SerializedProperty cloudProperty = serialized.FindProperty("_cloudPrefabs");
            if (cloudProperty.arraySize == 0)
            {
                cloudProperty.arraySize = clouds.Count;
                for (int i = 0; i < clouds.Count; i++)
                {
                    cloudProperty.GetArrayElementAtIndex(i).objectReferenceValue = clouds[i];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        /// <summary>
        /// 色调还是白色（未调过）的季节填上默认值，调过的保留。
        /// </summary>
        private static void FillSeasonTints()
        {
            for (int i = 0; i < SeasonNames.Length; i++)
            {
                var environment = AssetDatabase.LoadAssetAtPath<SeasonEnvironment>($"{SeasonFolder}/SeasonEnvironment_{SeasonNames[i]}.asset");
                if (environment == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(environment);
                SetColorIfWhite(serialized.FindProperty("_skyTint"), SkyTints[i]);
                SetColorIfWhite(serialized.FindProperty("_cloudTint"), CloudTints[i]);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupScene(SkySettings settings)
        {
            var controller = Object.FindObjectOfType<SeasonEnvironmentController>(true);
            if (controller == null)
            {
                Debug.LogError($"[{nameof(SkySetup)}] 当前场景没有 {nameof(SeasonEnvironmentController)}，先运行 Setup Season Environment。资源已生成，场景未修改");
                return;
            }

            var controllerData = new SerializedObject(controller);
            if (!controller.TryGetComponent(out SkyView sky))
            {
                sky = Undo.AddComponent<SkyView>(controller.gameObject);
            }

            GameUiBuilder.SetReference(sky, "_settings", settings);
            GameUiBuilder.SetReference(sky, "_camera", controllerData.FindProperty("_camera").objectReferenceValue);
            GameUiBuilder.SetReference(sky, "_levelView", controllerData.FindProperty("_levelView").objectReferenceValue);
            GameUiBuilder.SetReference(sky, "_visualConfig", controllerData.FindProperty("_visualConfig").objectReferenceValue);
            GameUiBuilder.SetReference(controller, "_sky", sky);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeObject = sky;
            Debug.Log($"[{nameof(SkySetup)}] 天空已就绪，记得保存场景", sky);
        }

        private static Texture GetSkyTexture(string domePath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(domePath);
            Renderer domeRenderer = prefab.GetComponent<Renderer>();
            Material material = domeRenderer != null ? domeRenderer.sharedMaterial : null;
            if (material == null)
            {
                return null;
            }

            // 原材质可能是内置着色器（_MainTex），也可能已经是本工具生成的（_BaseMap）
            return material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
        }

        private static Material GetOrCreateMaterial(string materialName, Shader shader, Texture texture, int renderQueue)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(shader) { renderQueue = renderQueue };
            material.SetTexture("_BaseMap", texture);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void RemoveCollidersAndShadows(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (Renderer skyRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                skyRenderer.shadowCastingMode = ShadowCastingMode.Off;
                skyRenderer.receiveShadows = false;
            }
        }

        private static void SetColorIfWhite(SerializedProperty property, Color value)
        {
            if (property.colorValue == Color.white)
            {
                property.colorValue = value;
            }
        }

        private static string FindPrefab(string keyword)
        {
            List<string> paths = FindPrefabs(keyword);
            return paths.Count > 0 ? paths[0] : null;
        }

        private static List<string> FindPrefabs(string keyword)
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets($"t:Prefab {keyword}", new[] { EnvironmentFolder }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            return paths;
        }
    }
}
