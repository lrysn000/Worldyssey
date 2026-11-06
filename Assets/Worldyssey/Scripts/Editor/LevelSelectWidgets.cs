using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 选关页的组件生成：关卡卡片预制体、右侧详情栏、删除确认框。由 <see cref="LevelSelectUiBuilder"/> 调用。
    /// </summary>
    internal sealed class LevelSelectWidgets
    {
        internal const string StatusIconFolder = ProjectPaths.Textures + "/UI/StatusIcons";
        internal const float DetailWidth = 520f;
        private const string CardPrefabPath = ProjectPaths.UiPrefabs + "/P_LevelCard.prefab";
        private const float Inset = 32f;

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeft = Vector2.zero;
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private readonly UiThemeScope _scope;
        private readonly UiStyleKit.Context _context;
        private readonly LevelEditorLayout _layout;

        public LevelSelectWidgets(UiThemeScope scope, UiStyleKit.Context context)
        {
            _scope = scope;
            _context = context;
            _layout = new LevelEditorLayout(scope, context);
        }

        internal TMP_Text CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment,
            Vector2 anchor, Vector2 position, Vector2 rectSize, UiColorRole role, bool isBold, UiThemeScope scope)
        {
            TMP_Text text = GameUiBuilder.CreateText(parent, name, content, size, alignment, anchor, position, rectSize);
            UiStyleKit.StyleText(text, role, scope, _context, isBold);
            return text;
        }

        /// <summary>
        /// 关卡卡片预制体：每次重新生成，保证与代码中的布局一致。主题着色的作用域留空，运行时自动找选关面板。
        /// </summary>
        internal LevelCard CreateCardPrefab(UiMotionConfig motion)
        {
            var root = new GameObject("P_LevelCard", typeof(RectTransform));
            GameUiBuilder.Place((RectTransform)root.transform, Center, Vector2.zero, new Vector2(220f, 200f));
            var group = root.AddComponent<CanvasGroup>();

            RectTransform frame = GameUiBuilder.CreateRect("SelectionFrame", root.transform);
            GameUiBuilder.Stretch(frame);
            frame.offsetMin = new Vector2(-6f, -6f);
            frame.offsetMax = new Vector2(6f, 6f);
            Image frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.raycastTarget = false;
            UiStyleKit.SetSliced(frameImage, _context.PanelSprite);
            UiStyleKit.Theme(frameImage, UiColorRole.Accent, null, _context);
            frame.gameObject.SetActive(false);

            RectTransform background = GameUiBuilder.CreateRect("Background", root.transform);
            GameUiBuilder.Stretch(background);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            UiStyleKit.SetSliced(backgroundImage, _context.PanelSprite);
            UiStyleKit.Theme(backgroundImage, UiColorRole.InnerFrame, null, _context);
            var button = root.AddComponent<Button>();
            button.targetGraphic = backgroundImage;

            TMP_Text number = CreateText(root.transform, "Number", "01", 52f, TextAlignmentOptions.TopLeft, TopLeft, new Vector2(18f, -12f), new Vector2(130f, 64f), UiColorRole.Header, true, null);
            TMP_Text levelName = CreateText(root.transform, "Name", "Level", 22f, TextAlignmentOptions.MidlineLeft, TopLeft, new Vector2(18f, -82f), new Vector2(184f, 32f), UiColorRole.Text, true, null);
            SetSingleLine(levelName, 14f, 22f);
            TMP_Text status = CreateText(root.transform, "Status", string.Empty, 20f, TextAlignmentOptions.MidlineLeft, BottomLeft, new Vector2(18f, 14f), new Vector2(184f, 30f), UiColorRole.Text, false, null);

            // 卡片宽度由卡片墙按每行 4 张分配，名字和状态文字横向拉伸，跟着卡片变宽
            StretchHorizontally(levelName.rectTransform, 18f);
            StretchHorizontally(status.rectTransform, 18f);
            Image statusIcon = CreateImage(root.transform, "StatusIcon", null, TopRight, new Vector2(-16f, -16f), 36f);
            ThemedGraphic statusTheme = UiStyleKit.Theme(statusIcon, UiColorRole.Accent, null, _context);

            var worldIcons = new Object[4];
            for (int i = 0; i < worldIcons.Length; i++)
            {
                worldIcons[i] = CreateImage(root.transform, "WorldIcon" + i, null, BottomLeft, new Vector2(18f + i * 36f, 50f), 30f);
            }

            var card = root.AddComponent<LevelCard>();
            var hover = root.AddComponent<UiHoverScale>();
            GameUiBuilder.SetReference(hover, "_config", motion);
            GameUiBuilder.SetReference(card, "_button", button);
            GameUiBuilder.SetReference(card, "_numberText", number);
            GameUiBuilder.SetReference(card, "_nameText", levelName);
            GameUiBuilder.SetReference(card, "_statusText", status);
            GameUiBuilder.SetReference(card, "_statusIcon", statusIcon);
            GameUiBuilder.SetReference(card, "_statusIconTheme", statusTheme);
            GameUiBuilder.SetReference(card, "_selectionFrame", frame.gameObject);
            GameUiBuilder.SetReference(card, "_contentGroup", group);
            UiThemeSceneSetup.SetReferenceArray(card, "_worldIcons", worldIcons);

            AssetFolderUtility.EnsureFolder(ProjectPaths.UiPrefabs);
            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<LevelCard>(CardPrefabPath);
        }

        /// <summary>
        /// 右侧详情栏：编号、名字、世界页签、只读小地图（与编辑器网格同一套绘制）、锁、最佳步数、编辑 / 删除、开始。
        /// </summary>
        internal LevelDetailPanel BuildDetail(RectTransform card, Sprite lockIcon)
        {
            float width = DetailWidth - Inset * 2f;
            var detail = card.gameObject.AddComponent<LevelDetailPanel>();
            TMP_Text number = CreateText(card, "Number", "01", 64f, TextAlignmentOptions.MidlineLeft, TopLeft, new Vector2(Inset, -24f), new Vector2(200f, 72f), UiColorRole.Header, true, _scope);
            TMP_Text levelName = CreateText(card, "Name", "Level", 34f, TextAlignmentOptions.MidlineLeft, TopLeft, new Vector2(Inset, -100f), new Vector2(width, 48f), UiColorRole.Text, true, _scope);
            SetSingleLine(levelName, 20f, 34f);

            var tabs = new Object[4];
            var tabIcons = new Object[4];
            for (int i = 0; i < tabs.Length; i++)
            {
                Button tab = GameUiBuilder.CreateButton(card, "WorldTab" + i, string.Empty, TopLeft, new Vector2(Inset + i * 64f, -164f), new Vector2(56f, 56f));
                UiStyleKit.SetSliced(tab.image, _context.PanelSprite);
                Object.DestroyImmediate(tab.GetComponentInChildren<TMP_Text>(true).gameObject);
                tabs[i] = tab;
                tabIcons[i] = CreateImage(tab.transform, "Icon", null, Center, Vector2.zero, 36f);
            }

            TMP_Text worldName = CreateText(card, "WorldName", string.Empty, 24f, TextAlignmentOptions.MidlineLeft, TopLeft, new Vector2(Inset + 4f * 64f + 4f, -164f), new Vector2(width - 260f, 56f), UiColorRole.Text, false, _scope);

            var mapArea = new Vector2(width, 340f);
            EditorGridView miniMap = BuildMiniMap(card, new Vector2(Inset, -236f), mapArea);
            RectTransform locked = GameUiBuilder.CreateRect("LockedOverlay", card);
            GameUiBuilder.Place(locked, TopLeft, new Vector2(Inset, -236f), mapArea);
            Image lockImage = CreateImage(locked, "Lock", lockIcon, Center, Vector2.zero, 120f);
            UiStyleKit.Theme(lockImage, UiColorRole.Disabled, _scope, _context);

            TMP_Text best = CreateText(card, "Best", string.Empty, 28f, TextAlignmentOptions.MidlineLeft, TopLeft, new Vector2(Inset, -592f), new Vector2(width, 40f), UiColorRole.Text, false, _scope);
            float half = (width - 16f) * 0.5f;
            Button edit = _layout.CreateButton(card, "EditButton", UiText.EditLevel, TopLeft, new Vector2(Inset, -648f), new Vector2(half, 60f), UiColorRole.Accent);
            Button delete = _layout.CreateButton(card, "DeleteButton", UiText.DeleteLevel, TopLeft, new Vector2(Inset + half + 16f, -648f), new Vector2(half, 60f), UiColorRole.Cancel);
            Button play = _layout.CreateButton(card, "PlayButton", UiText.PlayLevel, BottomCenter, new Vector2(0f, Inset), new Vector2(width, 84f), UiColorRole.Confirm);
            play.GetComponentInChildren<TMP_Text>(true).fontSize = 36f;

            GameUiBuilder.SetReference(detail, "_numberText", number);
            GameUiBuilder.SetReference(detail, "_nameText", levelName);
            GameUiBuilder.SetReference(detail, "_worldNameText", worldName);
            GameUiBuilder.SetReference(detail, "_bestText", best);
            GameUiBuilder.SetReference(detail, "_miniMap", miniMap);
            GameUiBuilder.SetReference(detail, "_lockedOverlay", locked.gameObject);
            GameUiBuilder.SetReference(detail, "_playButton", play);
            GameUiBuilder.SetReference(detail, "_editButton", edit);
            GameUiBuilder.SetReference(detail, "_deleteButton", delete);
            GameUiBuilder.SetReference(detail, "_themeScope", _scope);
            UiThemeSceneSetup.SetReferenceArray(detail, "_worldTabs", tabs);
            UiThemeSceneSetup.SetReferenceArray(detail, "_worldTabIcons", tabIcons);
            return detail;
        }

        /// <summary>删除确认框：全屏半透明遮罩上一张小卡片，确认键用取消色（红）提示这是危险操作。</summary>
        internal ConfirmDialog BuildConfirmDialog(Transform parent)
        {
            RectTransform root = GameUiBuilder.CreateFullscreen("ConfirmDialog", parent, GameUiBuilder.DimBackground, true);
            var dialog = root.gameObject.AddComponent<ConfirmDialog>();
            RectTransform card = UiStyleKit.CreateCard(root, "Card", Center, Vector2.zero, new Vector2(640f, 340f), UiColorRole.PanelBackground, _scope, _context);
            TMP_Text message = CreateText(card, "Message", string.Empty, 30f, TextAlignmentOptions.Center, Center, new Vector2(0f, 50f), new Vector2(560f, 160f), UiColorRole.Text, false, _scope);
            Button confirm = _layout.CreateButton(card, "ConfirmButton", UiText.DeleteLevel, BottomCenter, new Vector2(-140f, Inset), new Vector2(240f, 72f), UiColorRole.Cancel);
            Button cancel = _layout.CreateButton(card, "CancelButton", UiText.Cancel, BottomCenter, new Vector2(140f, Inset), new Vector2(240f, 72f), UiColorRole.Accent);
            GameUiBuilder.SetReference(dialog, "_messageText", message);
            GameUiBuilder.SetReference(dialog, "_confirmButton", confirm);
            GameUiBuilder.SetReference(dialog, "_cancelButton", cancel);
            root.gameObject.SetActive(false);
            return dialog;
        }

        private EditorGridView BuildMiniMap(RectTransform card, Vector2 position, Vector2 size)
        {
            RectTransform area = GameUiBuilder.CreateRect("MiniMap", card);
            GameUiBuilder.Place(area, TopLeft, position, size);
            var grid = area.gameObject.AddComponent<EditorGridView>();
            string brushes = LevelEditorUiBuilder.BrushFolder;
            GameUiBuilder.SetReference(grid, "_gridArea", area);
            GameUiBuilder.SetReference(grid, "_cellPrefab", EditorGridCellPrefabBuilder.GetOrUpdate(_context));
            GameUiBuilder.SetReference(grid, "_palette", LevelEditorUiBuilder.GetOrCreateGridPalette());
            GameUiBuilder.SetReference(grid, "_worldPalette", GameUiBuilder.FindAsset<WorldPalette>(null));
            GameUiBuilder.SetReference(grid, "_playerIcon", UiStyleKit.LoadIconSprite(brushes + "/Grid_Player.png"));
            GameUiBuilder.SetReference(grid, "_boxIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Box.png"));
            GameUiBuilder.SetReference(grid, "_portalIcon", UiStyleKit.LoadIconSprite(brushes + "/Brush_Portal.png"));
            var serialized = new SerializedObject(grid);
            serialized.FindProperty("_isReadOnly").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return grid;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, float size)
        {
            RectTransform rect = GameUiBuilder.CreateRect(name, parent);
            GameUiBuilder.Place(rect, anchor, position, new Vector2(size, size));
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void StretchHorizontally(RectTransform rect, float margin)
        {
            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
            rect.offsetMin = new Vector2(margin, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-margin, rect.offsetMax.y);
        }

        private static void SetSingleLine(TMP_Text text, float minSize, float maxSize)
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
        }
    }
}
