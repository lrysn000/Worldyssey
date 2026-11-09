using System.Collections.Generic;
using System.Text;

namespace Worldyssey.UI
{
    /// <summary>
    /// 读取 CSV（RFC 4180）：逗号分隔，含逗号、引号或换行的字段用双引号包住，字段内的双引号写成两个。
    /// 在 Excel 里另存为“CSV UTF-8”即符合这个格式。
    /// </summary>
    public static class CsvTable
    {
        /// <summary>解析全部行，每行是字段列表。空行跳过。</summary>
        public static List<List<string>> Parse(string content)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool isQuoted = false;
            int start = content.Length > 0 && content[0] == '﻿' ? 1 : 0;
            for (int i = start; i < content.Length; i++)
            {
                char c = content[i];
                if (isQuoted)
                {
                    if (c != '"')
                    {
                        field.Append(c);
                    }
                    else if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        isQuoted = false;
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        isQuoted = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        EndRow(rows, row, field);
                        row = new List<string>();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            EndRow(rows, row, field);
            return rows;
        }

        private static void EndRow(List<List<string>> rows, List<string> row, StringBuilder field)
        {
            row.Add(field.ToString());
            field.Clear();
            bool isEmpty = row.Count == 1 && row[0].Length == 0;
            if (!isEmpty)
            {
                rows.Add(row);
            }
        }
    }
}
