using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 撤销快照栈。每成功一步压入一份走之前的状态，因此 <see cref="Count"/> 就是当前步数。
    /// </summary>
    public sealed class UndoHistory
    {
        private readonly Stack<LevelState> _snapshots = new Stack<LevelState>();

        /// <summary>快照数量，即当前步数。</summary>
        public int Count => _snapshots.Count;

        /// <summary>
        /// 压入一份快照。<see cref="LevelState"/> 对外只读，直接保存引用即可。
        /// </summary>
        public void Push(LevelState snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _snapshots.Push(snapshot);
        }

        /// <summary>
        /// 弹出最近一份快照。没有可撤销的步骤时返回 false。
        /// </summary>
        public bool TryUndo(out LevelState snapshot)
        {
            if (_snapshots.Count == 0)
            {
                snapshot = null;
                return false;
            }

            snapshot = _snapshots.Pop();
            return true;
        }

        /// <summary>清空历史，用于重开。</summary>
        public void Clear()
        {
            _snapshots.Clear();
        }
    }
}
