using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.UI;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 在当前打开的场景里一键搭建正式游戏所需的全部物体：游戏逻辑物体、EventSystem、整套占位 UI，并连好所有引用。
    /// 生成的是朴素的占位界面，位置和样式之后可以在编辑器里随意调整，M8 再做美术。
    /// </summary>
    public static class GameUiBuilder
    {
        private const string MenuPath = "Worldyssey/Setup/Build Game Scene (Logic + UI)";
        private const string LevelButtonPrefabFolder = "Assets/Worldyssey/Prefabs/UI";
        private const string LevelButtonPrefabPath = LevelButtonPrefabFolder + "/P_LevelButton.prefab";
        private const string PreferredCatalogName = "LevelCatalog_Tests";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        internal static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        internal static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        internal static readonly Vector2 MenuButtonSize = new Vector2(380f, 84f);
        private static readonly Vector2 HudButtonSize = new Vector2(260f, 68f);

        internal static readonly Color MenuBackground = new Color(0.08f, 0.09f, 0.12f, 1f);
        internal static readonly Color DimBackground = new Color(0f, 0f, 0f, 0.65f);
        internal static readonly Color ButtonColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color TextColor = Color.white;
        private static readonly Color ConflictTextColor = new Color(1f, 0.6f, 0.3f, 1f);

        [MenuItem(MenuPath)]
        public static void BuildGameScene()
        {
            if (Object.FindObjectOfType<GameFlow>() != null)
            {
                Debug.LogError($"[{nameof(GameUiBuilder)}] 当前场景已经有 {nameof(GameFlow)}，为避免重复未做任何修改");
                return;
            }

            var visualConfig = FindAsset<LevelVisualConfig>(null);
            var gameplayConfig = FindAsset<GameplayConfig>(null);
            var inputActions = FindAsset<InputActionAsset>(null);
            var catalog = FindAsset<LevelCatalog>(PreferredCatalogName);
            if (visualConfig == null || gameplayConfig == null || inputActions == null || catalog == null)
            {
                Debug.LogError($"[{nameof(GameUiBuilder)}] 缺少必要资源（LevelVisualConfig / GameplayConfig / Input Actions / LevelCatalog），未做任何修改。"
                    + "请先运行占位预制体菜单、创建 GameplayConfig、导入关卡生成目录");
                return;
            }

            LevelButton levelButtonPrefab = GetOrCreateLevelButtonPrefab();
            EnsureEventSystem();

            // 游戏逻辑物体
            var game = new GameObject("Game");
            Undo.RegisterCreatedObjectUndo(game, "Build Game Scene");
            var levelView = game.AddComponent<LevelView>();
            var inputReader = game.AddComponent<PlayerInputReader>();
            var stepPlayer = game.AddComponent<StepPlayer>();
            var levelSession = game.AddComponent<LevelSession>();
            var gameFlow = game.AddComponent<GameFlow>();

            // UI
            Transform canvas = CreateCanvas();
            WorldTransition worldTransition = CreateWorldFade(canvas);
            HudPanel hud = CreateHud(canvas, levelSession, gameplayConfig);
            MainMenuPanel mainMenu = CreateMainMenu(canvas);
            LevelSelectPanel levelSelect = CreateLevelSelect(canvas, levelButtonPrefab);
            PausePanel pause = CreatePause(canvas);
            ResultPanel result = CreateResult(canvas);

            SetReference(levelView, "_visualConfig", visualConfig);
            SetReference(inputReader, "_inputActions", inputActions);
            SetReference(stepPlayer, "_levelView", levelView);
            SetReference(stepPlayer, "_worldTransition", worldTransition);
            SetReference(stepPlayer, "_gameplayConfig", gameplayConfig);
            SetReference(levelSession, "_gameplayConfig", gameplayConfig);
            SetReference(levelSession, "_inputReader", inputReader);
            SetReference(levelSession, "_levelView", levelView);
            SetReference(levelSession, "_stepPlayer", stepPlayer);
            SetReference(gameFlow, "_catalog", catalog);
            SetReference(gameFlow, "_levelSession", levelSession);
            SetReference(gameFlow, "_inputReader", inputReader);
            SetReference(gameFlow, "_mainMenuPanel", mainMenu);
            SetReference(gameFlow, "_levelSelectPanel", levelSelect);
            SetReference(gameFlow, "_hudPanel", hud);
            SetReference(gameFlow, "_pausePanel", pause);
            SetReference(gameFlow, "_resultPanel", result);

            EditorSceneManager.MarkSceneDirty(game.scene);
            Selection.activeObject = game;
            Debug.Log($"[{nameof(GameUiBuilder)}] 已生成游戏物体和 UI，关卡目录：{catalog.name}。记得保存场景并调整相机");
        }

        internal static T FindAsset<T>(string preferredName) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            T first = null;
            for (int i = 0; i < guids.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset == null)
                {
                    continue;
                }

                if (preferredName != null && asset.name == preferredName)
                {
                    return asset;
                }

                if (first == null)
                {
                    first = asset;
                }
            }

            if (first == null)
            {
                Debug.LogError($"[{nameof(GameUiBuilder)}] 项目中找不到 {typeof(T).Name} 资源");
            }

            return first;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            // 项目使用新输入系统，UI 也用对应的输入模块
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Build Game Scene");
        }

        private static Transform CreateCanvas()
        {
            var canvasObject = new GameObject("GameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Build Game Scene");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject.transform;
        }

        private static WorldTransition CreateWorldFade(Transform canvas)
        {
            // 放在第一个子物体：渲染在最下层，HUD 文字不会被遮住
            RectTransform fade = CreateFullscreen("WorldFade", canvas, Color.black, false);
            fade.gameObject.AddComponent<CanvasGroup>();
            var transition = fade.gameObject.AddComponent<WorldTransition>();
            SetReference(transition, "_overlay", fade.GetComponent<CanvasGroup>());
            return transition;
        }

        private static HudPanel CreateHud(Transform canvas, LevelSession levelSession, GameplayConfig gameplayConfig)
        {
            RectTransform root = CreateRect("Hud", canvas);
            Stretch(root);
            var hud = root.gameObject.AddComponent<HudPanel>();

            TMP_Text levelName = CreateText(root, "LevelName", "Level", 40f, TextAlignmentOptions.TopLeft, TopLeft, new Vector2(40f, -36f), new Vector2(900f, 52f));
            TMP_Text worldName = CreateText(root, "WorldName", "World", 32f, TextAlignmentOptions.TopLeft, TopLeft, new Vector2(40f, -92f), new Vector2(900f, 44f));
            TMP_Text steps = CreateText(root, "Steps", "Steps: 0", 32f, TextAlignmentOptions.TopLeft, TopLeft, new Vector2(40f, -140f), new Vector2(900f, 44f));
            TMP_Text conflict = CreateText(root, "Conflict", string.Empty, 28f, TextAlignmentOptions.TopLeft, TopLeft, new Vector2(40f, -188f), new Vector2(900f, 40f));
            conflict.color = ConflictTextColor;
            TMP_Text status = CreateText(root, "Status", string.Empty, 56f, TextAlignmentOptions.Center, TopCenter, new Vector2(0f, -60f), new Vector2(800f, 70f));
            TMP_Text hint = CreateText(root, "Hint", string.Empty, 36f, TextAlignmentOptions.Center, BottomCenter, new Vector2(0f, 120f), new Vector2(1200f, 50f));

            Button undo = CreateButton(root, "UndoButton", UiText.Undo, TopRight, new Vector2(-40f, -36f), HudButtonSize);
            Button restart = CreateButton(root, "RestartButton", UiText.Restart, TopRight, new Vector2(-40f, -116f), HudButtonSize);
            Button pause = CreateButton(root, "PauseButton", UiText.Pause, TopRight, new Vector2(-40f, -196f), HudButtonSize);

            SetReference(hud, "_levelSession", levelSession);
            SetReference(hud, "_gameplayConfig", gameplayConfig);
            SetReference(hud, "_stepCountText", steps);
            SetReference(hud, "_levelNameText", levelName);
            SetReference(hud, "_worldNameText", worldName);
            SetReference(hud, "_conflictText", conflict);
            SetReference(hud, "_statusText", status);
            SetReference(hud, "_hintText", hint);
            SetReference(hud, "_undoButton", undo);
            SetReference(hud, "_restartButton", restart);
            SetReference(hud, "_pauseButton", pause);
            return hud;
        }

        private static MainMenuPanel CreateMainMenu(Transform canvas)
        {
            RectTransform root = CreateFullscreen("MainMenu", canvas, MenuBackground, true);
            var panel = root.gameObject.AddComponent<MainMenuPanel>();
            CreateText(root, "Title", UiText.GameTitle, 110f, TextAlignmentOptions.Center, Center, new Vector2(0f, 220f), new Vector2(1400f, 160f));
            Button start = CreateButton(root, "StartButton", UiText.StartGame, Center, new Vector2(0f, 0f), MenuButtonSize);
            Button quit = CreateButton(root, "QuitButton", UiText.Quit, Center, new Vector2(0f, -120f), MenuButtonSize);
            SetReference(panel, "_startButton", start);
            SetReference(panel, "_quitButton", quit);
            return panel;
        }

        private static LevelSelectPanel CreateLevelSelect(Transform canvas, LevelButton levelButtonPrefab)
        {
            RectTransform root = CreateFullscreen("LevelSelect", canvas, MenuBackground, true);
            var panel = root.gameObject.AddComponent<LevelSelectPanel>();
            CreateText(root, "Title", UiText.SelectLevelTitle, 64f, TextAlignmentOptions.Center, Center, new Vector2(0f, 440f), new Vector2(1200f, 90f));
            CreateText(root, "BuiltInHeader", UiText.BuiltInLevelsHeader, 40f, TextAlignmentOptions.Center, Center, new Vector2(0f, 340f), new Vector2(1200f, 60f));
            Transform builtInList = CreateGrid(root, "BuiltInList", new Vector2(0f, 170f));
            CreateText(root, "CustomHeader", UiText.CustomLevelsHeader, 40f, TextAlignmentOptions.Center, Center, new Vector2(0f, -10f), new Vector2(1200f, 60f));
            Transform customList = CreateGrid(root, "CustomList", new Vector2(0f, -180f));
            TMP_Text noCustom = CreateText(root, "NoCustomLevels", UiText.NoCustomLevels, 30f, TextAlignmentOptions.Center, Center, new Vector2(0f, -90f), new Vector2(1200f, 50f));
            Button back = CreateButton(root, "BackButton", UiText.Back, Center, new Vector2(0f, -440f), MenuButtonSize);

            SetReference(panel, "_levelButtonPrefab", levelButtonPrefab);
            SetReference(panel, "_builtInListRoot", builtInList);
            SetReference(panel, "_customListRoot", customList);
            SetReference(panel, "_noCustomLevelsText", noCustom);
            SetReference(panel, "_backButton", back);
            return panel;
        }

        private static PausePanel CreatePause(Transform canvas)
        {
            RectTransform root = CreateFullscreen("Pause", canvas, DimBackground, true);
            var panel = root.gameObject.AddComponent<PausePanel>();
            CreateText(root, "Title", UiText.PausedTitle, 72f, TextAlignmentOptions.Center, Center, new Vector2(0f, 200f), new Vector2(1000f, 100f));
            Button resume = CreateButton(root, "ResumeButton", UiText.Resume, Center, new Vector2(0f, 50f), MenuButtonSize);
            Button restart = CreateButton(root, "RestartButton", UiText.Restart, Center, new Vector2(0f, -60f), MenuButtonSize);
            Button levelSelect = CreateButton(root, "LevelSelectButton", UiText.LevelSelect, Center, new Vector2(0f, -170f), MenuButtonSize);
            SetReference(panel, "_resumeButton", resume);
            SetReference(panel, "_restartButton", restart);
            SetReference(panel, "_levelSelectButton", levelSelect);
            return panel;
        }

        private static ResultPanel CreateResult(Transform canvas)
        {
            RectTransform root = CreateFullscreen("Result", canvas, DimBackground, true);
            var panel = root.gameObject.AddComponent<ResultPanel>();
            CreateText(root, "Title", UiText.LevelCompleteTitle, 72f, TextAlignmentOptions.Center, Center, new Vector2(0f, 250f), new Vector2(1000f, 100f));
            TMP_Text steps = CreateText(root, "Steps", "Steps: 0", 44f, TextAlignmentOptions.Center, Center, new Vector2(0f, 140f), new Vector2(1000f, 60f));
            TMP_Text best = CreateText(root, "Best", "Best: 0", 36f, TextAlignmentOptions.Center, Center, new Vector2(0f, 80f), new Vector2(1000f, 50f));
            Button next = CreateButton(root, "NextButton", UiText.NextLevel, Center, new Vector2(0f, -40f), MenuButtonSize);
            Button replay = CreateButton(root, "ReplayButton", UiText.Replay, Center, new Vector2(0f, -150f), MenuButtonSize);
            Button levelSelect = CreateButton(root, "LevelSelectButton", UiText.LevelSelect, Center, new Vector2(0f, -260f), MenuButtonSize);
            SetReference(panel, "_stepsText", steps);
            SetReference(panel, "_bestText", best);
            SetReference(panel, "_nextButton", next);
            SetReference(panel, "_replayButton", replay);
            SetReference(panel, "_levelSelectButton", levelSelect);
            return panel;
        }

        internal static LevelButton GetOrCreateLevelButtonPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LevelButton>(LevelButtonPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            AssetFolderUtility.EnsureFolder(LevelButtonPrefabFolder);
            var root = new GameObject("P_LevelButton", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(260f, 100f);
            var image = root.AddComponent<Image>();
            image.color = ButtonColor;
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text nameText = CreateText(rect, "Name", "Level", 30f, TextAlignmentOptions.Center, Center, new Vector2(0f, 16f), new Vector2(240f, 44f));
            TMP_Text detailText = CreateText(rect, "Detail", "Not cleared", 22f, TextAlignmentOptions.Center, Center, new Vector2(0f, -26f), new Vector2(240f, 34f));

            var levelButton = root.AddComponent<LevelButton>();
            SetReference(levelButton, "_button", button);
            SetReference(levelButton, "_nameText", nameText);
            SetReference(levelButton, "_detailText", detailText);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, LevelButtonPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LevelButton>();
        }

        internal static Transform CreateGrid(Transform parent, string name, Vector2 position)
        {
            RectTransform rect = CreateRect(name, parent);
            Place(rect, Center, position, new Vector2(1440f, 240f));
            var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260f, 100f);
            grid.spacing = new Vector2(20f, 20f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            return rect;
        }

        internal static RectTransform CreateFullscreen(string name, Transform parent, Color color, bool blocksRaycasts)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksRaycasts;
            return rect;
        }

        internal static TMP_Text CreateText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions alignment, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            Place(rect, anchor, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            return text;
        }

        internal static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            Place(rect, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform labelRect = CreateRect("Label", rect);
            Stretch(labelRect);
            var text = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 32f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = TextColor;
            text.raycastTarget = false;
            return button;
        }

        internal static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 以 <paramref name="anchor"/> 同时作为锚点和轴心放置，靠边的元素位置值就是到边缘的距离。
        /// </summary>
        internal static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        internal static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[{nameof(GameUiBuilder)}] {target.GetType().Name} 中找不到字段 {propertyName}", target);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
