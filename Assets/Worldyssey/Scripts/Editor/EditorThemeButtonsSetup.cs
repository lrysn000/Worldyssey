using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 给关卡编辑器顶栏加上春夏秋冬四个主题按钮，并连上季节调色板。
    /// 新搭建的编辑器由 <see cref="LevelEditorUiBuilder"/> 调用；已有场景用菜单补上，已经有按钮的会跳过。
    /// </summary>
    public static class EditorThemeButtonsSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Add World Theme Buttons To Level Editor";
        private const float ButtonGap = 10f;

        // 放在“增删世界”按钮右侧的同一行
        private static readonly Vector2 FirstButtonPosition = new Vector2(1350f, -96f);
        private static readonly Vector2 ButtonSize = new Vector2(120f, 56f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        [MenuItem(MenuPath)]
        public static void AddToCurrentScene()
        {
            var header = Object.FindObjectOfType<EditorHeaderBar>(true);
            if (header == null)
            {
                Debug.LogError($"[{nameof(EditorThemeButtonsSetup)}] 当前场景没有关卡编辑器（{nameof(EditorHeaderBar)}），未做任何修改");
                return;
            }

            if (CreateThemeButtons(header))
            {
                EditorSceneManager.MarkSceneDirty(header.gameObject.scene);
                Debug.Log($"[{nameof(EditorThemeButtonsSetup)}] 已添加主题按钮，记得保存场景", header);
            }
        }

        /// <summary>
        /// 创建主题按钮并写入顶栏。已经有按钮时只补上调色板引用，返回 false。
        /// </summary>
        internal static bool CreateThemeButtons(EditorHeaderBar header)
        {
            var serialized = new SerializedObject(header);
            SerializedProperty palette = serialized.FindProperty("_palette");
            if (palette.objectReferenceValue == null)
            {
                palette.objectReferenceValue = GameUiBuilder.FindAsset<WorldPalette>(null);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedProperty buttons = serialized.FindProperty("_themeButtons");
            if (buttons.arraySize > 0)
            {
                Debug.Log($"[{nameof(EditorThemeButtonsSetup)}] 顶栏已有主题按钮，跳过", header);
                return false;
            }

            buttons.arraySize = WorldThemes.Count;
            for (int i = 0; i < WorldThemes.Count; i++)
            {
                string label = WorldThemes.DisplayName((WorldTheme)i);
                var position = FirstButtonPosition + Vector2.right * ((ButtonSize.x + ButtonGap) * i);
                Button button = GameUiBuilder.CreateButton(header.transform, "Theme_" + label, label, TopLeft, position, ButtonSize);
                Undo.RegisterCreatedObjectUndo(button.gameObject, "Add Theme Buttons");
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = button;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
