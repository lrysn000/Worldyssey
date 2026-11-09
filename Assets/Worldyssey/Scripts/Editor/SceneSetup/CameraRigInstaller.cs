using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给当前场景的主相机装上 <see cref="CameraRig"/>，并连到场景里的 <see cref="LevelSession"/>。Main 和 Sandbox 场景都适用。
    /// </summary>
    public static class CameraRigInstaller
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Add Camera Rig to Scene";

        [MenuItem(MenuPath)]
        public static void AddCameraRig()
        {
            Camera camera = Camera.main;
            var levelSession = Object.FindObjectOfType<LevelSession>(true);
            if (camera == null || levelSession == null)
            {
                Debug.LogError($"[{nameof(CameraRigInstaller)}] 当前场景缺少标记为 MainCamera 的相机或 LevelSession，未做任何修改");
                return;
            }

            if (camera.GetComponent<CameraRig>() != null)
            {
                Debug.LogError($"[{nameof(CameraRigInstaller)}] {camera.name} 上已经有 {nameof(CameraRig)}，未做任何修改", camera);
                return;
            }

            var gameplayConfig = GameUiBuilder.FindAsset<GameplayConfig>(null);
            if (gameplayConfig == null)
            {
                return;
            }

            CameraRig rig = Undo.AddComponent<CameraRig>(camera.gameObject);
            GameUiBuilder.SetReference(rig, "_camera", camera);
            GameUiBuilder.SetReference(rig, "_gameplayConfig", gameplayConfig);
            GameUiBuilder.SetReference(levelSession, "_cameraRig", rig);

            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            Selection.activeObject = camera.gameObject;
            Debug.Log($"[{nameof(CameraRigInstaller)}] 已在 {camera.name} 上加入 {nameof(CameraRig)} 并连到 {levelSession.name}，记得保存场景", rig);
        }
    }
}
