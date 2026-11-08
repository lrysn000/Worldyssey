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
    /// 生成（或重新生成）关卡编辑器界面：删除场景中已有的编辑器面板，按新布局整个重建并连好引用，
    /// 然后重新登记主题作用域、补上主菜单入口和悬停放大。运行逻辑都在 UI 脚本里，重建不会丢数据。
    /// </summary>
    public static class LevelEditorUiBuilder
    {
        private const string MenuPath = "Worldyssey/Scene Setup/Rebuild Level Editor UI";
        private const string UndoName = "Rebuild Level Editor UI";
        private const string GridPalettePath = ProjectPaths.Config + "/EditorGridPalette.asset";
        internal const string BrushFolder = ProjectPaths.Textures + "/UI/EditorBrushes";

        // 与 EditorTool 枚举顺序一致
        private static readonly string[] BrushNames =
        {
            "Brush_Floor", "Brush_Wall", "Brush_Pedestal", "Brush_Goal", "Brush_UniqueWall", "Brush_Box", "Brush_Player", "Brush_Portal", "Brush_Eraser",
        };

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            var mainMenu = Object.FindObjectOfType<MainMenuPanel>(true);
            var themeSet = GameUiBuilder.FindAsset<UiThemeSet>(null);
            if (gameFlow == null || mainMenu == null || themeSet == null)
            {
                Debug.LogError($"[{nameof(LevelEditorUiBuilder)}] 当前场景没有 GameFlow 或主菜单，或缺少主题资源，未做任何修改");
                return;
            }

            var inputReader = UiThemeSceneSetup.GetReference<PlayerInputReader>(new SerializedObject(gameFlow), "_inputReader");
            Transform canvas = mainMenu.transform.parent;
            int siblingIndex = -1;
            var old = Object.FindObjectOfType<LevelEditorPanel>(true);
            if (old != null)
            {
                siblingIndex = old.transform.GetSiblingIndex();
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            var context = new UiStyleKit.Context(themeSet.NeutralTheme);
            LevelEditorPanel editor = CreateEditorPanel(canvas, inputReader, themeSet.NeutralTheme, context);
            Undo.RegisterCreatedObjectUndo(editor.gameObject, UndoName);
            if (siblingIndex >= 0)
            {
                editor.transform.SetSiblingIndex(siblingIndex);
            }

            GameUiBuilder.SetReference(gameFlow, "_levelEditorPanel", editor);
            UiThemeSceneSetup.AddButton(mainMenu, "_levelEditorButton", UiText.LevelEditor, "_quitButton", true);

            // 重新登记主题作用域（旧面板的作用域已随面板删除），并给新按钮加悬停放大
            UiThemeSceneSetup.Setup();
            UiHoverScaleSetup.Setup();

            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            Selection.activeObject = editor.gameObject;
            Debug.Log($"[{nameof(LevelEditorUiBuilder)}] 关卡编辑器界面已重建，网格配色在 {GridPalettePath}，记得保存场景", editor);
        }

        private static LevelEditorPanel CreateEditorPanel(Transform canvas, PlayerInputReader inputReader, UiTheme neutral, UiStyleKit.Context context)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("LevelEditor", canvas, GameUiBuilder.MenuBackground, true);
            var panel = root.gameObject.AddComponent<LevelEditorPanel>();
            UiThemeScope scope = UiStyleKit.EnsureScope(panel, neutral);
            UiStyleKit.Theme(root.GetComponent<Image>(), UiColorRole.PanelBackground, scope, context);
            var layout = new LevelEditorLayout(scope, context);
            UiMotionConfig motion = UiHoverScaleSetup.GetOrCreateConfig();
            var worldPalette = GameUiBuilder.FindAsset<WorldPalette>(null);

            float gap = LevelEditorLayout.Gap;
            float belowTop = -gap - LevelEditorLayout.TopBarHeight - gap;
            RectTransform topBar = layout.CreateStretchCard(root, "TopBar", new Vector2(0f, 1f), Vector2.one,
                new Vector2(gap, -gap - LevelEditorLayout.TopBarHeight), new Vector2(-gap, -gap), UiColorRole.Header);
            RectTransform toolsCard = layout.CreateStretchCard(root, "ToolsCard", Vector2.zero, new Vector2(0f, 1f),
                new Vector2(gap, gap), new Vector2(gap + LevelEditorLayout.ToolsWidth, belowTop), UiColorRole.InnerFrame);
            RectTransform sideCard = layout.CreateStretchCard(root, "SideCard", new Vector2(1f, 0f), Vector2.one,
                new Vector2(-gap - LevelEditorLayout.SideWidth, gap), new Vector2(-gap, belowTop), UiColorRole.InnerFrame);
            RectTransform centerCard = layout.CreateStretchCard(root, "CenterCard", Vector2.zero, Vector2.one,
                new Vector2(gap * 2f + LevelEditorLayout.ToolsWidth, gap), new Vector2(-gap * 2f - LevelEditorLayout.SideWidth, belowTop), UiColorRole.InnerFrame);

            // 顶栏 + 世界栏 → EditorHeaderBar
            layout.BuildTopBar(topBar, out LevelEditorTopBarParts top);
            layout.BuildWorldBar(centerCard, out LevelEditorWorldBarParts worlds);
            var header = topBar.gameObject.AddComponent<EditorHeaderBar>();
            GameUiBuilder.SetReference(header, "_nameInput", top.NameInput);
            GameUiBuilder.SetReference(header, "_sizeText", top.SizeText);
            GameUiBuilder.SetReference(header, "_widthMinusButton", top.WidthMinus);
            GameUiBuilder.SetReference(header, "_widthPlusButton", top.WidthPlus);
            GameUiBuilder.SetReference(header, "_heightMinusButton", top.HeightMinus);
            GameUiBuilder.SetReference(header, "_heightPlusButton", top.HeightPlus);
            GameUiBuilder.SetReference(header, "_addWorldButton", worlds.AddWorld);
            GameUiBuilder.SetReference(header, "_removeWorldButton", worlds.RemoveWorld);
            GameUiBuilder.SetReference(header, "_palette", worldPalette);
            GameUiBuilder.SetReference(header, "_themeScope", scope);
            UiThemeSceneSetup.SetReferenceArray(header, "_worldTabs", worlds.Tabs);
            UiThemeSceneSetup.SetReferenceArray(header, "_worldTabIcons", worlds.TabIcons);
            UiThemeSceneSetup.SetReferenceArray(header, "_themeButtons", worlds.ThemeButtons);
            UiThemeSceneSetup.SetReferenceArray(header, "_themeIcons", worlds.ThemeIcons);
            UiThemeSceneSetup.SetReferenceArray(header, "_seasonIcons", UiStyleKit.LoadWorldIcons());

            // 左侧画笔栏
            var toolbar = toolsCard.gameObject.AddComponent<EditorToolbar>();
            UiThemeSceneSetup.SetReferenceArray(toolbar, "_toolButtons", layout.BuildToolbar(toolsCard, LoadBrushIcons()));

            // 中间网格：世界栏下方到状态提示上方
            RectTransform gridArea = GameUiBuilder.CreateRect("GridArea", centerCard);
            gridArea.anchorMin = Vector2.zero;
            gridArea.anchorMax = Vector2.one;
            gridArea.offsetMin = new Vector2(24f, 88f);
            gridArea.offsetMax = new Vector2(-24f, -96f);
            var grid = gridArea.gameObject.AddComponent<EditorGridView>();
            GameUiBuilder.SetReference(grid, "_gridArea", gridArea);
            GameUiBuilder.SetReference(grid, "_cellPrefab", EditorGridCellPrefabBuilder.GetOrUpdate(context));
            GameUiBuilder.SetReference(grid, "_palette", GetOrCreateGridPalette());
            GameUiBuilder.SetReference(grid, "_worldPalette", worldPalette);
            GameUiBuilder.SetReference(grid, "_motionConfig", motion);
            EditorGridIcons.Assign(grid);
            EditorGridIcons.EnsureCoordinateText(grid, context);

            EditorPairList pairList = BuildPairSection(sideCard, layout, out TMP_Text issuesHeader);
            EditorActionBar actionBar = BuildActionBar(sideCard, centerCard, layout, top, issuesHeader, motion);
            EditorOpenDialog openDialog = CreateOpenDialog(root, scope, context);

            GameUiBuilder.SetReference(panel, "_grid", grid);
            GameUiBuilder.SetReference(panel, "_toolbar", toolbar);
            GameUiBuilder.SetReference(panel, "_header", header);
            GameUiBuilder.SetReference(panel, "_pairList", pairList);
            GameUiBuilder.SetReference(panel, "_actionBar", actionBar);
            GameUiBuilder.SetReference(panel, "_openDialog", openDialog);
            GameUiBuilder.SetReference(panel, "_inputReader", inputReader);

            // 默认隐藏，由 GameFlow 切换
            root.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>
        /// 右侧上半：配对（目标世界、增删、列表）；同时放好下半的问题标题，由调用方接到 EditorActionBar。
        /// </summary>
        private static EditorPairList BuildPairSection(RectTransform card, LevelEditorLayout layout, out TMP_Text issuesHeader)
        {
            float width = LevelEditorLayout.SideWidth - 48f;
            layout.CreateTitle(card, "PairsHeader", UiText.PairsHeader, new Vector2(24f, -20f), width);
            var pairList = card.gameObject.AddComponent<EditorPairList>();
            var topLeft = new Vector2(0f, 1f);
            Button target = layout.CreateButton(card, "TargetButton", string.Format(UiText.TargetWorldFormat, "-"), topLeft,
                new Vector2(24f, -72f), new Vector2(width, 56f), UiColorRole.Accent);
            float half = (width - 16f) * 0.5f;
            Button addPair = layout.CreateButton(card, "AddPair", UiText.AddPair, topLeft, new Vector2(24f, -140f), new Vector2(half, 56f), UiColorRole.Accent);
            Button removePair = layout.CreateButton(card, "RemovePair", UiText.RemovePair, topLeft, new Vector2(40f + half, -140f), new Vector2(half, 56f), UiColorRole.Cancel);

            // 列表项未选中时是白底，垫一块浅灰底板，免得和白色卡片混在一起
            UiStyleKit.CreateCard(card, "PairListBackground", topLeft, new Vector2(24f, -212f), new Vector2(width, 260f),
                UiColorRole.PanelBackground, layout.Scope, layout.Context);
            ScrollRect items = PairListScroll.Create(card, new Vector2(32f, -220f), new Vector2(width - 16f, 244f));

            GameUiBuilder.SetReference(pairList, "_itemPrefab", GameUiBuilder.GetOrCreateLevelButtonPrefab());
            PairListScroll.Link(pairList, items);
            GameUiBuilder.SetReference(pairList, "_targetButton", target);
            GameUiBuilder.SetReference(pairList, "_targetLabel", target.GetComponentInChildren<TMP_Text>());
            GameUiBuilder.SetReference(pairList, "_addButton", addPair);
            GameUiBuilder.SetReference(pairList, "_removeButton", removePair);

            issuesHeader = layout.CreateTitle(card, "IssuesHeader", UiText.IssuesHeader, new Vector2(24f, -496f), width);
            return pairList;
        }

        private static EditorActionBar BuildActionBar(RectTransform sideCard, RectTransform centerCard, LevelEditorLayout layout,
            LevelEditorTopBarParts top, TMP_Text issuesHeader, UiMotionConfig motion)
        {
            TMP_Text issues = layout.BuildIssueScroll(sideCard, 544f);
            TMP_Text status = layout.BuildStatusToast(centerCard, out CanvasGroup statusGroup);
            var actionBar = sideCard.gameObject.AddComponent<EditorActionBar>();
            GameUiBuilder.SetReference(actionBar, "_issuesText", issues);
            GameUiBuilder.SetReference(actionBar, "_statusText", status);
            GameUiBuilder.SetReference(actionBar, "_newButton", top.New);
            GameUiBuilder.SetReference(actionBar, "_openButton", top.Open);
            GameUiBuilder.SetReference(actionBar, "_saveButton", top.Save);
            GameUiBuilder.SetReference(actionBar, "_testPlayButton", top.TestPlay);
            GameUiBuilder.SetReference(actionBar, "_backButton", top.Back);
            GameUiBuilder.SetReference(actionBar, "_issuesHeaderText", issuesHeader);
            GameUiBuilder.SetReference(actionBar, "_statusGroup", statusGroup);
            GameUiBuilder.SetReference(actionBar, "_motionConfig", motion);
            return actionBar;
        }

        /// <summary>
        /// 打开关卡对话框：半透明遮罩上一张“标题栏 + 卡片”，与暂停、结算面板同一风格。
        /// </summary>
        private static EditorOpenDialog CreateOpenDialog(Transform parent, UiThemeScope scope, UiStyleKit.Context context)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("OpenDialog", parent, GameUiBuilder.DimBackground, true);
            var dialog = root.gameObject.AddComponent<EditorOpenDialog>();
            var center = new Vector2(0.5f, 0.5f);
            RectTransform card = UiStyleKit.CreateCard(root, "Card", center, Vector2.zero, new Vector2(1000f, 720f), UiColorRole.PanelBackground, scope, context);
            RectTransform header = UiStyleKit.CreateHeaderBar(card, scope, context);
            TMP_Text title = GameUiBuilder.CreateText(card, "Title", UiText.OpenLevelTitle, 44f, TextAlignmentOptions.Center, center, Vector2.zero, Vector2.zero);
            UiStyleKit.PutTitleInHeader(title, header, scope, context);

            Transform list = CreateList(card, "Items", new Vector2(40f, -140f), new Vector2(920f, 440f), new Vector2(288f, 96f), 3, new Vector2(0f, 1f));
            Button cancel = GameUiBuilder.CreateButton(card, "CancelButton", UiText.Cancel, new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(300f, 72f));
            UiStyleKit.StyleButton(cancel, UiColorRole.Cancel, scope, context);

            GameUiBuilder.SetReference(dialog, "_itemPrefab", GameUiBuilder.GetOrCreateLevelButtonPrefab());
            GameUiBuilder.SetReference(dialog, "_listRoot", list);
            GameUiBuilder.SetReference(dialog, "_cancelButton", cancel);
            root.gameObject.SetActive(false);
            return dialog;
        }

        /// <summary>
        /// 创建一个从左上角开始排列的网格列表区域。
        /// </summary>
        private static Transform CreateList(Transform parent, string name, Vector2 position, Vector2 size, Vector2 cellSize, int columns, Vector2 anchor)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            return rect;
        }

        private static Sprite[] LoadBrushIcons()
        {
            var icons = new Sprite[BrushNames.Length];
            for (int i = 0; i < BrushNames.Length; i++)
            {
                icons[i] = UiStyleKit.LoadIconSprite($"{BrushFolder}/{BrushNames[i]}.png");
            }

            return icons;
        }

        internal static EditorGridPalette GetOrCreateGridPalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<EditorGridPalette>(GridPalettePath);
            if (palette != null)
            {
                return palette;
            }

            AssetFolderUtility.EnsureFolder(ProjectPaths.Config);
            palette = ScriptableObject.CreateInstance<EditorGridPalette>();
            AssetDatabase.CreateAsset(palette, GridPalettePath);
            AssetDatabase.SaveAssets();
            return palette;
        }
    }
}
