using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Gameplay;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把关卡编辑器界面加入已用 <see cref="GameUiBuilder"/> 生成的场景：编辑器面板、打开关卡对话框、主菜单入口按钮，并连好引用。
    /// </summary>
    public static class LevelEditorUiBuilder
    {
        private const string MenuPath = "Worldyssey/Setup/Add Level Editor to Scene";
        private const string CellPrefabFolder = ProjectPaths.UiPrefabs;
        private const string CellPrefabPath = CellPrefabFolder + "/P_EditorGridCell.prefab";

        private static readonly Vector2 SmallButton = new Vector2(64f, 56f);
        private static readonly Vector2 ToolButtonSize = new Vector2(230f, 60f);
        private static readonly Vector2 TabSize = new Vector2(150f, 56f);
        private static readonly Vector2 HalfButton = new Vector2(205f, 56f);
        private static readonly Vector2 ThirdButton = new Vector2(135f, 56f);

        [MenuItem(MenuPath)]
        public static void AddLevelEditor()
        {
            var gameFlow = Object.FindObjectOfType<GameFlow>();
            var mainMenu = Object.FindObjectOfType<MainMenuPanel>(true);
            if (gameFlow == null || mainMenu == null)
            {
                Debug.LogError($"[{nameof(LevelEditorUiBuilder)}] 当前场景没有 GameFlow 或主菜单，请先运行 Build Game Scene");
                return;
            }

            if (Object.FindObjectOfType<LevelEditorPanel>(true) != null)
            {
                Debug.LogError($"[{nameof(LevelEditorUiBuilder)}] 当前场景已经有关卡编辑器，为避免重复未做任何修改");
                return;
            }

            var serializedFlow = new SerializedObject(gameFlow);
            var inputReader = serializedFlow.FindProperty("_inputReader").objectReferenceValue as PlayerInputReader;
            Transform canvas = mainMenu.transform.parent;
            LevelButton listItemPrefab = GameUiBuilder.GetOrCreateLevelButtonPrefab();
            EditorGridCell cellPrefab = GetOrCreateCellPrefab();

            LevelEditorPanel editor = CreateEditorPanel(canvas, inputReader, listItemPrefab, cellPrefab);
            Undo.RegisterCreatedObjectUndo(editor.gameObject, "Add Level Editor");
            AddMainMenuButton(mainMenu);
            GameUiBuilder.SetReference(gameFlow, "_levelEditorPanel", editor);

            EditorSceneManager.MarkSceneDirty(gameFlow.gameObject.scene);
            Selection.activeObject = editor.gameObject;
            Debug.Log($"[{nameof(LevelEditorUiBuilder)}] 已加入关卡编辑器，记得保存场景");
        }

        private static void AddMainMenuButton(MainMenuPanel mainMenu)
        {
            var serialized = new SerializedObject(mainMenu);
            var quit = serialized.FindProperty("_quitButton").objectReferenceValue as Button;

            // 编辑器按钮放在 Start 和 Quit 之间，Quit 下移一格
            Button editorButton = GameUiBuilder.CreateButton(mainMenu.transform, "LevelEditorButton", UiText.LevelEditor,
                GameUiBuilder.Center, new Vector2(0f, -120f), GameUiBuilder.MenuButtonSize);
            if (quit != null)
            {
                Undo.RecordObject(quit.transform, "Add Level Editor");
                ((RectTransform)quit.transform).anchoredPosition = new Vector2(0f, -240f);
            }

            GameUiBuilder.SetReference(mainMenu, "_levelEditorButton", editorButton);
        }

        private static LevelEditorPanel CreateEditorPanel(Transform canvas, PlayerInputReader inputReader, LevelButton listItemPrefab, EditorGridCell cellPrefab)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("LevelEditor", canvas, GameUiBuilder.MenuBackground, true);
            var panel = root.gameObject.AddComponent<LevelEditorPanel>();
            Vector2 topLeft = GameUiBuilder.TopLeft;

            // 左侧画笔栏
            GameUiBuilder.CreateText(root, "ToolsHeader", UiText.ToolsHeader, 34f, TextAlignmentOptions.Left, topLeft, new Vector2(30f, -110f), new Vector2(230f, 44f));
            var toolbarRoot = GameUiBuilder.CreateRect("Toolbar", root);
            GameUiBuilder.Stretch(toolbarRoot);
            var toolbar = toolbarRoot.gameObject.AddComponent<EditorToolbar>();
            var toolButtons = new Button[UiText.EditorToolNames.Length];
            for (int i = 0; i < toolButtons.Length; i++)
            {
                toolButtons[i] = GameUiBuilder.CreateButton(toolbarRoot, "Tool_" + UiText.EditorToolNames[i], UiText.EditorToolNames[i],
                    topLeft, new Vector2(30f, -165f - i * 70f), ToolButtonSize);
            }

            SetArray(toolbar, "_toolButtons", toolButtons);

            // 顶部：关卡名、尺寸、世界标签
            RectTransform headerRoot = GameUiBuilder.CreateRect("Header", root);
            GameUiBuilder.Stretch(headerRoot);
            var header = headerRoot.gameObject.AddComponent<EditorHeaderBar>();
            TMP_InputField nameInput = CreateInputField(headerRoot, new Vector2(290f, -30f), new Vector2(460f, 56f));
            TMP_Text sizeText = GameUiBuilder.CreateText(headerRoot, "SizeText", "9 x 7", 30f, TextAlignmentOptions.Center, topLeft, new Vector2(840f, -30f), new Vector2(120f, 56f));
            Button widthMinus = GameUiBuilder.CreateButton(headerRoot, "WidthMinus", "W-", topLeft, new Vector2(770f, -30f), SmallButton);
            Button widthPlus = GameUiBuilder.CreateButton(headerRoot, "WidthPlus", "W+", topLeft, new Vector2(965f, -30f), SmallButton);
            Button heightMinus = GameUiBuilder.CreateButton(headerRoot, "HeightMinus", "H-", topLeft, new Vector2(1035f, -30f), SmallButton);
            Button heightPlus = GameUiBuilder.CreateButton(headerRoot, "HeightPlus", "H+", topLeft, new Vector2(1105f, -30f), SmallButton);
            var worldTabs = new Button[4];
            for (int i = 0; i < worldTabs.Length; i++)
            {
                worldTabs[i] = GameUiBuilder.CreateButton(headerRoot, "WorldTab_" + (i + 1), string.Format(UiText.WorldTabFormat, i + 1),
                    topLeft, new Vector2(1200f + i * 160f, -30f), TabSize);
            }

            Button addWorld = GameUiBuilder.CreateButton(headerRoot, "AddWorld", "+", topLeft, new Vector2(1200f, -96f), SmallButton);
            Button removeWorld = GameUiBuilder.CreateButton(headerRoot, "RemoveWorld", "-", topLeft, new Vector2(1270f, -96f), SmallButton);
            GameUiBuilder.SetReference(header, "_nameInput", nameInput);
            GameUiBuilder.SetReference(header, "_sizeText", sizeText);
            GameUiBuilder.SetReference(header, "_widthMinusButton", widthMinus);
            GameUiBuilder.SetReference(header, "_widthPlusButton", widthPlus);
            GameUiBuilder.SetReference(header, "_heightMinusButton", heightMinus);
            GameUiBuilder.SetReference(header, "_heightPlusButton", heightPlus);
            SetArray(header, "_worldTabs", worldTabs);
            GameUiBuilder.SetReference(header, "_addWorldButton", addWorld);
            GameUiBuilder.SetReference(header, "_removeWorldButton", removeWorld);
            EditorThemeButtonsSetup.CreateThemeButtons(header);

            // 中间网格
            RectTransform gridArea = GameUiBuilder.CreateRect("GridArea", root);
            GameUiBuilder.Place(gridArea, topLeft, new Vector2(290f, -170f), new Vector2(1140f, 820f));
            var grid = gridArea.gameObject.AddComponent<EditorGridView>();
            GameUiBuilder.SetReference(grid, "_gridArea", gridArea);
            GameUiBuilder.SetReference(grid, "_cellPrefab", cellPrefab);

            // 右侧：配对、问题、操作
            float right = 1460f;
            GameUiBuilder.CreateText(root, "PairsHeader", UiText.PairsHeader, 34f, TextAlignmentOptions.Left, topLeft, new Vector2(right, -170f), new Vector2(430f, 44f));
            var pairRoot = GameUiBuilder.CreateRect("PairList", root);
            GameUiBuilder.Stretch(pairRoot);
            var pairList = pairRoot.gameObject.AddComponent<EditorPairList>();
            Button target = GameUiBuilder.CreateButton(pairRoot, "TargetButton", string.Format(UiText.TargetWorldFormat, "-"), topLeft, new Vector2(right, -222f), new Vector2(430f, 56f));
            Button addPair = GameUiBuilder.CreateButton(pairRoot, "AddPair", UiText.AddPair, topLeft, new Vector2(right, -288f), HalfButton);
            Button removePair = GameUiBuilder.CreateButton(pairRoot, "RemovePair", UiText.RemovePair, topLeft, new Vector2(right + 225f, -288f), HalfButton);
            Transform pairItems = CreateList(pairRoot, "PairItems", new Vector2(right, -356f), new Vector2(430f, 300f), new Vector2(205f, 80f), 2);
            GameUiBuilder.SetReference(pairList, "_itemPrefab", listItemPrefab);
            GameUiBuilder.SetReference(pairList, "_listRoot", pairItems);
            GameUiBuilder.SetReference(pairList, "_targetButton", target);
            GameUiBuilder.SetReference(pairList, "_targetLabel", target.GetComponentInChildren<TMP_Text>());
            GameUiBuilder.SetReference(pairList, "_addButton", addPair);
            GameUiBuilder.SetReference(pairList, "_removeButton", removePair);

            RectTransform actionRoot = GameUiBuilder.CreateRect("ActionBar", root);
            GameUiBuilder.Stretch(actionRoot);
            var actionBar = actionRoot.gameObject.AddComponent<EditorActionBar>();
            GameUiBuilder.CreateText(actionRoot, "IssuesHeader", UiText.IssuesHeader, 34f, TextAlignmentOptions.Left, topLeft, new Vector2(right, -670f), new Vector2(430f, 44f));
            TMP_Text issues = GameUiBuilder.CreateText(actionRoot, "IssuesText", UiText.NoIssues, 20f, TextAlignmentOptions.TopLeft, topLeft, new Vector2(right, -720f), new Vector2(430f, 200f));
            TMP_Text status = GameUiBuilder.CreateText(actionRoot, "StatusText", string.Empty, 28f, TextAlignmentOptions.Left, topLeft, new Vector2(290f, -1010f), new Vector2(1140f, 50f));
            Button newButton = GameUiBuilder.CreateButton(actionRoot, "NewButton", UiText.NewLevel, topLeft, new Vector2(right, -940f), ThirdButton);
            Button openButton = GameUiBuilder.CreateButton(actionRoot, "OpenButton", UiText.OpenLevel, topLeft, new Vector2(right + 147f, -940f), ThirdButton);
            Button saveButton = GameUiBuilder.CreateButton(actionRoot, "SaveButton", UiText.SaveLevel, topLeft, new Vector2(right + 294f, -940f), ThirdButton);
            Button testButton = GameUiBuilder.CreateButton(actionRoot, "TestPlayButton", UiText.TestPlay, topLeft, new Vector2(right, -1006f), HalfButton);
            Button backButton = GameUiBuilder.CreateButton(actionRoot, "BackButton", UiText.Back, topLeft, new Vector2(right + 225f, -1006f), HalfButton);
            GameUiBuilder.SetReference(actionBar, "_issuesText", issues);
            GameUiBuilder.SetReference(actionBar, "_statusText", status);
            GameUiBuilder.SetReference(actionBar, "_newButton", newButton);
            GameUiBuilder.SetReference(actionBar, "_openButton", openButton);
            GameUiBuilder.SetReference(actionBar, "_saveButton", saveButton);
            GameUiBuilder.SetReference(actionBar, "_testPlayButton", testButton);
            GameUiBuilder.SetReference(actionBar, "_backButton", backButton);

            EditorOpenDialog openDialog = CreateOpenDialog(root, listItemPrefab);

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

        private static EditorOpenDialog CreateOpenDialog(Transform parent, LevelButton listItemPrefab)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("OpenDialog", parent, GameUiBuilder.DimBackground, true);
            var dialog = root.gameObject.AddComponent<EditorOpenDialog>();
            GameUiBuilder.CreateText(root, "Title", UiText.OpenLevelTitle, 60f, TextAlignmentOptions.Center, GameUiBuilder.Center, new Vector2(0f, 380f), new Vector2(1000f, 80f));
            Transform list = CreateList(root, "Items", new Vector2(-720f, 300f), new Vector2(1440f, 600f), new Vector2(260f, 100f), 5, GameUiBuilder.Center);
            Button cancel = GameUiBuilder.CreateButton(root, "CancelButton", UiText.Cancel, GameUiBuilder.Center, new Vector2(0f, -400f), GameUiBuilder.MenuButtonSize);
            GameUiBuilder.SetReference(dialog, "_itemPrefab", listItemPrefab);
            GameUiBuilder.SetReference(dialog, "_listRoot", list);
            GameUiBuilder.SetReference(dialog, "_cancelButton", cancel);
            root.gameObject.SetActive(false);
            return dialog;
        }

        /// <summary>
        /// 创建一个从左上角开始排列的网格列表区域。
        /// </summary>
        private static Transform CreateList(Transform parent, string name, Vector2 position, Vector2 size, Vector2 cellSize, int columns, Vector2? anchor = null)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            Vector2 at = anchor ?? GameUiBuilder.TopLeft;
            rect.anchorMin = at;
            rect.anchorMax = at;
            rect.pivot = GameUiBuilder.TopLeft;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(20f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            return rect;
        }

        private static TMP_InputField CreateInputField(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "NameInput";
            go.transform.SetParent(parent, false);
            GameUiBuilder.Place((RectTransform)go.transform, GameUiBuilder.TopLeft, position, size);
            var input = go.GetComponent<TMP_InputField>();
            input.textComponent.fontSize = 28f;
            input.characterLimit = 40;
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = UiText.NamePlaceholder;
                placeholder.fontSize = 28f;
            }

            return input;
        }

        private static EditorGridCell GetOrCreateCellPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EditorGridCell>(CellPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(CellPrefabFolder);
            var root = new GameObject("P_EditorGridCell", typeof(RectTransform));
            var frame = root.AddComponent<Image>();

            Image terrain = CreateInsetImage(root.transform, "Terrain", 2f);
            Image entity = CreateInsetImage(root.transform, "Entity", 9f);
            RectTransform labelRect = GameUiBuilder.CreateRect("Label", root.transform);
            GameUiBuilder.Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 20f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var cell = root.AddComponent<EditorGridCell>();
            GameUiBuilder.SetReference(cell, "_frame", frame);
            GameUiBuilder.SetReference(cell, "_terrain", terrain);
            GameUiBuilder.SetReference(cell, "_entity", entity);
            GameUiBuilder.SetReference(cell, "_label", label);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<EditorGridCell>();
        }

        /// <summary>
        /// 四边各缩进 <paramref name="inset"/> 像素的图像。只有最外层的外框接收鼠标事件。
        /// </summary>
        private static Image CreateInsetImage(Transform parent, string name, float inset)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            GameUiBuilder.Stretch(rect);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void SetArray(Object target, string propertyName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[{nameof(LevelEditorUiBuilder)}] {target.GetType().Name} 中找不到字段 {propertyName}", target);
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
