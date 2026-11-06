using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 关卡编辑器各区块的生成。区块用锚点拉伸随屏幕比例伸缩，区块内控件按固定尺寸摆放；引用由 <see cref="LevelEditorUiBuilder"/> 连接。
    /// </summary>
    internal sealed class LevelEditorLayout
    {
        internal const float Gap = UiStyleKit.Padding;
        internal const float TopBarHeight = 96f;
        internal const float ToolsWidth = 264f;
        internal const float SideWidth = 440f;
        private const float Inset = 24f;
        private const float RowHeight = 56f;
        private const float TabWidth = 176f;
        private const float SmallButtonSize = 52f;

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 Left = new Vector2(0f, 0.5f);
        private static readonly Vector2 Right = new Vector2(1f, 0.5f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        private readonly UiThemeScope _scope;
        private readonly UiStyleKit.Context _context;

        internal UiThemeScope Scope => _scope;
        internal UiStyleKit.Context Context => _context;
        public LevelEditorLayout(UiThemeScope scope, UiStyleKit.Context context)
        {
            _scope = scope;
            _context = context;
        }

        /// <summary>按锚点拉伸的卡片，<paramref name="offsetMin"/>/<paramref name="offsetMax"/> 为到锚点框的边距。</summary>
        internal RectTransform CreateStretchCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, UiColorRole role)
        {
            RectTransform card = UiStyleKit.CreateCard(parent, name, Center, Vector2.zero, Vector2.zero, role, _scope, _context);
            card.anchorMin = anchorMin;
            card.anchorMax = anchorMax;
            card.offsetMin = offsetMin;
            card.offsetMax = offsetMax;
            return card;
        }

        internal TMP_Text CreateTitle(Transform parent, string name, string text, Vector2 position, float width)
        {
            TMP_Text title = GameUiBuilder.CreateText(parent, name, text, 30f, TextAlignmentOptions.MidlineLeft, TopLeft, position, new Vector2(width, 40f));
            UiStyleKit.StyleText(title, UiColorRole.Header, _scope, _context, true);
            return title;
        }

        internal Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, UiColorRole role)
        {
            Button button = GameUiBuilder.CreateButton(parent, name, label, anchor, position, size);
            UiStyleKit.StyleButton(button, role, _scope, _context);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text.fontSize = Mathf.Min(28f, size.y * 0.45f);
            return button;
        }

        /// <summary>
        /// 顶栏：返回、关卡名、尺寸调整（左），新建、打开、保存、试玩（右）。
        /// </summary>
        internal void BuildTopBar(RectTransform bar, out LevelEditorTopBarParts parts)
        {
            var small = new Vector2(60f, 60f);
            parts.Back = CreateButton(bar, "BackButton", UiText.Back, Left, new Vector2(20f, 0f), new Vector2(140f, 60f), UiColorRole.Accent);
            parts.NameInput = CreateInputField(bar, new Vector2(180f, 0f), new Vector2(440f, 60f));
            parts.WidthMinus = CreateButton(bar, "WidthMinus", "W-", Left, new Vector2(648f, 0f), small, UiColorRole.Accent);
            parts.WidthPlus = CreateButton(bar, "WidthPlus", "W+", Left, new Vector2(716f, 0f), small, UiColorRole.Accent);
            parts.SizeText = GameUiBuilder.CreateText(bar, "SizeText", "9 x 7", 30f, TextAlignmentOptions.Center, Left, new Vector2(784f, 0f), new Vector2(120f, 60f));
            UiStyleKit.StyleText(parts.SizeText, UiColorRole.HeaderText, _scope, _context, true);
            parts.HeightMinus = CreateButton(bar, "HeightMinus", "H-", Left, new Vector2(912f, 0f), small, UiColorRole.Accent);
            parts.HeightPlus = CreateButton(bar, "HeightPlus", "H+", Left, new Vector2(980f, 0f), small, UiColorRole.Accent);

            var action = new Vector2(150f, 60f);
            parts.TestPlay = CreateButton(bar, "TestPlayButton", UiText.TestPlay, Right, new Vector2(-20f, 0f), new Vector2(200f, 60f), UiColorRole.Confirm);
            parts.Save = CreateButton(bar, "SaveButton", UiText.SaveLevel, Right, new Vector2(-232f, 0f), action, UiColorRole.Accent);
            parts.Open = CreateButton(bar, "OpenButton", UiText.OpenLevel, Right, new Vector2(-394f, 0f), action, UiColorRole.Accent);
            parts.New = CreateButton(bar, "NewButton", UiText.NewLevel, Right, new Vector2(-556f, 0f), action, UiColorRole.Accent);
        }

        /// <summary>
        /// 左侧画笔栏：按地形、本世界、物体分组，橡皮单独放在最后。按钮顺序与 EditorTool 枚举一致。
        /// </summary>
        internal Button[] BuildToolbar(RectTransform card, Sprite[] brushIcons)
        {
            CreateTitle(card, "ToolsHeader", UiText.ToolsHeader, new Vector2(Inset, -20f), ToolsWidth - Inset * 2f);
            string[] groupNames = { UiText.ToolGroupTerrain, UiText.ToolGroupWorld, UiText.ToolGroupObjects, null };
            int[] groupSizes = { 4, 1, 3, 1 };
            var buttons = new Button[UiText.EditorToolNames.Length];
            float width = ToolsWidth - Inset * 2f;
            float y = -72f;
            int tool = 0;
            for (int g = 0; g < groupNames.Length; g++)
            {
                if (groupNames[g] != null)
                {
                    TMP_Text label = GameUiBuilder.CreateText(card, "Group_" + groupNames[g], groupNames[g], 20f, TextAlignmentOptions.BottomLeft,
                        TopLeft, new Vector2(Inset, y), new Vector2(width, 28f));
                    UiStyleKit.StyleText(label, UiColorRole.Text, _scope, _context, false);
                    y -= 32f;
                }

                for (int i = 0; i < groupSizes[g]; i++, tool++)
                {
                    Sprite icon = tool < brushIcons.Length ? brushIcons[tool] : null;
                    buttons[tool] = CreateToolButton(card, tool, icon, new Vector2(Inset, y), width);
                    y -= 60f;
                }

                y -= 12f;
            }

            return buttons;
        }

        /// <summary>
        /// 中间区块顶部的世界栏：世界页签、增删世界（左），四个主题按钮（右）。
        /// </summary>
        internal void BuildWorldBar(RectTransform card, out LevelEditorWorldBarParts parts)
        {
            parts.Tabs = new Button[4];
            parts.TabIcons = new Image[4];
            for (int i = 0; i < parts.Tabs.Length; i++)
            {
                parts.Tabs[i] = CreateSeasonButton(card, "WorldTab_" + (i + 1), string.Format(UiText.WorldTabFormat, i + 1), TopLeft,
                    new Vector2(Inset + i * (TabWidth + 6f), -20f), new Vector2(TabWidth, RowHeight), out parts.TabIcons[i]);
            }

            // 1920 宽时中间区块约 1120：4 个页签 + 增删 + 4 个主题按钮刚好放下，改宽度时注意不要和右侧主题按钮重叠
            float afterTabs = Inset + parts.Tabs.Length * (TabWidth + 6f);
            var small = new Vector2(SmallButtonSize, RowHeight);
            parts.AddWorld = CreateButton(card, "AddWorld", "+", TopLeft, new Vector2(afterTabs, -20f), small, UiColorRole.Accent);
            parts.RemoveWorld = CreateButton(card, "RemoveWorld", "-", TopLeft, new Vector2(afterTabs + SmallButtonSize + 6f, -20f), small, UiColorRole.Cancel);

            parts.ThemeButtons = new Button[4];
            parts.ThemeIcons = new Image[4];
            for (int i = 0; i < parts.ThemeButtons.Length; i++)
            {
                var position = new Vector2(-Inset - (parts.ThemeButtons.Length - 1 - i) * (SmallButtonSize + 6f), -20f);
                parts.ThemeButtons[i] = CreateSeasonButton(card, "Theme_" + i, null, TopRight, position, small, out parts.ThemeIcons[i]);
            }
        }

        /// <summary>中间区块底部的状态提示：圆角底板 + 文字，初始透明，由 EditorActionBar 淡入淡出。</summary>
        internal TMP_Text BuildStatusToast(RectTransform card, out CanvasGroup group)
        {
            RectTransform toast = UiStyleKit.CreateCard(card, "StatusToast", BottomCenter, new Vector2(0f, 20f), new Vector2(720f, 52f),
                UiColorRole.Header, _scope, _context);
            group = toast.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            TMP_Text text = GameUiBuilder.CreateText(toast, "StatusText", string.Empty, 24f, TextAlignmentOptions.Center, Center, Vector2.zero, Vector2.zero);
            GameUiBuilder.Stretch(text.rectTransform);
            UiStyleKit.StyleText(text, UiColorRole.HeaderText, _scope, _context, false);
            return text;
        }

        /// <summary>右侧问题列表：可滚动的文字区域，内容高度随文字自动变化。</summary>
        internal TMP_Text BuildIssueScroll(RectTransform card, float top)
        {
            RectTransform scrollRect = GameUiBuilder.CreateRect("IssuesScroll", card);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(Inset, Inset);
            scrollRect.offsetMax = new Vector2(-Inset, -top);
            Image background = scrollRect.gameObject.AddComponent<Image>();
            UiStyleKit.SetSliced(background, _context.PanelSprite);
            UiStyleKit.Theme(background, UiColorRole.PanelBackground, _scope, _context);
            scrollRect.gameObject.AddComponent<RectMask2D>();

            TMP_Text text = GameUiBuilder.CreateText(scrollRect, "IssuesText", UiText.NoIssues, 20f, TextAlignmentOptions.TopLeft, TopLeft, Vector2.zero, Vector2.zero);
            RectTransform content = text.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            text.margin = new Vector4(16f, 12f, 16f, 12f);
            text.enableWordWrapping = true;
            var fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UiStyleKit.StyleText(text, UiColorRole.Text, _scope, _context, false);

            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.viewport = scrollRect;
            scroll.content = content;
            return text;
        }

        /// <summary>
        /// 画笔按钮：左边图标、右边文字，底板、图标、文字都挂主题着色，选中时由 EditorToolbar 切换颜色角色。
        /// </summary>
        private Button CreateToolButton(Transform parent, int tool, Sprite icon, Vector2 position, float width)
        {
            string toolName = UiText.EditorToolNames[tool];
            Button button = GameUiBuilder.CreateButton(parent, "Tool_" + toolName, toolName, TopLeft, position, new Vector2(width, 52f));
            UiStyleKit.SetSliced(button.image, _context.PanelSprite);
            UiStyleKit.Theme(button.image, UiColorRole.PanelBackground, _scope, _context);

            Image iconImage = CreateIcon(button.transform, icon, Left, new Vector2(14f, 0f), 32f);
            UiStyleKit.Theme(iconImage, UiColorRole.Text, _scope, _context);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.rectTransform.offsetMin = new Vector2(58f, 0f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 24f;
            UiStyleKit.StyleText(label, UiColorRole.Text, _scope, _context, false);
            return button;
        }

        /// <summary>
        /// 世界页签和主题按钮：颜色由 EditorHeaderBar 按季节直接设置，所以不挂主题着色。
        /// <paramref name="label"/> 为 null 时是只有图标的按钮。
        /// </summary>
        private Button CreateSeasonButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, out Image icon)
        {
            Button button = GameUiBuilder.CreateButton(parent, name, label ?? string.Empty, anchor, position, size);
            UiStyleKit.SetSliced(button.image, _context.PanelSprite);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                Object.DestroyImmediate(text.gameObject);
                icon = CreateIcon(button.transform, null, Center, Vector2.zero, 36f);
                return button;
            }

            icon = CreateIcon(button.transform, null, Left, new Vector2(12f, 0f), 32f);
            text.rectTransform.offsetMin = new Vector2(48f, 0f);
            text.rectTransform.offsetMax = new Vector2(-4f, 0f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            // 不换行：名字放不下时缩小字号，而不是折成两行
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 22f;
            UiStyleKit.ApplyFont(text, _context, true);

            return button;
        }

        private static Image CreateIcon(Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, float size)
        {
            RectTransform rect = GameUiBuilder.CreateRect("Icon", parent);
            GameUiBuilder.Place(rect, anchor, position, new Vector2(size, size));
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private TMP_InputField CreateInputField(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "NameInput";
            go.transform.SetParent(parent, false);
            GameUiBuilder.Place((RectTransform)go.transform, Left, position, size);
            var input = go.GetComponent<TMP_InputField>();
            input.characterLimit = 40;
            UiStyleKit.SetSliced(input.image, _context.PanelSprite);
            UiStyleKit.Theme(input.image, UiColorRole.InnerFrame, _scope, _context);

            input.textComponent.fontSize = 28f;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            UiStyleKit.StyleText(input.textComponent, UiColorRole.Text, _scope, _context, false);
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = UiText.NamePlaceholder;
                placeholder.fontSize = 28f;
                placeholder.alignment = TextAlignmentOptions.MidlineLeft;
                placeholder.color = new Color(0.55f, 0.58f, 0.62f, 1f);
                if (_context.Font != null)
                {
                    placeholder.font = _context.Font;
                }
            }

            return input;
        }
    }
}
