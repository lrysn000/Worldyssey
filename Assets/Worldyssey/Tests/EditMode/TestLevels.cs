using System;
using System.Collections.Generic;
using Worldyssey.Core;

namespace Worldyssey.Tests
{
    /// <summary>
    /// 用经典推箱子字符画快速构造单世界测试关卡，让测试一眼能看懂地图。
    /// '#' 墙，'@' 玩家，'+' 玩家在终点上，'$' 箱子，'*' 箱子在终点上，'.' 终点，空格为空地。
    /// </summary>
    public static class TestLevels
    {
        public static LevelData FromXsb(params string[] rows)
        {
            return FromXsb(Direction.Down, rows);
        }

        public static LevelData FromXsb(Direction facing, params string[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("至少需要一行", nameof(rows));
            }

            int height = rows.Length;
            int width = rows[0].Length;
            var sharedRows = new string[height];
            var emptyWallRows = new string[height];
            var boxes = new List<GridPos>();
            GridPos? player = null;

            for (int rowIndex = 0; rowIndex < height; rowIndex++)
            {
                string row = rows[rowIndex];
                if (row.Length != width)
                {
                    throw new ArgumentException($"第 {rowIndex} 行长度 {row.Length} 与第 0 行 {width} 不一致", nameof(rows));
                }

                int y = height - 1 - rowIndex;
                var shared = new char[width];
                for (int x = 0; x < width; x++)
                {
                    char cell = row[x];
                    bool isGoal = cell == '.' || cell == '+' || cell == '*';
                    shared[x] = cell == '#' ? '#' : isGoal ? '.' : ' ';

                    if (cell == '@' || cell == '+')
                    {
                        if (player.HasValue)
                        {
                            throw new ArgumentException("字符画里有多个玩家", nameof(rows));
                        }

                        player = new GridPos(x, y);
                    }
                    else if (cell == '$' || cell == '*')
                    {
                        boxes.Add(new GridPos(x, y));
                    }
                    else if (cell != '#' && cell != '.' && cell != ' ')
                    {
                        throw new ArgumentException($"未知字符 '{cell}'", nameof(rows));
                    }
                }

                sharedRows[rowIndex] = new string(shared);
                emptyWallRows[rowIndex] = new string(' ', width);
            }

            if (!player.HasValue)
            {
                throw new ArgumentException("字符画里没有玩家", nameof(rows));
            }

            return new LevelData(
                "test",
                "测试关卡",
                width,
                height,
                0,
                sharedRows,
                new[] { new WorldData("世界一", emptyWallRows) },
                new PlayerSpawnData(player.Value.X, player.Value.Y, facing),
                boxes.ToArray(),
                Array.Empty<PortalPairData>(),
                Array.Empty<PortalBoxData>());
        }

        /// <summary>
        /// 构造多世界关卡。
        /// 共享层字符：'#' 共享墙，'P' 底座，'.' 终点，'$' 目标箱子，'*' 箱子在终点上，'@' 玩家，'+' 玩家在终点上，空格为空地。
        /// 每个世界一张独有层（可为 null 表示全空）：'x' 独有墙，'a'~'d' 表示配对 0~3 在这个世界的一侧，空格为空。
        /// <paramref name="pairs"/> 每项为 {世界A, 世界B}，配对编号即下标。
        /// </summary>
        public static LevelData MultiWorld(string[] shared, string[][] worldOverlays, int[][] pairs, Direction facing = Direction.Right, int startWorld = 0)
        {
            int height = shared.Length;
            int width = shared[0].Length;
            var sharedRows = new string[height];
            var boxes = new List<GridPos>();
            GridPos? player = null;

            for (int rowIndex = 0; rowIndex < height; rowIndex++)
            {
                string row = shared[rowIndex];
                if (row.Length != width)
                {
                    throw new ArgumentException($"共享层第 {rowIndex} 行长度不一致", nameof(shared));
                }

                int y = height - 1 - rowIndex;
                var cells = new char[width];
                for (int x = 0; x < width; x++)
                {
                    char cell = row[x];
                    bool isGoal = cell == '.' || cell == '+' || cell == '*';
                    cells[x] = cell == '#' ? '#' : cell == 'P' ? 'P' : isGoal ? '.' : ' ';
                    if (cell == '@' || cell == '+')
                    {
                        player = new GridPos(x, y);
                    }
                    else if (cell == '$' || cell == '*')
                    {
                        boxes.Add(new GridPos(x, y));
                    }
                    else if (cell != '#' && cell != 'P' && cell != '.' && cell != ' ')
                    {
                        throw new ArgumentException($"共享层未知字符 '{cell}'", nameof(shared));
                    }
                }

                sharedRows[rowIndex] = new string(cells);
            }

            if (!player.HasValue)
            {
                throw new ArgumentException("共享层里没有玩家", nameof(shared));
            }

            var worlds = new WorldData[worldOverlays.Length];
            var portalBoxes = new List<PortalBoxData>();
            for (int world = 0; world < worldOverlays.Length; world++)
            {
                string[] overlay = worldOverlays[world];
                var wallRows = new string[height];
                for (int rowIndex = 0; rowIndex < height; rowIndex++)
                {
                    string row = overlay != null ? overlay[rowIndex] : new string(' ', width);
                    var walls = new char[width];
                    for (int x = 0; x < width; x++)
                    {
                        char cell = row[x];
                        walls[x] = cell == 'x' ? 'x' : ' ';
                        if (cell >= 'a' && cell <= 'd')
                        {
                            portalBoxes.Add(new PortalBoxData(cell - 'a', world, x, height - 1 - rowIndex));
                        }
                        else if (cell != 'x' && cell != ' ')
                        {
                            throw new ArgumentException($"世界 {world} 独有层未知字符 '{cell}'", nameof(worldOverlays));
                        }
                    }

                    wallRows[rowIndex] = new string(walls);
                }

                worlds[world] = new WorldData($"World {world}", wallRows);
            }

            var pairData = new PortalPairData[pairs.Length];
            for (int i = 0; i < pairs.Length; i++)
            {
                pairData[i] = new PortalPairData(i, pairs[i][0], pairs[i][1]);
            }

            return new LevelData(
                "multi",
                "多世界测试",
                width,
                height,
                startWorld,
                sharedRows,
                worlds,
                new PlayerSpawnData(player.Value.X, player.Value.Y, facing),
                boxes.ToArray(),
                pairData,
                portalBoxes.ToArray());
        }

        public static LevelRules RulesMultiWorld(string[] shared, string[][] worldOverlays, int[][] pairs, Direction facing = Direction.Right, int startWorld = 0)
        {
            return new LevelRules(new LevelMap(MultiWorld(shared, worldOverlays, pairs, facing, startWorld)));
        }

        /// <summary>
        /// 连续执行若干步移动，每一步都必须成功或只转向，返回最终状态。
        /// </summary>
        public static LevelState Walk(LevelRules rules, LevelState state, params Direction[] directions)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                state = rules.TryMove(state, directions[i]).State;
            }

            return state;
        }

        public static LevelRules RulesFromXsb(params string[] rows)
        {
            return new LevelRules(new LevelMap(FromXsb(rows)));
        }

        public static LevelRules RulesFromXsb(Direction facing, params string[] rows)
        {
            return new LevelRules(new LevelMap(FromXsb(facing, rows)));
        }
    }
}
