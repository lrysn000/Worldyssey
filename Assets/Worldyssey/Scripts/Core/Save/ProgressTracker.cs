using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 进度规则：记录通关和最佳步数、判断解锁。只修改传入的 <see cref="SaveData"/>，读写文件由 <see cref="SaveService"/> 负责。
    /// </summary>
    public sealed class ProgressTracker
    {
        /// <summary>自建关卡记录键的前缀，避免与内置关卡 id 冲突。</summary>
        public const string CustomLevelKeyPrefix = "custom/";

        private readonly SaveData _data;

        public ProgressTracker(SaveData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public SaveData Data => _data;

        /// <summary>内置关卡的记录键，即关卡 id。</summary>
        public static string BuiltInKey(string levelId)
        {
            return levelId ?? string.Empty;
        }

        /// <summary>自建关卡的记录键。</summary>
        public static string CustomKey(string levelId)
        {
            return CustomLevelKeyPrefix + (levelId ?? string.Empty);
        }

        public bool IsCompleted(string levelKey)
        {
            return FindRecord(levelKey) != null;
        }

        /// <summary>
        /// 查询最佳步数，未通关返回 false。
        /// </summary>
        public bool TryGetBestSteps(string levelKey, out int bestSteps)
        {
            LevelRecord record = FindRecord(levelKey);
            bestSteps = record != null ? record.BestSteps : 0;
            return record != null;
        }

        /// <summary>
        /// 记录一次通关。首次通关或步数更少时更新并返回 true。
        /// </summary>
        public bool RecordCompletion(string levelKey, int steps)
        {
            if (string.IsNullOrEmpty(levelKey))
            {
                throw new ArgumentException("记录键不能为空", nameof(levelKey));
            }

            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), steps, "步数不能为负数");
            }

            LevelRecord record = FindRecord(levelKey);
            if (record == null)
            {
                _data.RecordList.Add(new LevelRecord(levelKey, steps));
                return true;
            }

            if (steps < record.BestSteps)
            {
                record.SetBestSteps(steps);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 删除一条通关记录（例如自建关卡被删除时）。没有该记录时返回 false。
        /// </summary>
        public bool RemoveRecord(string levelKey)
        {
            LevelRecord record = FindRecord(levelKey);
            return record != null && _data.RecordList.Remove(record);
        }

        /// <summary>
        /// 线性解锁：第一关始终解锁，其余关卡在前一关通关后解锁。
        /// </summary>
        public bool IsUnlocked(IReadOnlyList<string> orderedLevelIds, int index)
        {
            if (orderedLevelIds == null)
            {
                throw new ArgumentNullException(nameof(orderedLevelIds));
            }

            if (index < 0 || index >= orderedLevelIds.Count)
            {
                return false;
            }

            return index == 0 || IsCompleted(BuiltInKey(orderedLevelIds[index - 1]));
        }

        private LevelRecord FindRecord(string levelKey)
        {
            List<LevelRecord> records = _data.RecordList;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null && records[i].LevelKey == levelKey)
                {
                    return records[i];
                }
            }

            return null;
        }
    }
}
