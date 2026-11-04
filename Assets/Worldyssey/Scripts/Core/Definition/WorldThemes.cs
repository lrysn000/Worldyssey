using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// <see cref="WorldTheme"/> 的解析、回退和显示名称。
    /// 关卡数据里主题存为字符串（"Spring" 等），JSON 可读，旧数据缺少这个字段时读出来是空字符串。
    /// </summary>
    public static class WorldThemes
    {
        /// <summary>主题数量，等于每关最多的世界数，保证每个世界都能分到不同的主题。</summary>
        public const int Count = 4;

        /// <summary>
        /// 解析数据里的主题字符串。空字符串为 <see cref="WorldTheme.Unset"/>；无法识别时返回 false。
        /// </summary>
        public static bool TryParse(string text, out WorldTheme theme)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                theme = WorldTheme.Unset;
                return true;
            }

            if (Enum.TryParse(text.Trim(), true, out theme) && IsValid(theme))
            {
                return true;
            }

            theme = WorldTheme.Unset;
            return false;
        }

        /// <summary>
        /// 未指定主题时按世界编号回退，保证旧关卡的表现不变。
        /// </summary>
        public static WorldTheme Resolve(WorldTheme theme, int world)
        {
            return theme != WorldTheme.Unset ? theme : (WorldTheme)(((world % Count) + Count) % Count);
        }

        public static bool IsValid(WorldTheme theme)
        {
            return theme >= WorldTheme.Spring && theme <= WorldTheme.Winter;
        }

        /// <summary>写入关卡数据的字符串。</summary>
        public static string ToData(WorldTheme theme)
        {
            return IsValid(theme) ? theme.ToString() : string.Empty;
        }

        /// <summary>
        /// 世界的显示名称。世界名称跟随主题，HUD 和编辑器里直接显示季节名。
        /// </summary>
        public static string DisplayName(WorldTheme theme)
        {
            return IsValid(theme) ? theme.ToString() : string.Empty;
        }
    }
}
