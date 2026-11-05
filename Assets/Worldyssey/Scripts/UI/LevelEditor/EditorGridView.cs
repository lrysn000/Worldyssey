using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 关卡编辑器的俯视网格：显示当前世界的全部内容，用外框标出校验问题，并把鼠标绘制转成笔画事件。
    /// 左键用当前画笔，右键总是橡皮擦；按住拖动连续绘制，一次按下到松开为一笔。
    /// 配色来自 <see cref="EditorGridPalette"/>；独有墙用当前世界的季节色，传送箱子用目标世界的季节色。
    /// </summary>
    public class EditorGridView : MonoBehaviour
    {
        private static readonly Color Hidden = new Color(0f, 0f, 0f, 0f);
        private const string PortalLabelPrefix = "P";

        [SerializeField] private RectTransform _gridArea;
        [SerializeField] private EditorGridCell _cellPrefab;

        [Tooltip("只读：只显示不响应鼠标，用于选关页的小地图")]
        [SerializeField] private bool _isReadOnly;
        [SerializeField] private EditorGridPalette _palette;

        [Tooltip("季节色：独有墙和传送箱子按世界主题着色")]
        [SerializeField] private WorldPalette _worldPalette;

        [Header("实体图标（可选，不设置时显示色块）")]
        [Tooltip("尖端朝上的玩家图标，按朝向旋转")]
        [SerializeField] private Sprite _playerIcon;
        [SerializeField] private Sprite _boxIcon;
        [SerializeField] private Sprite _portalIcon;

        [Tooltip("可选：绘制时格子弹跳的幅度和时长。不设置则不弹")]
        [SerializeField] private UiMotionConfig _motionConfig;

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
            if (_gridArea == null || _cellPrefab == null || _palette == null || _worldPalette == null)
            {
                Debug.LogError($"[{nameof(EditorGridView)}] {name} 缺少网格区域、格子预制体或配色引用", this);
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
            if (_gridArea == null || _cellPrefab == null || _palette == null || _worldPalette == null)
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
            if (_isReadOnly || (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right))
            {
                return;
            }

            _isStroking = true;
            PopCell(cell);
            StrokeStarted?.Invoke(cell.Position, button == PointerEventData.InputButton.Right);
        }

        internal void HandleCellEnter(EditorGridCell cell)
        {
            if (_isStroking)
            {
                PopCell(cell);
                StrokeContinued?.Invoke(cell.Position);
            }
        }

        internal void HandleCellUp()
        {
            EndStroke();
        }

        private void PopCell(EditorGridCell cell)
        {
            if (_motionConfig != null)
            {
                cell.Pop(_motionConfig.CellPopScale, _motionConfig.CellPopDuration);
            }
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
                cell.SetInteractable(!_isReadOnly);
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
            Color frame = _errorCells[index] ? _palette.ErrorFrame : _warningCells[index] ? _palette.WarningFrame : _palette.Frame;
            Color terrain = TerrainColor(draft, world, pos);

            Color entity = Hidden;
            Sprite icon = null;
            float angle = 0f;
            string label = string.Empty;
            int portal = draft.FindPortal(world, pos);
            if (world == draft.StartWorld && pos == draft.PlayerPosition)
            {
                // 朝向按 上、右、下、左 排列，图标尖端朝上，顺时针每档转 90°（UI 中顺时针为负角度）
                entity = _palette.Player;
                icon = _playerIcon;
                angle = -90f * (int)draft.PlayerFacing;
            }
            else if (draft.FindMappedBox(pos) >= 0)
            {
                entity = _palette.MappedBox;
                icon = _boxIcon;
            }
            else if (portal >= 0)
            {
                int pairIndex = draft.Portals[portal].Pair;
                DraftPair pair = draft.Pairs[pairIndex];
                int target = pair.WorldA == world ? pair.WorldB : pair.WorldA;
                entity = SeasonColor(draft, target, _palette.PortalBoxFallback);
                icon = _portalIcon;
                label = PortalLabelPrefix + pairIndex;
            }

            cell.Render(frame, terrain, entity, icon, angle, label);
        }

        private Color TerrainColor(LevelDraft draft, int world, GridPos pos)
        {
            SharedCell shared = draft.GetSharedCell(pos);
            if (shared == SharedCell.Wall)
            {
                return _palette.SharedWall;
            }

            if (draft.IsUniqueWall(world, pos))
            {
                return SeasonColor(draft, world, _palette.UniqueWallFallback);
            }

            switch (shared)
            {
                case SharedCell.Pedestal:
                    return _palette.Pedestal;
                case SharedCell.Goal:
                    return _palette.Goal;
                default:
                    return _palette.Floor;
            }
        }

        private Color SeasonColor(LevelDraft draft, int world, Color fallback)
        {
            return world >= 0 && world < draft.WorldCount && _worldPalette.TryGetColor((int)draft.GetWorldTheme(world), out Color color)
                ? color
                : fallback;
        }
    }
}
