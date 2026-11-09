using System.Text;
using TMPro;
using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 屏幕下方的教学提示条：不拦截点击、不暂停游戏，换文字和消失时淡入淡出。
    /// 文字里方括号包着的按键（例如 [F]）用按键颜色显示。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TutorialHintPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        [Tooltip("按键文字（方括号部分）的颜色")]
        [SerializeField] private Color _keyColor = new Color(1f, 0.85f, 0.45f, 1f);

        [Tooltip("淡入淡出时长（秒）")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _fadeSeconds = 0.2f;

        private readonly StringBuilder _builder = new StringBuilder(128);
        private CanvasGroup _group;
        private string _keyColorTag;
        private float _targetAlpha;

        /// <summary>提示条当前是否在显示（含淡出途中）。</summary>
        public bool IsShowing => _targetAlpha > 0f;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _keyColorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(_keyColor)}>";
            if (_text == null)
            {
                Debug.LogError($"[{nameof(TutorialHintPanel)}] {name} 缺少引用：{nameof(_text)}", this);
            }
        }

        private void OnDisable()
        {
            // HUD 隐藏时直接清空，下次显示不会残留上一关的提示
            _targetAlpha = 0f;
            if (_group != null)
            {
                _group.alpha = 0f;
            }
        }

        private void Update()
        {
            // 用不受暂停影响的时间，暂停菜单打开时也能正常淡出
            if (!Mathf.Approximately(_group.alpha, _targetAlpha))
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, Time.unscaledDeltaTime / _fadeSeconds);
            }
        }

        /// <summary>
        /// 显示一条提示，替换当前文字。
        /// </summary>
        public void Show(string message)
        {
            if (_text == null)
            {
                return;
            }

            _text.text = FormatKeys(message);
            _targetAlpha = 1f;
        }

        /// <summary>淡出隐藏。</summary>
        public void Hide()
        {
            _targetAlpha = 0f;
        }

        private string FormatKeys(string message)
        {
            // 只在换提示时调用一次，不在每帧分配
            _builder.Clear();
            for (int i = 0; i < message.Length; i++)
            {
                char c = message[i];
                if (c == '[')
                {
                    _builder.Append(_keyColorTag).Append(c);
                }
                else if (c == ']')
                {
                    _builder.Append(c).Append("</color>");
                }
                else
                {
                    _builder.Append(c);
                }
            }

            return _builder.ToString();
        }
    }
}
