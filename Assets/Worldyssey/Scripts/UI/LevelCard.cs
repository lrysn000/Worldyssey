using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关页的一张关卡卡片：编号、名字、用到的世界的季节图标、通关状态（勾 + 最佳步数 / NEW / 锁）。
    /// 未解锁的卡片可以选中查看，但整体变淡。选中时显示强调色描边。
    /// </summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _numberText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _statusText;

        [Tooltip("右上角的状态图标：通关为勾、下一关为星、未解锁为锁")]
        [SerializeField] private Image _statusIcon;

        [Tooltip("状态图标的主题着色，用来按状态切换颜色角色")]
        [SerializeField] private ThemedGraphic _statusIconTheme;

        [Tooltip("按世界顺序排列的季节图标位，最多 4 个")]
        [SerializeField] private Image[] _worldIcons = new Image[0];

        [Tooltip("选中时显示的描边")]
        [SerializeField] private GameObject _selectionFrame;

        [Tooltip("未解锁时降低整张卡片的透明度")]
        [SerializeField] private CanvasGroup _contentGroup;

        [Range(0f, 1f)]
        [SerializeField] private float _lockedAlpha = 0.5f;

        private int _index;
        private Action<int> _onClicked;

        private void Awake()
        {
            if (_button == null || _numberText == null || _nameText == null || _statusText == null || _statusIcon == null)
            {
                Debug.LogError($"[{nameof(LevelCard)}] {name} 引用未配置完整", this);
            }
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }

        /// <summary>
        /// 填充卡片。<paramref name="number"/> 为显示用的编号（从 1 开始），<paramref name="seasonIcons"/> 按季节排列。
        /// </summary>
        public void Setup(LevelSelectItem item, int index, int number, Sprite[] seasonIcons, WorldPalette palette, LevelCardIcons statusIcons,
            Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;
            _numberText.text = number.ToString("00");
            _nameText.text = item.DisplayName;
            SetupWorldIcons(item, seasonIcons, palette);
            SetupStatus(item, statusIcons);
            if (_contentGroup != null)
            {
                _contentGroup.alpha = item.IsLocked ? _lockedAlpha : 1f;
            }

            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionFrame != null)
            {
                _selectionFrame.SetActive(isSelected);
            }
        }

        private void SetupWorldIcons(LevelSelectItem item, Sprite[] seasonIcons, WorldPalette palette)
        {
            int worldCount = item.Level != null ? item.Level.Worlds.Count : 0;
            for (int i = 0; i < _worldIcons.Length; i++)
            {
                Image icon = _worldIcons[i];
                bool isUsed = i < worldCount && !item.IsLocked;
                icon.gameObject.SetActive(isUsed);
                if (!isUsed)
                {
                    continue;
                }

                int season = (int)item.GetWorldTheme(i);
                icon.sprite = season >= 0 && season < seasonIcons.Length ? seasonIcons[season] : null;
                icon.color = palette != null && palette.TryGetColor(season, out Color color) ? color : Color.white;
            }
        }

        private void SetupStatus(LevelSelectItem item, LevelCardIcons icons)
        {
            Sprite sprite;
            UiColorRole role;
            if (item.IsLocked)
            {
                sprite = icons.Locked;
                role = UiColorRole.Disabled;
                _statusText.text = UiText.Locked;
            }
            else if (item.IsCleared)
            {
                sprite = icons.Cleared;
                role = UiColorRole.Confirm;
                _statusText.text = string.Format(UiText.BestStepsFormat, item.BestSteps);
            }
            else
            {
                // 自建关卡没有“下一关”的概念，未通关时不显示星
                sprite = item.IsNext ? icons.Next : null;
                role = UiColorRole.Accent;
                _statusText.text = item.IsNext ? UiText.NextLevelTag : UiText.NotCleared;
            }

            _statusIcon.sprite = sprite;
            _statusIcon.enabled = sprite != null;
            if (_statusIconTheme != null)
            {
                _statusIconTheme.SetRole(role);
            }
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_index);
        }
    }
}
