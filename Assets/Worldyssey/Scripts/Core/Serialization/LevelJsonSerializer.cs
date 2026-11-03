using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// <see cref="LevelData"/> 与 JSON 互转，用于玩家自建关卡和内置关卡的源文件。
    /// 只负责格式层面；内容是否合法由 <see cref="LevelValidator"/> 检查。
    /// </summary>
    /// <remarks>
    /// 使用 JsonUtility：字段名即键名，枚举写成数字（facing：0 上、1 右、2 下、3 左）。
    /// </remarks>
    public static class LevelJsonSerializer
    {
        /// <summary>
        /// 转为带缩进的 JSON，字符行一行一个，便于阅读和比对。
        /// </summary>
        public static string ToJson(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            return JsonUtility.ToJson(level, true);
        }

        /// <summary>
        /// 解析 JSON。文本为空、格式错误或版本过新时返回 false 并给出原因。
        /// 缺失的字段会保持默认值，由校验器报告。
        /// </summary>
        public static bool TryFromJson(string json, out LevelData level, out string error)
        {
            level = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON 内容为空";
                return false;
            }

            try
            {
                level = JsonUtility.FromJson<LevelData>(json);
            }
            catch (ArgumentException exception)
            {
                // JsonUtility 在语法错误时抛 ArgumentException，这里转成失败原因交给调用方展示
                error = $"JSON 格式错误：{exception.Message}";
                return false;
            }

            if (level == null)
            {
                error = "JSON 解析结果为空";
                return false;
            }

            if (level.FormatVersion > LevelData.CurrentFormatVersion)
            {
                error = $"关卡格式版本 {level.FormatVersion} 高于当前支持的 {LevelData.CurrentFormatVersion}，请更新游戏";
                level = null;
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
