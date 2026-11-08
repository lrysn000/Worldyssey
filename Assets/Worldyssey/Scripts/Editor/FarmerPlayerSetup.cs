using UnityEditor;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 用农夫模型生成玩家预制体（只有走路动画，站立用走路第一帧定格）。已改用 Ranger，保留以便对比；
    /// 不需要时可连同 Art/Models/Farmer、FarmerModelSetup 一起删除。
    /// </summary>
    public static class FarmerPlayerSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Build Farmer Player";
        private const string Folder = ProjectPaths.Models + "/Farmer";
        private const string ModelPath = Folder + "/Farmer.fbx";

        [MenuItem(MenuPath)]
        public static void Build()
        {
            var spec = new CharacterPrefabBuilder.Spec
            {
                ModelPath = ModelPath,
                TexturePath = Folder + "/textures/texture01.jpg",
                MaterialPath = Folder + "/M_Farmer.mat",
                ControllerPath = Folder + "/AC_Farmer.controller",
                PrefabPath = ProjectPaths.GameplayPrefabs + "/P_Farmer.prefab",
                IdleClip = null,
                WalkClip = CharacterPrefabBuilder.LoadClip(ModelPath, "Walk"),
                TargetHeight = 0.9f,
                ModelYaw = 0f,
                WalkAnimationSpeed = 3f,
            };

            EntityView player = CharacterPrefabBuilder.Build(spec);
            if (player != null)
            {
                Selection.activeObject = player.gameObject;
            }
        }
    }
}
