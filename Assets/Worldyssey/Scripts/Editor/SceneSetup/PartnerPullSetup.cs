using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.Data;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 在 <see cref="StepPlayer"/> 所在物体上挂 <see cref="PartnerPullEffect"/>（回程箱子合成动画）并接好引用。可以重复运行。
    /// </summary>
    public static class PartnerPullSetup
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Add Partner Pull Effect";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var stepPlayer = Object.FindObjectOfType<StepPlayer>(true);
            if (stepPlayer == null)
            {
                Debug.LogError($"[{nameof(PartnerPullSetup)}] 场景里没有 StepPlayer，未做任何修改");
                return;
            }

            if (!stepPlayer.TryGetComponent(out PartnerPullEffect effect))
            {
                effect = Undo.AddComponent<PartnerPullEffect>(stepPlayer.gameObject);
            }

            var stepSerialized = new SerializedObject(stepPlayer);
            GameUiBuilder.SetReference(effect, "_gameplayConfig", UiThemeSceneSetup.GetReference<GameplayConfig>(stepSerialized, "_gameplayConfig"));
            GameUiBuilder.SetReference(effect, "_levelView", UiThemeSceneSetup.GetReference<LevelView>(stepSerialized, "_levelView"));
            GameUiBuilder.SetReference(stepPlayer, "_partnerPullEffect", effect);

            EditorSceneManager.MarkSceneDirty(stepPlayer.gameObject.scene);
            Selection.activeObject = effect;
            Debug.Log($"[{nameof(PartnerPullSetup)}] 回程箱子合成动画已接好，参数在 GameplayConfig 的“回程箱子合成”一栏，记得保存场景", effect);
        }
    }
}
