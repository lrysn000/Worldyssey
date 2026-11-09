using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把 <see cref="PaletteUsage"/> 写成网页：上面是按图片方向排列的色块网格，下面是每个用到的色块被哪些模型使用、能否单独修改。
    /// 坐标按图片编辑器的习惯，以左上角为原点，方便在 Photoshop 等软件里定位。
    /// </summary>
    internal static class PaletteReportHtml
    {
        private const float GridWidth = 720f;
        private const int MinCellPixels = 14;

        public static string Build(PaletteUsage usage, List<string> unreadable)
        {
            var html = new StringBuilder();
            html.Append("<!doctype html><html lang=\"zh\"><head><meta charset=\"utf-8\"><title>调色板色块报告 - ")
                .Append(Escape(usage.Texture.name)).Append("</title><style>")
                .Append("body{font-family:system-ui,'Microsoft YaHei',sans-serif;margin:24px;background:#f6f6f4;color:#222}")
                .Append("table.grid{border-collapse:collapse}table.grid td{text-align:center;font-size:11px;padding:0}")
                .Append(".unused{opacity:.3}.single{outline:2px solid #1a7f37;outline-offset:-2px}.shared{outline:3px solid #d1242f;outline-offset:-3px}")
                .Append("table.list{border-collapse:collapse;margin-top:16px}table.list th,table.list td{border:1px solid #ccc;padding:4px 8px;font-size:13px;vertical-align:top}")
                .Append(".swatch{display:inline-block;width:18px;height:18px;border:1px solid #888;vertical-align:middle}.ok{color:#1a7f37}.warn{color:#d1242f}")
                .Append("</style></head><body>");

            html.Append("<h1>调色板色块报告：").Append(Escape(usage.Texture.name)).Append("</h1><p>")
                .Append(Escape(AssetDatabase.GetAssetPath(usage.Texture))).Append("　").Append(usage.Width).Append(" × ").Append(usage.Height)
                .Append(" 像素，色块 ").Append(usage.CellSize).Append(" 像素，共 ").Append(usage.Columns).Append(" 列 × ").Append(usage.Rows).Append(" 行</p>")
                .Append("<p>色块里的字是用到它的模型类别。<span class=\"single\">绿框</span>：只有一类模型在用，可以放心改；")
                .Append("<span class=\"shared\">红框</span>：多类模型共用，改了会一起变；半透明：没有模型用到。鼠标悬停可看具体模型。坐标以图片左上角为原点。</p>");

            AppendGrid(html, usage);
            AppendList(html, usage);
            if (unreadable.Count > 0)
            {
                html.Append("<p class=\"warn\">以下模型的网格不可读，没有统计进来（在模型导入设置里开启 Read/Write 后重新生成）：")
                    .Append(Escape(string.Join("、", unreadable))).Append("</p>");
            }

            html.Append("</body></html>");
            return html.ToString();
        }

        private static void AppendGrid(StringBuilder html, PaletteUsage usage)
        {
            int cellPixels = Mathf.Max(MinCellPixels, Mathf.FloorToInt(GridWidth / usage.Columns));
            html.Append("<table class=\"grid\">");

            // 图片第一行对应 UV 的最上面一行
            for (int imageRow = 0; imageRow < usage.Rows; imageRow++)
            {
                html.Append("<tr>");
                for (int column = 0; column < usage.Columns; column++)
                {
                    var cell = new Vector2Int(column, usage.Rows - 1 - imageRow);
                    usage.Cells.TryGetValue(cell, out HashSet<PrefabLabel> labels);
                    HashSet<string> categories = Categories(labels);
                    string cssClass = labels == null ? "unused" : categories.Count > 1 ? "shared" : "single";
                    Color32 color = usage.GetCellColor(cell);
                    html.Append("<td class=\"").Append(cssClass).Append("\" style=\"width:").Append(cellPixels).Append("px;height:").Append(cellPixels)
                        .Append("px;background:").Append(Hex(color)).Append(";color:").Append(TextColor(color)).Append("\" title=\"")
                        .Append(Escape(Tooltip(usage, column, imageRow, color, labels))).Append("\">");
                    if (labels != null && cellPixels >= 28)
                    {
                        html.Append(Escape(string.Join(" ", categories)));
                    }

                    html.Append("</td>");
                }

                html.Append("</tr>");
            }

            html.Append("</table>");
        }

        private static void AppendList(StringBuilder html, PaletteUsage usage)
        {
            html.Append("<h2>用到的色块</h2><table class=\"list\"><tr><th>色块（列, 行）</th><th>像素范围</th><th>颜色</th><th>结论</th><th>模型</th></tr>");
            for (int imageRow = 0; imageRow < usage.Rows; imageRow++)
            {
                for (int column = 0; column < usage.Columns; column++)
                {
                    var cell = new Vector2Int(column, usage.Rows - 1 - imageRow);
                    if (!usage.Cells.TryGetValue(cell, out HashSet<PrefabLabel> labels))
                    {
                        continue;
                    }

                    HashSet<string> categories = Categories(labels);
                    Color32 color = usage.GetCellColor(cell);
                    string verdict = categories.Count > 1
                        ? $"<span class=\"warn\">共用：{Escape(string.Join("、", categories))}，改了会一起变</span>"
                        : $"<span class=\"ok\">只有{Escape(string.Join("", categories))}，可以单独改</span>";
                    html.Append("<tr><td>").Append(column).Append(", ").Append(imageRow).Append("</td><td>").Append(PixelRange(usage, column, imageRow))
                        .Append("</td><td><span class=\"swatch\" style=\"background:").Append(Hex(color)).Append("\"></span> ").Append(Hex(color))
                        .Append("</td><td>").Append(verdict).Append("</td><td>").Append(Escape(Names(labels))).Append("</td></tr>");
                }
            }

            html.Append("</table>");
        }

        private static HashSet<string> Categories(HashSet<PrefabLabel> labels)
        {
            var categories = new HashSet<string>();
            if (labels != null)
            {
                foreach (PrefabLabel label in labels)
                {
                    categories.Add(label.Category);
                }
            }

            return categories;
        }

        private static string Names(HashSet<PrefabLabel> labels)
        {
            var names = new List<string>();
            foreach (PrefabLabel label in labels)
            {
                names.Add(label.Name);
            }

            names.Sort();
            return string.Join("、", names);
        }

        private static string Tooltip(PaletteUsage usage, int column, int imageRow, Color32 color, HashSet<PrefabLabel> labels)
        {
            string head = $"色块 ({column}, {imageRow})  像素 {PixelRange(usage, column, imageRow)}  {Hex(color)}";
            return labels == null ? head + "\n未使用" : head + "\n" + Names(labels).Replace("、", "\n");
        }

        private static string PixelRange(PaletteUsage usage, int column, int imageRow)
        {
            int x = column * usage.CellSize;
            int y = imageRow * usage.CellSize;
            int last = usage.CellSize - 1;
            return usage.CellSize == 1 ? $"x {x}, y {y}" : $"x {x}~{x + last}, y {y}~{y + last}";
        }

        private static string Hex(Color32 color)
        {
            return $"#{color.r:X2}{color.g:X2}{color.b:X2}";
        }

        private static string TextColor(Color32 color)
        {
            float luminance = (0.299f * color.r + 0.587f * color.g + 0.114f * color.b) / 255f;
            return luminance > 0.55f ? "#000" : "#fff";
        }

        private static string Escape(string text)
        {
            return WebUtility.HtmlEncode(text);
        }
    }
}
