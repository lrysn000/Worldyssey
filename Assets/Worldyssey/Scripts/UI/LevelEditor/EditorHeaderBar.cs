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
    /// </summary>
    public class EditorHeaderBar : MonoBehaviour
    {
        private static readonly Color TabNormalColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color TabSelectedColor = new Color(0.30f, 0.50f, 0.85f, 1f);

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

        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();

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
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();
            if (_nameInput != null)
            {
                _nameInput.onEndEdit.RemoveListener(HandleNameEndEdit);
            }
        }

        public void Render(LevelDraft draft, int currentWorld)
        {
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

                tab.image.color = i == currentWorld ? SelectedColor(draft.GetWorldTheme(i)) : TabNormalColor;
                TMP_Text label = tab.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = draft.GetWorldName(i) + (i == draft.StartWorld ? UiText.StartWorldMark : string.Empty);
                }
            }

            RenderThemeButtons(draft.GetWorldTheme(currentWorld));
        }

        private void RenderThemeButtons(WorldTheme currentTheme)
        {
            for (int i = 0; i < _themeButtons.Length; i++)
            {
                Button button = _themeButtons[i];
                if (button == null)
                {
                    continue;
                }

                var theme = (WorldTheme)i;
                button.image.color = theme == currentTheme ? SelectedColor(theme) : TabNormalColor;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = WorldThemes.DisplayName(theme);
                }
            }
        }

        private Color SelectedColor(WorldTheme theme)
        {
            return _palette != null && _palette.TryGetColor((int)theme, out Color color) ? color : TabSelectedColor;
        }

        private void HandleNameEndEdit(string text)
        {
            NameEdited?.Invoke(text.Trim());
        }
    }
}
