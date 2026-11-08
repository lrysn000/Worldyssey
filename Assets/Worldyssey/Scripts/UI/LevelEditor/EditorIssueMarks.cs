using System;
using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 记录编辑器网格里哪些格子有错误或警告，用来给格子外框换色。只标与当前世界相关的问题。
    /// </summary>
    internal sealed class EditorIssueMarks
    {
        private readonly bool[] _errorCells = new bool[LevelLimits.MaxWidth * LevelLimits.MaxHeight];
        private readonly bool[] _warningCells = new bool[LevelLimits.MaxWidth * LevelLimits.MaxHeight];
        private int _width;

        /// <summary>按校验结果重新标记。<paramref name="report"/> 为空时清空全部标记。</summary>
        public void Mark(LevelDraft draft, int world, LevelValidationReport report)
        {
            Array.Clear(_errorCells, 0, _errorCells.Length);
            Array.Clear(_warningCells, 0, _warningCells.Length);
            _width = draft.Width;
            if (report == null)
            {
                return;
            }

            IReadOnlyList<LevelIssue> issues = report.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                LevelIssue issue = issues[i];
                if (!issue.Position.HasValue || !draft.IsInBounds(issue.Position.Value))
                {
                    continue;
                }

                // 只标出与当前世界相关的问题；不属于任何世界的问题在所有世界都标
                if (issue.World != LevelIssue.NoWorld && issue.World != world)
                {
                    continue;
                }

                int index = issue.Position.Value.Y * _width + issue.Position.Value.X;
                if (issue.IsError)
                {
                    _errorCells[index] = true;
                }
                else
                {
                    _warningCells[index] = true;
                }
            }
        }

        /// <summary>格子外框颜色：错误优先于警告。</summary>
        public Color FrameColor(GridPos pos, EditorGridPalette palette)
        {
            int index = pos.Y * _width + pos.X;
            return _errorCells[index] ? palette.ErrorFrame : _warningCells[index] ? palette.WarningFrame : palette.Frame;
        }
    }
}
