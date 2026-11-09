using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键配置视觉特效：材质开启发光、生成通关粒子和后处理配置，并在当前场景加入后处理 Volume、开启相机后处理。
    /// 已存在的资源和场景物体不会重复创建，可以对 Main 和 Sandbox 场景各运行一次。
    /// </summary>
    public static class VisualEffectsSetup
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Setup Visual Effects (Current Scene)";
        private const string MaterialFolder = ProjectPaths.Materials + "/Placeholder";
        private const string PrefabFolder = ProjectPaths.GameplayPrefabs;
        private const string ConfigFolder = ProjectPaths.Config;
        private const string ParticleMaterialPath = MaterialFolder + "/M_Particle.mat";
        private const string BurstPrefabPath = PrefabFolder + "/P_CelebrationBurst.prefab";
        private const string ProfilePath = ConfigFolder + "/PostProcessProfile.asset";
        private const string RewindProfilePath = ConfigFolder + "/RewindProfile.asset";
        private const string RewindVolumeName = "RewindVolume";
        private const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const string EmissionKeyword = "_EMISSION";

        // 这些材质的发光由脚本通过 MaterialPropertyBlock 控制，材质本身只需打开开关、默认不发光
        private static readonly string[] EmissiveMaterials = { "M_HintEnterable", "M_PortalBox", "M_Goal" };

        [MenuItem(MenuPath)]
        public static void SetupVisualEffects()
        {
            EnableEmission();
            ParticleSystem burst = GetOrCreateBurstPrefab();
            AssignBurstToConfig(burst);
            VolumeProfile profile = GetOrCreateProfile();
            SetupScene(profile);
            SetupRewind(GetOrCreateRewindProfile());
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(VisualEffectsSetup)}] 视觉特效已配置，记得保存场景");
        }

        private static void EnableEmission()
        {
            for (int i = 0; i < EmissiveMaterials.Length; i++)
            {
                string path = $"{MaterialFolder}/{EmissiveMaterials[i]}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Debug.LogError($"[{nameof(VisualEffectsSetup)}] 找不到材质 {path}，请先运行占位材质菜单");
                    continue;
                }

                material.EnableKeyword(EmissionKeyword);
                material.SetColor("_EmissionColor", Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(material);
            }
        }

        private static ParticleSystem GetOrCreateBurstPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ParticleSystem>(BurstPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            Material particleMaterial = GetOrCreateParticleMaterial();
            AssetFolderUtility.EnsureFolder(PrefabFolder);

            var root = new GameObject("P_CelebrationBurst");
            var particles = root.AddComponent<ParticleSystem>();
            // 默认锥形朝 +Z，转到朝上喷
            root.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 1f, 0.8f));
            main.gravityModifier = 0.8f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.2f;

            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = particleMaterial;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BurstPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ParticleSystem>();
        }

        private static Material GetOrCreateParticleMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(ParticleShaderName);
            if (shader == null)
            {
                Debug.LogError($"[{nameof(VisualEffectsSetup)}] 找不到着色器 {ParticleShaderName}，粒子将使用默认材质");
                return null;
            }

            AssetFolderUtility.EnsureFolder(MaterialFolder);
            var material = new Material(shader) { color = Color.white };
            AssetDatabase.CreateAsset(material, ParticleMaterialPath);
            return material;
        }

        private static void AssignBurstToConfig(ParticleSystem burst)
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            if (config == null || burst == null)
            {
                return;
            }

            var serialized = new SerializedObject(config);
            SerializedProperty property = serialized.FindProperty("_celebrationBurstPrefab");
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = burst;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }
        }

        private static VolumeProfile GetOrCreateProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(ConfigFolder);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            // 泛光只让 HDR 亮度大于 1 的发光部分亮起来，保持画面干净
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.6f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);

            // Volume 组件需要作为子资源存进配置文件，否则重新打开项目后会丢失
            AssetDatabase.AddObjectToAsset(bloom, profile);
            AssetDatabase.AddObjectToAsset(tonemapping, profile);
            AssetDatabase.AddObjectToAsset(vignette, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VolumeProfile GetOrCreateRewindProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RewindProfilePath);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(ConfigFolder);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, RewindProfilePath);

            // 回溯时画面大幅褪色并略微变暗；强度直接在这个资源上调
            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.saturation.Override(-70f);
            colorAdjustments.postExposure.Override(-0.2f);
            AssetDatabase.AddObjectToAsset(colorAdjustments, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void SetupRewind(VolumeProfile rewindProfile)
        {
            if (Object.FindObjectOfType<RewindEffect>(true) != null)
            {
                return;
            }

            var gameplayConfig = GameUiBuilder.FindAsset<Worldyssey.Data.GameplayConfig>(null);
            var levelSession = Object.FindObjectOfType<Worldyssey.Gameplay.LevelSession>(true);
            if (gameplayConfig == null || levelSession == null)
            {
                Debug.LogError($"[{nameof(VisualEffectsSetup)}] 缺少 GameplayConfig 或场景中没有 LevelSession，未加入回溯效果");
                return;
            }

            var rewindObject = new GameObject(RewindVolumeName);
            Undo.RegisterCreatedObjectUndo(rewindObject, "Setup Visual Effects");
            var volume = rewindObject.AddComponent<Volume>();
            volume.isGlobal = true;

            // 优先级高于常规后处理，权重平时为 0，由 RewindEffect 在撤销时拉起
            volume.priority = 10f;
            volume.weight = 0f;
            volume.sharedProfile = rewindProfile;
            var effect = rewindObject.AddComponent<RewindEffect>();
            GameUiBuilder.SetReference(effect, "_volume", volume);
            GameUiBuilder.SetReference(effect, "_gameplayConfig", gameplayConfig);
            GameUiBuilder.SetReference(levelSession, "_rewindEffect", effect);
        }

        private static void SetupScene(VolumeProfile profile)
        {
            if (Object.FindObjectOfType<Volume>() == null)
            {
                var volumeObject = new GameObject("PostProcessVolume");
                Undo.RegisterCreatedObjectUndo(volumeObject, "Setup Visual Effects");
                var volume = volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError($"[{nameof(VisualEffectsSetup)}] 当前场景没有标记为 MainCamera 的相机，未开启后处理");
            }
            else
            {
                UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
                Undo.RecordObject(cameraData, "Setup Visual Effects");
                cameraData.renderPostProcessing = true;
                EditorUtility.SetDirty(cameraData);
            }

            // 传送时的相机推拉需要 StepPlayer 引用 CameraRig
            var stepPlayer = Object.FindObjectOfType<StepPlayer>(true);
            CameraRig rig = camera != null ? camera.GetComponent<CameraRig>() : null;
            if (stepPlayer != null && rig != null)
            {
                GameUiBuilder.SetReference(stepPlayer, "_cameraRig", rig);
            }
            else if (stepPlayer != null)
            {
                Debug.LogWarning($"[{nameof(VisualEffectsSetup)}] 相机上没有 CameraRig，传送时不会推拉相机。请先运行 Add Camera Rig to Scene 再运行本菜单");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
