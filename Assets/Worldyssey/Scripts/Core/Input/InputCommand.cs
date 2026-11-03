using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一条待执行的玩家命令：向某方向移动，或传送。
    /// </summary>
    public readonly struct InputCommand : IEquatable<InputCommand>
    {
        private InputCommand(InputCommandType type, Direction direction)
        {
            Type = type;
            Direction = direction;
        }

        public InputCommandType Type { get; }

        /// <summary>移动方向，仅 <see cref="InputCommandType.Move"/> 有意义。</summary>
        public Direction Direction { get; }

        public static InputCommand Move(Direction direction)
        {
            return new InputCommand(InputCommandType.Move, direction);
        }

        public static InputCommand Teleport()
        {
            return new InputCommand(InputCommandType.Teleport, default);
        }

        public bool Equals(InputCommand other)
        {
            return Type == other.Type && (Type != InputCommandType.Move || Direction == other.Direction);
        }

        public override bool Equals(object obj)
        {
            return obj is InputCommand other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Type == InputCommandType.Move ? 1 + (int)Direction : 0;
        }

        public override string ToString()
        {
            return Type == InputCommandType.Move ? $"Move({Direction})" : "Teleport";
        }
    }
}
