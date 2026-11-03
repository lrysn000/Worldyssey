using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡初始时已出现的一个传送箱子。未列出的一侧视为未出现。规则逻辑在 M4 接入。
    /// </summary>
    /// <remarks>字段名即 JSON 键名，因此不加下划线。</remarks>
    [Serializable]
    public class PortalBoxData
    {
        [SerializeField] private int pair;
        [SerializeField] private int world;
        [SerializeField] private int x;
        [SerializeField] private int y;

        public PortalBoxData()
        {
        }

        public PortalBoxData(int pair, int world, int x, int y)
        {
            this.pair = pair;
            this.world = world;
            this.x = x;
            this.y = y;
        }

        public int Pair => pair;
        public int World => world;
        public GridPos Position => new GridPos(x, y);
    }
}
