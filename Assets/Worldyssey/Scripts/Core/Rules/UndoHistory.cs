using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 撤销快照栈。每成功一步压入一份走之前的状态，并记下这一步是否计入步数（传送可以撤销，但不算一步）。
    /// </summary>
    public sealed class UndoHistory
    {
        private readonly Stack<Entry> _entries = new Stack<Entry>();

        /// <summary>快照数量，即可撤销的次数。</summary>
        public int Count => _entries.Count;

        /// <summary>计入步数的快照数量，即显示给玩家的步数。</summary>
        public int StepCount { get; private set; }

        /// <summary>
        /// 压入一份快照。<see cref="LevelState"/> 对外只读，直接保存引用即可。
        /// </summary>
        public void Push(LevelState snapshot, bool isCountedStep)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _entries.Push(new Entry(snapshot, isCountedStep));
            if (isCountedStep)
            {
                StepCount++;
            }
        }

        /// <summary>
        /// 弹出最近一份快照。没有可撤销的步骤时返回 false。
        /// </summary>
        public bool TryUndo(out LevelState snapshot)
        {
            if (_entries.Count == 0)
            {
                snapshot = null;
                return false;
            }

            Entry entry = _entries.Pop();
            if (entry.IsCountedStep)
            {
                StepCount--;
            }

            snapshot = entry.Snapshot;
            return true;
        }

        /// <summary>清空历史，用于重开。</summary>
        public void Clear()
        {
            _entries.Clear();
            StepCount = 0;
        }

        private readonly struct Entry
        {
            public readonly LevelState Snapshot;
            public readonly bool IsCountedStep;

            public Entry(LevelState snapshot, bool isCountedStep)
            {
                Snapshot = snapshot;
                IsCountedStep = isCountedStep;
            }
        }
    }
}
