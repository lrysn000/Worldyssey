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
    /// 重建选关界面：删除场景中已有的选关面板，按“顶栏 + 卡片墙 + 详情栏”布局重新生成并连好引用，
    /// 背景改为半透明，让相机里的天空和云透出来。之后重新登记主题作用域并补上悬停放大。
    /// </summary>
    public static class LevelSelectUiBuilder
    {
        private const string MenuPath = "Worldyssey/Setup/Rebuild Level Select UI";
        private const string UndoName = "Rebuild Level Select UI";
        private const float CardsAlpha = 0.75f;
        private const float DetailAlpha = 0.92f;

        // 很淡的暗色，让白色卡片和文字在明亮的天空上依然清楚
        private static readonly Color BackdropColor = new Color(0.04f, 0.06f, 0.10f, 0.18f);
        private static readonly Vector2 Left = new Vector2(0f, 0.5f);
        private static readonly Vector2 Right = new Vector2(1f, 0.5f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            var mainMenu = Object.FindObjectOfType<MainMenuPanel>(true);
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (gameFlow == null || mainMenu == null || themeSet == null)
            {
                Debug.LogError($"[{nameof(LevelSelectUiBuilder)}] 当前场景没有 GameFlow 或主菜单，或缺少主题资源，未做任何修改");
                return;
            }

            int siblingIndex = -1;
            var old = Object.FindObjectOfType<LevelSelectPanel>(true);
            if (old != null)
            {
                siblingIndex = old.transform.GetSiblingIndex();
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            LevelSelectPanel panel = CreatePanel(mainMenu.transform.parent, themeSet.NeutralTheme, new UiStyleKit.Context(themeSet.NeutralTheme));
            Undo.RegisterCreatedObjectUndo(panel.gameObject, UndoName);
            if (siblingIndex >= 0)
            {
                panel.transform.SetSiblingIndex(siblingIndex);
            }

            GameUiBuilder.SetReference(gameFlow, "_levelSelectPanel", panel);
            UiThemeSceneSetup.Setup();
            UiHoverScaleSetup.Setup();

            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            Selection.activeObject = panel.gameObject;
            Debug.Log($"[{nameof(LevelSelectUiBuilder)}] 选关界面已重建，记得保存场景", panel);
        }

        private static LevelSelectPanel CreatePanel(Transform canvas, UiTheme neutral, UiStyleKit.Context context)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("LevelSelect", canvas, BackdropColor, true);
            var panel = root.gameObject.AddComponent<LevelSelectPanel>();
            UiThemeScope scope = UiStyleKit.EnsureScope(panel, neutral);
            var layout = new LevelEditorLayout(scope, context);
            var widgets = new LevelSelectWidgets(scope, context);
            UiMotionConfig motion = UiHoverScaleSetup.GetOrCreateConfig();

            float gap = LevelEditorLayout.Gap;
            float belowTop = -gap - LevelEditorLayout.TopBarHeight - gap;
            RectTransform topBar = layout.CreateStretchCard(root, "TopBar", new Vector2(0f, 1f), Vector2.one,
                new Vector2(gap, -gap - LevelEditorLayout.TopBarHeight), new Vector2(-gap, -gap), UiColorRole.Header);
            RectTransform cardsArea = layout.CreateStretchCard(root, "CardsArea", Vector2.zero, Vector2.one,
                new Vector2(gap, gap), new Vector2(-gap * 2f - LevelSelectWidgets.DetailWidth, belowTop), UiColorRole.PanelBackground);
            RectTransform detailCard = layout.CreateStretchCard(root, "DetailCard", new Vector2(1f, 0f), Vector2.one,
                new Vector2(-gap - LevelSelectWidgets.DetailWidth, gap), new Vector2(-gap, belowTop), UiColorRole.InnerFrame);
            SetTranslucent(cardsArea, CardsAlpha);
            SetTranslucent(detailCard, DetailAlpha);

            // 顶栏：返回、标题（左），两个页签（中），通关进度（右）
            Button back = layout.CreateButton(topBar, "BackButton", UiText.Back, Left, new Vector2(20f, 0f), new Vector2(140f, 60f), UiColorRole.Accent);
            widgets.CreateText(topBar, "Title", UiText.SelectLevelTitle, 44f, TextAlignmentOptions.MidlineLeft, Left, new Vector2(184f, 0f), new Vector2(420f, 60f), UiColorRole.HeaderText, true, scope);
            Button builtInTab = layout.CreateButton(topBar, "LevelsTab", UiText.BuiltInLevelsHeader, Center, new Vector2(-114f, 0f), new Vector2(220f, 60f), UiColorRole.Accent);
            Button customTab = layout.CreateButton(topBar, "CustomTab", UiText.CustomLevelsHeader, Center, new Vector2(114f, 0f), new Vector2(220f, 60f), UiColorRole.Accent);
            TMP_Text progress = widgets.CreateText(topBar, "Progress", string.Empty, 28f, TextAlignmentOptions.MidlineRight, Right, new Vector2(-24f, 0f), new Vector2(320f, 60f), UiColorRole.HeaderText, false, scope);

            Transform cardList = CreateCardScroll(cardsArea);
            TMP_Text empty = widgets.CreateText(cardsArea, "EmptyText", UiText.NoCustomLevels, 32f, TextAlignmentOptions.Center, Center, Vector2.zero, new Vector2(800f, 60f), UiColorRole.Text, false, scope);
            empty.gameObject.SetActive(false);

            Sprite lockIcon = UiStyleKit.LoadIconSprite(LevelSelectWidgets.StatusIconFolder + "/Icon_Lock.png");
            LevelDetailPanel detail = widgets.BuildDetail(detailCard, lockIcon);
            ConfirmDialog confirm = widgets.BuildConfirmDialog(root);

            GameUiBuilder.SetReference(panel, "_cardPrefab", widgets.CreateCardPrefab(motion));
            GameUiBuilder.SetReference(panel, "_cardListRoot", cardList);
            GameUiBuilder.SetReference(panel, "_emptyText", empty);
            GameUiBuilder.SetReference(panel, "_progressText", progress);
            GameUiBuilder.SetReference(panel, "_builtInTab", builtInTab);
            GameUiBuilder.SetReference(panel, "_customTab", customTab);
            GameUiBuilder.SetReference(panel, "_backButton", back);
            GameUiBuilder.SetReference(panel, "_detailPanel", detail);
            GameUiBuilder.SetReference(panel, "_confirmDialog", confirm);
            GameUiBuilder.SetReference(panel, "_worldPalette", GameUiBuilder.FindAsset<WorldPalette>(null));
            GameUiBuilder.SetReference(panel, "_clearedIcon", UiStyleKit.LoadIconSprite(LevelSelectWidgets.StatusIconFolder + "/Icon_Check.png"));
            GameUiBuilder.SetReference(panel, "_nextIcon", UiStyleKit.LoadIconSprite(LevelSelectWidgets.StatusIconFolder + "/Icon_New.png"));
            GameUiBuilder.SetReference(panel, "_lockedIcon", lockIcon);
            UiThemeSceneSetup.SetReferenceArray(panel, "_seasonIcons", UiStyleKit.LoadWorldIcons());

            // 默认隐藏，由 GameFlow 切换
            root.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>
        /// 卡片墙：可竖向滚动的区域，卡片按固定尺寸自动换行排列，内容高度随卡片数量变化。
        /// </summary>
        private static Transform CreateCardScroll(RectTransform area)
        {
            RectTransform viewport = GameUiBuilder.CreateRect("CardScroll", area);
            GameUiBuilder.Stretch(viewport);
            viewport.offsetMin = new Vector2(24f, 24f);
            viewport.offsetMax = new Vector2(-24f, -24f);

            // 透明底板只用来接收滚轮和拖动
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = GameUiBuilder.CreateRect("Cards", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 200f);
            grid.spacing = new Vector2(24f, 24f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperLeft;

            // 每行固定 4 张，卡片宽度随卡片区宽度伸缩
            content.gameObject.AddComponent<GridColumnFitter>();
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        /// <summary>让主题着色只换颜色、保留这里设定的透明度，透出背后的天空。</summary>
        private static void SetTranslucent(RectTransform card, float alpha)
        {
            var image = card.GetComponent<Image>();
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            var serialized = new SerializedObject(card.GetComponent<ThemedGraphic>());
            serialized.FindProperty("_keepAlpha").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
