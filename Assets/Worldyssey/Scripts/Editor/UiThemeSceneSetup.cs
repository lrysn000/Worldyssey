using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.UI;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 在已经搭好的游戏场景里接入界面主题：给各面板加主题作用域、创建主题控制器，
    /// 并生成设置面板空壳和主菜单、暂停菜单里的设置按钮，连好所有引用。已存在的部分会跳过，可以重复运行。
    /// </summary>
    public static class UiThemeSceneSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Add UI Theme + Settings Panel";
        private const float ButtonRowSpacing = 120f;

        private static readonly Vector2 SettingsCardSize = new Vector2(720f, 560f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        [MenuItem(MenuPath)]
        public static void SetupFromMenu()
        {
            Setup();
        }

        /// <summary>
        /// 执行接入，成功返回场景中的 <see cref="GameFlow"/>，失败返回 null（原因已输出到控制台）。
        /// </summary>
        internal static GameFlow Setup()
        {
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (gameFlow == null || themeSet == null || themeSet.NeutralTheme == null)
            {
                Debug.LogError($"[{nameof(UiThemeSceneSetup)}] 需要场景中有 {nameof(GameFlow)}（先运行 Build Game Scene），"
                    + "并且已生成主题资源（先运行 Worldyssey/UI/Create Theme Assets），未做任何修改");
                return null;
            }

            var flow = new SerializedObject(gameFlow);
            var mainMenu = GetReference<MainMenuPanel>(flow, "_mainMenuPanel");
            var levelSelect = GetReference<LevelSelectPanel>(flow, "_levelSelectPanel");
            var hud = GetReference<HudPanel>(flow, "_hudPanel");
            var pause = GetReference<PausePanel>(flow, "_pausePanel");
            var result = GetReference<ResultPanel>(flow, "_resultPanel");
            var levelEditor = GetReference<LevelEditorPanel>(flow, "_levelEditorPanel");
            var settings = GetReference<SettingsPanel>(flow, "_settingsPanel");
            var session = GetReference<LevelSession>(flow, "_levelSession");
            LevelView levelView = session != null ? GetReference<LevelView>(new SerializedObject(session), "_levelView") : null;
            if (mainMenu == null || levelSelect == null || hud == null || pause == null || result == null || levelView == null)
            {
                Debug.LogError($"[{nameof(UiThemeSceneSetup)}] {nameof(GameFlow)} 或 {nameof(LevelSession)} 的界面引用不完整，未做任何修改", gameFlow);
                return null;
            }

            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            if (settings == null)
            {
                settings = CreateSettingsPanel(mainMenu.transform.parent, context);
            }

            AddButton(mainMenu, "_settingsButton", UiText.Settings, "_quitButton", true);
            AddButton(pause, "_settingsButton", UiText.Settings, "_levelSelectButton", false);

            UiTheme neutral = themeSet.NeutralTheme;
            UiThemeScope[] worldScopes =
            {
                UiStyleKit.EnsureScope(hud, neutral), UiStyleKit.EnsureScope(pause, neutral), UiStyleKit.EnsureScope(result, neutral),
            };
            UiThemeScope[] neutralScopes = levelEditor != null
                ? new[] { UiStyleKit.EnsureScope(mainMenu, neutral), UiStyleKit.EnsureScope(levelSelect, neutral), UiStyleKit.EnsureScope(levelEditor, neutral) }
                : new[] { UiStyleKit.EnsureScope(mainMenu, neutral), UiStyleKit.EnsureScope(levelSelect, neutral) };

            var controller = Object.FindObjectOfType<UiThemeController>(true);
            if (controller == null)
            {
                controller = Undo.AddComponent<UiThemeController>(gameFlow.gameObject);
            }

            GameUiBuilder.SetReference(controller, "_levelView", levelView);
            GameUiBuilder.SetReference(controller, "_themeSet", themeSet);
            GameUiBuilder.SetReference(controller, "_settingsScope", UiStyleKit.EnsureScope(settings, neutral));
            SetReferenceArray(controller, "_worldScopes", worldScopes);
            SetReferenceArray(controller, "_neutralScopes", neutralScopes);
            GameUiBuilder.SetReference(gameFlow, "_settingsPanel", settings);
            GameUiBuilder.SetReference(gameFlow, "_uiThemeController", controller);

            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            Debug.Log($"[{nameof(UiThemeSceneSetup)}] 已接入界面主题和设置面板，记得保存场景", controller);
            return gameFlow;
        }

        /// <summary>
        /// 在 <paramref name="anchorField"/> 指向的按钮位置插入一个按钮，写入 <paramref name="buttonField"/>；已有则跳过。
        /// <paramref name="pushAnchorDown"/> 为 true 时原按钮下移一格（主菜单的退出键始终在最下面），否则新按钮放在原按钮下面一格。
        /// </summary>
        internal static void AddButton(UiPanel panel, string buttonField, string label, string anchorField, bool pushAnchorDown)
        {
            var serialized = new SerializedObject(panel);
            if (serialized.FindProperty(buttonField).objectReferenceValue != null)
            {
                return;
            }

            var anchorButton = serialized.FindProperty(anchorField).objectReferenceValue as Button;
            Vector2 position = anchorButton != null ? ((RectTransform)anchorButton.transform).anchoredPosition : Vector2.zero;
            if (pushAnchorDown && anchorButton != null)
            {
                Undo.RecordObject(anchorButton.transform, UiStyleKit.UndoName);
                ((RectTransform)anchorButton.transform).anchoredPosition = position + Vector2.down * ButtonRowSpacing;
            }
            else
            {
                position += Vector2.down * ButtonRowSpacing;
            }

            Button button = GameUiBuilder.CreateButton(panel.transform, buttonField.TrimStart('_'), label, GameUiBuilder.Center, position, GameUiBuilder.MenuButtonSize);
            Undo.RegisterCreatedObjectUndo(button.gameObject, UiStyleKit.UndoName);
            GameUiBuilder.SetReference(panel, buttonField, button);
        }

        private static SettingsPanel CreateSettingsPanel(Transform canvas, UiStyleKit.Context context)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("Settings", canvas, GameUiBuilder.DimBackground, true);
            Undo.RegisterCreatedObjectUndo(root.gameObject, UiStyleKit.UndoName);

            // 放在最后一个子物体，盖在主菜单和暂停菜单上面
            root.SetAsLastSibling();
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            UiThemeScope scope = UiStyleKit.EnsureScope(panel, context.Preview);

            RectTransform card = UiStyleKit.CreateCard(root, "Card", GameUiBuilder.Center, Vector2.zero, SettingsCardSize, UiColorRole.PanelBackground, scope, context);
            RectTransform header = UiStyleKit.CreateHeaderBar(card, scope, context);
            TMP_Text title = GameUiBuilder.CreateText(header, "Title", UiText.SettingsTitle, 44f, TextAlignmentOptions.MidlineLeft, GameUiBuilder.Center, Vector2.zero, Vector2.zero);
            UiStyleKit.PutTitleInHeader(title, header, scope, context);

            Button close = GameUiBuilder.CreateButton(card, "CloseButton", UiText.Close, BottomCenter, new Vector2(0f, UiStyleKit.Padding), GameUiBuilder.MenuButtonSize);
            UiStyleKit.StyleButton(close, UiColorRole.Accent, scope, context);

            // 内容区：标题栏和关闭按钮之间，以后放具体设置项
            RectTransform body = UiStyleKit.CreateCard(card, "Body", GameUiBuilder.Center, Vector2.zero, Vector2.zero, UiColorRole.InnerFrame, scope, context);
            GameUiBuilder.Stretch(body);
            body.offsetMin = new Vector2(UiStyleKit.Padding, UiStyleKit.Padding * 2f + GameUiBuilder.MenuButtonSize.y);
            body.offsetMax = new Vector2(-UiStyleKit.Padding, -(UiStyleKit.HeaderHeight + UiStyleKit.Padding));

            TMP_Text placeholder = GameUiBuilder.CreateText(body, "Placeholder", UiText.SettingsPlaceholder, 32f, TextAlignmentOptions.Center, GameUiBuilder.Center, Vector2.zero, Vector2.zero);
            GameUiBuilder.Stretch(placeholder.rectTransform);
            UiStyleKit.StyleText(placeholder, UiColorRole.Text, scope, context, false);

            GameUiBuilder.SetReference(panel, "_closeButton", close);
            root.gameObject.SetActive(false);
            return panel;
        }

        internal static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void SetReferenceArray(Object target, string propertyName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
