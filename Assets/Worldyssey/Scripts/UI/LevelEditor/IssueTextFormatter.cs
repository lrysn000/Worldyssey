using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 把校验器产出的 <see cref="IssueText"/> 按当前语言格式化。
    /// 模板和嵌套的标签（"World {0}"、"Box {0}" 等）各自查翻译表，数字、坐标等普通参数原样输出。
    /// </summary>
    public static class IssueTextFormatter
    {
        /// <summary>返回当前语言下的文字。</summary>
        public static string Format(IssueText text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            string template = Localization.Translate(text.Template);
            int count = text.Arguments.Count;
            if (count == 0)
            {
                return template;
            }

            var arguments = new object[count];
            for (int i = 0; i < count; i++)
            {
                object argument = text.Arguments[i];
                arguments[i] = argument is IssueText nested ? Format(nested) : argument;
            }

            try
            {
                return string.Format(template, arguments);
            }
            catch (System.FormatException exception)
            {
                // 译文占位符写错时 string.Format 会抛异常；为一条提示让整个列表失败不划算，所以退回英文原文
                UnityEngine.Debug.LogWarning($"[{nameof(IssueTextFormatter)}] 译文模板格式错误，改用英文：\"{template}\"（{exception.Message}）");
                return text.ToString();
            }
        }
    }
}
