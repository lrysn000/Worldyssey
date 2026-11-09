using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Gameplay;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 在 <see cref="LevelSession"/> 所在物体上挂 <see cref="CameraPanController"/>，并连到它使用的 <see cref="CameraRig"/> 和输入。可以重复运行。
    /// </summary>
    public static class CameraPanSetup
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Add Camera Pan";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var levelSession = Object.FindObjectOfType<LevelSession>(true);
            var rig = levelSession != null ? UiThemeSceneSetup.GetReference<CameraRig>(new SerializedObject(levelSession), "_cameraRig") : null;
            if (rig == null)
            {
                Debug.LogError($"[{nameof(CameraPanSetup)}] 当前场景没有 LevelSession，或它还没连相机（先运行 Add Camera Rig to Scene），未做任何修改");
                return;
            }

            if (!levelSession.TryGetComponent(out CameraPanController pan))
            {
                pan = Undo.AddComponent<CameraPanController>(levelSession.gameObject);
            }

            GameUiBuilder.SetReference(pan, "_levelSession", levelSession);
            GameUiBuilder.SetReference(pan, "_cameraRig", rig);
            GameUiBuilder.SetReference(pan, "_inputReader", UiThemeSceneSetup.GetReference<PlayerInputReader>(new SerializedObject(levelSession), "_inputReader"));
            EditorSceneManager.MarkSceneDirty(levelSession.gameObject.scene);
            Selection.activeObject = pan;
            Debug.Log($"[{nameof(CameraPanSetup)}] 已加入相机拖动，记得保存场景", pan);
        }
    }
}
