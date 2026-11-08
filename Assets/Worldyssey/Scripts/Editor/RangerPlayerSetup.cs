using UnityEditor;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 玩家换成 KayKit Adventurers 的 Ranger（CC0）：设置角色和动画文件的导入（Generic 骨骼、循环片段、去掉片段名前缀），
    /// 然后用 <see cref="CharacterPrefabBuilder"/> 生成 P_Ranger 并设为玩家。动画来自同一套 Rig_Medium 骨骼的动画包。可以重复运行。
    /// </summary>
    public static class RangerPlayerSetup
    {
        private const string MenuPath = "Worldyssey/Content/Build Ranger Player";
        private const string Folder = ProjectPaths.Models + "/Ranger";
        private const string ModelPath = Folder + "/Ranger.fbx";
        private const string GeneralAnimationPath = Folder + "/Animations/Rig_Medium_General.fbx";
        private const string MovementAnimationPath = Folder + "/Animations/Rig_Medium_MovementBasic.fbx";
        private const string IdleClipName = "Idle_A";
        private const string WalkClipName = "Walking_A";

        // 需要循环播放的片段名前缀
        private static readonly string[] LoopingPrefixes = { "Idle", "Walking", "Running", "Jump_Idle" };

        [MenuItem(MenuPath)]
        public static void Build()
        {
            if (!ConfigureModel(ModelPath, false) || !ConfigureModel(GeneralAnimationPath, true) || !ConfigureModel(MovementAnimationPath, true))
            {
                return;
            }

            var spec = new CharacterPrefabBuilder.Spec
            {
                ModelPath = ModelPath,
                TexturePath = Folder + "/ranger_texture.png",
                MaterialPath = Folder + "/M_Ranger.mat",
                ControllerPath = Folder + "/AC_Ranger.controller",
                PrefabPath = ProjectPaths.GameplayPrefabs + "/P_Ranger.prefab",
                IdleClip = CharacterPrefabBuilder.LoadClip(GeneralAnimationPath, IdleClipName),
                WalkClip = CharacterPrefabBuilder.LoadClip(MovementAnimationPath, WalkClipName),

                // Q 版大头身，整体约一格高；箱子边长 0.8
                TargetHeight = 0.95f,
                ModelYaw = 0f,

                // 走路一个循环约 1.1 秒，走一格只要零点几秒，加快一些脚才不打滑
                WalkAnimationSpeed = 2f,
            };

            EntityView player = CharacterPrefabBuilder.Build(spec);
            if (player != null)
            {
                Selection.activeObject = player.gameObject;
                Debug.Log($"[{nameof(RangerPlayerSetup)}] 玩家已换成 Ranger：{spec.PrefabPath}", player);
            }
        }

        /// <summary>
        /// Generic 骨骼。动画文件：导入全部片段，去掉“Rig_Medium|”前缀，待机和移动类片段循环，不导入材质（里面是灰色假人）。
        /// </summary>
        private static bool ConfigureModel(string path, bool isAnimationFile)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError($"[{nameof(RangerPlayerSetup)}] 找不到 {path}，先把 KayKit 文件放进 {Folder}");
                return false;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = isAnimationFile;
            if (isAnimationFile)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.clipAnimations = BuildClips(importer.defaultClipAnimations);
            }

            importer.SaveAndReimport();
            return true;
        }

        private static ModelImporterClipAnimation[] BuildClips(ModelImporterClipAnimation[] clips)
        {
            foreach (ModelImporterClipAnimation clip in clips)
            {
                int separator = clip.name.LastIndexOf('|');
                string name = separator >= 0 ? clip.name.Substring(separator + 1) : clip.name;
                clip.name = name;
                clip.loopTime = IsLooping(name);
                clip.loopPose = clip.loopTime;

                // 动画是原地的，锁住根节点，人物不会被带着漂移
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }

            return clips;
        }

        private static bool IsLooping(string clipName)
        {
            foreach (string prefix in LoopingPrefixes)
            {
                if (clipName.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
