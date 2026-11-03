namespace Worldyssey.Core
{
    /// <summary>
    /// 一个通过校验、可以游玩的玩家自建关卡文件。
    /// </summary>
    public sealed class CustomLevelEntry
    {
        public CustomLevelEntry(string filePath, LevelData level)
        {
            FilePath = filePath;
            Level = level;
        }

        public string FilePath { get; }
        public LevelData Level { get; }
    }
}
