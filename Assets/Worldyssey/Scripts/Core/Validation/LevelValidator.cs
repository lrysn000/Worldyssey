using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡合法性检查（需求文档 6.3 节）。一次返回全部问题，不抛异常，供关卡编辑器、导入工具和加载流程使用。
    /// </summary>
    /// <remarks>
    /// 不依赖 <see cref="LevelMap"/>：LevelMap 遇到第一个结构问题就抛出，而这里要尽量继续检查，把问题列全。
    /// 某一层字符行结构不对时，只跳过依赖这一层的检查。
    /// </remarks>
    public static class LevelValidator
    {
        public static LevelValidationReport Validate(LevelData data)
        {
            var issues = new List<LevelIssue>();
            if (data == null)
            {
                issues.Add(Error("关卡数据为空"));
                return new LevelValidationReport(issues);
            }

            var context = new Context(data, issues);
            context.Run();
            return new LevelValidationReport(issues);
        }

        private static LevelIssue Error(string message, GridPos? position = null, int world = LevelIssue.NoWorld)
        {
            return new LevelIssue(LevelIssueSeverity.Error, message, position, world);
        }

        private static LevelIssue Warning(string message, GridPos? position = null, int world = LevelIssue.NoWorld)
        {
            return new LevelIssue(LevelIssueSeverity.Warning, message, position, world);
        }

        /// <summary>
        /// 一次校验的中间数据。拆成实例是为了在各项检查之间共享解析结果，而不是到处传参。
        /// </summary>
        private sealed class Context
        {
            private readonly LevelData _data;
            private readonly List<LevelIssue> _issues;
            private readonly int _width;
            private readonly int _height;
            private readonly int _worldCount;

            private bool[] _sharedWalls;
            private bool[] _pedestals;
            private bool[][] _uniqueWalls;
            private int _goalCount;
            private int[] _pairWorldA;
            private int[] _pairWorldB;
            private Dictionary<int, int> _pairIndexById;
            private readonly List<PlacedPortal> _portals = new List<PlacedPortal>();

            public Context(LevelData data, List<LevelIssue> issues)
            {
                _data = data;
                _issues = issues;
                _width = data.Width;
                _height = data.Height;
                _worldCount = data.Worlds.Count;
            }

            public void Run()
            {
                CheckHeader();
                if (_width <= 0 || _height <= 0)
                {
                    _issues.Add(Error($"尺寸非法：{_width}x{_height}"));
                    return;
                }

                CheckWorlds();
                ParseSharedRows();
                ParseWorldWalls();
                CheckGoalsAndBoxCount();
                CheckPedestals();
                ParsePairs();
                ParsePortalBoxes();
                CheckPlacement();
            }

            private void CheckHeader()
            {
                if (string.IsNullOrWhiteSpace(_data.Id))
                {
                    _issues.Add(Error("关卡 id 为空"));
                }

                if (string.IsNullOrWhiteSpace(_data.DisplayName))
                {
                    _issues.Add(Warning("显示名称为空"));
                }

                if (_data.FormatVersion > LevelData.CurrentFormatVersion)
                {
                    _issues.Add(Error($"关卡格式版本 {_data.FormatVersion} 高于当前支持的 {LevelData.CurrentFormatVersion}"));
                }
                else if (_data.FormatVersion < 1)
                {
                    _issues.Add(Error($"关卡格式版本 {_data.FormatVersion} 非法"));
                }
            }

            private void CheckWorlds()
            {
                if (!LevelLimits.IsValidWorldCount(_worldCount))
                {
                    _issues.Add(Error($"世界数量 {_worldCount} 不在 {LevelLimits.MinWorldCount} 到 {LevelLimits.MaxWorldCount} 之间"));
                }

                if (_data.StartWorld < 0 || _data.StartWorld >= _worldCount)
                {
                    _issues.Add(Error($"起始世界 {_data.StartWorld} 不存在"));
                }

                for (int world = 0; world < _worldCount; world++)
                {
                    WorldData worldData = _data.Worlds[world];
                    if (worldData == null)
                    {
                        _issues.Add(Error($"世界 {world} 为空", null, world));
                    }
                    else if (string.IsNullOrWhiteSpace(worldData.Name))
                    {
                        _issues.Add(Warning($"世界 {world} 没有名称", null, world));
                    }
                }
            }

            private void ParseSharedRows()
            {
                IReadOnlyList<string> rows = _data.SharedRows;
                if (rows.Count != _height)
                {
                    _issues.Add(Error($"共享层行数 {rows.Count} 与高度 {_height} 不一致"));
                    return;
                }

                var walls = new bool[_width * _height];
                var pedestals = new bool[_width * _height];
                bool isValid = true;
                for (int rowIndex = 0; rowIndex < _height; rowIndex++)
                {
                    string row = rows[rowIndex] ?? string.Empty;
                    if (row.Length != _width)
                    {
                        _issues.Add(Error($"共享层第 {rowIndex} 行长度 {row.Length} 与宽度 {_width} 不一致"));
                        isValid = false;
                        continue;
                    }

                    for (int x = 0; x < _width; x++)
                    {
                        var pos = new GridPos(x, RowToY(rowIndex));
                        switch (row[x])
                        {
                            case LevelMap.SharedWallChar:
                                walls[ToIndex(pos)] = true;
                                break;
                            case LevelMap.PedestalChar:
                                pedestals[ToIndex(pos)] = true;
                                break;
                            case LevelMap.GoalChar:
                                _goalCount++;
                                break;
                            case LevelMap.EmptyChar:
                                break;
                            default:
                                _issues.Add(Error($"共享层有未知字符 '{row[x]}'", pos));
                                isValid = false;
                                break;
                        }
                    }
                }

                if (isValid)
                {
                    _sharedWalls = walls;
                    _pedestals = pedestals;
                }
            }

            private void ParseWorldWalls()
            {
                _uniqueWalls = new bool[_worldCount][];
                for (int world = 0; world < _worldCount; world++)
                {
                    WorldData worldData = _data.Worlds[world];
                    if (worldData == null)
                    {
                        continue;
                    }

                    IReadOnlyList<string> rows = worldData.WallRows;
                    var walls = new bool[_width * _height];

                    // 没有独有墙的世界可以省略整张独有层
                    if (rows.Count == 0)
                    {
                        _uniqueWalls[world] = walls;
                        continue;
                    }

                    if (rows.Count != _height)
                    {
                        _issues.Add(Error($"世界 {world} 独有层行数 {rows.Count} 与高度 {_height} 不一致", null, world));
                        continue;
                    }

                    bool isValid = true;
                    for (int rowIndex = 0; rowIndex < _height; rowIndex++)
                    {
                        string row = rows[rowIndex] ?? string.Empty;
                        if (row.Length != _width)
                        {
                            _issues.Add(Error($"世界 {world} 独有层第 {rowIndex} 行长度 {row.Length} 与宽度 {_width} 不一致", null, world));
                            isValid = false;
                            continue;
                        }

                        for (int x = 0; x < _width; x++)
                        {
                            var pos = new GridPos(x, RowToY(rowIndex));
                            if (row[x] == LevelMap.UniqueWallChar)
                            {
                                walls[ToIndex(pos)] = true;
                            }
                            else if (row[x] != LevelMap.EmptyChar)
                            {
                                _issues.Add(Error($"世界 {world} 独有层有未知字符 '{row[x]}'", pos, world));
                                isValid = false;
                            }
                        }
                    }

                    if (isValid)
                    {
                        _uniqueWalls[world] = walls;
                    }
                }
            }

            private void CheckGoalsAndBoxCount()
            {
                if (_sharedWalls == null)
                {
                    return;
                }

                int boxCount = _data.MappedBoxes.Count;
                if (_goalCount == 0)
                {
                    _issues.Add(Error("关卡没有终点"));
                }

                if (boxCount != _goalCount)
                {
                    _issues.Add(Error($"目标箱子数量 {boxCount} 与终点数量 {_goalCount} 不一致"));
                }
            }

            private void CheckPedestals()
            {
                if (_pedestals == null)
                {
                    return;
                }

                for (int world = 0; world < _worldCount; world++)
                {
                    bool[] walls = _uniqueWalls[world];
                    if (walls == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < walls.Length; i++)
                    {
                        if (walls[i] && _pedestals[i])
                        {
                            _issues.Add(Error($"世界 {world} 的独有墙压在底座上", FromIndex(i), world));
                        }
                    }
                }
            }

            private void ParsePairs()
            {
                IReadOnlyList<PortalPairData> pairs = _data.PortalPairs;
                _pairWorldA = new int[pairs.Count];
                _pairWorldB = new int[pairs.Count];
                _pairIndexById = new Dictionary<int, int>(pairs.Count);

                for (int i = 0; i < pairs.Count; i++)
                {
                    PortalPairData pair = pairs[i];
                    if (pair == null)
                    {
                        _issues.Add(Error($"配对 {i} 为空"));
                        continue;
                    }

                    if (_pairIndexById.ContainsKey(pair.Id))
                    {
                        _issues.Add(Error($"配对编号 {pair.Id} 重复"));
                        continue;
                    }

                    bool isWorldAValid = IsWorldIndex(pair.WorldA);
                    bool isWorldBValid = IsWorldIndex(pair.WorldB);
                    if (!isWorldAValid || !isWorldBValid)
                    {
                        _issues.Add(Error($"配对 {pair.Id} 引用了不存在的世界"));
                        continue;
                    }

                    if (pair.WorldA == pair.WorldB)
                    {
                        _issues.Add(Error($"配对 {pair.Id} 两侧是同一个世界 {pair.WorldA}"));
                        continue;
                    }

                    _pairIndexById.Add(pair.Id, i);
                    _pairWorldA[i] = pair.WorldA;
                    _pairWorldB[i] = pair.WorldB;
                }
            }

            private void ParsePortalBoxes()
            {
                var placedSides = new HashSet<int>();
                IReadOnlyList<PortalBoxData> boxes = _data.PortalBoxes;
                for (int i = 0; i < boxes.Count; i++)
                {
                    PortalBoxData box = boxes[i];
                    if (box == null)
                    {
                        _issues.Add(Error($"传送箱子 {i} 为空"));
                        continue;
                    }

                    if (!_pairIndexById.TryGetValue(box.Pair, out int pairIndex))
                    {
                        _issues.Add(Error($"传送箱子 {i} 引用了不存在或无效的配对 {box.Pair}", box.Position));
                        continue;
                    }

                    int side;
                    if (box.World == _pairWorldA[pairIndex])
                    {
                        side = pairIndex * 2;
                    }
                    else if (box.World == _pairWorldB[pairIndex])
                    {
                        side = pairIndex * 2 + 1;
                    }
                    else
                    {
                        _issues.Add(Error($"传送箱子 {i} 所在世界 {box.World} 不属于配对 {box.Pair}", box.Position));
                        continue;
                    }

                    if (!placedSides.Add(side))
                    {
                        _issues.Add(Error($"配对 {box.Pair} 在世界 {box.World} 的一侧出现了不止一次", box.Position, box.World));
                        continue;
                    }

                    if (!IsInBounds(box.Position))
                    {
                        _issues.Add(Error($"传送箱子 {i} 坐标 {box.Position} 越界", null, box.World));
                        continue;
                    }

                    _portals.Add(new PlacedPortal(box.World, box.Position));
                }
            }

            private void CheckPlacement()
            {
                int startWorld = _data.StartWorld;
                bool canCheckStartWorld = _sharedWalls != null && IsWorldIndex(startWorld) && _uniqueWalls[startWorld] != null;

                // 起始世界里每格最多一个物体：玩家、目标箱子、本世界的传送箱子
                var startWorldOccupants = new Dictionary<GridPos, string>();

                GridPos player = _data.Player.Position;
                if (!IsInBounds(player))
                {
                    _issues.Add(Error($"玩家坐标 {player} 越界"));
                }
                else if (canCheckStartWorld)
                {
                    CheckNotOnWall(player, startWorld, "玩家");
                    startWorldOccupants[player] = "玩家";
                }

                IReadOnlyList<GridPos> mappedBoxes = _data.MappedBoxes;
                for (int i = 0; i < mappedBoxes.Count; i++)
                {
                    GridPos pos = mappedBoxes[i];
                    string label = $"目标箱子 {i}";
                    if (!IsInBounds(pos))
                    {
                        _issues.Add(Error($"{label} 坐标 {pos} 越界"));
                        continue;
                    }

                    if (!canCheckStartWorld)
                    {
                        continue;
                    }

                    CheckNotOnWall(pos, startWorld, label);
                    AddOccupant(startWorldOccupants, pos, label, startWorld);
                    CheckInitialConflicts(pos, label, startWorld);
                }

                CheckPortalPlacement(startWorld, canCheckStartWorld, startWorldOccupants);
            }

            private void CheckPortalPlacement(int startWorld, bool canCheckStartWorld, Dictionary<GridPos, string> startWorldOccupants)
            {
                var otherWorldOccupants = new HashSet<(int, GridPos)>();
                for (int i = 0; i < _portals.Count; i++)
                {
                    PlacedPortal portal = _portals[i];
                    if (_sharedWalls == null || !IsWorldIndex(portal.World) || _uniqueWalls[portal.World] == null)
                    {
                        continue;
                    }

                    string label = $"世界 {portal.World} 的传送箱子";
                    CheckNotOnWall(portal.Position, portal.World, label);

                    if (portal.World == startWorld)
                    {
                        if (canCheckStartWorld)
                        {
                            AddOccupant(startWorldOccupants, portal.Position, label, startWorld);
                        }
                    }
                    else if (!otherWorldOccupants.Add((portal.World, portal.Position)))
                    {
                        _issues.Add(Error($"{label} 与同世界的另一个传送箱子重叠", portal.Position, portal.World));
                    }
                }
            }

            /// <summary>
            /// 目标箱子在其他世界压着独有墙或传送箱子是允许的（玩法上叫初始冲突），只提醒。
            /// </summary>
            private void CheckInitialConflicts(GridPos pos, string label, int startWorld)
            {
                for (int world = 0; world < _worldCount; world++)
                {
                    if (world == startWorld || _uniqueWalls[world] == null)
                    {
                        continue;
                    }

                    if (_uniqueWalls[world][ToIndex(pos)])
                    {
                        _issues.Add(Warning($"{label} 压着世界 {world} 的独有墙，该世界初始处于冲突", pos, world));
                    }

                    for (int i = 0; i < _portals.Count; i++)
                    {
                        if (_portals[i].World == world && _portals[i].Position == pos)
                        {
                            _issues.Add(Warning($"{label} 压着世界 {world} 的传送箱子，该世界初始处于冲突", pos, world));
                        }
                    }
                }
            }

            private void CheckNotOnWall(GridPos pos, int world, string label)
            {
                if (_sharedWalls[ToIndex(pos)])
                {
                    _issues.Add(Error($"{label} 在共享墙上", pos, world));
                }
                else if (_uniqueWalls[world][ToIndex(pos)])
                {
                    _issues.Add(Error($"{label} 在世界 {world} 的独有墙上", pos, world));
                }
            }

            private void AddOccupant(Dictionary<GridPos, string> occupants, GridPos pos, string label, int world)
            {
                if (occupants.TryGetValue(pos, out string existing))
                {
                    _issues.Add(Error($"{label} 与{existing}重叠", pos, world));
                    return;
                }

                occupants.Add(pos, label);
            }

            private bool IsWorldIndex(int world)
            {
                return world >= 0 && world < _worldCount;
            }

            private bool IsInBounds(GridPos pos)
            {
                return pos.X >= 0 && pos.X < _width && pos.Y >= 0 && pos.Y < _height;
            }

            private int RowToY(int rowIndex)
            {
                return _height - 1 - rowIndex;
            }

            private int ToIndex(GridPos pos)
            {
                return pos.Y * _width + pos.X;
            }

            private GridPos FromIndex(int index)
            {
                return new GridPos(index % _width, index / _width);
            }
        }

        private readonly struct PlacedPortal
        {
            public readonly int World;
            public readonly GridPos Position;

            public PlacedPortal(int world, GridPos position)
            {
                World = world;
                Position = position;
            }
        }
    }
}
