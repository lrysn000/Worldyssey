using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 玩家初始位置和朝向。所在世界由 <see cref="LevelData.StartWorld"/> 决定。
    /// </summary>
    /// <remarks>字段名即 JSON 键名，因此不加下划线。</remarks>
    [Serializable]
    public class PlayerSpawnData
    {
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private Direction facing = Direction.Down;

        public PlayerSpawnData()
        {
        }

        public PlayerSpawnData(int x, int y, Direction facing)
        {
            this.x = x;
            this.y = y;
            this.facing = facing;
        }

        public GridPos Position => new GridPos(x, y);
        public Direction Facing => facing;
    }
}
