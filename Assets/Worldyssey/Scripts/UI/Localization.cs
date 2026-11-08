using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 界面文字翻译。英文原文就是查找键：代码里的英文（UiText、教学文字、世界名）经 <see cref="Translate"/> 换成当前语言，
    /// 翻译表第一列是英文原文，之后每列一种语言（列序与 <see cref="GameLanguage"/> 一致）。找不到翻译时原样返回英文。
    /// 由 GameSettingsController 在启动时加载翻译表并设置语言。
    /// </summary>
    public static class Localization
    {
        // 设置文件里保存的语言代码，下标与 GameLanguage 一致
        private static readonly string[] LanguageCodes = { "en", "zh-Hans" };

        // 设置面板里显示的语言名，用各自语言书写，不随界面语言变化
        private static readonly string[] NativeNames = { "English", "简体中文" };

        private static readonly Dictionary<string, string>[] Tables = new Dictionary<string, string>[LanguageCodes.Length];
        private static readonly HashSet<string> ReportedMissing = new HashSet<string>();

        /// <summary>当前语言。</summary>
        public static GameLanguage Current { get; private set; } = GameLanguage.English;

        /// <summary>支持的语言数量。</summary>
        public static int LanguageCount => LanguageCodes.Length;

        /// <summary>语言切换了。界面上由代码生成的文字应订阅它并重新生成。</summary>
        public static event Action LanguageChanged;

        /// <summary>
        /// 加载翻译表（CSV，首行为语言代码表头）。表头里没有的语言保持为空，查找时退回英文。
        /// </summary>
        public static void LoadTable(TextAsset table)
        {
            for (int i = 0; i < Tables.Length; i++)
            {
                Tables[i] = null;
            }

            ReportedMissing.Clear();
            if (table == null)
            {
                return;
            }

            List<List<string>> rows = CsvTable.Parse(table.text);
            if (rows.Count == 0)
            {
                Debug.LogError($"[{nameof(Localization)}] 翻译表 {table.name} 是空的");
                return;
            }

            List<string> header = rows[0];
            for (int column = 1; column < header.Count; column++)
            {
                int language = Array.IndexOf(LanguageCodes, header[column].Trim());
                if (language < 0)
                {
                    Debug.LogWarning($"[{nameof(Localization)}] 翻译表 {table.name} 的列 {header[column]} 不是支持的语言代码，已忽略");
                    continue;
                }

                Tables[language] = ReadColumn(rows, column);
            }
        }

        /// <summary>切换语言并通知界面刷新。语言没变时不通知。</summary>
        public static void SetLanguage(GameLanguage language)
        {
            if (language == Current)
            {
                return;
            }

            Current = language;
            LanguageChanged?.Invoke();
        }

        /// <summary>把英文原文换成当前语言；英文或缺少翻译时原样返回。</summary>
        public static string Translate(string source)
        {
            if (Current == GameLanguage.English || string.IsNullOrEmpty(source))
            {
                return source;
            }

            Dictionary<string, string> table = Tables[(int)Current];
            if (table != null && table.TryGetValue(source, out string translated))
            {
                return translated;
            }

            ReportMissing(source);
            return source;
        }

        /// <summary>语言代码（存进设置文件）。</summary>
        public static string ToCode(GameLanguage language)
        {
            int index = (int)language;
            return index >= 0 && index < LanguageCodes.Length ? LanguageCodes[index] : LanguageCodes[0];
        }

        /// <summary>按语言代码找语言，未知代码返回英文。</summary>
        public static GameLanguage FromCode(string code)
        {
            int index = Array.IndexOf(LanguageCodes, code);
            return index >= 0 ? (GameLanguage)index : GameLanguage.English;
        }

        /// <summary>语言的本地名称，例如“简体中文”。</summary>
        public static string GetNativeName(GameLanguage language)
        {
            int index = (int)language;
            return index >= 0 && index < NativeNames.Length ? NativeNames[index] : language.ToString();
        }

        private static Dictionary<string, string> ReadColumn(List<List<string>> rows, int column)
        {
            var table = new Dictionary<string, string>(rows.Count);
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                // 没填译文的行不收录，查找时退回英文
                if (row.Count > column && row[0].Length > 0 && row[column].Length > 0)
                {
                    table[row[0]] = row[column];
                }
            }

            return table;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void ReportMissing(string source)
        {
            // 只在编辑器里提示，每条只报一次，方便补全翻译表
            if (ReportedMissing.Add(source))
            {
                Debug.LogWarning($"[{nameof(Localization)}] 缺少 {ToCode(Current)} 翻译：\"{source}\"");
            }
        }
    }
}
