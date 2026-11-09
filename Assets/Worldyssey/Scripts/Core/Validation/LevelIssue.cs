namespace Worldyssey.Core
{
    /// <summary>
    /// 校验发现的一条问题。能定位时带上坐标和世界编号，供关卡编辑器高亮。
    /// </summary>
    public sealed class LevelIssue
    {
        public const int NoWorld = -1;

        public LevelIssue(LevelIssueSeverity severity, IssueText text, GridPos? position = null, int world = NoWorld)
        {
            Severity = severity;
            Text = text ?? IssueText.Of(string.Empty);
            Message = Text.ToString();
            Position = position;
            World = world;
        }

        /// <summary>直接用一段英文创建问题，文字不会被翻译。</summary>
        public LevelIssue(LevelIssueSeverity severity, string message, GridPos? position = null, int world = NoWorld)
            : this(severity, IssueText.Of(message ?? string.Empty), position, world)
        {
        }

        public LevelIssueSeverity Severity { get; }

        /// <summary>英文原文，供日志、导入工具和测试使用。</summary>
        public string Message { get; }

        /// <summary>可翻译的模板 + 参数，游戏内编辑器显示时使用。</summary>
        public IssueText Text { get; }

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
