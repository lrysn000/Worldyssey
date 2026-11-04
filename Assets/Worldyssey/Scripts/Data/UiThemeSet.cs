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

        [Tooltip("按世界编号排列：0 春、1 夏、2 秋、3 冬")]
        [SerializeField] private UiTheme[] _worldThemes = new UiTheme[LevelLimits.MaxWorldCount];

        [Tooltip("跟随世界换色时的渐变时长（秒），0 为瞬间切换")]
        [Min(0f)]
        [SerializeField] private float _fadeDuration = 0.15f;

        public UiTheme NeutralTheme => _neutralTheme;
        public float FadeDuration => _fadeDuration;

        /// <summary>
        /// 取某个世界的主题；编号越界或该位置没配置时回退为中性主题。
        /// </summary>
        public UiTheme GetWorldTheme(int world)
        {
            if (_worldThemes != null && world >= 0 && world < _worldThemes.Length && _worldThemes[world] != null)
            {
                return _worldThemes[world];
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
