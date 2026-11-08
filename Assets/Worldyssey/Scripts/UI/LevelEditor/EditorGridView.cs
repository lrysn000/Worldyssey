using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 关卡编辑器的俯视网格：显示当前世界的全部内容，用外框标出校验问题，并把鼠标绘制转成笔画事件。
    /// 左键用当前画笔，右键总是橡皮擦；按住拖动连续绘制，一次按下到松开为一笔。
    /// 每格显示什么由 <see cref="EditorCellPainter"/> 计算；鼠标悬停时预览当前画笔的效果（<see cref="EditorGridHover"/>）。
    /// </summary>
    public class EditorGridView : MonoBehaviour
    {
        [SerializeField] private RectTransform _gridArea;
        [SerializeField] private EditorGridCell _cellPrefab;

        [Tooltip("只读：只显示不响应鼠标，用于选关页的小地图")]
        [SerializeField] private bool _isReadOnly;
        [SerializeField] private EditorGridPalette _palette;

        [Tooltip("季节色：独有墙和传送箱子按世界主题着色")]
        [SerializeField] private WorldPalette _worldPalette;

        [Header("地形图标（可选）：叠在地形色块上，空地不显示")]
        [SerializeField] private Sprite _wallIcon;
        [SerializeField] private Sprite _uniqueWallIcon;
        [SerializeField] private Sprite _pedestalIcon;
        [SerializeField] private Sprite _goalIcon;

        [Header("实体图标（可选，不设置时显示色块）")]
        [Tooltip("尖端朝上的玩家图标，按朝向旋转")]
        [SerializeField] private Sprite _playerIcon;
        [SerializeField] private Sprite _boxIcon;
        [SerializeField] private Sprite _portalIcon;

        [Tooltip("可选：配对符号，画在传送箱子格子的右下角")]
        [SerializeField] private PairSymbolSet _pairSymbols;

        [Tooltip("按 EditorTool 顺序排列的画笔图标，用于悬停预览")]
        [SerializeField] private Sprite[] _toolIcons = new Sprite[0];

        [Tooltip("可选：显示悬停格子坐标的文字")]
        [SerializeField] private TMP_Text _coordinateText;

        [Tooltip("可选：绘制时格子弹跳的幅度和时长。不设置则不弹")]
        [SerializeField] private UiMotionConfig _motionConfig;

        private readonly List<EditorGridCell> _cells = new List<EditorGridCell>();
        private readonly EditorIssueMarks _issueMarks = new EditorIssueMarks();
        private EditorCellPainter _painter;
        private EditorGridHover _hover;
        private EditorGridCell _hoveredCell;
        private LevelDraft _draft;
        private int _world;
        private EditorTool _brush;
        private int _selectedPair = -1;
        private int _lineLength;
        private bool _isStroking;

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
                return;
            }

            EnsureHelpers();
        }

        private void OnDisable()
        {
            // 面板被关闭时收不到松开事件，结束未完成的一笔
            EndStroke();
        }

        /// <summary>按草稿内容重绘当前世界。格子按最大尺寸一次性创建，之后只复用。</summary>
        public void Render(LevelDraft draft, int world, LevelValidationReport report)
        {
            // 选关页会在面板还没激活（Awake 未执行）时就绘制小地图，所以这里按需创建
            if (!EnsureHelpers())
            {
                return;
            }

            _draft = draft;
            _world = world;
            EnsureCells();
            _issueMarks.Mark(draft, world, report);

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

            RefreshHover();
        }

        /// <summary>当前画笔和选中的配对，用于悬停预览。</summary>
        public void SetBrush(EditorTool tool, int selectedPair)
        {
            _brush = tool;
            _selectedPair = selectedPair;
            RefreshHover();
        }

        /// <summary>正在画的直线长度（格），大于 1 时坐标文字里显示长度。</summary>
        public void SetLineLength(int length)
        {
            _lineLength = length;
            RefreshHover();
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
            _hoveredCell = cell;
            RefreshHover();
            if (_isStroking)
            {
                PopCell(cell);
                StrokeContinued?.Invoke(cell.Position);
            }
        }

        internal void HandleCellExit(EditorGridCell cell)
        {
            if (_hoveredCell == cell)
            {
                _hoveredCell = null;
                RefreshHover();
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

        private bool EnsureHelpers()
        {
            if (_gridArea == null || _cellPrefab == null || _palette == null || _worldPalette == null)
            {
                return false;
            }

            if (_painter == null)
            {
                var icons = new EditorGridIconSet(_wallIcon, _uniqueWallIcon, _pedestalIcon, _goalIcon, _playerIcon, _boxIcon, _portalIcon, _pairSymbols);
                _painter = new EditorCellPainter(_palette, _worldPalette, icons);
            }

            if (_hover == null && !_isReadOnly)
            {
                _hover = new EditorGridHover(_gridArea, _coordinateText);
            }

            return true;
        }

        private void RenderCell(EditorGridCell cell, LevelDraft draft, int world, GridPos pos)
        {
            cell.Render(_painter.Paint(draft, world, pos, _issueMarks.FrameColor(pos, _palette)));
        }

        private void RefreshHover()
        {
            if (_hover == null)
            {
                return;
            }

            if (_hoveredCell == null || _draft == null || !_hoveredCell.gameObject.activeSelf)
            {
                _hover.Hide();
                return;
            }

            int toolIndex = (int)_brush;
            Sprite icon = toolIndex >= 0 && toolIndex < _toolIcons.Length ? _toolIcons[toolIndex] : null;
            Color color = _painter.PreviewColor(_draft, _world, _brush, _selectedPair);
            GridPos pos = _hoveredCell.Position;
            _hover.Show(_hoveredCell, color, icon, pos.X, pos.Y, _lineLength);
        }
    }
}
