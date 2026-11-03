namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡问题的严重程度。
    /// </summary>
    public enum LevelIssueSeverity
    {
        /// <summary>允许但值得注意，例如初始冲突。不影响保存和游玩。</summary>
        Warning = 0,

        /// <summary>关卡不合法，不能保存为可游玩状态，也不能加载。</summary>
        Error = 1,
    }
}
