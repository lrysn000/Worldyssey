using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一对传送箱子所连接的两个世界。规则逻辑在 M4 接入。
    /// </summary>
    /// <remarks>字段名即 JSON 键名，因此不加下划线。</remarks>
    [Serializable]
    public class PortalPairData
    {
        [SerializeField] private int id;
        [SerializeField] private int worldA;
        [SerializeField] private int worldB;

        public PortalPairData()
        {
        }

        public PortalPairData(int id, int worldA, int worldB)
        {
            this.id = id;
            this.worldA = worldA;
            this.worldB = worldB;
        }

        public int Id => id;
        public int WorldA => worldA;
        public int WorldB => worldB;
    }
}
