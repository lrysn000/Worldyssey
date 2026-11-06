using TMPro;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.View;

namespace Worldyssey.UI
{
    /// <summary>
    /// 画面正上方的世界指示器：上面是世界的季节图标，下面是世界名字。冲突中的世界显示为灰色图标加叉号。
    /// 当前世界跟随 <see cref="LevelView.ShownWorldChanged"/>，与 3D 画面和界面换色在同一时刻切换；
    /// 冲突状态跟随 <see cref="LevelSession.StateChanged"/>。
    /// </summary>
    public class WorldIndicatorPanel : MonoBehaviour
    {
        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private LevelView _levelView;

        [Tooltip("冲突图标的灰色和叉号的红色取自这里，与按钮的禁用、取消色一致")]
        [SerializeField] private UiFunctionalPalette _functionalPalette;

        [Tooltip("可选：切换世界时图标弹一下的幅度和时长。不设置则不弹")]
        [SerializeField] private UiMotionConfig _motionConfig;

        [Header("内容")]
        [Tooltip("按季节编号排列：春、夏、秋、冬。白色剪影，运行时按季节色着色")]
        [SerializeField] private Sprite[] _seasonIcons = new Sprite[WorldThemes.Count];

        [Tooltip("图标位，数量不少于关卡最多的世界数")]
        [SerializeField] private WorldIconSlot[] _slots = new WorldIconSlot[0];

        [SerializeField] private TMP_Text _worldNameText;

        [Header("显示方式")]
        [Tooltip("勾选：排出全部世界，当前世界放大。不勾选：只显示当前世界，以及冲突中的其他世界")]
        [SerializeField] private bool _showAllWorlds;

        [Tooltip("当前世界图标的边长")]
        [Min(8f)]
        [SerializeField] private float _currentIconSize = 56f;

        [Tooltip("其他世界图标的边长")]
        [Min(8f)]
        [SerializeField] private float _otherIconSize = 36f;

        [Tooltip("图标之间的间距")]
        [Min(0f)]
        [SerializeField] private float _iconSpacing = 16f;

        [Tooltip("没有冲突的其他世界图标的不透明度，让当前世界更突出")]
        [Range(0f, 1f)]
        [SerializeField] private float _otherWorldAlpha = 0.45f;

        private int _shownWorld = -1;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelSession, nameof(_levelSession))
                & CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_functionalPalette, nameof(_functionalPalette))
                & CheckReference(_worldNameText, nameof(_worldNameText));

            for (int i = 0; i < _slots.Length; i++)
            {
                _hasReferences &= CheckReference(_slots[i], $"{nameof(_slots)}[{i}]");
            }
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _levelSession.StateChanged += HandleStateChanged;
            _levelView.ShownWorldChanged += HandleShownWorldChanged;

            // HUD 可能在关卡生成之后才显示，显示时补一次
            _shownWorld = _levelView.ShownWorld;
            Refresh();
        }

        private void OnDisable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _levelSession.StateChanged -= HandleStateChanged;
            _levelView.ShownWorldChanged -= HandleShownWorldChanged;
        }

        private void HandleStateChanged()
        {
            Refresh();
        }

        private void HandleShownWorldChanged(int world)
        {
            _shownWorld = world;
            WorldIconSlot current = Refresh();
            if (current != null && _motionConfig != null)
            {
                current.Pop(_motionConfig.WorldPopScale, _motionConfig.WorldPopDuration);
            }
        }

        /// <summary>
        /// 按当前世界和冲突状态重新排列图标，返回当前世界所在的图标位（没有关卡时为 null）。
        /// </summary>
        private WorldIconSlot Refresh()
        {
            LevelMap map = _levelSession.HasLevel ? _levelSession.Session.Rules.Map : null;
            if (map == null || _shownWorld < 0 || _shownWorld >= map.WorldCount)
            {
                HideFrom(0);
                _worldNameText.text = string.Empty;
                return null;
            }

            _worldNameText.text = map.GetWorldName(_shownWorld);
            int worldCount = Mathf.Min(map.WorldCount, _slots.Length);

            // 第一遍算总宽度，第二遍从左到右摆放，整行居中；不需要临时列表
            float totalWidth = 0f;
            int shownCount = 0;
            for (int world = 0; world < worldCount; world++)
            {
                if (ShouldShow(world))
                {
                    totalWidth += GetIconSize(world);
                    shownCount++;
                }
            }

            totalWidth += Mathf.Max(0, shownCount - 1) * _iconSpacing;
            float left = -totalWidth * 0.5f;
            int slotIndex = 0;
            WorldIconSlot currentSlot = null;
            for (int world = 0; world < worldCount; world++)
            {
                if (!ShouldShow(world))
                {
                    continue;
                }

                float size = GetIconSize(world);
                bool isConflicted = _levelSession.IsWorldConflicted(world);
                WorldIconSlot slot = _slots[slotIndex++];
                slot.Show(GetIcon(world), GetIconColor(world, isConflicted), left + size * 0.5f, size, isConflicted, _functionalPalette.Cancel);
                left += size + _iconSpacing;
                if (world == _shownWorld)
                {
                    currentSlot = slot;
                }
            }

            HideFrom(slotIndex);
            return currentSlot;
        }

        private bool ShouldShow(int world)
        {
            return _showAllWorlds || world == _shownWorld || _levelSession.IsWorldConflicted(world);
        }

        private float GetIconSize(int world)
        {
            return world == _shownWorld ? _currentIconSize : _otherIconSize;
        }

        private Sprite GetIcon(int world)
        {
            int season = _levelView.GetWorldThemeIndex(world);
            return season >= 0 && season < _seasonIcons.Length ? _seasonIcons[season] : null;
        }

        private Color GetIconColor(int world, bool isConflicted)
        {
            if (isConflicted)
            {
                return _functionalPalette.Disabled;
            }

            Color color = _levelView.GetWorldColor(world);
            if (world != _shownWorld)
            {
                color.a *= _otherWorldAlpha;
            }

            return color;
        }

        private void HideFrom(int firstSlot)
        {
            for (int i = firstSlot; i < _slots.Length; i++)
            {
                _slots[i].Hide();
            }
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(WorldIndicatorPanel)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
