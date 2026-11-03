using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 内置关卡的有序列表，决定选关顺序和线性解锁顺序。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Worldyssey/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [Tooltip("按游玩顺序排列，第一关初始解锁")]
        [SerializeField] private LevelAsset[] _levels = new LevelAsset[0];

        public IReadOnlyList<LevelAsset> Levels => _levels;
        public int Count => _levels.Length;

        /// <summary>
        /// 按关卡 id 查找下标，找不到返回 -1。
        /// </summary>
        public int IndexOf(string levelId)
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                if (_levels[i] != null && _levels[i].Level.Id == levelId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnValidate()
        {
            // id 是存档里记录解锁和最佳步数的键，重复会让两关共用一份记录
            var seenIds = new HashSet<string>();
            for (int i = 0; i < _levels.Length; i++)
            {
                LevelAsset level = _levels[i];
                if (level == null)
                {
                    Debug.LogError($"[{nameof(LevelCatalog)}] {name} 第 {i} 项为空", this);
                    continue;
                }

                if (!seenIds.Add(level.Level.Id))
                {
                    Debug.LogError($"[{nameof(LevelCatalog)}] {name} 中关卡 id '{level.Level.Id}' 重复（{level.name}）", this);
                }
            }
        }
    }
}
