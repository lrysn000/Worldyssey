using TMPro;
using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 固定文字的翻译：保存英文原文，启用时和切换语言时换成当前语言。
    /// 由 Worldyssey/Content/Localize Scene Texts 自动挂到与翻译表原文完全一致的文字上；由代码改文字的地方用 <see cref="SetSource"/>。
    /// 如果文字已经被别的代码改成了其他内容（例如按键说明、步数），就不再覆盖，由那段代码自己负责翻译和刷新。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("英文原文，即翻译表第一列的内容")]
        [SerializeField] private string _source = string.Empty;

        private TMP_Text _text;
        private string _lastApplied;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Localization.LanguageChanged += HandleLanguageChanged;
            Apply(false);
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= HandleLanguageChanged;
        }

        /// <summary>
        /// 给文字设置新的英文原文并立即翻译。文字上挂了本组件时由它记住原文，切换语言后自动更新；否则只翻译一次。
        /// </summary>
        public static void SetSource(TMP_Text text, string source)
        {
            if (text == null)
            {
                return;
            }

            if (text.TryGetComponent(out LocalizedText localized))
            {
                localized._source = source;
                localized.Apply(true);
            }
            else
            {
                text.text = Localization.Translate(source);
            }
        }

        private void Apply(bool isForced)
        {
            // 从未激活过的物体不会执行 Awake，代码先调 SetSource 时在这里取组件
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            if (_text == null)
            {
                return;
            }

            // 文字既不是原文、也不是上次翻译的结果，说明被别的代码接管了，不去覆盖
            string current = _text.text;
            bool isOwned = current == _source || current == _lastApplied;
            if (!isForced && !isOwned)
            {
                return;
            }

            _lastApplied = Localization.Translate(_source);
            _text.text = _lastApplied;
        }

        private void HandleLanguageChanged()
        {
            Apply(false);
        }
    }
}
