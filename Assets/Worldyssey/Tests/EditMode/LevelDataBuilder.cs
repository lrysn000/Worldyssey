using System;
using System.Collections.Generic;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// 校验器测试用：先给出一个合法的双世界关卡，每个测试只改动要验证的那一处。
    /// 默认关卡（7x4）：玩家 (1,1)，目标箱子 (3,1)，终点 (5,1)，底座 (2,2)，配对 0 连接世界 0 和 1，A 侧在底座上。
    /// </summary>
    public sealed class LevelDataBuilder
    {
        public int FormatVersion = LevelData.CurrentFormatVersion;
        public string Id = "builder";
        public string DisplayName = "Builder";
        public int Width = 7;
        public int Height = 4;
        public int StartWorld;
        public string[] SharedRows = { "#######", "# P   #", "#    .#", "#######" };
        public List<WorldData> Worlds = new List<WorldData>
        {
            new WorldData("World 0", Array.Empty<string>()),
            new WorldData("World 1", Array.Empty<string>()),
        };
        public PlayerSpawnData Player = new PlayerSpawnData(1, 1, Direction.Right);
        public List<GridPos> MappedBoxes = new List<GridPos> { new GridPos(3, 1) };
        public List<PortalPairData> Pairs = new List<PortalPairData> { new PortalPairData(0, 0, 1) };
        public List<PortalBoxData> PortalBoxes = new List<PortalBoxData> { new PortalBoxData(0, 0, 2, 2) };

        /// <summary>
        /// 给某个世界设置独有墙行。
        /// </summary>
        public LevelDataBuilder WithWallRows(int world, params string[] rows)
        {
            Worlds[world] = new WorldData(Worlds[world].Name, rows);
            return this;
        }

        public LevelData Build()
        {
            var data = new LevelData(
                Id, DisplayName, Width, Height, StartWorld,
                SharedRows, Worlds.ToArray(), Player, MappedBoxes.ToArray(), Pairs.ToArray(), PortalBoxes.ToArray());
            if (FormatVersion == LevelData.CurrentFormatVersion)
            {
                return data;
            }

            // 构造函数不暴露版本号，借助 JSON 改写，模拟读到其他版本的文件
            string json = LevelJsonSerializer.ToJson(data).Replace(
                $"\"formatVersion\": {LevelData.CurrentFormatVersion}",
                $"\"formatVersion\": {FormatVersion}");
            return UnityEngine.JsonUtility.FromJson<LevelData>(json);
        }
    }
}
