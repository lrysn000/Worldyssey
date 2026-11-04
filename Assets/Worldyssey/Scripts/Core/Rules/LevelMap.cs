using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 由 <see cref="LevelData"/> 解析出的只读地形查询结构，构造后不再变化。
    /// </summary>
    /// <remarks>
    /// 单独做这一层，是因为字符行适合存储和阅读，却不适合每一步都去解析；
    /// 规则判定和以后的求解器都需要按坐标 O(1) 查询。
    /// 这里只拒绝会让运行时出错的结构问题（遇到第一个就抛出），完整的合法性检查由关卡校验器负责。
    /// </remarks>
    public sealed class LevelMap
    {
        public const char SharedWallChar = '#';
        public const char PedestalChar = 'P';
        public const char GoalChar = '.';
        public const char EmptyChar = ' ';
        public const char UniqueWallChar = 'x';

        private readonly bool[] _sharedWalls;
        private readonly bool[] _pedestals;
        private readonly bool[] _goals;
        private readonly bool[][] _uniqueWalls;
        private readonly WorldTheme[] _worldThemes;
        private readonly int[] _portalSideWorlds;
        private readonly GridPos[] _goalPositions;
        private readonly GridPos[] _mappedBoxStarts;
        private readonly PortalBoxState[] _portalBoxStarts;

        /// <summary>
        /// 解析关卡数据。结构不合法时抛出 <see cref="ArgumentException"/>。
        /// </summary>
        public LevelMap(LevelData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Width <= 0 || data.Height <= 0)
            {
                throw Error(data, $"尺寸非法：{data.Width}x{data.Height}");
            }

            Id = data.Id;
            Width = data.Width;
            Height = data.Height;
            StartWorld = data.StartWorld;
            PlayerStart = data.Player.Position;
            PlayerStartFacing = data.Player.Facing;

            int cellCount = Width * Height;
            _sharedWalls = new bool[cellCount];
            _pedestals = new bool[cellCount];
            _goals = new bool[cellCount];

            var goalPositions = new List<GridPos>();
            ParseSharedRows(data, goalPositions);
            _goalPositions = goalPositions.ToArray();

            int worldCount = data.Worlds.Count;
            if (!LevelLimits.IsValidWorldCount(worldCount))
            {
                throw Error(data, $"世界数量 {worldCount} 不在 {LevelLimits.MinWorldCount} 到 {LevelLimits.MaxWorldCount} 之间");
            }

            if (StartWorld < 0 || StartWorld >= worldCount)
            {
                throw Error(data, $"起始世界 {StartWorld} 不存在");
            }

            _worldThemes = new WorldTheme[worldCount];
            _uniqueWalls = new bool[worldCount][];
            for (int world = 0; world < worldCount; world++)
            {
                WorldData worldData = data.Worlds[world] ?? throw Error(data, $"世界 {world} 为空");
                // 主题写错时同样按编号回退，具体错误由 LevelValidator 报告
                WorldThemes.TryParse(worldData.ThemeText, out WorldTheme theme);
                _worldThemes[world] = WorldThemes.Resolve(theme, world);
                _uniqueWalls[world] = ParseWallRows(data, world, worldData.WallRows);
            }

            _mappedBoxStarts = new GridPos[data.MappedBoxes.Count];
            for (int i = 0; i < _mappedBoxStarts.Length; i++)
            {
                _mappedBoxStarts[i] = data.MappedBoxes[i];
            }

            _portalSideWorlds = ParsePortalPairs(data, worldCount, out Dictionary<int, int> pairIndexById);
            _portalBoxStarts = ParsePortalBoxes(data, pairIndexById);
        }

        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public int StartWorld { get; }
        public GridPos PlayerStart { get; }
        public Direction PlayerStartFacing { get; }
        public int WorldCount => _worldThemes.Length;

        /// <summary>配对数量。传送箱子总数（含未出现的）为它的两倍。</summary>
        public int PortalPairCount => _portalSideWorlds.Length / 2;

        /// <summary>所有终点坐标。</summary>
        public IReadOnlyList<GridPos> GoalPositions => _goalPositions;

        /// <summary>目标箱子的初始坐标，下标即箱子编号。</summary>
        public IReadOnlyList<GridPos> MappedBoxStarts => _mappedBoxStarts;

        /// <summary>传送箱子的初始状态，下标规则见 <see cref="PortalBoxState"/>。</summary>
        public IReadOnlyList<PortalBoxState> PortalBoxStarts => _portalBoxStarts;

        /// <summary>世界的显示名称，跟随主题。</summary>
        public string GetWorldName(int world)
        {
            return WorldThemes.DisplayName(_worldThemes[world]);
        }

        /// <summary>世界的主题，未指定时已按世界编号回退，不会是 <see cref="WorldTheme.Unset"/>。</summary>
        public WorldTheme GetWorldTheme(int world)
        {
            return _worldThemes[world];
        }

        /// <summary>
        /// 传送箱子所在的世界。下标规则见 <see cref="PortalBoxState"/>。
        /// </summary>
        public int GetPortalWorld(int portalIndex)
        {
            return _portalSideWorlds[portalIndex];
        }

        /// <summary>
        /// 同一配对另一侧的下标（A、B 两侧下标只差最低位）。
        /// </summary>
        public static int GetPartnerIndex(int portalIndex)
        {
            return portalIndex ^ 1;
        }

        public bool IsInBounds(GridPos pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        /// <summary>
        /// 该格是否为共享墙。越界返回 false，越界由调用方单独判断。
        /// </summary>
        public bool IsSharedWall(GridPos pos)
        {
            return IsInBounds(pos) && _sharedWalls[ToIndex(pos)];
        }

        /// <summary>
        /// 该格在指定世界是否为独有墙。越界返回 false。
        /// </summary>
        public bool IsUniqueWall(int world, GridPos pos)
        {
            return IsInBounds(pos) && _uniqueWalls[world][ToIndex(pos)];
        }

        public bool IsPedestal(GridPos pos)
        {
            return IsInBounds(pos) && _pedestals[ToIndex(pos)];
        }

        public bool IsGoal(GridPos pos)
        {
            return IsInBounds(pos) && _goals[ToIndex(pos)];
        }

        private int ToIndex(GridPos pos)
        {
            return pos.Y * Width + pos.X;
        }

        /// <summary>
        /// 字符行从上往下存，第 0 行是最上面一行，对应 y = Height - 1。
        /// </summary>
        private int RowToY(int rowIndex)
        {
            return Height - 1 - rowIndex;
        }

        private void ParseSharedRows(LevelData data, List<GridPos> goalPositions)
        {
            IReadOnlyList<string> rows = data.SharedRows;
            if (rows.Count != Height)
            {
                throw Error(data, $"共享层行数 {rows.Count} 与高度 {Height} 不一致");
            }

            for (int rowIndex = 0; rowIndex < Height; rowIndex++)
            {
                string row = rows[rowIndex] ?? string.Empty;
                if (row.Length != Width)
                {
                    throw Error(data, $"共享层第 {rowIndex} 行长度 {row.Length} 与宽度 {Width} 不一致");
                }

                int y = RowToY(rowIndex);
                for (int x = 0; x < Width; x++)
                {
                    var pos = new GridPos(x, y);
                    int index = ToIndex(pos);
                    char cell = row[x];
                    switch (cell)
                    {
                        case SharedWallChar:
                            _sharedWalls[index] = true;
                            break;
                        case PedestalChar:
                            _pedestals[index] = true;
                            break;
                        case GoalChar:
                            _goals[index] = true;
                            goalPositions.Add(pos);
                            break;
                        case EmptyChar:
                            break;
                        default:
                            throw Error(data, $"共享层第 {rowIndex} 行第 {x} 列有未知字符 '{cell}'");
                    }
                }
            }
        }

        private bool[] ParseWallRows(LevelData data, int world, IReadOnlyList<string> rows)
        {
            var walls = new bool[Width * Height];

            // 没有独有墙的世界可以省略整张独有层
            if (rows.Count == 0)
            {
                return walls;
            }

            if (rows.Count != Height)
            {
                throw Error(data, $"世界 {world} 独有层行数 {rows.Count} 与高度 {Height} 不一致");
            }

            for (int rowIndex = 0; rowIndex < Height; rowIndex++)
            {
                string row = rows[rowIndex] ?? string.Empty;
                if (row.Length != Width)
                {
                    throw Error(data, $"世界 {world} 独有层第 {rowIndex} 行长度 {row.Length} 与宽度 {Width} 不一致");
                }

                int y = RowToY(rowIndex);
                for (int x = 0; x < Width; x++)
                {
                    char cell = row[x];
                    if (cell == UniqueWallChar)
                    {
                        walls[ToIndex(new GridPos(x, y))] = true;
                    }
                    else if (cell != EmptyChar)
                    {
                        throw Error(data, $"世界 {world} 独有层第 {rowIndex} 行第 {x} 列有未知字符 '{cell}'");
                    }
                }
            }

            return walls;
        }

        private static int[] ParsePortalPairs(LevelData data, int worldCount, out Dictionary<int, int> pairIndexById)
        {
            IReadOnlyList<PortalPairData> pairs = data.PortalPairs;
            var sideWorlds = new int[pairs.Count * 2];
            pairIndexById = new Dictionary<int, int>(pairs.Count);

            for (int pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
            {
                PortalPairData pair = pairs[pairIndex] ?? throw Error(data, $"配对 {pairIndex} 为空");
                if (pairIndexById.ContainsKey(pair.Id))
                {
                    throw Error(data, $"配对编号 {pair.Id} 重复");
                }

                if (pair.WorldA < 0 || pair.WorldA >= worldCount || pair.WorldB < 0 || pair.WorldB >= worldCount)
                {
                    throw Error(data, $"配对 {pair.Id} 引用了不存在的世界");
                }

                if (pair.WorldA == pair.WorldB)
                {
                    throw Error(data, $"配对 {pair.Id} 两侧是同一个世界");
                }

                pairIndexById.Add(pair.Id, pairIndex);
                sideWorlds[pairIndex * 2] = pair.WorldA;
                sideWorlds[pairIndex * 2 + 1] = pair.WorldB;
            }

            return sideWorlds;
        }

        private PortalBoxState[] ParsePortalBoxes(LevelData data, Dictionary<int, int> pairIndexById)
        {
            var boxes = new PortalBoxState[_portalSideWorlds.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i] = new PortalBoxState(i / 2, _portalSideWorlds[i], false, default);
            }

            IReadOnlyList<PortalBoxData> placed = data.PortalBoxes;
            for (int i = 0; i < placed.Count; i++)
            {
                PortalBoxData box = placed[i] ?? throw Error(data, $"传送箱子 {i} 为空");
                if (!pairIndexById.TryGetValue(box.Pair, out int pairIndex))
                {
                    throw Error(data, $"传送箱子 {i} 引用了不存在的配对 {box.Pair}");
                }

                // 所在世界决定它是 A 侧还是 B 侧
                int sideIndex;
                if (box.World == _portalSideWorlds[pairIndex * 2])
                {
                    sideIndex = pairIndex * 2;
                }
                else if (box.World == _portalSideWorlds[pairIndex * 2 + 1])
                {
                    sideIndex = pairIndex * 2 + 1;
                }
                else
                {
                    throw Error(data, $"传送箱子 {i} 所在世界 {box.World} 不属于配对 {box.Pair}");
                }

                if (boxes[sideIndex].IsPresent)
                {
                    throw Error(data, $"配对 {box.Pair} 在世界 {box.World} 的一侧出现了不止一次");
                }

                if (!IsInBounds(box.Position))
                {
                    throw Error(data, $"传送箱子 {i} 坐标 {box.Position} 越界");
                }

                boxes[sideIndex] = boxes[sideIndex].PlacedAt(box.Position);
            }

            return boxes;
        }

        private static ArgumentException Error(LevelData data, string message)
        {
            return new ArgumentException($"关卡 '{data.Id}' {message}", nameof(data));
        }
    }
}
