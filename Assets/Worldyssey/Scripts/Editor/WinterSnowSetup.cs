using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.View;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 冬季积雪：给不该积雪的预制体（底座、终点、传送箱子、独有墙、目标箱子、玩家）挂 <see cref="SnowExclusion"/>，
    /// 并把冬季环境的积雪强度设为 1（已经调过的不覆盖）。可以重复运行；换了独有墙模型后再运行一次即可。
    /// </summary>
    public static class WinterSnowSetup
    {
        private const string MenuPath = "Worldyssey/Setup/Winter Snow";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var config = GameUiBuilder.FindAsset<LevelVisualConfig>(null);
            if (config == null)
            {
                Debug.LogError($"[{nameof(WinterSnowSetup)}] 找不到 LevelVisualConfig，未做任何修改");
                return;
            }

            var prefabs = new HashSet<GameObject>
            {
                config.PedestalPrefab,
                config.GoalPrefab,
                config.UniqueWallPrefab,
                config.PortalBoxPrefab != null ? config.PortalBoxPrefab.gameObject : null,
                config.MappedBoxPrefab != null ? config.MappedBoxPrefab.gameObject : null,
                config.PlayerPrefab != null ? config.PlayerPrefab.gameObject : null,
            };

            for (int season = 0; season < WorldThemes.Count; season++)
            {
                SeasonVisualSet set = config.GetSeasonSet(season);
                if (set == null)
                {
                    continue;
                }

                foreach (GameObject wall in set.UniqueWallPrefabs)
                {
                    prefabs.Add(wall);
                }
            }

            int added = 0;
            foreach (GameObject prefab in prefabs)
            {
                added += AddExclusion(prefab) ? 1 : 0;
            }

            EnableWinterSnow(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(WinterSnowSetup)}] 新增 {added} 个不积雪的预制体；雪色和覆盖角度在冬季的 SeasonEnvironment 里调整", config);
        }

        private static bool AddExclusion(GameObject prefab)
        {
            if (prefab == null || prefab.GetComponent<SnowExclusion>() != null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            contents.AddComponent<SnowExclusion>();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return true;
        }

        private static void EnableWinterSnow(LevelVisualConfig config)
        {
            SeasonVisualSet winter = config.GetSeasonSet((int)WorldTheme.Winter);
            SeasonEnvironment environment = winter != null ? winter.Environment : null;
            if (environment == null)
            {
                Debug.LogWarning($"[{nameof(WinterSnowSetup)}] 冬季还没有配置 SeasonEnvironment，积雪不会出现");
                return;
            }

            var serialized = new SerializedObject(environment);
            SerializedProperty amount = serialized.FindProperty("_snowAmount");
            if (amount.floatValue <= 0f)
            {
                amount.floatValue = 1f;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(environment);
            }
        }
    }
}
