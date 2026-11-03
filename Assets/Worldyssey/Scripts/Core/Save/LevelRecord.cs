using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 一关的通关记录。存在记录即表示通关过。
    /// </summary>
    /// <remarks>字段名即存档 JSON 的键名，因此不加下划线。</remarks>
    [Serializable]
    public class LevelRecord
    {
        [SerializeField] private string levelKey = string.Empty;
        [SerializeField] private int bestSteps;

        public LevelRecord()
        {
        }

        public LevelRecord(string levelKey, int bestSteps)
        {
            this.levelKey = levelKey ?? string.Empty;
            this.bestSteps = bestSteps;
        }

        /// <summary>记录键：内置关卡为关卡 id，自建关卡带 <see cref="ProgressTracker.CustomLevelKeyPrefix"/> 前缀。</summary>
        public string LevelKey => levelKey ?? string.Empty;

        public int BestSteps => bestSteps;

        internal void SetBestSteps(int steps)
        {
            bestSteps = steps;
        }
    }
}
