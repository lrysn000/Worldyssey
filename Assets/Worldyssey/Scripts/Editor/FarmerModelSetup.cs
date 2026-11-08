using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 农夫模型的导入设置：Generic 骨骼、走路动画循环、根节点位移不带进动画（移动由代码控制）、
    /// 材质用外部贴图。模型由 glb 在 Blender 里重新导出（去掉描边外壳、合并网格和材质），原 Sketchfab 的 FBX 骨骼绑定有问题不再使用。
    /// 可以重复运行。
    /// </summary>
    public static class FarmerModelSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Configure Farmer Model";
        private const string ModelPath = ProjectPaths.Models + "/Farmer/Farmer.fbx";
        private const string WalkClipName = "Walk";

        [MenuItem(MenuPath)]
        public static void Configure()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
            {
                Debug.LogError($"[{nameof(FarmerModelSetup)}] 找不到模型 {ModelPath}");
                return;
            }

            // 只有一个动画、不和其他角色共用，Generic 不要求骨骼符合人形标准，最稳妥
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                Debug.LogError($"[{nameof(FarmerModelSetup)}] {ModelPath} 里没有动画", importer);
                return;
            }

            // 只取第一段（走路），原地循环；根节点的旋转、高度、水平位移都锁在原位，人物不会被动画带着漂走
            ModelImporterClipAnimation walk = clips[0];
            walk.name = WalkClipName;
            walk.loopTime = true;
            walk.loopPose = true;
            walk.lockRootRotation = true;
            walk.lockRootHeightY = true;
            walk.lockRootPositionXZ = true;
            walk.keepOriginalOrientation = true;
            walk.keepOriginalPositionY = true;
            walk.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { walk };
            importer.SaveAndReimport();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Debug.Log($"[{nameof(FarmerModelSetup)}] 农夫模型导入设置完成，动画片段 {WalkClipName}（循环）", importer);
        }
    }
}
