namespace Worldyssey.Core
{
    /// <summary>
    /// 校验发现的一条问题。能定位时带上坐标和世界编号，供关卡编辑器高亮。
    /// </summary>
    public sealed class LevelIssue
    {
        public const int NoWorld = -1;

        public LevelIssue(LevelIssueSeverity severity, string message, GridPos? position = null, int world = NoWorld)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Position = position;
            World = world;
        }

        public LevelIssueSeverity Severity { get; }
        public string Message { get; }

        /// <summary>问题所在的格子，无法定位到格子时为 null。</summary>
        public GridPos? Position { get; }

        /// <summary>问题所在的世界，与世界无关时为 <see cref="NoWorld"/>。</summary>
        public int World { get; }

        public bool IsError => Severity == LevelIssueSeverity.Error;

        public override string ToString()
        {
            return $"[{Severity}] {Message}";
        }
    }
}
