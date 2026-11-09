using System;
using System.Collections.Generic;
using System.Globalization;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一段可翻译的提示文字：英文模板（string.Format 格式）加参数。
    /// Core 层不依赖 UI 的本地化模块，所以这里只记录"模板 + 参数"，由显示方按当前语言翻译模板后再格式化。
    /// 参数本身也可以是 <see cref="IssueText"/>（例如 "World 2"、"Box 0"），显示方应递归翻译。
    /// </summary>
    public sealed class IssueText
    {
        private static readonly object[] EmptyArguments = Array.Empty<object>();

        private readonly object[] _arguments;
        private string _english;

        private IssueText(string template, object[] arguments)
        {
            Template = template ?? string.Empty;
            _arguments = arguments ?? EmptyArguments;
        }

        /// <summary>英文模板，同时是翻译表的查找键。</summary>
        public string Template { get; }

        /// <summary>模板参数：数字、坐标、字符，或者嵌套的 <see cref="IssueText"/>。</summary>
        public IReadOnlyList<object> Arguments => _arguments;

        /// <summary>创建一段提示文字。没有参数时模板按原样输出，不会被当成格式串解析。</summary>
        public static IssueText Of(string template, params object[] arguments)
        {
            return new IssueText(template, arguments);
        }

        /// <summary>英文原文，日志和测试使用。</summary>
        public override string ToString()
        {
            if (_english == null)
            {
                _english = _arguments.Length == 0 ? Template : string.Format(CultureInfo.InvariantCulture, Template, _arguments);
            }

            return _english;
        }
    }
}
