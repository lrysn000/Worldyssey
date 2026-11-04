using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 一套界面配色：每个 <see cref="UiColorRole"/> 对应一种颜色。中性主题和四季主题各一份。
    /// 功能色不在这里定义，统一引用 <see cref="UiFunctionalPalette"/>。
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Worldyssey/UI/Theme")]
    public class UiTheme : ScriptableObject
    {
        // 功能色缺失时用醒目的品红，让问题在画面上一眼可见
        private static readonly Color MissingColor = Color.magenta;

        [Header("结构")]
        [Tooltip("面板标题栏、底部按钮栏")]
        [SerializeField] private Color _header = new Color(0.33f, 0.39f, 0.47f, 1f);

        [Tooltip("面板外框和底色")]
        [SerializeField] private Color _panelBackground = new Color(0.93f, 0.94f, 0.95f, 1f);

        [Tooltip("面板内的分组卡片")]
        [SerializeField] private Color _innerFrame = Color.white;

        [Header("强调与文字")]
        [Tooltip("普通按钮、滑条填充、选中状态。下方填了调色板时以调色板为准")]
        [SerializeField] private Color _accent = new Color(0.38f, 0.55f, 0.75f, 1f);

        [Tooltip("面板内的正文")]
        [SerializeField] private Color _text = new Color(0.20f, 0.23f, 0.28f, 1f);

        [Tooltip("标题栏和按钮上的文字，需要与标题栏、强调色有足够对比度")]
        [SerializeField] private Color _headerText = Color.white;

        [Tooltip("可着色的图标")]
        [SerializeField] private Color _icon = new Color(0.30f, 0.34f, 0.40f, 1f);

        [Header("强调色来源")]
        [Tooltip("可选：填了就用调色板中对应世界的颜色作为强调色，与 3D 的传送箱子、传送遮罩保持一致。中性主题留空")]
        [SerializeField] private WorldPalette _worldPalette;

        [Tooltip("调色板中的世界编号：0 春、1 夏、2 秋、3 冬")]
        [Min(0)]
        [SerializeField] private int _paletteWorld;

        [Header("功能色")]
        [Tooltip("确认、取消、禁用的颜色，所有主题引用同一份")]
        [SerializeField] private UiFunctionalPalette _functionalPalette;

        /// <summary>
        /// 取某个角色在本主题下的颜色。
        /// </summary>
        public Color GetColor(UiColorRole role)
        {
            switch (role)
            {
                case UiColorRole.Header:
                    return _header;
                case UiColorRole.PanelBackground:
                    return _panelBackground;
                case UiColorRole.InnerFrame:
                    return _innerFrame;
                case UiColorRole.Accent:
                    return _worldPalette != null && _worldPalette.TryGetColor(_paletteWorld, out Color accent) ? accent : _accent;
                case UiColorRole.Text:
                    return _text;
                case UiColorRole.HeaderText:
                    return _headerText;
                case UiColorRole.Icon:
                    return _icon;
                case UiColorRole.Confirm:
                    return _functionalPalette != null ? _functionalPalette.Confirm : MissingColor;
                case UiColorRole.Cancel:
                    return _functionalPalette != null ? _functionalPalette.Cancel : MissingColor;
                case UiColorRole.Disabled:
                    return _functionalPalette != null ? _functionalPalette.Disabled : MissingColor;
                default:
                    Debug.LogError($"[{nameof(UiTheme)}] {name} 未处理的颜色角色：{role}", this);
                    return MissingColor;
            }
        }

        private void OnValidate()
        {
            if (_functionalPalette == null)
            {
                Debug.LogError($"[{nameof(UiTheme)}] {name} 缺少引用：{nameof(_functionalPalette)}", this);
            }
        }
    }
}
