using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 两处像素字显示问题：
    /// 1. 关卡名输入框的文字区域高度是奇数、还偏了半格，文字落在半像素上，点采样的像素字会采到字库里相邻的字形，出现杂点；
    /// 2. 列表项（配对列表、打开关卡列表）的文字框是固定 240 宽，项变宽后长文字仍会折行，改成随项宽拉伸、不折行。
    /// 可以重复运行。
    /// </summary>
    public static class EditorListFixes
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Fix Editor Text Alignment";
        private const string TextAreaName = "Text Area";
        private const float ListTextPadding = 12f;

        // 文字区域四边留白取偶数，保证区域高度与输入框同奇偶、居中后落在整像素上
        internal static readonly Vector2 TextAreaInset = new Vector2(12f, 8f);

        [MenuItem(MenuPath)]
        public static void FixFromMenu()
        {
            int fields = 0;
            foreach (TMP_InputField input in Object.FindObjectsOfType<TMP_InputField>(true))
            {
                fields += AlignTextArea(input) ? 1 : 0;
            }

            bool isPrefabFixed = FixLevelButtonPrefab();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[{nameof(EditorListFixes)}] 已对齐 {fields} 个输入框；列表项预制体{(isPrefabFixed ? "已更新" : "未找到")}。记得保存场景");
        }

        /// <summary>把输入框的文字区域改为四边对称留白、不偏移。</summary>
        internal static bool AlignTextArea(TMP_InputField input)
        {
            var area = input.transform.Find(TextAreaName) as RectTransform;
            if (area == null)
            {
                return false;
            }

            Undo.RecordObject(area, UiStyleKit.UndoName);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.pivot = new Vector2(0.5f, 0.5f);
            area.offsetMin = TextAreaInset;
            area.offsetMax = -TextAreaInset;
            return true;
        }

        private static bool FixLevelButtonPrefab()
        {
            LevelButton prefab = GameUiBuilder.GetOrCreateLevelButtonPrefab();
            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var serialized = new SerializedObject(contents.GetComponent<LevelButton>());
            StretchText(UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_nameText"));
            StretchText(UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_detailText"));
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return true;
        }

        /// <summary>文字框横向随父物体拉伸，保留原来的纵向位置和高度；单行显示，过长截断。</summary>
        private static void StretchText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            float y = rect.anchoredPosition.y;
            float height = rect.sizeDelta.y;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-ListTextPadding * 2f, height);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
        }
    }
}
