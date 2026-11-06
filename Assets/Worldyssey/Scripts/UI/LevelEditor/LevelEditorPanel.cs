using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Worldyssey.Core;
using Worldyssey.Gameplay;

namespace Worldyssey.UI
{
    /// <summary>
    /// 关卡编辑器主界面：把各子控件的操作转给 <see cref="LevelEditorModel"/>，按返回结果刷新显示和提示；
    /// 负责保存、打开等文件操作。试玩和返回交给 GameFlow。
    /// </summary>
    public class LevelEditorPanel : UiPanel
    {
        [SerializeField] private EditorGridView _grid;
        [SerializeField] private EditorToolbar _toolbar;
        [SerializeField] private EditorHeaderBar _header;
        [SerializeField] private EditorPairList _pairList;
        [SerializeField] private EditorActionBar _actionBar;
        [SerializeField] private EditorOpenDialog _openDialog;
        [SerializeField] private PlayerInputReader _inputReader;

        private List<CustomLevelEntry> _openableLevels = new List<CustomLevelEntry>();
        private SaveService _saveService;
        private LevelEditorModel _model;
        private bool _hasReferences;

        /// <summary>请求试玩，参数为当前草稿转出的关卡数据（已确认没有校验错误）。</summary>
        public event Action<LevelData> TestPlayRequested;

        public event Action BackRequested;

        /// <summary>
        /// 注入存档服务，用于保存和打开自建关卡。由 GameFlow 在启动时调用。
        /// </summary>
        public void Initialize(SaveService saveService)
        {
            _saveService = saveService;
        }

        /// <summary>
        /// 打开一个已有关卡继续编辑（从选关页的“编辑”进入）。面板未显示时也可以调用，显示时直接展示这一关。
        /// 返回是否成功；失败时保留原来的草稿。
        /// </summary>
        public bool OpenLevel(LevelData level)
        {
            LevelDraft draft;
            try
            {
                draft = LevelDraft.FromLevelData(level);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"[{nameof(LevelEditorPanel)}] 打开关卡 '{level.Id}' 失败：{exception.Message}", this);
                return false;
            }

            if (_model == null)
            {
                _model = new LevelEditorModel(draft);
            }
            else
            {
                _model.Load(draft);
            }

            if (isActiveAndEnabled)
            {
                RefreshAll();
            }

            _actionBar.SetStatus(string.Format(UiText.StatusOpenedFormat, draft.DisplayName));
            return true;
        }

        private void Awake()
        {
            _hasReferences = CheckReference(_grid, nameof(_grid))
                & CheckReference(_toolbar, nameof(_toolbar))
                & CheckReference(_header, nameof(_header))
                & CheckReference(_pairList, nameof(_pairList))
                & CheckReference(_actionBar, nameof(_actionBar))
                & CheckReference(_openDialog, nameof(_openDialog))
                & CheckReference(_inputReader, nameof(_inputReader));
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _grid.StrokeStarted += HandleStrokeStarted;
            _grid.StrokeContinued += HandleStrokeContinued;
            _grid.StrokeEnded += HandleStrokeEnded;
            _toolbar.ToolSelected += HandleToolSelected;
            _header.NameEdited += HandleNameEdited;
            _header.ResizeRequested += HandleResize;
            _header.WorldSelected += HandleWorldSelected;
            _header.AddWorldRequested += HandleAddWorld;
            _header.RemoveWorldRequested += HandleRemoveWorld;
            _header.ThemeSelected += HandleThemeSelected;
            _pairList.PairSelected += HandlePairSelected;
            _pairList.TargetCycleRequested += HandleTargetCycle;
            _pairList.AddRequested += HandleAddPair;
            _pairList.RemoveRequested += HandleRemovePair;
            _actionBar.NewRequested += HandleNew;
            _actionBar.OpenRequested += HandleOpen;
            _actionBar.SaveRequested += HandleSave;
            _actionBar.TestPlayRequested += HandleTestPlay;
            _actionBar.BackRequested += HandleBack;
            _openDialog.LevelChosen += HandleOpenChosen;
            _openDialog.Cancelled += HandleOpenCancelled;
            _inputReader.UndoRequested += HandleUndo;
            _openDialog.SetVisible(false);

            // 草稿在面板关闭后保留：试玩回来或从主菜单再次进入时继续编辑
            if (_model == null)
            {
                _model = new LevelEditorModel(CreateNewDraft());
            }

            RefreshAll();
        }

