using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡中单个世界的静态定义：名称和独有墙。
    /// </summary>
    /// <remarks>字段名即 JSON 键名，因此不加下划线。</remarks>
    [Serializable]
    public class WorldData
    {
        [SerializeField] private string name = string.Empty;
        [Tooltip("独有墙，从上到下每行一个字符串：'x' 为墙，空格为空")]
        [SerializeField] private string[] wallRows = Array.Empty<string>();

        public WorldData()
        {
        }

        public WorldData(string name, string[] wallRows)
        {
            this.name = name ?? string.Empty;
            this.wallRows = wallRows ?? Array.Empty<string>();
        }

        public string Name => name ?? string.Empty;
        public IReadOnlyList<string> WallRows => wallRows ?? Array.Empty<string>();
    }
}
