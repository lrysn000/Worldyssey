using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 存档内容：版本号和各关通关记录。不保存关卡内进度；解锁状态由通关记录推算，不单独存。
    /// </summary>
    /// <remarks>字段名即存档 JSON 的键名，因此不加下划线。</remarks>
    [Serializable]
    public class SaveData
    {
        /// <summary>当前存档格式版本。格式变化时递增，读取旧版本时据此迁移。</summary>
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private List<LevelRecord> records = new List<LevelRecord>();

        public int Version => version;

        public IReadOnlyList<LevelRecord> Records => RecordList;

        // JSON 中缺少 records 时反序列化可能留下 null，统一兜底
        internal List<LevelRecord> RecordList => records ??= new List<LevelRecord>();
    }
}
