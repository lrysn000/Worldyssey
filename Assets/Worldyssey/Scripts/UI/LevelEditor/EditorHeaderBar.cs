using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器顶部栏：关卡名、尺寸调整、世界标签与增删世界、当前世界的主题。只发出请求事件，不直接修改草稿。
    /// 选中的世界页签和主题按钮用季节色作底；未选中的用主题的面板底色，图标保留季节色，方便辨认。
    /// </summary>
    public class EditorHeaderBar : MonoBehaviour
    {
        private static readonly Color FallbackNormalColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color FallbackSelectedColor = new Color(0.30f, 0.50f, 0.85f, 1f);

        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private TMP_Text _sizeText;
        [SerializeField] private Button _widthMinusButton;
        [SerializeField] private Button _widthPlusButton;
        [SerializeField] private Button _heightMinusButton;
        [SerializeField] private Button _heightPlusButton;

        [Tooltip("最多 4 个世界标签，按世界编号排列")]
        [SerializeField] private Button[] _worldTabs = new Button[0];
        [SerializeField] private Button _addWorldButton;
        [SerializeField] private Button _removeWorldButton;

        [Tooltip("可选：主题按钮，按 春、夏、秋、冬 顺序。点被其他世界占用的主题时两个世界交换主题")]
        [SerializeField] private Button[] _themeButtons = new Button[0];

        [Tooltip("可选：季节调色板，选中的世界页签和主题按钮用季节色显示")]
        [SerializeField] private WorldPalette _palette;

        [Header("外观（可选）")]
        [Tooltip("未选中页签的底色和文字色取自这里；不设置时用内置深色底、白字")]
        [SerializeField] private UiThemeScope _themeScope;

        [Tooltip("按 春、夏、秋、冬 排列的季节图标，显示在世界页签和主题按钮上")]
        [SerializeField] private Sprite[] _seasonIcons = new Sprite[WorldThemes.Count];

        [Tooltip("与世界页签一一对应的图标")]
        [SerializeField] private Image[] _worldTabIcons = new Image[0];

        [Tooltip("与主题按钮一一对应的图标")]
        [SerializeField] private Image[] _themeIcons = new Image[0];

        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();
        private LevelDraft _lastDraft;
        private int _lastWorld;

        public event Action<string> NameEdited;

        /// <summary>请求改变尺寸，参数为宽、高的增量。</summary>
        public event Action<int, int> ResizeRequested;

        public event Action<int> WorldSelected;
        public event Action AddWorldRequested;
        public event Action RemoveWorldRequested;

        /// <summary>请求把当前世界设为这个主题。</summary>
        public event Action<WorldTheme> ThemeSelected;

        private void Awake()
        {
            if (_nameInput == null || _sizeText == null || _worldTabs.Length == 0)
            {
                Debug.LogError($"[{nameof(EditorHeaderBar)}] {name} 引用未配置完整", this);
            }

            _bindings.Add(_widthMinusButton, () => ResizeRequested?.Invoke(-1, 0));
            _bindings.Add(_widthPlusButton, () => ResizeRequested?.Invoke(1, 0));
            _bindings.Add(_heightMinusButton, () => ResizeRequested?.Invoke(0, -1));
            _bindings.Add(_heightPlusButton, () => ResizeRequested?.Invoke(0, 1));
            _bindings.Add(_addWorldButton, () => AddWorldRequested?.Invoke());
            _bindings.Add(_removeWorldButton, () => RemoveWorldRequested?.Invoke());
            for (int i = 0; i < _worldTabs.Length; i++)
            {
                int world = i;
                _bindings.Add(_worldTabs[i], () => WorldSelected?.Invoke(world));
            }

            for (int i = 0; i < _themeButtons.Length; i++)
            {
                var theme = (WorldTheme)i;
                _bindings.Add(_themeButtons[i], () => ThemeSelected?.Invoke(theme));
            }
        }

        private void OnEnable()
        {
            _bindings.Subscribe();
            if (_nameInput != null)
            {
                _nameInput.onEndEdit.AddListener(HandleNameEndEdit);
            }

            if (_themeScope != null)
            {
                _themeScope.ColorsChanged += HandleColorsChanged;
            }
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();
            if (_nameInput != null)
            {
                _nameInput.onEndEdit.RemoveListener(HandleNameEndEdit);
            }

            if (_themeScope != null)
            {
                _themeScope.ColorsChanged -= HandleColorsChanged;
            }
        }

        public void Render(LevelDraft draft, int currentWorld)
        {
            _lastDraft = draft;
            _lastWorld = currentWorld;
            if (_nameInput != null)
            {
                _nameInput.SetTextWithoutNotify(draft.DisplayName);
            }

            if (_sizeText != null)
            {
                _sizeText.text = string.Format(UiText.SizeFormat, draft.Width, draft.Height);
            }

            for (int i = 0; i < _worldTabs.Length; i++)
            {
                Button tab = _worldTabs[i];
                if (tab == null)
                {
                    continue;
                }

                bool exists = i < draft.WorldCount;
                tab.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                string tabName = draft.GetWorldName(i) + (i == draft.StartWorld ? UiText.StartWorldMark : string.Empty);
                Image icon = i < _worldTabIcons.Length ? _worldTabIcons[i] : null;
                RenderButton(tab, icon, draft.GetWorldTheme(i), i == currentWorld, tabName);
            }

            WorldTheme currentTheme = draft.GetWorldTheme(currentWorld);
            for (int i = 0; i < _themeButtons.Length; i++)
            {
                if (_themeButtons[i] == null)
                {
                    continue;
                }

                var theme = (WorldTheme)i;
                Image icon = i < _themeIcons.Length ? _themeIcons[i] : null;
                RenderButton(_themeButtons[i], icon, theme, theme == currentTheme, WorldThemes.DisplayName(theme));
            }
        }

        /// <summary>
        /// 选中：季节色底 + 浅色图标和文字；未选中：面板底色 + 季节色图标 + 正文色文字。按钮上的文字可以没有（只有图标的主题按钮）。
        /// </summary>
        private void RenderButton(Button button, Image icon, WorldTheme theme, bool isSelected, string text)
        {
            Color season = SeasonColor(theme);
            Color background = isSelected ? season : GetThemeColor(UiColorRole.PanelBackground, FallbackNormalColor);
            Color content = isSelected ? GetThemeColor(UiColorRole.HeaderText, Color.white) : GetThemeColor(UiColorRole.Text, Color.white);
            button.image.color = background;

            if (icon != null)
            {
                int seasonIndex = (int)theme;
                icon.sprite = seasonIndex >= 0 && seasonIndex < _seasonIcons.Length ? _seasonIcons[seasonIndex] : null;
                icon.enabled = icon.sprite != null;
                icon.color = isSelected ? content : season;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                label.color = content;
            }
        }

        private Color SeasonColor(WorldTheme theme)
        {
            return _palette != null && _palette.TryGetColor((int)theme, out Color color) ? color : FallbackSelectedColor;
        }

        private Color GetThemeColor(UiColorRole role, Color fallback)
        {
            return _themeScope != null && _themeScope.TryGetColor(role, out Color color) ? color : fallback;
        }

        private void HandleColorsChanged()
        {
            if (_lastDraft != null)
            {
                Render(_lastDraft, _lastWorld);
            }
        }

        private void HandleNameEndEdit(string text)
        {
            NameEdited?.Invoke(text.Trim());
        }
    }
}
