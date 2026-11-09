using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 在编辑器里调用 <see cref="LevelSolver"/>：一键检查所有关卡是否可解，也供关卡 Inspector 单独检查。
    /// 检查在主线程上跑，带可取消的进度条。
    /// </summary>
    internal static class LevelSolverTool
    {
        private const string MenuPath = "Worldyssey/Levels/Check All Levels Solvable";
        private const string ProgressTitle = "检查关卡可解性";

        [MenuItem(MenuPath)]
        private static void SolveAll()
        {
            var levels = new List<LevelAsset>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(LevelAsset), new[] { ProjectPaths.Levels }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    levels.Add(asset);
                }
            }

            // 按关卡编号排序，报告读起来和关卡顺序一致
            levels.Sort((a, b) => string.CompareOrdinal(a.Level.Id, b.Level.Id));

            var report = new StringBuilder($"[{nameof(LevelSolverTool)}] 检查 {levels.Count} 个关卡是否可解：\n");
            int problemCount = 0;
            try
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelAsset asset = levels[i];
                    string label = $"{asset.Level.Id} {asset.Level.DisplayName}";
                    SolveResult result = Solve(asset.Level, $"{label}（{i + 1}/{levels.Count}）", out string error);
                    if (result == null)
                    {
                        problemCount++;
                        report.AppendLine($"  {label}：{error}");
                        continue;
                    }

                    if (result.Status == SolveStatus.Cancelled)
                    {
                        report.AppendLine("  已取消，后面的关卡没有检查");
                        break;
                    }

                    if (!result.IsSolvable)
                    {
                        problemCount++;
                    }

                    report.AppendLine($"  {label}：{Describe(result)}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (problemCount > 0)
            {
                Debug.LogWarning(report.ToString());
            }
            else
            {
                Debug.Log(report.ToString());
            }
        }

        /// <summary>
        /// 检查一个关卡。校验不通过时返回 null，原因写入 <paramref name="error"/>。调用方负责在结束后清除进度条。
        /// </summary>
        internal static SolveResult Solve(LevelData level, string progressLabel, out string error)
        {
            error = null;
            LevelValidationReport validation = LevelValidator.Validate(level);
            if (validation.HasErrors)
            {
                error = $"校验有 {validation.ErrorCount} 个错误，未检查";
                return null;
            }

            LevelRules rules;
            try
            {
                rules = new LevelRules(new LevelMap(level));
            }
            catch (Exception exception)
            {
                error = $"无法构建关卡：{exception.Message}";
                return null;
            }

            // 进度条按局面数占上限的比例显示；实际多半远小于上限，所以进度条只表示“还在算”，可以随时取消
            return LevelSolver.Solve(rules, LevelSolver.DefaultMaxStates, explored =>
                !EditorUtility.DisplayCancelableProgressBar(ProgressTitle, $"{progressLabel}：已搜索 {explored:N0} 个局面",
                    (float)explored / LevelSolver.DefaultMaxStates));
        }

        /// <summary>一行文字描述求解结果。</summary>
        internal static string Describe(SolveResult result)
        {
            switch (result.Status)
            {
                case SolveStatus.Solvable:
                    return $"可解（搜索 {result.ExploredStates:N0} 个局面）";
                case SolveStatus.Unsolvable:
                    return $"无解（搜完 {result.ExploredStates:N0} 个局面）";
                case SolveStatus.LimitReached:
                    return $"局面数超过上限 {LevelSolver.DefaultMaxStates:N0}，没有结论";
                default:
                    return "已取消";
            }
        }
    }
}
