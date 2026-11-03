using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡的静态定义，即关卡的存储格式。内置关卡的 ScriptableObject、玩家关卡的 JSON、关卡编辑器共用这一个类型。
    /// 运行时查询请使用由它解析出的 <see cref="LevelMap"/>。
    /// </summary>
    /// <remarks>
    /// 字段名即 JSON 键名，因此不加下划线。
    /// 本类不做合法性检查，完整校验由关卡校验器负责（M5）。
    /// </remarks>
    [Serializable]
    public class LevelData
    {
        /// <summary>当前关卡格式版本。格式变化时递增，读取旧版本时据此迁移。</summary>
        public const int CurrentFormatVersion = 1;

        [SerializeField] private int formatVersion = CurrentFormatVersion;
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [Min(1)] [SerializeField] private int width;
        [Min(1)] [SerializeField] private int height;
        [Tooltip("起始世界编号，从 0 开始")]
        [SerializeField] private int startWorld;
        [Tooltip("共享层，从上到下每行一个字符串：'#' 共享墙，'P' 底座，'.' 终点，空格为空地")]
        [SerializeField] private string[] sharedRows = Array.Empty<string>();
        [SerializeField] private WorldData[] worlds = Array.Empty<WorldData>();
        [SerializeField] private PlayerSpawnData player = new PlayerSpawnData();
        [SerializeField] private GridPos[] mappedBoxes = Array.Empty<GridPos>();
        [SerializeField] private PortalPairData[] portalPairs = Array.Empty<PortalPairData>();
        [SerializeField] private PortalBoxData[] portalBoxes = Array.Empty<PortalBoxData>();

        public LevelData()
        {
        }

        public LevelData(
            string id,
            string displayName,
            int width,
            int height,
            int startWorld,
            string[] sharedRows,
            WorldData[] worlds,
            PlayerSpawnData player,
            GridPos[] mappedBoxes,
            PortalPairData[] portalPairs,
            PortalBoxData[] portalBoxes)
        {
            this.id = id ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.width = width;
            this.height = height;
            this.startWorld = startWorld;
            this.sharedRows = sharedRows ?? Array.Empty<string>();
            this.worlds = worlds ?? Array.Empty<WorldData>();
            this.player = player ?? new PlayerSpawnData();
            this.mappedBoxes = mappedBoxes ?? Array.Empty<GridPos>();
            this.portalPairs = portalPairs ?? Array.Empty<PortalPairData>();
            this.portalBoxes = portalBoxes ?? Array.Empty<PortalBoxData>();
        }

        public int FormatVersion => formatVersion;

        // 反序列化可能给数组字段留下 null（例如 JSON 里缺了某个键），属性统一兜底为空集合
        public string Id => id ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public int Width => width;
        public int Height => height;
        public int StartWorld => startWorld;
        public IReadOnlyList<string> SharedRows => sharedRows ?? Array.Empty<string>();
        public IReadOnlyList<WorldData> Worlds => worlds ?? Array.Empty<WorldData>();
        public PlayerSpawnData Player => player ?? new PlayerSpawnData();
        public IReadOnlyList<GridPos> MappedBoxes => mappedBoxes ?? Array.Empty<GridPos>();
        public IReadOnlyList<PortalPairData> PortalPairs => portalPairs ?? Array.Empty<PortalPairData>();
        public IReadOnlyList<PortalBoxData> PortalBoxes => portalBoxes ?? Array.Empty<PortalBoxData>();
    }
}
