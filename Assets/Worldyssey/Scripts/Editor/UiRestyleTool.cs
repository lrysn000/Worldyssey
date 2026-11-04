using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把 Build Game Scene 生成的占位界面换成 Honeti 圆角风格并接上主题着色：主菜单、选关、HUD、暂停、结算，以及选关按钮预制体。
    /// 暂停和结算改为“标题栏 + 卡片”布局，暂停菜单底部加返回主菜单。已换过的面板会跳过，可以重复运行。
    /// 关卡编辑器的颜色由代码控制，不在这里处理。
    /// </summary>
    public static class UiRestyleTool
    {
        private const string MenuPath = "Worldyssey/Setup/Restyle UI (Honeti)";
        private const string LevelButtonPrefabPath = "Assets/Worldyssey/Prefabs/UI/P_LevelButton.prefab";
        private const float DialogWidth = 560f;
        private const float RowSpacing = 20f;

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        [MenuItem(MenuPath)]
        public static void Restyle()
        {
            // 先保证主题作用域、控制器、设置面板都在
            GameFlow gameFlow = UiThemeSceneSetup.Setup();
            if (gameFlow == null)
            {
                return;
            }

            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            var flow = new SerializedObject(gameFlow);
            var pause = UiThemeSceneSetup.GetReference<PausePanel>(flow, "_pausePanel");
            UiThemeSceneSetup.AddButton(pause, "_mainMenuButton", UiText.MainMenu, "_settingsButton", false);

            RestyleFullscreenMenu(UiThemeSceneSetup.GetReference<MainMenuPanel>(flow, "_mainMenuPanel"), context);
            RestyleFullscreenMenu(UiThemeSceneSetup.GetReference<LevelSelectPanel>(flow, "_levelSelectPanel"), context);
            RestyleHud(UiThemeSceneSetup.GetReference<HudPanel>(flow, "_hudPanel"), context);
            RestyleDialog(pause, context, null, new[] { "_resumeButton", "_restartButton", "_settingsButton", "_levelSelectButton", "_mainMenuButton" });
            RestyleDialog(UiThemeSceneSetup.GetReference<ResultPanel>(flow, "_resultPanel"), context,
                new[] { "_stepsText", "_bestText" }, new[] { "_nextButton", "_replayButton", "_levelSelectButton" });
            RestyleLevelButtonPrefab(context);

            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            string fontNote = context.Font != null ? $"字体 {context.Font.name}" : "未找到 Poppins 字体资源，保留默认字体";
            Debug.Log($"[{nameof(UiRestyleTool)}] 界面换皮完成（{fontNote}），记得保存场景", gameFlow);
        }

        /// <summary>
        /// 主菜单、选关：全屏浅色底，字号最大的文字作为标题，其余为正文，按钮用强调色。
        /// </summary>
        private static void RestyleFullscreenMenu(UiPanel panel, UiStyleKit.Context context)
        {
            if (!BeginPanel(panel, out UiThemeScope scope))
            {
                return;
            }

            if (panel.TryGetComponent(out Image background))
            {
                Undo.RecordObject(background, UiStyleKit.UndoName);
                background.sprite = null;
                UiStyleKit.Theme(background, UiColorRole.PanelBackground, scope, context);
            }

            List<TMP_Text> texts = UiStyleKit.GetLooseTexts(panel.transform);
            TMP_Text title = null;
            for (int i = 0; i < texts.Count; i++)
            {
                if (title == null || texts[i].fontSize > title.fontSize)
                {
                    title = texts[i];
                }
            }

            for (int i = 0; i < texts.Count; i++)
            {
                bool isTitle = texts[i] == title;
                UiStyleKit.StyleText(texts[i], isTitle ? UiColorRole.Header : UiColorRole.Text, scope, context, isTitle);
            }

            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            {
                UiStyleKit.StyleButton(button, UiColorRole.Accent, scope, context);
            }
        }

        /// <summary>
        /// HUD：左上角信息卡片，通关文字和传送提示各加一块有内容时才显示的底板，右上按钮用强调色。
        /// </summary>
        private static void RestyleHud(HudPanel hud, UiStyleKit.Context context)
        {
            if (!BeginPanel(hud, out UiThemeScope scope))
            {
                return;
            }

            var serialized = new SerializedObject(hud);
            RectTransform info = UiStyleKit.CreateCard(hud.transform, "InfoCard", TopLeft, new Vector2(UiStyleKit.Padding, -UiStyleKit.Padding),
                new Vector2(DialogWidth, 212f), UiColorRole.PanelBackground, scope, context);
            info.SetAsFirstSibling();
            PlaceText(serialized, "_levelNameText", TopLeft, new Vector2(48f, -36f), new Vector2(512f, 48f), 36f, UiColorRole.Header, true, scope, context);
            PlaceText(serialized, "_worldNameText", TopLeft, new Vector2(48f, -88f), new Vector2(512f, 40f), 28f, UiColorRole.Text, false, scope, context);
            PlaceText(serialized, "_stepCountText", TopLeft, new Vector2(48f, -130f), new Vector2(512f, 40f), 28f, UiColorRole.Text, false, scope, context);
            PlaceText(serialized, "_conflictText", TopLeft, new Vector2(48f, -172f), new Vector2(512f, 36f), 24f, UiColorRole.Cancel, false, scope, context);

            GameObject status = CreateTextBackground(serialized, "_statusText", TopCenter, new Vector2(0f, -48f), new Vector2(520f, 92f),
                UiColorRole.Header, UiColorRole.HeaderText, scope, context);
            GameObject hint = CreateTextBackground(serialized, "_hintText", BottomCenter, new Vector2(0f, 110f), new Vector2(1000f, 72f),
                UiColorRole.PanelBackground, UiColorRole.Text, scope, context);
            GameUiBuilder.SetReference(hud, "_statusBackground", status);
            GameUiBuilder.SetReference(hud, "_hintBackground", hint);

            foreach (Button button in hud.GetComponentsInChildren<Button>(true))
            {
                UiStyleKit.StyleButton(button, UiColorRole.Accent, scope, context);
            }
        }

        /// <summary>
        /// 暂停、结算：半透明遮罩上放一张卡片，标题进标题栏，<paramref name="textFields"/> 和 <paramref name="buttonFields"/> 依次竖排。
        /// 结算的“下一关”用确认色。
        /// </summary>
        private static void RestyleDialog(UiPanel panel, UiStyleKit.Context context, string[] textFields, string[] buttonFields)
        {
            if (!BeginPanel(panel, out UiThemeScope scope))
            {
                return;
            }

            var serialized = new SerializedObject(panel);
            var rows = new List<RectTransform>();
            var referenced = new List<TMP_Text>();
            if (textFields != null)
            {
                foreach (string field in textFields)
                {
                    var text = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, field);
                    if (text == null)
                    {
                        continue;
                    }

                    referenced.Add(text);
                    rows.Add(text.rectTransform);
                    UiStyleKit.StyleText(text, UiColorRole.Text, scope, context, rows.Count == 1);
                }
            }

            foreach (string field in buttonFields)
            {
                var button = UiThemeSceneSetup.GetReference<Button>(serialized, field);
                if (button == null)
                {
                    continue;
                }

                rows.Add((RectTransform)button.transform);
                UiStyleKit.StyleButton(button, field == "_nextButton" ? UiColorRole.Confirm : UiColorRole.Accent, scope, context);
            }

            float height = UiStyleKit.Padding * 0.5f + UiStyleKit.HeaderHeight + UiStyleKit.Padding * 2f;
            for (int i = 0; i < rows.Count; i++)
            {
                height += rows[i].sizeDelta.y + (i > 0 ? RowSpacing : 0f);
            }

            RectTransform card = UiStyleKit.CreateCard(panel.transform, "Card", GameUiBuilder.Center, Vector2.zero, new Vector2(DialogWidth, height),
                UiColorRole.PanelBackground, scope, context);
            card.SetAsFirstSibling();
            RectTransform header = UiStyleKit.CreateHeaderBar(card, scope, context);

            // 标题是面板下没有被脚本引用的那段文字
            foreach (TMP_Text text in UiStyleKit.GetLooseTexts(panel.transform))
            {
                if (!referenced.Contains(text))
                {
                    UiStyleKit.PutTitleInHeader(text, header, scope, context);
                    break;
                }
            }

            float y = UiStyleKit.Padding * 0.5f + UiStyleKit.HeaderHeight + UiStyleKit.Padding;
            for (int i = 0; i < rows.Count; i++)
            {
                float rowHeight = rows[i].sizeDelta.y;
                Undo.SetTransformParent(rows[i], card, UiStyleKit.UndoName);
                GameUiBuilder.Place(rows[i], TopCenter, new Vector2(0f, -y), new Vector2(DialogWidth - UiStyleKit.Padding * 2f, rowHeight));
                y += rowHeight + RowSpacing;
            }
        }

        /// <summary>
        /// 选关按钮预制体：白色卡片底，选中高亮改为切换主题角色。作用域留空，运行时自动用所在面板的。
        /// </summary>
        private static void RestyleLevelButtonPrefab(UiStyleKit.Context context)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(LevelButtonPrefabPath);
            try
            {
                if (root.GetComponentInChildren<ThemedGraphic>(true) != null || !root.TryGetComponent(out LevelButton levelButton))
                {
                    return;
                }

                var serialized = new SerializedObject(levelButton);
                var image = root.GetComponent<Image>();
                UiStyleKit.SetSliced(image, context.PanelSprite);
                ThemedGraphic themedImage = UiStyleKit.Theme(image, UiColorRole.InnerFrame, null, context);
                var nameText = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_nameText");
                var detailText = UiThemeSceneSetup.GetReference<TMP_Text>(serialized, "_detailText");
                if (nameText != null)
                {
                    UiStyleKit.StyleText(nameText, UiColorRole.Text, null, context, true);
                }

                if (detailText != null)
                {
                    UiStyleKit.StyleText(detailText, UiColorRole.Icon, null, context, false);
                }

                GameUiBuilder.SetReference(levelButton, "_themedImage", themedImage);
                PrefabUtility.SaveAsPrefabAsset(root, LevelButtonPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool BeginPanel(UiPanel panel, out UiThemeScope scope)
        {
            scope = panel != null ? panel.GetComponent<UiThemeScope>() : null;
            if (scope == null)
            {
                Debug.LogError($"[{nameof(UiRestyleTool)}] 面板引用缺失或没有 {nameof(UiThemeScope)}，已跳过", panel);
                return false;
            }

            if (panel.GetComponentInChildren<ThemedGraphic>(true) != null)
            {
                Debug.Log($"[{nameof(UiRestyleTool)}] {panel.name} 已经换过皮，跳过", panel);
                return false;
            }

            return true;
        }

        private static void PlaceText(SerializedObject owner, string field, Vector2 anchor, Vector2 position, Vector2 size, float fontSize,
            UiColorRole role, bool isBold, UiThemeScope scope, UiStyleKit.Context context)
        {
            var text = UiThemeSceneSetup.GetReference<TMP_Text>(owner, field);
            if (text == null)
            {
                return;
            }

            Undo.RecordObject(text.rectTransform, UiStyleKit.UndoName);
            GameUiBuilder.Place(text.rectTransform, anchor, position, size);
            text.fontSize = fontSize;
            UiStyleKit.StyleText(text, role, scope, context, isBold);
        }

        /// <summary>
        /// 在文字后面垫一块同位置的底板，返回底板物体（初始隐藏，由 HudPanel 按内容开关）。
        /// </summary>
        private static GameObject CreateTextBackground(SerializedObject owner, string field, Vector2 anchor, Vector2 position, Vector2 size,
            UiColorRole backgroundRole, UiColorRole textRole, UiThemeScope scope, UiStyleKit.Context context)
        {
            var text = UiThemeSceneSetup.GetReference<TMP_Text>(owner, field);
            if (text == null)
            {
                return null;
            }

            RectTransform background = UiStyleKit.CreateCard(text.transform.parent, field.TrimStart('_') + "Background", anchor, position, size,
                backgroundRole, scope, context);
            background.SetSiblingIndex(text.transform.GetSiblingIndex());
            background.gameObject.SetActive(false);
            Undo.RecordObject(text.rectTransform, UiStyleKit.UndoName);
            GameUiBuilder.Place(text.rectTransform, anchor, position, size);
            UiStyleKit.StyleText(text, textRole, scope, context, textRole == UiColorRole.HeaderText);
            return background.gameObject;
        }
    }
}
