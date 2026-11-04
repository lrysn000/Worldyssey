using System;
using System.Collections;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 挂在面板根节点上，持有这块界面当前使用的主题。子物体上的 <see cref="ThemedGraphic"/> 从这里取色。
    /// 换主题时可以渐变：渐变期间每帧通知一次，结束后停止。
    /// </summary>
    public class UiThemeScope : MonoBehaviour
    {
        [Tooltip("控制器设置主题之前使用的主题，一般填中性主题")]
        [SerializeField] private UiTheme _initialTheme;

        private UiTheme _fromTheme;
        private UiTheme _theme;
        private float _blend = 1f;
        private Coroutine _fadeRoutine;

        /// <summary>当前（渐变结束后）的主题。</summary>
        public UiTheme Theme => _theme;

        /// <summary>颜色发生了变化，子元素需要重新取色。</summary>
        public event Action ColorsChanged;

        private void Awake()
        {
            // 控制器可能在面板首次激活之前就设置了主题，此时不能用初始主题覆盖
            if (_theme == null)
            {
                _theme = _initialTheme;
                _fromTheme = _initialTheme;
            }

            if (_theme == null)
            {
                Debug.LogError($"[{nameof(UiThemeScope)}] {name} 缺少引用：{nameof(_initialTheme)}", this);
            }
        }

        private void OnDisable()
        {
            // 面板隐藏时直接落到目标主题，下次显示不会停在半途
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
                FinishFade();
            }
        }

        /// <summary>
        /// 切换主题。<paramref name="fadeDuration"/> 为 0 或面板未激活时立即切换。
        /// </summary>
        public void SetTheme(UiTheme theme, float fadeDuration)
        {
            if (theme == null)
            {
                Debug.LogError($"[{nameof(UiThemeScope)}] {name} 收到空主题，保持原主题", this);
                return;
            }

            if (theme == _theme && _fadeRoutine == null)
            {
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            // 渐变中途再次切换时，从上一次的目标开始，跳变不超过一次渐变，代价可以接受
            _fromTheme = _theme != null ? _theme : theme;
            _theme = theme;
            if (fadeDuration <= 0f || !isActiveAndEnabled || _fromTheme == _theme)
            {
                FinishFade();
                return;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(fadeDuration));
        }

        /// <summary>
        /// 取某个角色的当前颜色（渐变中为插值结果）。还没有任何主题时返回 false。
        /// </summary>
        public bool TryGetColor(UiColorRole role, out Color color)
        {
            if (_theme == null)
            {
                color = default;
                return false;
            }

            Color target = _theme.GetColor(role);
            color = _blend >= 1f || _fromTheme == null ? target : Color.Lerp(_fromTheme.GetColor(role), target, _blend);
            return true;
        }

        private IEnumerator FadeRoutine(float duration)
        {
            // 用不受时间缩放影响的时间，暂停菜单里切换也能正常完成
            float elapsed = 0f;
            _blend = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _blend = Mathf.Clamp01(elapsed / duration);
                ColorsChanged?.Invoke();
                yield return null;
            }

            _fadeRoutine = null;
            FinishFade();
        }

        private void FinishFade()
        {
            _fromTheme = _theme;
            _blend = 1f;
            ColorsChanged?.Invoke();
        }
    }
}
