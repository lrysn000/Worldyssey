using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 关卡编辑器的俯视网格：显示当前世界的全部内容，用外框标出校验问题，并把鼠标绘制转成笔画事件。
    /// 左键用当前画笔，右键总是橡皮擦；按住拖动连续绘制，一次按下到松开为一笔。
    /// </summary>
    public class EditorGridView : MonoBehaviour
    {
        private static readonly Color NoFrame = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color ErrorFrame = new Color(0.95f, 0.15f, 0.15f, 1f);
        private static readonly Color WarningFrame = new Color(1f, 0.6f, 0.1f, 1f);
        private static readonly Color FloorColor = new Color(0.55f, 0.55f, 0.52f, 1f);
        private static readonly Color SharedWallColor = new Color(0.18f, 0.19f, 0.22f, 1f);
        private static readonly Color UniqueWallColor = new Color(0.40f, 0.47f, 0.62f, 1f);
        private static readonly Color GoalColor = new Color(0.95f, 0.80f, 0.25f, 1f);
        private static readonly Color PedestalColor = new Color(0.30f, 0.72f, 0.75f, 1f);
        private static readonly Color MappedBoxColor = new Color(0.88f, 0.45f, 0.20f, 1f);
        private static readonly Color PlayerColor = new Color(0.20f, 0.50f, 0.95f, 1f);
        private static readonly Color PortalBoxColor = new Color(0.65f, 0.35f, 0.90f, 1f);
        private static readonly Color Hidden = new Color(0f, 0f, 0f, 0f);
        private static readonly string[] FacingLabels = { "@^", "@>", "@v", "@<" };
        private const string BoxLabel = "B";
        private const string PortalLabelPrefix = "P";

        [SerializeField] private RectTransform _gridArea;
        [SerializeField] private EditorGridCell _cellPrefab;

        private readonly List<EditorGridCell> _cells = new List<EditorGridCell>();
        private readonly bool[] _errorCells = new bool[LevelLimits.MaxWidth * LevelLimits.MaxHeight];
        private readonly bool[] _warningCells = new bool[LevelLimits.MaxWidth * LevelLimits.MaxHeight];
        private bool _isStroking;
        private int _width;

        /// <summary>开始一笔，参数为格子坐标和是否为擦除（右键）。</summary>
        public event Action<GridPos, bool> StrokeStarted;

        /// <summary>按住拖到了另一格。</summary>
        public event Action<GridPos> StrokeContinued;

        public event Action StrokeEnded;

        private void Awake()
        {
            if (_gridArea == null || _cellPrefab == null)
            {
                Debug.LogError($"[{nameof(EditorGridView)}] {name} 缺少网格区域或格子预制体引用", this);
            }
        }

        private void OnDisable()
        {
            // 面板被关闭时收不到松开事件，结束未完成的一笔
            EndStroke();
        }

        /// <summary>
        /// 按草稿内容重绘当前世界。格子按最大尺寸一次性创建，之后只复用。
        /// </summary>
        public void Render(LevelDraft draft, int world, LevelValidationReport report)
        {
            if (_gridArea == null || _cellPrefab == null)
            {
                return;
            }

            EnsureCells();
            _width = draft.Width;
            MarkIssues(draft, world, report);

            Rect area = _gridArea.rect;
            float cellSize = Mathf.Floor(Mathf.Min(area.width / draft.Width, area.height / draft.Height));
            var origin = new Vector2(-cellSize * draft.Width * 0.5f, -cellSize * draft.Height * 0.5f);

            int used = 0;
            for (int y = 0; y < draft.Height; y++)
            {
                for (int x = 0; x < draft.Width; x++)
                {
                    var pos = new GridPos(x, y);
                    EditorGridCell cell = _cells[used++];
                    cell.gameObject.SetActive(true);
                    cell.Bind(this, pos);
                    cell.RectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                    cell.RectTransform.anchoredPosition = origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                    RenderCell(cell, draft, world, pos);
                }
            }

            for (int i = used; i < _cells.Count; i++)
            {
                _cells[i].gameObject.SetActive(false);
            }
        }

        internal void HandleCellDown(EditorGridCell cell, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right)
            {
                return;
            }

            _isStroking = true;
            StrokeStarted?.Invoke(cell.Position, button == PointerEventData.InputButton.Right);
        }

        internal void HandleCellEnter(EditorGridCell cell)
        {
            if (_isStroking)
            {
                StrokeContinued?.Invoke(cell.Position);
            }
        }

        internal void HandleCellUp()
        {
            EndStroke();
        }

        private void EndStroke()
        {
            if (!_isStroking)
            {
                return;
            }

            _isStroking = false;
            StrokeEnded?.Invoke();
        }

        private void EnsureCells()
        {
            int needed = LevelLimits.MaxWidth * LevelLimits.MaxHeight;
            while (_cells.Count < needed)
            {
                EditorGridCell cell = Instantiate(_cellPrefab, _gridArea);
                cell.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                cell.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                cell.gameObject.SetActive(false);
                _cells.Add(cell);
            }
        }

        private void MarkIssues(LevelDraft draft, int world, LevelValidationReport report)
        {
            Array.Clear(_errorCells, 0, _errorCells.Length);
            Array.Clear(_warningCells, 0, _warningCells.Length);
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

                int index = issue.Position.Value.Y * draft.Width + issue.Position.Value.X;
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

        private void RenderCell(EditorGridCell cell, LevelDraft draft, int world, GridPos pos)
        {
            int index = pos.Y * _width + pos.X;
            Color frame = _errorCells[index] ? ErrorFrame : _warningCells[index] ? WarningFrame : NoFrame;
            Color terrain = TerrainColor(draft.GetSharedCell(pos), draft.IsUniqueWall(world, pos));

            Color entity = Hidden;
            string label = string.Empty;
            int portal = draft.FindPortal(world, pos);
            if (world == draft.StartWorld && pos == draft.PlayerPosition)
            {
                entity = PlayerColor;
                label = FacingLabels[(int)draft.PlayerFacing];
            }
            else if (draft.FindMappedBox(pos) >= 0)
            {
                entity = MappedBoxColor;
                label = BoxLabel;
            }
            else if (portal >= 0)
            {
                entity = PortalBoxColor;
                label = PortalLabelPrefix + draft.Portals[portal].Pair;
            }

            cell.Render(frame, terrain, entity, label);
        }

        private static Color TerrainColor(SharedCell shared, bool isUniqueWall)
        {
            if (shared == SharedCell.Wall)
            {
                return SharedWallColor;
            }

            if (isUniqueWall)
            {
                return UniqueWallColor;
            }

            switch (shared)
            {
                case SharedCell.Pedestal:
                    return PedestalColor;
                case SharedCell.Goal:
                    return GoalColor;
                default:
                    return FloorColor;
            }
        }
    }
}
