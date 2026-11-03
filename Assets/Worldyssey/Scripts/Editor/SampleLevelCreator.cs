using System;
using UnityEditor;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 生成 M2 用的单世界示例关卡。M5 有 JSON 导入后，这类关卡改为由 JSON 制作。
    /// </summary>
    public static class SampleLevelCreator
    {
        private const string MenuPath = "Worldyssey/Setup/Create Sample Level";
        private const string TwoWorldMenuPath = "Worldyssey/Setup/Create Sample Level (Two Worlds)";
        private const string LevelFolder = "Assets/Worldyssey/Levels";
        private const string LevelPath = LevelFolder + "/Level_Sandbox01.asset";
        private const string TwoWorldLevelPath = LevelFolder + "/Level_Sandbox02.asset";

        [MenuItem(MenuPath)]
        public static void CreateSampleLevel()
        {
            CreateLevelAsset(LevelPath, BuildSandbox01());
        }

        [MenuItem(TwoWorldMenuPath)]
        public static void CreateTwoWorldSampleLevel()
        {
            CreateLevelAsset(TwoWorldLevelPath, BuildSandbox02());
        }

        private static void CreateLevelAsset(string path, LevelData level)
        {
            if (AssetDatabase.LoadAssetAtPath<LevelAsset>(path) != null)
            {
                Debug.Log($"[{nameof(SampleLevelCreator)}] 已存在，未覆盖：{path}");
                return;
            }

            AssetFolderUtility.EnsureFolder(LevelFolder);

            var asset = ScriptableObject.CreateInstance<LevelAsset>();
            asset.SetLevel(level);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(SampleLevelCreator)}] 已生成示例关卡：{path}", asset);
        }

        /// <summary>
        /// 两个箱子、两个终点。解法：先把右边的箱子从下往上推进上方终点，
        /// 再绕到左边把左边的箱子往下推一格，最后往右推进下方终点。
        /// </summary>
        private static LevelData BuildSandbox01()
        {
            const int width = 8;
            const int height = 5;
            string[] sharedRows =
            {
                "########",
                "#  .   #",
                "#      #",
                "#  .   #",
                "########",
            };

            var emptyWallRows = new string[height];
            for (int i = 0; i < height; i++)
            {
                emptyWallRows[i] = new string(' ', width);
            }

            return new LevelData(
                "sandbox-01",
                "Sandbox 1",
                width,
                height,
                0,
                sharedRows,
                new[] { new WorldData("World 1", emptyWallRows) },
                new PlayerSpawnData(5, 2, Direction.Down),
                new[] { new GridPos(2, 2), new GridPos(3, 2) },
                Array.Empty<PortalPairData>(),
                Array.Empty<PortalBoxData>());
        }

        /// <summary>
        /// 双世界示例：开局时箱子压着世界 2 的独有墙，世界 2 处于冲突、进不去。
        /// 解法（最短 14 步）：在世界 1 把箱子往下推一格解除冲突，走到底座上方面朝底座按 E，
        /// 到世界 2 后穿过世界 1 被独有墙堵住的缺口，把箱子推进终点。
        /// </summary>
        private static LevelData BuildSandbox02()
        {
            string[] sharedRows =
            {
                "#########",
                "#   #   #",
                "#   #   #",
                "#   #   #",
                "#      .#",
                "#P  #   #",
                "#########",
            };
            string[] world1Walls =
            {
                "         ",
                "         ",
                "         ",
                "         ",
                "    x    ",
                "         ",
                "         ",
            };
            string[] world2Walls =
            {
                "         ",
                "         ",
                "         ",
                "  x      ",
                "         ",
                "         ",
                "         ",
            };

            return new LevelData(
                "sandbox-02",
                "Sandbox 2",
                9,
                7,
                0,
                sharedRows,
                new[] { new WorldData("World 1", world1Walls), new WorldData("World 2", world2Walls) },
                new PlayerSpawnData(2, 1, Direction.Left),
                new[] { new GridPos(2, 3) },
                new[] { new PortalPairData(0, 0, 1) },
                new[] { new PortalBoxData(0, 0, 1, 1) });
        }
    }
}
