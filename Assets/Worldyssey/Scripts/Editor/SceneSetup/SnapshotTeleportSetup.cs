using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键配置截图缩放转场：生成快照外壳材质，在 StepPlayer 所在物体上加 SnapshotTeleportTransition 并连好引用。
    /// 已有的不会重复创建。删掉 StepPlayer 上的转场引用即可回到颜色遮罩转场。
    /// </summary>
    public static class SnapshotTeleportSetup
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Setup Snapshot Teleport (Current Scene)";
        private const string ShaderName = "Worldyssey/SnapshotProjection";
        private const string MaterialFolder = ProjectPaths.Materials + "/Effects";
        private const string MaterialPath = MaterialFolder + "/M_SnapshotShell.mat";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var stepPlayer = Object.FindObjectOfType<StepPlayer>();
            Shader shader = Shader.Find(ShaderName);
            if (stepPlayer == null || shader == null)
            {
                Debug.LogError($"[{nameof(SnapshotTeleportSetup)}] 需要场景中有 {nameof(StepPlayer)}，且着色器 {ShaderName} 编译通过，未做任何修改");
                return;
            }

            var stepData = new SerializedObject(stepPlayer);
            Object levelView = stepData.FindProperty("_levelView").objectReferenceValue;
            Object config = stepData.FindProperty("_gameplayConfig").objectReferenceValue;
            Object cameraRig = stepData.FindProperty("_cameraRig").objectReferenceValue;
            if (levelView == null || config == null || cameraRig == null)
            {
                Debug.LogError($"[{nameof(SnapshotTeleportSetup)}] {nameof(StepPlayer)} 缺少 LevelView、GameplayConfig 或 CameraRig 引用（先运行 Add Camera Rig to Scene），未做任何修改", stepPlayer);
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                AssetFolderUtility.EnsureFolder(MaterialFolder);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
                AssetDatabase.SaveAssets();
            }

            if (!stepPlayer.TryGetComponent(out SnapshotTeleportTransition transition))
            {
                transition = Undo.AddComponent<SnapshotTeleportTransition>(stepPlayer.gameObject);
            }

            GameUiBuilder.SetReference(transition, "_levelView", levelView);
            GameUiBuilder.SetReference(transition, "_cameraRig", cameraRig);
            GameUiBuilder.SetReference(transition, "_gameplayConfig", config);
            GameUiBuilder.SetReference(transition, "_shellMaterial", material);
            GameUiBuilder.SetReference(stepPlayer, "_snapshotTransition", transition);
            EditorSceneManager.MarkSceneDirty(stepPlayer.gameObject.scene);
            Selection.activeObject = transition;
            Debug.Log($"[{nameof(SnapshotTeleportSetup)}] 截图缩放转场已就绪，记得保存场景", transition);
        }
    }
}
