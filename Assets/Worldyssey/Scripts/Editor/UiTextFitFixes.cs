using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 修正两处文字放不下：选关卡片的“Personal Best”状态改为单行自动缩小；关卡编辑器顶栏的尺寸文字加宽、缩小字号，H 按钮右移。
    /// 与 LevelSelectWidgets、LevelEditorLayout 的新布局一致，不用重建整个界面。可以重复运行。
    /// </summary>
    public static class UiTextFitFixes
    {
        private const string MenuPath = "Worldyssey/Setup/Fix UI Text Fit";

        [MenuItem(MenuPath)]
        public static void Fix()
        {
            FixLevelCardPrefab();
            FixEditorSizeLabel();
        }

        private static void FixLevelCardPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(LevelSelectWidgets.CardPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[{nameof(UiTextFitFixes)}] 找不到选关卡片预制体 {LevelSelectWidgets.CardPrefabPath}");
                return;
            }

            try
            {
                var card = root.GetComponent<LevelCard>();
                var status = card != null ? UiThemeSceneSetup.GetReference<TMP_Text>(new SerializedObject(card), "_statusText") : null;
                if (status == null)
                {
                    Debug.LogError($"[{nameof(UiTextFitFixes)}] 卡片预制体里找不到状态文字，未修改");
                    return;
                }

                LevelSelectWidgets.SetSingleLine(status, LevelSelectWidgets.LevelCardStatusMinSize, LevelSelectWidgets.LevelCardStatusMaxSize);
                PrefabUtility.SaveAsPrefabAsset(root, LevelSelectWidgets.CardPrefabPath);
                Debug.Log($"[{nameof(UiTextFitFixes)}] 选关卡片状态文字已改为单行自动缩小");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void FixEditorSizeLabel()
        {
            var header = Object.FindObjectOfType<EditorHeaderBar>(true);
            var sizeText = header != null ? UiThemeSceneSetup.GetReference<TMP_Text>(new SerializedObject(header), "_sizeText") : null;
            if (sizeText == null)
            {
                Debug.LogWarning($"[{nameof(UiTextFitFixes)}] 当前场景没有关卡编辑器顶栏，跳过尺寸文字");
                return;
            }

            Undo.RecordObject(sizeText, UiStyleKit.UndoName);
            Undo.RecordObject(sizeText.rectTransform, UiStyleKit.UndoName);
            sizeText.enableAutoSizing = false;
            sizeText.fontSize = LevelEditorLayout.SizeFontSize;
            sizeText.enableWordWrapping = false;
            sizeText.overflowMode = TextOverflowModes.Overflow;
            SetX(sizeText.rectTransform, LevelEditorLayout.SizeTextX, LevelEditorLayout.SizeTextWidth);

            Transform bar = sizeText.transform.parent;
            MoveButton(bar, "HeightMinus", LevelEditorLayout.HeightMinusX);
            MoveButton(bar, "HeightPlus", LevelEditorLayout.HeightPlusX);
            EditorSceneManager.MarkSceneDirty(header.gameObject.scene);
            Debug.Log($"[{nameof(UiTextFitFixes)}] 编辑器尺寸文字已加宽，记得保存场景", header);
        }

        private static void MoveButton(Transform bar, string name, float x)
        {
            var rect = bar.Find(name) as RectTransform;
            if (rect == null)
            {
                Debug.LogWarning($"[{nameof(UiTextFitFixes)}] 顶栏里找不到 {name}，没有移动");
                return;
            }

            Undo.RecordObject(rect, UiStyleKit.UndoName);
            SetX(rect, x, rect.sizeDelta.x);
        }

        private static void SetX(RectTransform rect, float x, float width)
        {
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }
    }
}
