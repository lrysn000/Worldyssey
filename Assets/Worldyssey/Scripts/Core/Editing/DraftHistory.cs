using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 编辑器撤销栈：每一笔编辑之前压入草稿快照。超过容量时丢弃最早的快照。
    /// </summary>
    public sealed class DraftHistory
    {
        public const int DefaultCapacity = 100;

        private readonly List<LevelDraft> _snapshots = new List<LevelDraft>();
        private readonly int _capacity;

        public DraftHistory(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须大于 0");
            }

            _capacity = capacity;
        }

        public int Count => _snapshots.Count;

        /// <summary>
        /// 保存 <paramref name="draft"/> 当前内容的副本。
        /// </summary>
        public void Push(LevelDraft draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            if (_snapshots.Count >= _capacity)
            {
                _snapshots.RemoveAt(0);
            }

            _snapshots.Add(draft.Clone());
        }

        /// <summary>
        /// 取回最近一份快照。没有可撤销的内容时返回 false。
        /// </summary>
        public bool TryUndo(out LevelDraft draft)
        {
            if (_snapshots.Count == 0)
            {
                draft = null;
                return false;
            }

            int last = _snapshots.Count - 1;
            draft = _snapshots[last];
            _snapshots.RemoveAt(last);
            return true;
        }

        public void Clear()
        {
            _snapshots.Clear();
        }
    }
}
