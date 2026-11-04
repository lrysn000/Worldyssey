using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 让一个 Image 或文字跟随主题着色：选一个颜色角色，从所属的 <see cref="UiThemeScope"/> 取色。
    /// 素材是白色精灵图，着色后就是主题色。
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class ThemedGraphic : MonoBehaviour
    {
        [SerializeField] private UiColorRole _role = UiColorRole.Accent;

        [Tooltip("保留物体原本的透明度，只换颜色。用于半透明的遮罩、阴影")]
        [SerializeField] private bool _keepAlpha;

        [Tooltip("留空时自动使用最近的父级 UiThemeScope")]
        [SerializeField] private UiThemeScope _scope;

        private Graphic _graphic;

        public UiColorRole Role => _role;

        private void Reset()
        {
            // 添加组件时自动填好作用域，Inspector 里一眼能看出它跟随哪块面板
            _scope = GetComponentInParent<UiThemeScope>(true);
        }

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            if (_scope == null)
            {
                _scope = GetComponentInParent<UiThemeScope>(true);
            }

            if (_scope == null)
            {
                Debug.LogError($"[{nameof(ThemedGraphic)}] {name} 找不到父级 {nameof(UiThemeScope)}，不会跟随主题", this);
            }
        }

        private void OnEnable()
        {
            if (_scope == null)
            {
                return;
            }

            _scope.ColorsChanged += HandleColorsChanged;

            // 隐藏期间主题可能已经变了，显示时补一次
            Apply();
        }

        private void OnDisable()
        {
            if (_scope != null)
            {
                _scope.ColorsChanged -= HandleColorsChanged;
            }
        }

        /// <summary>
        /// 运行时切换颜色角色，例如列表项的选中高亮。
        /// </summary>
        public void SetRole(UiColorRole role)
        {
            _role = role;
            Apply();
        }

        private void HandleColorsChanged()
        {
            Apply();
        }

        private void Apply()
        {
            if (_graphic == null || _scope == null || !_scope.TryGetColor(_role, out Color color))
            {
                return;
            }

            if (_keepAlpha)
            {
                color.a = _graphic.color.a;
            }

            _graphic.color = color;
        }
    }
}
