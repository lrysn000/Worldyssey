using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 读写存档文件和玩家自建关卡目录。根目录由构造参数传入，游戏中为 Application.persistentDataPath，测试中为临时目录。
    /// </summary>
    public sealed class SaveService
    {
        public const string SaveFileName = "save.json";
        public const string CustomLevelFolderName = "Levels";
        public const string CustomLevelExtension = ".json";
        private const string TempSuffix = ".tmp";
        private const string CorruptSuffix = ".corrupt";

        // 不带 BOM，方便用其他工具查看和比对
        private static readonly Encoding FileEncoding = new UTF8Encoding(false);

        public SaveService(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("根目录不能为空", nameof(rootDirectory));
            }

            RootDirectory = rootDirectory;
        }

        public string RootDirectory { get; }
        public string SaveFilePath => Path.Combine(RootDirectory, SaveFileName);
        public string CustomLevelDirectory => Path.Combine(RootDirectory, CustomLevelFolderName);

        /// <summary>
        /// 读取存档。文件不存在时返回全新存档；读取失败时把坏文件备份为 .corrupt，返回全新存档，
        /// 并通过 <paramref name="error"/> 给出原因（成功时为空字符串），由调用方记录日志。
        /// </summary>
        public SaveData LoadOrCreate(out string error)
        {
            error = string.Empty;
            string path = SaveFilePath;
            if (!File.Exists(path))
            {
                return new SaveData();
            }

            string failure;
            try
            {
                string json = File.ReadAllText(path, FileEncoding);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    failure = "存档内容为空";
                }
                else if (data.Version > SaveData.CurrentVersion)
                {
                    failure = $"存档版本 {data.Version} 高于当前支持的 {SaveData.CurrentVersion}";
                }
                else if (data.Version < 1)
                {
                    failure = $"存档版本 {data.Version} 非法";
                }
                else
                {
                    return data;
                }
            }
            catch (ArgumentException exception)
            {
                // JsonUtility 在 JSON 语法错误时抛 ArgumentException
                failure = $"存档格式错误：{exception.Message}";
            }
            catch (IOException exception)
            {
                failure = $"读取存档失败：{exception.Message}";
            }

            error = $"{failure}；已备份为 {SaveFileName}{CorruptSuffix} 并使用全新存档";
            BackupCorruptFile(path);
            return new SaveData();
        }

        /// <summary>
        /// 写入存档。先写临时文件再替换，避免写到一半中断导致存档损坏。IO 失败时抛出异常，由调用方记录。
        /// </summary>
        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Directory.CreateDirectory(RootDirectory);
            WriteAtomically(SaveFilePath, JsonUtility.ToJson(data, true));
        }

        /// <summary>
        /// 先写临时文件再替换目标文件，避免写到一半中断导致文件损坏。
        /// </summary>
        private static void WriteAtomically(string path, string content)
        {
            string tempPath = path + TempSuffix;
            File.WriteAllText(tempPath, content, FileEncoding);

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        /// <summary>
        /// 生成新的自建关卡 id，包含创建时间，基本不会重复。
        /// </summary>
        public static string GenerateCustomLevelId(DateTime now)
        {
            return "custom-" + now.ToString("yyyyMMdd-HHmmss");
        }

        /// <summary>
        /// 把关卡 id 转为可用作文件名的字符串。
        /// </summary>
        public static string ToFileName(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return "unnamed";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = levelId.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        /// <summary>
        /// 保存自建关卡到自建关卡目录，文件名由 id 决定，同 id 覆盖。调用方应先确认校验无错误。
        /// IO 失败时抛出异常，由调用方记录。返回写入的文件路径。
        /// </summary>
        public string SaveCustomLevel(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            Directory.CreateDirectory(CustomLevelDirectory);
            string path = Path.Combine(CustomLevelDirectory, ToFileName(level.Id) + CustomLevelExtension);
            WriteAtomically(path, LevelJsonSerializer.ToJson(level));
            return path;
        }

        /// <summary>
        /// 读取自建关卡目录下的全部 JSON，按文件名排序。无法解析或校验有错误的文件会被跳过，
        /// 原因追加到 <paramref name="problems"/>。目录不存在时返回空列表。
        /// </summary>
        public List<CustomLevelEntry> LoadCustomLevels(List<string> problems)
        {
            var entries = new List<CustomLevelEntry>();
            string directory = CustomLevelDirectory;
            if (!Directory.Exists(directory))
            {
                return entries;
            }

            string[] files = Directory.GetFiles(directory, "*" + CustomLevelExtension);
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                string json;
                try
                {
                    json = File.ReadAllText(files[i], FileEncoding);
                }
                catch (IOException exception)
                {
                    problems?.Add($"{fileName}：读取失败：{exception.Message}");
                    continue;
                }

                if (!LevelJsonSerializer.TryFromJson(json, out LevelData level, out string error))
                {
                    problems?.Add($"{fileName}：{error}");
                    continue;
                }

                LevelValidationReport report = LevelValidator.Validate(level);
                if (report.HasErrors)
                {
                    problems?.Add($"{fileName}：校验有 {report.ErrorCount} 个错误，第一个：{FirstError(report)}");
                    continue;
                }

                entries.Add(new CustomLevelEntry(files[i], level));
            }

            return entries;
        }

        private static string FirstError(LevelValidationReport report)
        {
            for (int i = 0; i < report.Issues.Count; i++)
            {
                if (report.Issues[i].IsError)
                {
                    return report.Issues[i].Message;
                }
            }

            return string.Empty;
        }

        private static void BackupCorruptFile(string path)
        {
            string backupPath = path + CorruptSuffix;
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            File.Move(path, backupPath);
        }
    }
}
