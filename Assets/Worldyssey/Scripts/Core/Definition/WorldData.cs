using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡中单个世界的静态定义：名称、主题和独有墙。名称只为兼容保留，显示时以主题为准。
    /// </summary>
    /// <remarks>字段名即 JSON 键名，因此不加下划线。</remarks>
    [Serializable]
    public class WorldData
    {
        [SerializeField] private string name = string.Empty;
        [Tooltip("独有墙，从上到下每行一个字符串：'x' 为墙，空格为空")]
        [SerializeField] private string[] wallRows = Array.Empty<string>();
        [Tooltip("主题：Spring / Summer / Autumn / Winter。留空时按世界编号决定（旧关卡没有这个字段）")]
        [SerializeField] private string theme = string.Empty;

        public WorldData()
        {
        }

        public WorldData(string name, string[] wallRows)
            : this(name, wallRows, string.Empty)
        {
        }

        public WorldData(string name, string[] wallRows, string theme)
        {
            this.name = name ?? string.Empty;
            this.wallRows = wallRows ?? Array.Empty<string>();
            this.theme = theme ?? string.Empty;
        }

        public string Name => name ?? string.Empty;
        public IReadOnlyList<string> WallRows => wallRows ?? Array.Empty<string>();

        /// <summary>数据里的主题原文，用 <see cref="WorldThemes.TryParse"/> 解析。</summary>
        public string ThemeText => theme ?? string.Empty;
    }
}
