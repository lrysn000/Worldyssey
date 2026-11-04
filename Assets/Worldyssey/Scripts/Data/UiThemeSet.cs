using UnityEngine;
using Worldyssey.Core;

namespace Worldyssey.Data
{
    /// <summary>
    /// 全部界面主题的入口：一份中性主题，加上按世界编号排列的季节主题（0 春、1 夏、2 秋、3 冬）。
    /// </summary>
    [CreateAssetMenu(fileName = "UiThemeSet", menuName = "Worldyssey/UI/Theme Set")]
    public class UiThemeSet : ScriptableObject
    {
        [Tooltip("主菜单、选关，以及没有当前世界时使用")]
        [SerializeField] private UiTheme _neutralTheme;

        [Tooltip("按季节排列：0 春、1 夏、2 秋、3 冬")]
        [SerializeField] private UiTheme[] _worldThemes = new UiTheme[LevelLimits.MaxWorldCount];

        [Tooltip("跟随世界换色时的渐变时长（秒），0 为瞬间切换")]
        [Min(0f)]
        [SerializeField] private float _fadeDuration = 0.15f;

        public UiTheme NeutralTheme => _neutralTheme;
        public float FadeDuration => _fadeDuration;

        /// <summary>
        /// 取某个季节的 UI 主题（参数是季节编号，不是世界编号）；越界或没配置时回退为中性主题。
        /// </summary>
        public UiTheme GetSeasonTheme(int season)
        {
            if (_worldThemes != null && season >= 0 && season < _worldThemes.Length && _worldThemes[season] != null)
            {
                return _worldThemes[season];
            }

            return _neutralTheme;
        }

        private void OnValidate()
        {
            if (_neutralTheme == null)
            {
                Debug.LogError($"[{nameof(UiThemeSet)}] {name} 缺少引用：{nameof(_neutralTheme)}", this);
            }
        }
    }
}
