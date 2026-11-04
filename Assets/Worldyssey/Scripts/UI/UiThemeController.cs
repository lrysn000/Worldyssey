using UnityEngine;
using Worldyssey.Data;
using Worldyssey.View;

namespace Worldyssey.UI
{
    /// <summary>
    /// 决定每块界面用哪套主题：跟随世界的界面在显示的世界变化时换成对应季节，中性界面始终用中性主题，
    /// 设置面板由打开它的地方决定。
    /// 世界变化来自 <see cref="LevelView.ShownWorldChanged"/>，所以 UI 换色与 3D 画面切换世界是同一时刻。
    /// </summary>
    public class UiThemeController : MonoBehaviour
    {
        private const int NoWorld = -1;

        [SerializeField] private LevelView _levelView;
        [SerializeField] private UiThemeSet _themeSet;

        [Header("作用范围")]
        [Tooltip("跟随当前世界换季节色：HUD、暂停、结算")]
        [SerializeField] private UiThemeScope[] _worldScopes = new UiThemeScope[0];

        [Tooltip("始终使用中性主题：主菜单、选关、关卡编辑器")]
        [SerializeField] private UiThemeScope[] _neutralScopes = new UiThemeScope[0];

        [Tooltip("可选：设置面板。从主菜单打开用中性主题，从暂停打开跟随当前世界")]
        [SerializeField] private UiThemeScope _settingsScope;

        private int _currentWorld = NoWorld;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_themeSet, nameof(_themeSet));
        }

        private void OnEnable()
        {
            if (_hasReferences)
            {
                _levelView.ShownWorldChanged += HandleShownWorldChanged;
            }
        }

        private void Start()
        {
            if (!_hasReferences)
            {
                return;
            }

            ApplyToAll(_neutralScopes, _themeSet.NeutralTheme, 0f);
            ApplyToAll(_worldScopes, _themeSet.GetWorldTheme(_currentWorld), 0f);
            if (_settingsScope != null)
            {
                _settingsScope.SetTheme(_themeSet.NeutralTheme, 0f);
            }
        }

        private void OnDisable()
        {
            if (_hasReferences)
            {
                _levelView.ShownWorldChanged -= HandleShownWorldChanged;
            }
        }

        /// <summary>
        /// 打开设置面板前调用：<paramref name="followsWorld"/> 为 true 时用当前世界的季节主题，否则用中性主题。
        /// </summary>
        public void SetSettingsFollowsWorld(bool followsWorld)
        {
            if (!_hasReferences || _settingsScope == null)
            {
                return;
            }

            UiTheme theme = followsWorld ? _themeSet.GetWorldTheme(_currentWorld) : _themeSet.NeutralTheme;
            _settingsScope.SetTheme(theme, 0f);
        }

        private void HandleShownWorldChanged(int world)
        {
            _currentWorld = world;
            ApplyToAll(_worldScopes, _themeSet.GetWorldTheme(world), _themeSet.FadeDuration);
        }

        private void ApplyToAll(UiThemeScope[] scopes, UiTheme theme, float fadeDuration)
        {
            for (int i = 0; i < scopes.Length; i++)
            {
                if (scopes[i] == null)
                {
                    Debug.LogError($"[{nameof(UiThemeController)}] {name} 的作用范围列表第 {i} 项为空", this);
                    continue;
                }

                scopes[i].SetTheme(theme, fadeDuration);
            }
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(UiThemeController)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
