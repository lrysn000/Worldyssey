using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 决定什么时候执行哪条命令：动画期间缓存 1 条命令（移动或传送），并处理按住方向键连续移动。
    /// 当前时间由调用方传入，因此时序逻辑可以脱离引擎做单元测试。
    /// </summary>
    /// <remarks>
    /// 规则：
    /// 1. 每次按下方向键或传送键都会写入缓存（全局只保留最后一条），空闲时优先执行缓存，所以快速点按不会丢。
    /// 2. 同时按住多个方向时以最后按下的为准；松开它会回到仍按住的方向。
    /// 3. 当前方向按住达到 <see cref="HoldRepeatDelay"/> 后，每次空闲都继续往该方向走。
    /// </remarks>
    public sealed class InputCommandScheduler
    {
        private readonly List<HeldDirection> _heldDirections = new List<HeldDirection>(4);
        private InputCommand? _bufferedCommand;

        public InputCommandScheduler(float holdRepeatDelay)
        {
            if (holdRepeatDelay < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(holdRepeatDelay), holdRepeatDelay, "不能为负数");
            }

            HoldRepeatDelay = holdRepeatDelay;
        }

        /// <summary>按住多久后开始连续移动，单位秒。</summary>
        public float HoldRepeatDelay { get; }

        /// <summary>
        /// 最近一次 <see cref="TryGetNextCommand"/> 返回的命令是否来自按住连续移动（而不是一次按下）。
        /// 表现层据此只在真正按下时播放撞墙回弹，避免顶着墙按住时不停回弹。
        /// </summary>
        public bool LastCommandWasRepeat { get; private set; }

        /// <summary>
        /// 方向键按下。
        /// </summary>
        public void Press(Direction direction, float time)
        {
            RemoveHeld(direction);
            _heldDirections.Add(new HeldDirection(direction, time));
            _bufferedCommand = InputCommand.Move(direction);
        }

        /// <summary>
        /// 传送键按下。传送没有按住连续触发，只进缓存。
        /// </summary>
        public void PressTeleport()
        {
            _bufferedCommand = InputCommand.Teleport();
        }

        /// <summary>
        /// 方向键松开。已经缓存的这次按键仍会执行，保证快速点按不丢。
        /// </summary>
        public void Release(Direction direction)
        {
            RemoveHeld(direction);
        }

        /// <summary>
        /// 清掉缓存的命令，按住状态保留。撤销、重开时调用，避免旧命令在新状态上执行。
        /// </summary>
        public void ClearBuffer()
        {
            _bufferedCommand = null;
        }

        /// <summary>
        /// 清空全部按键状态。输入被停用、收不到松开事件时调用，避免方向键“卡住”。
        /// </summary>
        public void Reset()
        {
            _heldDirections.Clear();
            _bufferedCommand = null;
        }

        /// <summary>
        /// 询问这一帧是否应执行命令。<paramref name="isBusy"/> 为 true（动画播放中）时总是返回 false。
        /// </summary>
        public bool TryGetNextCommand(float time, bool isBusy, out InputCommand command)
        {
            command = default;
            if (isBusy)
            {
                return false;
            }

            if (_bufferedCommand.HasValue)
            {
                command = _bufferedCommand.Value;
                _bufferedCommand = null;
                LastCommandWasRepeat = false;
                return true;
            }

            if (_heldDirections.Count == 0)
            {
                return false;
            }

            HeldDirection current = _heldDirections[_heldDirections.Count - 1];
            if (time - current.PressTime < HoldRepeatDelay)
            {
                return false;
            }

            command = InputCommand.Move(current.Direction);
            LastCommandWasRepeat = true;
            return true;
        }

        private void RemoveHeld(Direction direction)
        {
            for (int i = _heldDirections.Count - 1; i >= 0; i--)
            {
                if (_heldDirections[i].Direction == direction)
                {
                    _heldDirections.RemoveAt(i);
                }
            }
        }

        private readonly struct HeldDirection
        {
            public readonly Direction Direction;
            public readonly float PressTime;

            public HeldDirection(Direction direction, float pressTime)
            {
                Direction = direction;
                PressTime = pressTime;
            }
        }
    }
}
