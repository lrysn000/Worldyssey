using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关界面：顶栏切换内置 / 自建关卡，左侧卡片墙，右侧详情栏。
    /// 点卡片先选中查看详情，再点开始、双击卡片或按回车进入；自建关卡可以编辑和删除（删除前确认）。
    /// </summary>
    public class LevelSelectPanel : UiPanel
    {
        private const float DoubleClickSeconds = 0.35f;

        [SerializeField] private LevelCard _cardPrefab;
        [SerializeField] private Transform _cardListRoot;
        [SerializeField] private TMP_Text _emptyText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Button _builtInTab;
        [SerializeField] private Button _customTab;
        [SerializeField] private Button _backButton;
        [SerializeField] private LevelDetailPanel _detailPanel;
        [SerializeField] private ConfirmDialog _confirmDialog;

        [Header("图标与颜色")]
        [SerializeField] private WorldPalette _worldPalette;

        [Tooltip("按 春、夏、秋、冬 排列的季节图标")]
        [SerializeField] private Sprite[] _seasonIcons = new Sprite[0];
        [SerializeField] private Sprite _clearedIcon;
        [SerializeField] private Sprite _nextIcon;
        [SerializeField] private Sprite _lockedIcon;

        private readonly List<LevelCard> _cards = new List<LevelCard>();
        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();
        private IReadOnlyList<LevelSelectItem> _builtInItems = Array.Empty<LevelSelectItem>();
        private IReadOnlyList<LevelSelectItem> _customItems = Array.Empty<LevelSelectItem>();
        private ThemedGraphic[] _builtInTabParts = new ThemedGraphic[0];
        private ThemedGraphic[] _customTabParts = new ThemedGraphic[0];
        private bool _isCustomTab;
        private int _selected = -1;
        private float _lastClickTime = float.NegativeInfinity;

        /// <summary>选择了内置关卡，参数为关卡目录中的下标。</summary>
        public event Action<int> BuiltInLevelChosen;

        /// <summary>选择了自建关卡，参数为自建列表中的下标。</summary>
        public event Action<int> CustomLevelChosen;

        /// <summary>请求在关卡编辑器中打开自建关卡，参数为自建列表中的下标。</summary>
        public event Action<int> CustomLevelEditRequested;

        /// <summary>玩家已确认删除自建关卡，参数为自建列表中的下标。</summary>
        public event Action<int> CustomLevelDeleteRequested;

        public event Action BackRequested;

        private IReadOnlyList<LevelSelectItem> CurrentItems => _isCustomTab ? _customItems : _builtInItems;

        private void Awake()
        {
            CheckReference(_cardPrefab, nameof(_cardPrefab));
            CheckReference(_cardListRoot, nameof(_cardListRoot));
            CheckReference(_detailPanel, nameof(_detailPanel));
            CheckReference(_confirmDialog, nameof(_confirmDialog));
            CheckReference(_backButton, nameof(_backButton));

            _bindings.Add(_backButton, () => BackRequested?.Invoke());
            _bindings.Add(_builtInTab, () => SetTab(false));
            _bindings.Add(_customTab, () => SetTab(true));
            CacheTabParts();
        }

        private void OnEnable()
        {
            _bindings.Subscribe();

            // 首次打开时 Populate 先于 Awake 执行，那时页签着色组件还没缓存，这里补一次
            RenderTabs();
            if (_detailPanel != null)
            {
                _detailPanel.PlayRequested += HandlePlayRequested;
                _detailPanel.EditRequested += HandleEditRequested;
                _detailPanel.DeleteRequested += HandleDeleteRequested;
            }
        }

        private void Update()
        {
            // 回车直接开始选中的关卡；确认框打开时不响应，避免误操作
            Keyboard keyboard = Keyboard.current;
            bool isConfirmOpen = _confirmDialog != null && _confirmDialog.IsVisible;
            if (keyboard != null && !isConfirmOpen && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                HandlePlayRequested();
            }
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();
            if (_detailPanel != null)
            {
                _detailPanel.PlayRequested -= HandlePlayRequested;
                _detailPanel.EditRequested -= HandleEditRequested;
                _detailPanel.DeleteRequested -= HandleDeleteRequested;
            }

            if (_confirmDialog != null)
            {
                _confirmDialog.SetVisible(false);
            }
        }

        /// <summary>
        /// 用最新数据重建卡片，保持当前页签。内置关卡默认选中下一个该玩的关卡。
        /// 选关界面打开频率低、卡片少，直接销毁重建，不做对象池。
        /// </summary>
        public void Populate(IReadOnlyList<LevelSelectItem> builtInLevels, IReadOnlyList<LevelSelectItem> customLevels)
        {
            _builtInItems = builtInLevels;
            _customItems = customLevels;
            if (_detailPanel != null)
            {
                _detailPanel.SetSeasonVisuals(_seasonIcons, _worldPalette);
            }

            if (_isCustomTab && customLevels.Count == 0)
            {
                // 自建关卡全删光后回到内置关卡页
                _isCustomTab = false;
            }

            Rebuild();
        }

        private void SetTab(bool isCustom)
        {
            if (_isCustomTab == isCustom)
            {
                return;
            }

            _isCustomTab = isCustom;
            Rebuild();
        }

        private void Rebuild()
        {
            ClearCards();
            RenderTabs();
            RenderProgress();
            IReadOnlyList<LevelSelectItem> items = CurrentItems;
            if (_cardPrefab != null && _cardListRoot != null)
            {
                var icons = new LevelCardIcons(_clearedIcon, _nextIcon, _lockedIcon);
                for (int i = 0; i < items.Count; i++)
                {
                    LevelCard card = Instantiate(_cardPrefab, _cardListRoot);
                    card.Setup(items[i], i, i + 1, _seasonIcons, _worldPalette, icons, HandleCardClicked);
                    _cards.Add(card);
                }
            }

            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(items.Count == 0);
                _emptyText.text = _isCustomTab ? Localization.Translate(UiText.NoCustomLevels) : Localization.Translate(UiText.NoLevels);
            }

            _selected = -1;
            Select(_isCustomTab ? (items.Count > 0 ? 0 : -1) : LevelSelectSummary.FindDefaultSelection(items));
        }

        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].SetSelected(i == index);
            }

            if (_detailPanel == null)
            {
                return;
            }

            _detailPanel.gameObject.SetActive(index >= 0);
            if (index >= 0)
            {
                _detailPanel.Show(CurrentItems[index], index + 1, _isCustomTab);
            }
        }

        private void RenderTabs()
        {
            SetTabRoles(_builtInTabParts, _builtInTab, !_isCustomTab);
            SetTabRoles(_customTabParts, _customTab, _isCustomTab);
        }

        /// <summary>选中的页签用强调色底 + 浅色字，未选中用面板底色 + 正文色字。</summary>
        private static void SetTabRoles(ThemedGraphic[] parts, Button tab, bool isSelected)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                bool isBackground = tab != null && parts[i].gameObject == tab.gameObject;
                UiColorRole role = isBackground
                    ? isSelected ? UiColorRole.Accent : UiColorRole.PanelBackground
                    : isSelected ? UiColorRole.HeaderText : UiColorRole.Text;
                parts[i].SetRole(role);
            }
        }

        private void RenderProgress()
        {
            if (_progressText == null)
            {
                return;
            }

            _progressText.gameObject.SetActive(!_isCustomTab);
            _progressText.text = string.Format(Localization.Translate(UiText.ClearedProgressFormat), LevelSelectSummary.CountCleared(_builtInItems), _builtInItems.Count);
        }

        private void CacheTabParts()
        {
            _builtInTabParts = _builtInTab != null ? _builtInTab.GetComponentsInChildren<ThemedGraphic>(true) : new ThemedGraphic[0];
            _customTabParts = _customTab != null ? _customTab.GetComponentsInChildren<ThemedGraphic>(true) : new ThemedGraphic[0];
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    Destroy(_cards[i].gameObject);
                }
            }

            _cards.Clear();
        }

        private void HandleCardClicked(int index)
        {
            // 再次点击已选中的卡片且间隔够短视为双击，直接开始
            bool isDoubleClick = index == _selected && Time.unscaledTime - _lastClickTime <= DoubleClickSeconds;
            _lastClickTime = Time.unscaledTime;
            if (isDoubleClick)
            {
                HandlePlayRequested();
                return;
            }

            Select(index);
        }

        private void HandlePlayRequested()
        {
            IReadOnlyList<LevelSelectItem> items = CurrentItems;
            if (_selected < 0 || _selected >= items.Count || items[_selected].IsLocked)
            {
                return;
            }

            if (_isCustomTab)
            {
                CustomLevelChosen?.Invoke(_selected);
            }
            else
            {
                BuiltInLevelChosen?.Invoke(_selected);
            }
        }

        private void HandleEditRequested()
        {
            if (_isCustomTab && _selected >= 0)
            {
                CustomLevelEditRequested?.Invoke(_selected);
            }
        }

        private void HandleDeleteRequested()
        {
            if (!_isCustomTab || _selected < 0 || _confirmDialog == null)
            {
                return;
            }

            int index = _selected;
            string message = string.Format(Localization.Translate(UiText.DeleteLevelConfirmFormat), _customItems[index].DisplayName);
            _confirmDialog.Show(message, () => CustomLevelDeleteRequested?.Invoke(index));
        }
    }
}
