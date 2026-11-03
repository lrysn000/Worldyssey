using System;
using System.Collections.Generic;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡编辑器中的可编辑关卡。所有编辑操作都保持结构合法（行列一致、世界和配对编号连续），
    /// 玩法层面的合法性（箱子数与终点数、重叠等）由 <see cref="LevelValidator"/> 对 <see cref="ToLevelData"/> 的结果实时检查。
    /// </summary>
    public sealed class LevelDraft
    {
        public const int DefaultWidth = 9;
        public const int DefaultHeight = 7;
        private const string WorldNameFormat = "World {0}";

        private readonly List<string> _worldNames = new List<string>();
        private readonly List<bool[]> _uniqueWalls = new List<bool[]>();
        private readonly List<GridPos> _mappedBoxes = new List<GridPos>();
        private readonly List<DraftPair> _pairs = new List<DraftPair>();
        private readonly List<DraftPortal> _portals = new List<DraftPortal>();
        private SharedCell[] _shared;

        private LevelDraft(string id, string displayName, int width, int height)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Width = width;
            Height = height;
            _shared = new SharedCell[width * height];
        }

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int WorldCount => _worldNames.Count;

        /// <summary>起始世界，玩家只存在于这个世界。</summary>
        public int StartWorld { get; private set; }

        public GridPos PlayerPosition { get; private set; }
        public Direction PlayerFacing { get; private set; }
        public IReadOnlyList<GridPos> MappedBoxes => _mappedBoxes;
        public IReadOnlyList<DraftPair> Pairs => _pairs;
        public IReadOnlyList<DraftPortal> Portals => _portals;

        /// <summary>
        /// 新建默认关卡：一圈共享墙、一个世界、玩家在左下角内侧。
        /// </summary>
        public static LevelDraft CreateNew(string id, string displayName)
        {
            var draft = new LevelDraft(id, displayName, DefaultWidth, DefaultHeight);
            draft._worldNames.Add(string.Format(WorldNameFormat, 1));
            draft._uniqueWalls.Add(new bool[DefaultWidth * DefaultHeight]);
            for (int y = 0; y < DefaultHeight; y++)
            {
                for (int x = 0; x < DefaultWidth; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == DefaultWidth - 1 || y == DefaultHeight - 1;
                    draft._shared[draft.ToIndex(new GridPos(x, y))] = isBorder ? SharedCell.Wall : SharedCell.Empty;
                }
            }

            draft.PlayerPosition = new GridPos(1, 1);
            draft.PlayerFacing = Direction.Down;
            return draft;
        }

        /// <summary>
        /// 从关卡数据创建草稿。结构不合法（<see cref="LevelMap"/> 无法解析）时抛出 <see cref="ArgumentException"/>。
        /// 配对编号会按出现顺序重新编为 0、1、2……
        /// </summary>
        public static LevelDraft FromLevelData(LevelData data)
        {
            var map = new LevelMap(data);
            var draft = new LevelDraft(data.Id, data.DisplayName, map.Width, map.Height);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    draft._shared[draft.ToIndex(pos)] = map.IsSharedWall(pos) ? SharedCell.Wall
                        : map.IsPedestal(pos) ? SharedCell.Pedestal
                        : map.IsGoal(pos) ? SharedCell.Goal
                        : SharedCell.Empty;
                }
            }

            for (int world = 0; world < map.WorldCount; world++)
            {
                draft._worldNames.Add(map.GetWorldName(world));
                var walls = new bool[map.Width * map.Height];
                for (int i = 0; i < walls.Length; i++)
                {
                    walls[i] = map.IsUniqueWall(world, draft.FromIndex(i));
                }

                draft._uniqueWalls.Add(walls);
            }

            for (int pair = 0; pair < map.PortalPairCount; pair++)
            {
                draft._pairs.Add(new DraftPair(map.GetPortalWorld(pair * 2), map.GetPortalWorld(pair * 2 + 1)));
            }

            IReadOnlyList<PortalBoxState> portals = map.PortalBoxStarts;
            for (int i = 0; i < portals.Count; i++)
            {
                if (portals[i].IsPresent)
                {
                    draft._portals.Add(new DraftPortal(portals[i].PairIndex, portals[i].World, portals[i].Position));
                }
            }

            draft._mappedBoxes.AddRange(map.MappedBoxStarts);
            draft.StartWorld = map.StartWorld;
            draft.PlayerPosition = map.PlayerStart;
            draft.PlayerFacing = map.PlayerStartFacing;
            return draft;
        }

        /// <summary>
        /// 转为关卡数据。没有独有墙的世界省略独有层。
        /// </summary>
        public LevelData ToLevelData()
        {
            var sharedRows = new string[Height];
            var cells = new char[Width];
            for (int row = 0; row < Height; row++)
            {
                int y = Height - 1 - row;
                for (int x = 0; x < Width; x++)
                {
                    cells[x] = ToChar(_shared[ToIndex(new GridPos(x, y))]);
                }

                sharedRows[row] = new string(cells);
            }

            var worlds = new WorldData[WorldCount];
            for (int world = 0; world < WorldCount; world++)
            {
                worlds[world] = new WorldData(_worldNames[world], BuildWallRows(_uniqueWalls[world], cells));
            }

            var pairs = new PortalPairData[_pairs.Count];
            for (int i = 0; i < pairs.Length; i++)
            {
                pairs[i] = new PortalPairData(i, _pairs[i].WorldA, _pairs[i].WorldB);
            }

            var portals = new PortalBoxData[_portals.Count];
            for (int i = 0; i < portals.Length; i++)
            {
                DraftPortal portal = _portals[i];
                portals[i] = new PortalBoxData(portal.Pair, portal.World, portal.Position.X, portal.Position.Y);
            }

            return new LevelData(
                Id,
                DisplayName,
                Width,
                Height,
                StartWorld,
                sharedRows,
                worlds,
                new PlayerSpawnData(PlayerPosition.X, PlayerPosition.Y, PlayerFacing),
                _mappedBoxes.ToArray(),
                pairs,
                portals);
        }

        /// <summary>
        /// 深拷贝，用于编辑器撤销快照。
        /// </summary>
        public LevelDraft Clone()
        {
            var copy = new LevelDraft(Id, DisplayName, Width, Height)
            {
                StartWorld = StartWorld,
                PlayerPosition = PlayerPosition,
                PlayerFacing = PlayerFacing,
            };
            Array.Copy(_shared, copy._shared, _shared.Length);
            copy._worldNames.AddRange(_worldNames);
            for (int i = 0; i < _uniqueWalls.Count; i++)
            {
                copy._uniqueWalls.Add((bool[])_uniqueWalls[i].Clone());
            }

            copy._mappedBoxes.AddRange(_mappedBoxes);
            copy._pairs.AddRange(_pairs);
            copy._portals.AddRange(_portals);
            return copy;
        }

        public bool IsInBounds(GridPos pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public string GetWorldName(int world)
        {
            return _worldNames[world];
        }

        public SharedCell GetSharedCell(GridPos pos)
        {
            return IsInBounds(pos) ? _shared[ToIndex(pos)] : SharedCell.Wall;
        }

        public bool IsUniqueWall(int world, GridPos pos)
        {
            return IsInBounds(pos) && _uniqueWalls[world][ToIndex(pos)];
        }

        /// <summary>该格的目标箱子下标，没有返回 -1。</summary>
        public int FindMappedBox(GridPos pos)
        {
            return _mappedBoxes.IndexOf(pos);
        }

        /// <summary>指定世界该格的传送箱子在 <see cref="Portals"/> 中的下标，没有返回 -1。</summary>
        public int FindPortal(int world, GridPos pos)
        {
            for (int i = 0; i < _portals.Count; i++)
            {
                if (_portals[i].World == world && _portals[i].Position == pos)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按画笔编辑一格。返回内容是否发生了变化。
        /// </summary>
        public bool Apply(EditorTool tool, int world, GridPos pos, int selectedPair)
        {
            if (!IsInBounds(pos) || world < 0 || world >= WorldCount)
            {
                return false;
            }

            switch (tool)
            {
                case EditorTool.Floor:
                    return SetSharedCell(pos, SharedCell.Empty);
                case EditorTool.SharedWall:
                    return SetSharedCell(pos, SharedCell.Wall);
                case EditorTool.Pedestal:
                    return SetSharedCell(pos, SharedCell.Pedestal);
                case EditorTool.Goal:
                    return SetSharedCell(pos, SharedCell.Goal);
                case EditorTool.UniqueWall:
                    return SetUniqueWall(world, pos, true);
                case EditorTool.MappedBox:
                    return PlaceMappedBox(pos);
                case EditorTool.Player:
                    return PlacePlayer(world, pos);
                case EditorTool.PortalBox:
                    return PlacePortal(selectedPair, world, pos);
                case EditorTool.Eraser:
                    return Erase(world, pos);
                default:
                    throw new ArgumentOutOfRangeException(nameof(tool), tool, "未知画笔");
            }
        }

        public bool SetSharedCell(GridPos pos, SharedCell cell)
        {
            int index = ToIndex(pos);
            if (_shared[index] == cell)
            {
                return false;
            }

            _shared[index] = cell;
            return true;
        }

        public bool SetUniqueWall(int world, GridPos pos, bool isWall)
        {
            int index = ToIndex(pos);
            if (_uniqueWalls[world][index] == isWall)
            {
                return false;
            }

            _uniqueWalls[world][index] = isWall;
            return true;
        }

        public bool PlaceMappedBox(GridPos pos)
        {
            if (_mappedBoxes.Contains(pos))
            {
                return false;
            }

            _mappedBoxes.Add(pos);
            return true;
        }

        /// <summary>
        /// 放置玩家并把起始世界设为 <paramref name="world"/>。点在玩家所在格上时改为顺时针转向。
        /// </summary>
        public bool PlacePlayer(int world, GridPos pos)
        {
            if (world == StartWorld && pos == PlayerPosition)
            {
                PlayerFacing = (Direction)(((int)PlayerFacing + 1) % 4);
                return true;
            }

            StartWorld = world;
            PlayerPosition = pos;
            return true;
        }

        /// <summary>
        /// 放置配对 <paramref name="pair"/> 位于 <paramref name="world"/> 的一侧；已放过则移动到新位置。
        /// 世界不属于该配对，或该格已有别的传送箱子时返回 false。
        /// </summary>
        public bool PlacePortal(int pair, int world, GridPos pos)
        {
            if (pair < 0 || pair >= _pairs.Count || !_pairs[pair].Contains(world))
            {
                return false;
            }

            int occupant = FindPortal(world, pos);
            if (occupant >= 0)
            {
                return false;
            }

            for (int i = 0; i < _portals.Count; i++)
            {
                if (_portals[i].Pair == pair && _portals[i].World == world)
                {
                    _portals[i] = new DraftPortal(pair, world, pos);
                    return true;
                }
            }

            _portals.Add(new DraftPortal(pair, world, pos));
            return true;
        }

        /// <summary>
        /// 清除该格在 <paramref name="world"/> 的目标箱子、传送箱子和独有墙。玩家不会被删除，地形保留。
        /// </summary>
        public bool Erase(int world, GridPos pos)
        {
            bool isChanged = _mappedBoxes.Remove(pos);
            int portal = FindPortal(world, pos);
            if (portal >= 0)
            {
                _portals.RemoveAt(portal);
                isChanged = true;
            }

            isChanged |= SetUniqueWall(world, pos, false);
            return isChanged;
        }

        /// <summary>
        /// 新增一个世界，返回其编号；已达上限返回 -1。
        /// </summary>
        public int AddWorld()
        {
            if (WorldCount >= LevelLimits.MaxWorldCount)
            {
                return -1;
            }

            _worldNames.Add(string.Format(WorldNameFormat, WorldCount + 1));
            _uniqueWalls.Add(new bool[Width * Height]);
            return WorldCount - 1;
        }

        /// <summary>
        /// 删除一个世界，连带删除引用它的配对及其传送箱子；之后的世界编号前移，名称按新编号重排。只剩一个世界时返回 false。
        /// </summary>
        public bool RemoveWorld(int world)
        {
            if (WorldCount <= LevelLimits.MinWorldCount || world < 0 || world >= WorldCount)
            {
                return false;
            }

            for (int pair = _pairs.Count - 1; pair >= 0; pair--)
            {
                if (_pairs[pair].Contains(world))
                {
                    RemovePair(pair);
                }
            }

            for (int i = 0; i < _pairs.Count; i++)
            {
                _pairs[i] = new DraftPair(ShiftWorld(_pairs[i].WorldA, world), ShiftWorld(_pairs[i].WorldB, world));
            }

            for (int i = 0; i < _portals.Count; i++)
            {
                DraftPortal portal = _portals[i];
                _portals[i] = new DraftPortal(portal.Pair, ShiftWorld(portal.World, world), portal.Position);
            }

            if (StartWorld == world)
            {
                StartWorld = 0;
            }
            else
            {
                StartWorld = ShiftWorld(StartWorld, world);
            }

            _worldNames.RemoveAt(world);
            _uniqueWalls.RemoveAt(world);
            for (int i = 0; i < _worldNames.Count; i++)
            {
                _worldNames[i] = string.Format(WorldNameFormat, i + 1);
            }

            return true;
        }

        /// <summary>
        /// 新增连接两个不同世界的配对，返回配对编号；参数非法返回 -1。
        /// </summary>
        public int AddPair(int worldA, int worldB)
        {
            if (worldA == worldB || worldA < 0 || worldB < 0 || worldA >= WorldCount || worldB >= WorldCount)
            {
                return -1;
            }

            _pairs.Add(new DraftPair(worldA, worldB));
            return _pairs.Count - 1;
        }

        /// <summary>
        /// 删除配对及其两侧传送箱子，之后的配对编号前移。
        /// </summary>
        public bool RemovePair(int pair)
        {
            if (pair < 0 || pair >= _pairs.Count)
            {
                return false;
            }

            _pairs.RemoveAt(pair);
            for (int i = _portals.Count - 1; i >= 0; i--)
            {
                DraftPortal portal = _portals[i];
                if (portal.Pair == pair)
                {
                    _portals.RemoveAt(i);
                }
                else if (portal.Pair > pair)
                {
                    _portals[i] = new DraftPortal(portal.Pair - 1, portal.World, portal.Position);
                }
            }

            return true;
        }

        /// <summary>
        /// 改变尺寸，左下角坐标不变，从右边和上边增减行列。超出范围的实体被移除，玩家被挪回边界内。
        /// 尺寸会被限制在 <see cref="LevelLimits"/> 允许的范围内。返回尺寸是否发生了变化。
        /// </summary>
        public bool Resize(int newWidth, int newHeight)
        {
            newWidth = Math.Max(LevelLimits.MinWidth, Math.Min(LevelLimits.MaxWidth, newWidth));
            newHeight = Math.Max(LevelLimits.MinHeight, Math.Min(LevelLimits.MaxHeight, newHeight));
            if (newWidth == Width && newHeight == Height)
            {
                return false;
            }

            int oldWidth = Width;
            var newShared = new SharedCell[newWidth * newHeight];
            for (int y = 0; y < Math.Min(Height, newHeight); y++)
            {
                for (int x = 0; x < Math.Min(Width, newWidth); x++)
                {
                    newShared[y * newWidth + x] = _shared[y * oldWidth + x];
                }
            }

            for (int world = 0; world < _uniqueWalls.Count; world++)
            {
                bool[] oldWalls = _uniqueWalls[world];
                var newWalls = new bool[newWidth * newHeight];
                for (int y = 0; y < Math.Min(Height, newHeight); y++)
                {
                    for (int x = 0; x < Math.Min(Width, newWidth); x++)
                    {
                        newWalls[y * newWidth + x] = oldWalls[y * oldWidth + x];
                    }
                }

                _uniqueWalls[world] = newWalls;
            }

            _shared = newShared;
            Width = newWidth;
            Height = newHeight;

            _mappedBoxes.RemoveAll(pos => !IsInBounds(pos));
            _portals.RemoveAll(portal => !IsInBounds(portal.Position));
            PlayerPosition = new GridPos(Math.Min(PlayerPosition.X, Width - 1), Math.Min(PlayerPosition.Y, Height - 1));
            return true;
        }

        private static int ShiftWorld(int world, int removedWorld)
        {
            return world > removedWorld ? world - 1 : world;
        }

        private string[] BuildWallRows(bool[] walls, char[] buffer)
        {
            bool hasAnyWall = Array.IndexOf(walls, true) >= 0;
            if (!hasAnyWall)
            {
                return Array.Empty<string>();
            }

            var rows = new string[Height];
            for (int row = 0; row < Height; row++)
            {
                int y = Height - 1 - row;
                for (int x = 0; x < Width; x++)
                {
                    buffer[x] = walls[ToIndex(new GridPos(x, y))] ? LevelMap.UniqueWallChar : LevelMap.EmptyChar;
                }

                rows[row] = new string(buffer, 0, Width);
            }

            return rows;
        }

        private static char ToChar(SharedCell cell)
        {
            switch (cell)
            {
                case SharedCell.Wall: return LevelMap.SharedWallChar;
                case SharedCell.Pedestal: return LevelMap.PedestalChar;
                case SharedCell.Goal: return LevelMap.GoalChar;
                default: return LevelMap.EmptyChar;
            }
        }

        private int ToIndex(GridPos pos)
        {
            return pos.Y * Width + pos.X;
        }

        private GridPos FromIndex(int index)
        {
            return new GridPos(index % Width, index / Width);
        }
    }
}
