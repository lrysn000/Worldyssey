using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡合法性检查（需求文档 6.3 节）。一次返回全部问题，不抛异常，供关卡编辑器、导入工具和加载流程使用。
    /// 提示文字为英文，因为游戏内编辑器会直接显示它们，而当前 UI 字体只支持英文。
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
                issues.Add(Error("Level data is missing"));
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
                    _issues.Add(Error($"Invalid size {_width}x{_height}"));
                    return;
                }

                if (!LevelLimits.IsValidSize(_width, _height))
                {
                    // 超出范围不影响其余检查，继续往下查，把问题列全
                    _issues.Add(Error($"Size {_width}x{_height} is outside the allowed range {LevelLimits.MinWidth}x{LevelLimits.MinHeight} to {LevelLimits.MaxWidth}x{LevelLimits.MaxHeight}"));
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
                    _issues.Add(Error("Level id is empty"));
                }

                if (string.IsNullOrWhiteSpace(_data.DisplayName))
                {
                    _issues.Add(Warning("Display name is empty"));
                }

                if (_data.FormatVersion > LevelData.CurrentFormatVersion)
                {
                    _issues.Add(Error($"Format version {_data.FormatVersion} is newer than supported version {LevelData.CurrentFormatVersion}"));
                }
                else if (_data.FormatVersion < 1)
                {
                    _issues.Add(Error($"Invalid format version {_data.FormatVersion}"));
                }
            }

            private void CheckWorlds()
            {
                if (!LevelLimits.IsValidWorldCount(_worldCount))
                {
                    _issues.Add(Error($"World count {_worldCount} must be between {LevelLimits.MinWorldCount} and {LevelLimits.MaxWorldCount}"));
                }

                if (_data.StartWorld < 0 || _data.StartWorld >= _worldCount)
                {
                    _issues.Add(Error($"Start world {WorldLabel(_data.StartWorld)} does not exist"));
                }

                for (int world = 0; world < _worldCount; world++)
                {
                    WorldData worldData = _data.Worlds[world];
                    if (worldData == null)
                    {
                        _issues.Add(Error($"{WorldLabel(world)} is missing", null, world));
                    }
                    else if (string.IsNullOrWhiteSpace(worldData.Name))
                    {
                        _issues.Add(Warning($"{WorldLabel(world)} has no name", null, world));
                    }
                }
            }

            private void ParseSharedRows()
            {
                IReadOnlyList<string> rows = _data.SharedRows;
                if (rows.Count != _height)
                {
                    _issues.Add(Error($"Shared layer row count {rows.Count} does not match height {_height}"));
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
                        _issues.Add(Error($"Shared layer row {rowIndex} length {row.Length} does not match width {_width}"));
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
                                _issues.Add(Error($"Unknown character '{row[x]}' in shared layer", pos));
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
                        _issues.Add(Error($"{WorldLabel(world)} unique layer row count {rows.Count} does not match height {_height}", null, world));
                        continue;
                    }

                    bool isValid = true;
                    for (int rowIndex = 0; rowIndex < _height; rowIndex++)
                    {
                        string row = rows[rowIndex] ?? string.Empty;
                        if (row.Length != _width)
                        {
                            _issues.Add(Error($"{WorldLabel(world)} unique layer row {rowIndex} length {row.Length} does not match width {_width}", null, world));
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
                                _issues.Add(Error($"Unknown character '{row[x]}' in {WorldLabel(world)} unique layer", pos, world));
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
                    _issues.Add(Error("Level has no goal"));
                }

                if (boxCount != _goalCount)
                {
                    _issues.Add(Error($"Box count {boxCount} does not match goal count {_goalCount}"));
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
                            _issues.Add(Error($"{WorldLabel(world)} has a unique wall on a pedestal", FromIndex(i), world));
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
                        _issues.Add(Error($"Pair {i} is missing"));
                        continue;
                    }

                    if (_pairIndexById.ContainsKey(pair.Id))
                    {
                        _issues.Add(Error($"Pair id {pair.Id} is duplicated"));
                        continue;
                    }

                    bool isWorldAValid = IsWorldIndex(pair.WorldA);
                    bool isWorldBValid = IsWorldIndex(pair.WorldB);
                    if (!isWorldAValid || !isWorldBValid)
                    {
                        _issues.Add(Error($"Pair {pair.Id} refers to a world that does not exist"));
                        continue;
                    }

                    if (pair.WorldA == pair.WorldB)
                    {
                        _issues.Add(Error($"Pair {pair.Id} connects {WorldLabel(pair.WorldA)} to itself"));
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
                        _issues.Add(Error($"Portal box {i} is missing"));
                        continue;
                    }

                    if (!_pairIndexById.TryGetValue(box.Pair, out int pairIndex))
                    {
                        _issues.Add(Error($"Portal box {i} refers to missing or invalid pair {box.Pair}", box.Position));
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
                        _issues.Add(Error($"Portal box {i} is in {WorldLabel(box.World)}, which is not part of pair {box.Pair}", box.Position));
                        continue;
                    }

                    if (!placedSides.Add(side))
                    {
                        _issues.Add(Error($"Pair {box.Pair} has more than one box in {WorldLabel(box.World)}", box.Position, box.World));
                        continue;
                    }

                    if (!IsInBounds(box.Position))
                    {
                        _issues.Add(Error($"Portal box {i} at {box.Position} is out of bounds", null, box.World));
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
                    _issues.Add(Error($"Player at {player} is out of bounds"));
                }
                else if (canCheckStartWorld)
                {
                    CheckNotOnWall(player, startWorld, "Player");
                    startWorldOccupants[player] = "Player";
                }

                IReadOnlyList<GridPos> mappedBoxes = _data.MappedBoxes;
                for (int i = 0; i < mappedBoxes.Count; i++)
                {
                    GridPos pos = mappedBoxes[i];
                    string label = $"Box {i}";
                    if (!IsInBounds(pos))
                    {
                        _issues.Add(Error($"{label} at {pos} is out of bounds"));
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

                    string label = $"Portal box in {WorldLabel(portal.World)}";
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
                        _issues.Add(Error($"{label} overlaps another portal box", portal.Position, portal.World));
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
                        _issues.Add(Warning($"{label} sits on a unique wall of {WorldLabel(world)}, so that world starts in conflict", pos, world));
                    }

                    for (int i = 0; i < _portals.Count; i++)
                    {
                        if (_portals[i].World == world && _portals[i].Position == pos)
                        {
                            _issues.Add(Warning($"{label} sits on a portal box of {WorldLabel(world)}, so that world starts in conflict", pos, world));
                        }
                    }
                }
            }

            private void CheckNotOnWall(GridPos pos, int world, string label)
            {
                if (_sharedWalls[ToIndex(pos)])
                {
                    _issues.Add(Error($"{label} is on a shared wall", pos, world));
                }
                else if (_uniqueWalls[world][ToIndex(pos)])
                {
                    _issues.Add(Error($"{label} is on a unique wall of {WorldLabel(world)}", pos, world));
                }
            }

            private void AddOccupant(Dictionary<GridPos, string> occupants, GridPos pos, string label, int world)
            {
                if (occupants.TryGetValue(pos, out string existing))
                {
                    _issues.Add(Error($"{label} overlaps {existing}", pos, world));
                    return;
                }

                occupants.Add(pos, label);
            }

            /// <summary>
            /// 提示中的世界编号从 1 开始，与默认世界名称 World 1、World 2 一致。
            /// </summary>
            private static string WorldLabel(int world)
            {
                return $"World {world + 1}";
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
