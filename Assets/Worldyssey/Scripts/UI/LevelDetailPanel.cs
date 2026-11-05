using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关页右侧的详情栏：编号、名字、各世界的季节页签、俯视小地图（复用编辑器网格，只读）、最佳步数，
    /// 以及开始、编辑、删除按钮（编辑和删除只对自建关卡显示）。未解锁的关卡不显示小地图，改为显示锁。
    /// </summary>
    public class LevelDetailPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _numberText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _worldNameText;
        [SerializeField] private TMP_Text _bestText;

        [Tooltip("按世界顺序排列的页签，最多 4 个")]
        [SerializeField] private Button[] _worldTabs = new Button[0];

        [Tooltip("与页签一一对应的图标")]
        [SerializeField] private Image[] _worldTabIcons = new Image[0];

        [SerializeField] private EditorGridView _miniMap;

        [Tooltip("未解锁时盖在小地图位置上的锁")]
        [SerializeField] private GameObject _lockedOverlay;

        [SerializeField] private Button _playButton;
        [SerializeField] private Button _editButton;
        [SerializeField] private Button _deleteButton;

        [Header("外观")]
        [Tooltip("未选中页签的底色和选中页签的图标色取自这里")]
        [SerializeField] private UiThemeScope _themeScope;

        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();
        private Sprite[] _seasonIcons = new Sprite[0];
        private WorldPalette _palette;
        private LevelDraft _draft;
        private int _world;

        public event Action PlayRequested;
        public event Action EditRequested;
        public event Action DeleteRequested;

        private void Awake()
        {
            if (_numberText == null || _nameText == null || _bestText == null || _miniMap == null || _playButton == null)
            {
                Debug.LogError($"[{nameof(LevelDetailPanel)}] {name} 引用未配置完整", this);
            }

            _bindings.Add(_playButton, () => PlayRequested?.Invoke());
            _bindings.Add(_editButton, () => EditRequested?.Invoke());
            _bindings.Add(_deleteButton, () => DeleteRequested?.Invoke());
            for (int i = 0; i < _worldTabs.Length; i++)
            {
                int world = i;
                _bindings.Add(_worldTabs[i], () => ShowWorld(world));
            }
        }

        private void OnEnable()
        {
            _bindings.Subscribe();
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();
        }

        /// <summary>季节图标（春、夏、秋、冬）和季节色，由选关面板在显示前传入。</summary>
        public void SetSeasonVisuals(Sprite[] seasonIcons, WorldPalette palette)
        {
            _seasonIcons = seasonIcons ?? new Sprite[0];
            _palette = palette;
        }

        /// <summary>
        /// 显示一个关卡的详情，默认看起始世界。<paramref name="isCustom"/> 为 true 时显示编辑和删除按钮。
        /// </summary>
        public void Show(LevelSelectItem item, int number, bool isCustom)
        {
            _numberText.text = number.ToString("00");
            _nameText.text = item.DisplayName;
            _bestText.text = item.IsLocked ? UiText.LockedHint
                : item.IsCleared ? string.Format(UiText.BestStepsFormat, item.BestSteps) : UiText.NotCleared;
            _playButton.interactable = !item.IsLocked;
            SetButtonVisible(_editButton, isCustom);
            SetButtonVisible(_deleteButton, isCustom);

            _draft = null;
            if (!item.IsLocked && item.Level != null)
            {
                try
                {
                    _draft = LevelDraft.FromLevelData(item.Level);
                }
                catch (ArgumentException exception)
                {
                    // 列表中的关卡都通过了校验，解析失败说明校验器与解析不一致，需要暴露出来
                    Debug.LogError($"[{nameof(LevelDetailPanel)}] 关卡 '{item.Level.Id}' 无法生成小地图：{exception.Message}", this);
                }
            }

            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(_draft == null);
            }

            _miniMap.gameObject.SetActive(_draft != null);
            ShowWorld(_draft != null ? _draft.StartWorld : 0);
        }

        private void ShowWorld(int world)
        {
            _world = world;
            int worldCount = _draft != null ? _draft.WorldCount : 0;
            for (int i = 0; i < _worldTabs.Length; i++)
            {
                bool exists = i < worldCount;
                _worldTabs[i].gameObject.SetActive(exists);
                if (exists)
                {
                    RenderTab(i);
                }
            }

            if (_worldNameText != null)
            {
                _worldNameText.text = _draft != null ? _draft.GetWorldName(world) : string.Empty;
            }

            if (_draft != null)
            {
                _miniMap.Render(_draft, world, null);
            }
        }

        /// <summary>与编辑器世界页签一致：选中为季节色底 + 浅色图标，未选中为面板底色 + 季节色图标。</summary>
        private void RenderTab(int world)
        {
            int season = (int)_draft.GetWorldTheme(world);
            Color seasonColor = _palette != null && _palette.TryGetColor(season, out Color color) ? color : Color.gray;
            bool isSelected = world == _world;
            _worldTabs[world].image.color = isSelected ? seasonColor : GetThemeColor(UiColorRole.PanelBackground, Color.white);

            Image icon = world < _worldTabIcons.Length ? _worldTabIcons[world] : null;
            if (icon != null)
            {
                icon.sprite = season >= 0 && season < _seasonIcons.Length ? _seasonIcons[season] : null;
                icon.color = isSelected ? GetThemeColor(UiColorRole.HeaderText, Color.white) : seasonColor;
            }
        }

        private Color GetThemeColor(UiColorRole role, Color fallback)
        {
            return _themeScope != null && _themeScope.TryGetColor(role, out Color color) ? color : fallback;
        }

        private static void SetButtonVisible(Button button, bool isVisible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isVisible);
            }
        }
    }
}
