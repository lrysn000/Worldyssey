using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡的动态状态。对外只读，只有同一程序集内的 <see cref="LevelRules"/> 能在克隆出的新实例上修改，
    /// 所以撤销快照可以直接保存引用，不用再复制。
    /// </summary>
    /// <remarks>
    /// 不包含步数：步数由撤销历史长度得出，这样求解器比较状态时不会因步数不同而误判。
    /// </remarks>
    public sealed class LevelState : IEquatable<LevelState>
    {
        private static readonly PortalBoxState[] NoPortalBoxes = Array.Empty<PortalBoxState>();

        private readonly GridPos[] _mappedBoxes;
        private readonly PortalBoxState[] _portalBoxes;

        /// <summary>
        /// 没有传送箱子的状态（单世界关卡）。
        /// </summary>
        public LevelState(int playerWorld, GridPos playerPosition, Direction playerFacing, IReadOnlyList<GridPos> mappedBoxes)
            : this(playerWorld, playerPosition, playerFacing, mappedBoxes, NoPortalBoxes)
        {
        }

        public LevelState(
            int playerWorld,
            GridPos playerPosition,
            Direction playerFacing,
            IReadOnlyList<GridPos> mappedBoxes,
            IReadOnlyList<PortalBoxState> portalBoxes)
        {
            if (mappedBoxes == null)
            {
                throw new ArgumentNullException(nameof(mappedBoxes));
            }

            if (portalBoxes == null)
            {
                throw new ArgumentNullException(nameof(portalBoxes));
            }

            PlayerWorld = playerWorld;
            PlayerPosition = playerPosition;
            PlayerFacing = playerFacing;

            // 复制一份，避免调用方之后修改传入的集合影响状态
            _mappedBoxes = new GridPos[mappedBoxes.Count];
            for (int i = 0; i < _mappedBoxes.Length; i++)
            {
                _mappedBoxes[i] = mappedBoxes[i];
            }

            _portalBoxes = new PortalBoxState[portalBoxes.Count];
            for (int i = 0; i < _portalBoxes.Length; i++)
            {
                _portalBoxes[i] = portalBoxes[i];
            }
        }

        public int PlayerWorld { get; private set; }
        public GridPos PlayerPosition { get; private set; }
        public Direction PlayerFacing { get; private set; }

        /// <summary>
        /// 目标箱子坐标，下标即箱子编号，在整局中保持不变，表现层据此对应模型。
        /// </summary>
        public IReadOnlyList<GridPos> MappedBoxes => _mappedBoxes;

        /// <summary>
        /// 所有传送箱子（含未出现的），下标规则见 <see cref="PortalBoxState"/>。
        /// </summary>
        public IReadOnlyList<PortalBoxState> PortalBoxes => _portalBoxes;

        /// <summary>
        /// 返回位于该坐标的目标箱子编号，没有则返回 -1。
        /// </summary>
        public int FindMappedBoxAt(GridPos pos)
        {
            for (int i = 0; i < _mappedBoxes.Length; i++)
            {
                if (_mappedBoxes[i] == pos)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 返回在指定世界、位于该坐标且已出现的传送箱子下标，没有则返回 -1。
        /// </summary>
        public int FindPortalBoxAt(int world, GridPos pos)
        {
            for (int i = 0; i < _portalBoxes.Length; i++)
            {
                PortalBoxState box = _portalBoxes[i];
                if (box.IsPresent && box.World == world && box.Position == pos)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 深拷贝。
        /// </summary>
        public LevelState Clone()
        {
            return new LevelState(PlayerWorld, PlayerPosition, PlayerFacing, _mappedBoxes, _portalBoxes);
        }

        public bool Equals(LevelState other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (PlayerWorld != other.PlayerWorld
                || PlayerPosition != other.PlayerPosition
                || PlayerFacing != other.PlayerFacing
                || _mappedBoxes.Length != other._mappedBoxes.Length
                || _portalBoxes.Length != other._portalBoxes.Length)
            {
                return false;
            }

            for (int i = 0; i < _mappedBoxes.Length; i++)
            {
                if (_mappedBoxes[i] != other._mappedBoxes[i])
                {
                    return false;
                }
            }

            for (int i = 0; i < _portalBoxes.Length; i++)
            {
                if (!_portalBoxes[i].Equals(other._portalBoxes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LevelState);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerWorld;
                hash = (hash * 397) ^ PlayerPosition.GetHashCode();
                hash = (hash * 397) ^ (int)PlayerFacing;
                for (int i = 0; i < _mappedBoxes.Length; i++)
                {
                    hash = (hash * 397) ^ _mappedBoxes[i].GetHashCode();
                }

                for (int i = 0; i < _portalBoxes.Length; i++)
                {
                    hash = (hash * 397) ^ _portalBoxes[i].GetHashCode();
                }

                return hash;
            }
        }

        internal void SetPlayerWorld(int world)
        {
            PlayerWorld = world;
        }

        internal void SetPlayerPosition(GridPos position)
        {
            PlayerPosition = position;
        }

        internal void SetPlayerFacing(Direction facing)
        {
            PlayerFacing = facing;
        }

        internal void SetMappedBox(int index, GridPos position)
        {
            _mappedBoxes[index] = position;
        }

        internal void SetPortalBox(int index, PortalBoxState box)
        {
            _portalBoxes[index] = box;
        }
    }
}
