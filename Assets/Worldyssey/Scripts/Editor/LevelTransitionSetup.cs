using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Gameplay;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 一键配置关卡进出场转场：在 GameFlow 所在物体上加 LevelTransitionPlayer，引用从 StepPlayer 和 LevelSession 上取，
    /// 再连到 GameFlow。删掉 GameFlow 上的转场引用即可回到直接进出关卡。
    /// </summary>
    public static class LevelTransitionSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Setup Level Transition (Current Scene)";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            var stepPlayer = Object.FindObjectOfType<StepPlayer>();
            if (gameFlow == null || stepPlayer == null)
            {
                Debug.LogError($"[{nameof(LevelTransitionSetup)}] 当前场景需要有 {nameof(GameFlow)} 和 {nameof(StepPlayer)}，未做任何修改");
                return;
            }

            var stepData = new SerializedObject(stepPlayer);
            Object levelView = stepData.FindProperty("_levelView").objectReferenceValue;
            Object config = stepData.FindProperty("_gameplayConfig").objectReferenceValue;
            Object overlay = stepData.FindProperty("_worldTransition").objectReferenceValue;
            Object cameraRig = stepData.FindProperty("_cameraRig").objectReferenceValue;
            if (levelView == null || config == null || overlay == null)
            {
                Debug.LogError($"[{nameof(LevelTransitionSetup)}] {nameof(StepPlayer)} 缺少 LevelView、GameplayConfig 或 WorldTransition 引用，未做任何修改", stepPlayer);
                return;
            }

            if (!gameFlow.TryGetComponent(out LevelTransitionPlayer transition))
            {
                transition = Undo.AddComponent<LevelTransitionPlayer>(gameFlow.gameObject);
            }

            GameUiBuilder.SetReference(transition, "_levelView", levelView);
            GameUiBuilder.SetReference(transition, "_gameplayConfig", config);
            GameUiBuilder.SetReference(transition, "_overlay", overlay);
            GameUiBuilder.SetReference(transition, "_cameraRig", cameraRig);
            GameUiBuilder.SetReference(gameFlow, "_levelTransition", transition);
            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            Selection.activeObject = transition;

            string cameraNote = cameraRig == null ? "（StepPlayer 没有 CameraRig，入场时相机不会推近）" : string.Empty;
            Debug.Log($"[{nameof(LevelTransitionSetup)}] 关卡转场已就绪{cameraNote}，记得保存场景", transition);
        }
    }
}
