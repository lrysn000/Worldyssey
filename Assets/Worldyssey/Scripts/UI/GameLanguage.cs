namespace Worldyssey.UI
{
    /// <summary>
    /// 游戏支持的语言。数值是翻译表里的列序号（第 0 列是英文原文）。
    /// </summary>
    /// <remarks>值会被转换成语言代码存进设置文件，新增语言只能加在末尾，并在翻译表里加一列。</remarks>
    public enum GameLanguage
    {
        English = 0,
        SimplifiedChinese = 1,
    }
}
