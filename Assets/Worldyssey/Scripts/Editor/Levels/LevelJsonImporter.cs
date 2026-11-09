using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把关卡 JSON 导入为 <see cref="LevelAsset"/>。校验有错误的文件不会导入。
    /// 已存在同 id 的资源时原地更新内容，保留 GUID，关卡目录和场景里的引用不会断。
    /// </summary>
    public static class LevelJsonImporter
    {
        private const string ImportMenuPath = "Worldyssey/Levels/Import JSON File...";
        private const string SourceFolder = ProjectPaths.LevelSources;
        private const string LevelFolder = ProjectPaths.Levels;

        // 正式的季节关卡（早期的测试关卡已删除）
        private const string SeasonMenuPath = "Worldyssey/Levels/Import Season Levels";
        private const string SeasonSourceFolder = SourceFolder + "/Seasons";
        private const string SeasonCatalogPath = LevelFolder + "/LevelCatalog_Seasons.asset";
        private const string LevelAssetPrefix = "Level_";

        [MenuItem(ImportMenuPath)]
        public static void ImportFileFromDialog()
        {
            string path = EditorUtility.OpenFilePanel("导入关卡 JSON", SourceFolder, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            LevelAsset asset = ImportFile(path);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        /// <summary>
        /// 导入 Source/Seasons 下的季节关卡，按文件名顺序（1-1、1-2……）生成独立的 LevelCatalog_Seasons。
        /// </summary>
        [MenuItem(SeasonMenuPath)]
        public static void ImportSeasonLevels()
        {
            ImportFolder(SeasonSourceFolder, SeasonCatalogPath);
        }

        /// <summary>导入一个目录下（不含子目录）的全部 JSON，并按文件名顺序重建关卡目录。</summary>
        private static void ImportFolder(string sourceFolder, string catalogPath)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Debug.LogError($"[{nameof(LevelJsonImporter)}] 找不到目录 {sourceFolder}");
                return;
            }

            string[] files = Directory.GetFiles(sourceFolder, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<LevelAsset>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                LevelAsset asset = ImportFile(files[i]);
                if (asset != null)
                {
                    imported.Add(asset);
                }
            }

            WriteCatalog(catalogPath, imported);
            Debug.Log($"[{nameof(LevelJsonImporter)}] 导入 {imported.Count}/{files.Length} 个关卡，关卡目录：{catalogPath}");
        }

        /// <summary>
        /// 导入单个 JSON 文件，成功返回资源，失败返回 null 并在 Console 列出原因。
        /// </summary>
        public static LevelAsset ImportFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string json = File.ReadAllText(filePath);
            if (!LevelJsonSerializer.TryFromJson(json, out LevelData level, out string error))
            {
                Debug.LogError($"[{nameof(LevelJsonImporter)}] {fileName} 解析失败：{error}");
                return null;
            }

            LevelValidationReport report = LevelValidator.Validate(level);
            IReadOnlyList<LevelIssue> issues = report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                string message = $"[{nameof(LevelJsonImporter)}] {fileName}：{issues[i].Message}";
                if (issues[i].IsError)
                {
                    Debug.LogError(message);
                }
                else
                {
                    Debug.LogWarning(message);
                }
            }

            if (report.HasErrors)
            {
                Debug.LogError($"[{nameof(LevelJsonImporter)}] {fileName} 有 {report.ErrorCount} 个错误，未导入");
                return null;
            }

            AssetFolderUtility.EnsureFolder(LevelFolder);
            string assetPath = $"{LevelFolder}/{LevelAssetPrefix}{ToSafeFileName(level.Id)}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelAsset>();
                asset.SetLevel(level);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                asset.SetLevel(level);
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>
        /// 把关卡 id 转为可用作文件名的字符串。
        /// </summary>
        public static string ToSafeFileName(string levelId)
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

        private static void WriteCatalog(string catalogPath, List<LevelAsset> levels)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty array = serialized.FindProperty("_levels");
            if (array == null)
            {
                Debug.LogError($"[{nameof(LevelJsonImporter)}] {nameof(LevelCatalog)} 中找不到字段 _levels");
                return;
            }

            array.arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