        private void Update()
        {
            // 快捷键：Tab / Shift+Tab 切换世界，Ctrl+S 保存。在输入框里打字或打开对话框时不响应
            Keyboard keyboard = Keyboard.current;
            if (!_hasReferences || _model == null || keyboard == null || _openDialog.IsVisible || IsTypingInInputField())
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame && _model.Draft.WorldCount > 1)
            {
                int count = _model.Draft.WorldCount;
                int step = keyboard.shiftKey.isPressed ? -1 : 1;
                Present(_model.SelectWorld((_model.CurrentWorld + step + count) % count));
            }

            if (keyboard.ctrlKey.isPressed && keyboard.sKey.wasPressedThisFrame)
            {
                HandleSave();
            }
        }

        private void OnDisable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _grid.StrokeStarted -= HandleStrokeStarted;
            _grid.StrokeContinued -= HandleStrokeContinued;
            _grid.StrokeEnded -= HandleStrokeEnded;
            _toolbar.ToolSelected -= HandleToolSelected;
            _header.NameEdited -= HandleNameEdited;
            _header.ResizeRequested -= HandleResize;
            _header.WorldSelected -= HandleWorldSelected;
            _header.AddWorldRequested -= HandleAddWorld;
            _header.RemoveWorldRequested -= HandleRemoveWorld;
            _header.ThemeSelected -= HandleThemeSelected;
            _pairList.PairSelected -= HandlePairSelected;
            _pairList.TargetCycleRequested -= HandleTargetCycle;
            _pairList.AddRequested -= HandleAddPair;
            _pairList.RemoveRequested -= HandleRemovePair;
            _actionBar.NewRequested -= HandleNew;
            _actionBar.OpenRequested -= HandleOpen;
            _actionBar.SaveRequested -= HandleSave;
            _actionBar.TestPlayRequested -= HandleTestPlay;
            _actionBar.BackRequested -= HandleBack;
            _openDialog.LevelChosen -= HandleOpenChosen;
            _openDialog.Cancelled -= HandleOpenCancelled;
            _inputReader.UndoRequested -= HandleUndo;
        }

        private static LevelDraft CreateNewDraft()
        {
            string id = SaveService.GenerateCustomLevelId(DateTime.Now);
            return LevelDraft.CreateNew(id, id);
        }

        private void HandleStrokeStarted(GridPos pos, bool isErase)
        {
            // 按住 Shift 开始的一笔画直线（横向或纵向）
            bool isLine = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            Present(_model.BeginStroke(pos, isErase, isLine));
            _grid.SetLineLength(_model.LineLength);
        }

        private void HandleStrokeContinued(GridPos pos)
        {
            Present(_model.ContinueStroke(pos));
            _grid.SetLineLength(_model.LineLength);
        }

        private void HandleStrokeEnded()
        {
            _model.EndStroke();
            _grid.SetLineLength(0);
        }

        private void HandleToolSelected(EditorTool tool)
        {
            Present(_model.SelectTool(tool));
        }

        private void HandleNameEdited(string displayName)
        {
            Present(_model.SetDisplayName(displayName));
        }

        private void HandleResize(int deltaWidth, int deltaHeight)
        {
            Present(_model.Resize(deltaWidth, deltaHeight));
        }

        private void HandleWorldSelected(int world)
        {
            Present(_model.SelectWorld(world));
        }

        private void HandleAddWorld()
        {
            Present(_model.AddWorld());
        }

        private void HandleRemoveWorld()
        {
            Present(_model.RemoveCurrentWorld());
        }

        private void HandleThemeSelected(WorldTheme theme)
        {
            Present(_model.SetCurrentWorldTheme(theme));
        }

        private void HandlePairSelected(int pair)
        {
            Present(_model.SelectPair(pair));
        }

        private void HandleTargetCycle()
        {
            Present(_model.CycleTargetWorld());
        }

        private void HandleAddPair()
        {
            Present(_model.AddPair());
        }

        private void HandleRemovePair()
        {
            Present(_model.RemoveSelectedPair());
        }

        private void HandleUndo()
        {
            // 正在输入关卡名时，Z 是普通字符，不是撤销
            if (IsVisible && !IsTypingInInputField())
            {
                Present(_model.Undo());
            }
        }

        private void HandleNew()
        {
            _model.Load(CreateNewDraft());
            RefreshAll();
            _actionBar.SetStatus(UiText.StatusNewLevel);
        }

        private void HandleOpen()
        {
            var problems = new List<string>();
            _openableLevels = _saveService != null ? _saveService.LoadCustomLevels(problems) : new List<CustomLevelEntry>();
            for (int i = 0; i < problems.Count; i++)
            {
                Debug.LogWarning($"[{nameof(LevelEditorPanel)}] 自建关卡已跳过：{problems[i]}", this);
            }

            if (_openableLevels.Count == 0)
            {
                _actionBar.SetStatus(UiText.StatusNoCustomLevels);
                return;
            }

            _openDialog.Show(_openableLevels);
        }

        private void HandleOpenChosen(int index)
        {
            _openDialog.SetVisible(false);
            LevelData level = _openableLevels[index].Level;
            try
            {
                _model.Load(LevelDraft.FromLevelData(level));
            }
            catch (ArgumentException exception)
            {
                // 列表里的关卡已通过校验，解析失败说明校验器与 LevelMap 不一致，必须暴露
                Debug.LogError($"[{nameof(LevelEditorPanel)}] 打开关卡 '{level.Id}' 失败：{exception.Message}", this);
                _actionBar.SetStatus(UiText.StatusOpenFailed);
                return;
            }

            RefreshAll();
            _actionBar.SetStatus(string.Format(UiText.StatusOpenedFormat, _model.Draft.DisplayName));
        }

        private void HandleOpenCancelled()
        {
            _openDialog.SetVisible(false);
        }

        private void HandleSave()
        {
            if (_model.Report.HasErrors)
            {
                _actionBar.SetStatus(UiText.StatusFixErrors);
                return;
            }

            if (_saveService == null)
            {
                Debug.LogError($"[{nameof(LevelEditorPanel)}] {name} 未初始化存档服务，无法保存", this);
                _actionBar.SetStatus(UiText.StatusSaveFailed);
                return;
            }

            try
            {
                string path = _saveService.SaveCustomLevel(_model.Draft.ToLevelData());
                Debug.Log($"[{nameof(LevelEditorPanel)}] 已保存：{path}", this);
                _actionBar.SetStatus(string.Format(UiText.StatusSavedFormat, _model.Draft.DisplayName));
            }
            catch (IOException exception)
            {
                Debug.LogError($"[{nameof(LevelEditorPanel)}] 保存失败：{exception.Message}", this);
                _actionBar.SetStatus(UiText.StatusSaveFailed);
            }
        }

        private void HandleTestPlay()
        {
            if (_model.Report.HasErrors)
            {
                _actionBar.SetStatus(UiText.StatusFixErrors);
                return;
            }

            TestPlayRequested?.Invoke(_model.Draft.ToLevelData());
        }

        private void HandleBack()
        {
            BackRequested?.Invoke();
        }

        private static bool IsTypingInInputField()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }

        /// <summary>
        /// 按操作结果决定刷新范围或显示提示。
        /// </summary>
        private void Present(EditorFeedback feedback)
        {
            switch (feedback)
            {
                case EditorFeedback.GridChanged:
                    RefreshGrid();
                    break;
                case EditorFeedback.AllChanged:
                    RefreshAll();
                    break;
                case EditorFeedback.SelectPairFirst:
                    _actionBar.SetStatus(UiText.StatusSelectPair);
                    break;
                case EditorFeedback.PortalRejected:
                    _actionBar.SetStatus(UiText.StatusPortalRejected);
                    break;
                case EditorFeedback.PairRejected:
                    _actionBar.SetStatus(UiText.StatusPairRejected);
                    break;
                case EditorFeedback.NothingToUndo:
                    _actionBar.SetStatus(UiText.StatusNothingToUndo);
                    break;
            }
        }

        private void RefreshAll()
        {
            RefreshGrid();
            _toolbar.SetSelected(_model.Tool);
            _grid.SetBrush(_model.Tool, _model.SelectedPair);
            _header.Render(_model.Draft, _model.CurrentWorld);
            _pairList.Render(_model.Draft, _model.SelectedPair, _model.TargetWorld);
        }

        /// <summary>
        /// 只重绘网格和问题列表。绘制格子时只做这一步，不重建配对列表等控件。
        /// </summary>
        private void RefreshGrid()
        {
            _grid.Render(_model.Draft, _model.CurrentWorld, _model.Report);
            _actionBar.RenderIssues(_model.Report);
        }
    }
}
