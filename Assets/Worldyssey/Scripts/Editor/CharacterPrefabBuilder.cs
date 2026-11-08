using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 用带骨骼动画的角色模型生成玩家预制体：复制当前占位玩家（保留 EntityView 等组件），换上角色模型，
    /// 生成卡通材质和动画控制器（站立 / 走路），挂上 <see cref="CharacterWalkAnimator"/>，再把 LevelVisualConfig 的玩家换成它。
    /// 每次调用都重新生成预制体、材质和控制器。
    /// </summary>
    internal static class CharacterPrefabBuilder
    {
        private const string ToonShaderName = "Worldyssey/Toon";
        private const string ModelChildName = "Model";
        private const string IsWalkingParameter = "IsWalking";
        private const string WalkSpeedParameter = "WalkSpeed";
        private const string PlaceholderPrefabPath = ProjectPaths.PlaceholderPrefabs + "/P_Player.prefab";

        // 占位玩家里要换掉的子物体：胶囊身体和鼻子
        private static readonly string[] PlaceholderChildren = { "Model", "Nose" };

        /// <summary>一个角色的素材和摆放参数。</summary>
        internal sealed class Spec
        {
            public string ModelPath;
            public string TexturePath;
            public string MaterialPath;
            public string ControllerPath;
            public string PrefabPath;

            /// <summary>站立动画，为 null 时用走路动画的第一帧定格。</summary>
            public AnimationClip IdleClip;
            public AnimationClip WalkClip;

            /// <summary>身高（格），按绑定姿势的包围盒缩放。</summary>
            public float TargetHeight;

            /// <summary>模型绕竖直轴的旋转。模型应面朝 +Z（网格的 Up），否则在这里补正。</summary>
            public float ModelYaw;
            public float WalkAnimationSpeed;
        }

        /// <summary>生成并启用玩家预制体，失败时返回 null（原因已写入日志）。</summary>
        internal static EntityView Build(Spec spec)
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            var placeholder = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPrefabPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath);
            Shader toon = Shader.Find(ToonShaderName);
            if (config == null || placeholder == null || model == null || spec.WalkClip == null || toon == null)
            {
                Debug.LogError($"[{nameof(CharacterPrefabBuilder)}] 缺少 LevelVisualConfig、占位玩家 {PlaceholderPrefabPath}、模型 {spec.ModelPath}、走路动画或 {ToonShaderName} 着色器");
                return null;
            }

            Material material = CreateMaterial(spec, toon);
            AnimatorController controller = CreateController(spec);
            EntityView player = BuildPrefab(spec, placeholder, model, material, controller);
            if (player != null)
            {
                GameUiBuilder.SetReference(config, "_playerPrefab", player);
                AssetDatabase.SaveAssets();
            }

            return player;
        }

        /// <summary>按名字取模型文件里的动画片段（不含 Unity 生成的预览片段）。</summary>
        internal static AnimationClip LoadClip(string assetPath, string clipName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                {
                    return clip;
                }
            }

            Debug.LogError($"[{nameof(CharacterPrefabBuilder)}] {assetPath} 里没有动画 {clipName}");
            return null;
        }

        private static Material CreateMaterial(Spec spec, Shader toon)
        {
            // 调色板贴图：每个色块一种颜色，用点采样、不生成 Mipmap，避免相邻色块混在一起
            if (AssetImporter.GetAtPath(spec.TexturePath) is TextureImporter importer)
            {
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);
            if (material == null)
            {
                material = new Material(toon);
                AssetDatabase.CreateAsset(material, spec.MaterialPath);
            }

            material.shader = toon;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.TexturePath));
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimatorController CreateController(Spec spec)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(spec.ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(spec.ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(spec.ControllerPath);
            controller.AddParameter(IsWalkingParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(WalkSpeedParameter, AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = machine.AddState("Idle");
            idle.motion = spec.IdleClip != null ? spec.IdleClip : spec.WalkClip;
            idle.speed = spec.IdleClip != null ? 1f : 0f;
            machine.defaultState = idle;

            AnimatorState walk = machine.AddState("Walk");
            walk.motion = spec.WalkClip;
            walk.speedParameterActive = true;
            walk.speedParameter = WalkSpeedParameter;

            AddTransition(idle, walk, AnimatorConditionMode.If, 0.08f);
            AddTransition(walk, idle, AnimatorConditionMode.IfNot, 0.15f);
            return controller;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(mode, 0f, IsWalkingParameter);
        }

        private static EntityView BuildPrefab(Spec spec, GameObject placeholder, GameObject model, Material material, AnimatorController controller)
        {
            // 总是从占位玩家复制，换角色时不会把上一个角色的模型带进来
            GameObject root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(placeholder));
            try
            {
                root.name = System.IO.Path.GetFileNameWithoutExtension(spec.PrefabPath);
                foreach (string childName in PlaceholderChildren)
                {
                    Transform child = root.transform.Find(childName);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                Animator animator = AddModel(spec, root.transform, model, material, controller);
                var walkAnimator = root.GetComponent<CharacterWalkAnimator>();
                if (walkAnimator == null)
                {
                    walkAnimator = root.AddComponent<CharacterWalkAnimator>();
                }

                var serialized = new SerializedObject(walkAnimator);
                serialized.FindProperty("_animator").objectReferenceValue = animator;
                serialized.FindProperty("_walkAnimationSpeed").floatValue = spec.WalkAnimationSpeed;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<EntityView>(spec.PrefabPath);
        }

        private static Animator AddModel(Spec spec, Transform root, GameObject model, Material material, AnimatorController controller)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
            instance.name = ModelChildName;
            instance.transform.localRotation = Quaternion.Euler(0f, spec.ModelYaw, 0f);

            // 按绑定姿势的包围盒缩放到目标身高，并让脚底贴地
            Bounds bounds = GetBounds(instance);
            float scale = bounds.size.y > 0f ? spec.TargetHeight / bounds.size.y : 1f;
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localPosition = new Vector3(0f, -bounds.min.y * scale, 0f);

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            return animator;
        }

        private static Bounds GetBounds(GameObject instance)
        {
            // 实例刚生成、缩放为 1，此时的世界包围盒就是模型原始尺寸（实例挂在预制体根下，根在原点）
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }
    }
}
