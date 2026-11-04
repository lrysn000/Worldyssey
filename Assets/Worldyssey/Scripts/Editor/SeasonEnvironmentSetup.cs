using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键配置季节环境：生成四季的环境配置、调色配置和天气粒子，连到各季节模块组，并在当前场景加入环境控制器。
    /// 已存在的资源不会覆盖，空着的引用才会填，可以重复运行。
    /// </summary>
    public static class SeasonEnvironmentSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Setup Season Environment (Current Scene)";
        private const string SeasonFolder = ProjectPaths.Seasons;

        private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        // 默认值：春暖白柔和，夏明亮高角度，秋金黄低角度影子长，冬冷白低角度、降低饱和度
        private static readonly Defaults[] SeasonDefaults =
        {
            new Defaults(new Color(1f, 0.95f, 0.9f), 1.1f, 50f, -30f,
                new Color(0.85f, 0.82f, 0.9f), new Color(0.75f, 0.7f, 0.72f), new Color(0.45f, 0.4f, 0.4f), new Color(0.97f, 0.88f, 0.9f), 5f, 5f, 0f),
            new Defaults(new Color(1f, 0.97f, 0.88f), 1.35f, 65f, -20f,
                new Color(0.7f, 0.85f, 0.95f), new Color(0.7f, 0.75f, 0.72f), new Color(0.4f, 0.45f, 0.38f), new Color(0.78f, 0.92f, 0.94f), 0f, 12f, 8f),
            new Defaults(new Color(1f, 0.78f, 0.5f), 1.05f, 32f, -40f,
                new Color(0.9f, 0.78f, 0.65f), new Color(0.75f, 0.62f, 0.5f), new Color(0.45f, 0.35f, 0.28f), new Color(0.97f, 0.85f, 0.68f), 18f, 6f, 5f),
            new Defaults(new Color(0.85f, 0.92f, 1f), 0.95f, 28f, -35f,
                new Color(0.8f, 0.88f, 1f), new Color(0.75f, 0.8f, 0.88f), new Color(0.6f, 0.65f, 0.72f), new Color(0.86f, 0.92f, 0.98f), -15f, -20f, -5f),
        };

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            AssetFolderUtility.EnsureFolder(SeasonFolder);
            ParticleSystem[] particles = SeasonParticleCreator.CreateAll();
            for (int i = 0; i < SeasonNames.Length; i++)
            {
                SeasonEnvironment environment = GetOrCreateEnvironment(i);
                SetIfEmpty(environment, "_colorProfile", GetOrCreateProfile(i));
                SetIfEmpty(environment, "_particlesPrefab", particles[i]);

                var set = AssetDatabase.LoadAssetAtPath<SeasonVisualSet>($"{SeasonFolder}/SeasonVisualSet_{SeasonNames[i]}.asset");
                if (set != null)
                {
                    SetIfEmpty(set, "_environment", environment);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(SeasonEnvironmentSetup)}] 没有 {SeasonNames[i]} 的季节模块组，环境未连上。先运行 Create + Link Season Visual Sets");
                }
            }

            AssetDatabase.SaveAssets();
            SetupScene();
        }

        private static SeasonEnvironment GetOrCreateEnvironment(int season)
        {
            string path = $"{SeasonFolder}/SeasonEnvironment_{SeasonNames[season]}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SeasonEnvironment>(path);
            if (existing != null)
            {
                return existing;
            }

            var environment = ScriptableObject.CreateInstance<SeasonEnvironment>();
            AssetDatabase.CreateAsset(environment, path);
            Defaults defaults = SeasonDefaults[season];
            var serialized = new SerializedObject(environment);
            serialized.FindProperty("_sunColor").colorValue = defaults.SunColor;
            serialized.FindProperty("_sunIntensity").floatValue = defaults.SunIntensity;
            serialized.FindProperty("_sunPitch").floatValue = defaults.SunPitch;
            serialized.FindProperty("_sunYaw").floatValue = defaults.SunYaw;
            serialized.FindProperty("_ambientSky").colorValue = defaults.AmbientSky;
            serialized.FindProperty("_ambientEquator").colorValue = defaults.AmbientEquator;
            serialized.FindProperty("_ambientGround").colorValue = defaults.AmbientGround;
            serialized.FindProperty("_backgroundColor").colorValue = defaults.Background;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return environment;
        }

        private static VolumeProfile GetOrCreateProfile(int season)
        {
            string path = $"{SeasonFolder}/SeasonColor_{SeasonNames[season]}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null)
            {
                return existing;
            }

            Defaults defaults = SeasonDefaults[season];
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.value = defaults.Temperature;
            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.saturation.value = defaults.Saturation;
            colorAdjustments.contrast.value = defaults.Contrast;

            // Volume 组件需要作为子资源存进配置文件，否则重新打开项目后会丢失
            AssetDatabase.AddObjectToAsset(whiteBalance, profile);
            AssetDatabase.AddObjectToAsset(colorAdjustments, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void SetupScene()
        {
            var levelView = Object.FindObjectOfType<LevelView>();
            Camera camera = Camera.main;
            Light sun = FindDirectionalLight();
            LevelVisualConfig config = levelView != null
                ? new SerializedObject(levelView).FindProperty("_visualConfig").objectReferenceValue as LevelVisualConfig
                : null;
            if (levelView == null || camera == null || sun == null || config == null)
            {
                Debug.LogError($"[{nameof(SeasonEnvironmentSetup)}] 当前场景需要有 LevelView（含表现配置）、MainCamera 和一盏平行光，资源已生成但场景未修改");
                return;
            }

            var controller = Object.FindObjectOfType<SeasonEnvironmentController>(true);
            if (controller == null)
            {
                var controllerObject = new GameObject("SeasonEnvironment");
                Undo.RegisterCreatedObjectUndo(controllerObject, "Setup Season Environment");
                controller = controllerObject.AddComponent<SeasonEnvironmentController>();
            }

            GameUiBuilder.SetReference(controller, "_levelView", levelView);
            GameUiBuilder.SetReference(controller, "_visualConfig", config);
            GameUiBuilder.SetReference(controller, "_sun", sun);
            GameUiBuilder.SetReference(controller, "_camera", camera);

            // 场景本身要开着线性雾：打包时 URP 按场景设置剥离着色器变体，场景不开雾的话运行时开雾也不会生效
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeObject = controller;
            Debug.Log($"[{nameof(SeasonEnvironmentSetup)}] 季节环境已就绪，记得保存场景", controller);
        }

        private static Light FindDirectionalLight()
        {
            foreach (Light light in Object.FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    return light;
                }
            }

            return null;
        }

        private static void SetIfEmpty(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue != null || value == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private readonly struct Defaults
        {
            public readonly Color SunColor;
            public readonly float SunIntensity;
            public readonly float SunPitch;
            public readonly float SunYaw;
            public readonly Color AmbientSky;
            public readonly Color AmbientEquator;
            public readonly Color AmbientGround;
            public readonly Color Background;
            public readonly float Temperature;
            public readonly float Saturation;
            public readonly float Contrast;

            public Defaults(Color sunColor, float sunIntensity, float sunPitch, float sunYaw, Color ambientSky, Color ambientEquator,
                Color ambientGround, Color background, float temperature, float saturation, float contrast)
            {
                SunColor = sunColor;
                SunIntensity = sunIntensity;
                SunPitch = sunPitch;
                SunYaw = sunYaw;
                AmbientSky = ambientSky;
                AmbientEquator = ambientEquator;
                AmbientGround = ambientGround;
                Background = background;
                Temperature = temperature;
                Saturation = saturation;
                Contrast = contrast;
            }
        }
    }
}
