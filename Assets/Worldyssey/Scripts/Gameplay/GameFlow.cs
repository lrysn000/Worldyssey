using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 界面流程状态机：主菜单 → 选关 → 游戏中 ⇄ 暂停 → 结算；以及主菜单 → 关卡编辑器 ⇄ 试玩。主菜单和暂停可以打开设置，关闭后回到原处。
    /// 负责切换面板、开关游戏输入、读写存档。
    /// 关卡规则和对局状态不在这里，分别在 Core 和 <see cref="LevelSession"/>。
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        private const int NoCatalogIndex = -1;

        [Header("数据")]
        [SerializeField] private LevelCatalog _catalog;

        [Header("游戏")]
        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private PlayerInputReader _inputReader;

        [Header("界面")]
        [SerializeField] private MainMenuPanel _mainMenuPanel;
        [SerializeField] private LevelSelectPanel _levelSelectPanel;
        [SerializeField] private HudPanel _hudPanel;
        [SerializeField] private PausePanel _pausePanel;
        [SerializeField] private ResultPanel _resultPanel;

        [Tooltip("可选：没有关卡编辑器的场景留空，主菜单不显示编辑器入口")]
        [SerializeField] private LevelEditorPanel _levelEditorPanel;

        [Tooltip("可选：没有设置面板的场景留空，主菜单和暂停的设置按钮不会生效")]
        [SerializeField] private SettingsPanel _settingsPanel;

        [Tooltip("可选：界面主题控制器，用来决定设置面板跟随世界还是用中性主题")]
        [SerializeField] private UiThemeController _uiThemeController;

        [Tooltip("可选：关卡进出场转场。留空时直接进入和离开关卡")]
        [SerializeField] private LevelTransitionPlayer _levelTransition;

        private readonly List<LevelSelectItem> _builtInItems = new List<LevelSelectItem>();
        private readonly List<LevelSelectItem> _customItems = new List<LevelSelectItem>();
        private readonly List<string> _catalogIds = new List<string>();
        private List<CustomLevelEntry> _customLevels = new List<CustomLevelEntry>();

        private SaveService _saveService;
        private ProgressTracker _progress;
        private GameFlowState _state;
        private GameFlowState _settingsReturnState;
        private LevelData _currentLevel;
        private string _currentRecordKey;
        private int _currentCatalogIndex = NoCatalogIndex;
        private bool _isTestPlay;
        private bool _hasReferences;

        public GameFlowState State => _state;

        private void Awake()
        {
            _hasReferences = CheckReference(_catalog, nameof(_catalog))
                & CheckReference(_levelSession, nameof(_levelSession))
                & CheckReference(_inputReader, nameof(_inputReader))
                & CheckReference(_mainMenuPanel, nameof(_mainMenuPanel))
                & CheckReference(_levelSelectPanel, nameof(_levelSelectPanel))
                & CheckReference(_hudPanel, nameof(_hudPanel))
                & CheckReference(_pausePanel, nameof(_pausePanel))
                & CheckReference(_resultPanel, nameof(_resultPanel));
            if (!_hasReferences)
            {
                enabled = false;
                return;
            }

            _saveService = new SaveService(Application.persistentDataPath);
            SaveData saveData = _saveService.LoadOrCreate(out string error);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[{nameof(GameFlow)}] {error}（{_saveService.SaveFilePath}）", this);
            }

            _progress = new ProgressTracker(saveData);
            if (_levelEditorPanel != null)
            {
                _levelEditorPanel.Initialize(_saveService);
            }
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _mainMenuPanel.StartRequested += HandleStartRequested;
            _mainMenuPanel.QuitRequested += HandleQuitRequested;
            _levelSelectPanel.BuiltInLevelChosen += HandleBuiltInLevelChosen;
            _levelSelectPanel.CustomLevelChosen += HandleCustomLevelChosen;
            _levelSelectPanel.CustomLevelEditRequested += HandleCustomLevelEditRequested;
            _levelSelectPanel.CustomLevelDeleteRequested += HandleCustomLevelDeleteRequested;
            _levelSelectPanel.BackRequested += HandleLevelSelectBack;
            _hudPanel.PauseRequested += HandlePauseToggle;
            _pausePanel.ResumeRequested += HandleResumeRequested;
            _pausePanel.RestartRequested += HandleRestartFromPause;
            _pausePanel.LevelSelectRequested += HandleBackToLevelSelect;
            _resultPanel.NextRequested += HandleNextRequested;
            _resultPanel.ReplayRequested += HandleReplayRequested;
            _resultPanel.LevelSelectRequested += HandleBackToLevelSelect;
            _inputReader.PauseRequested += HandlePauseToggle;
            _levelSession.LevelCompleted += HandleLevelCompleted;
            _mainMenuPanel.LevelEditorRequested += HandleLevelEditorRequested;
            if (_levelEditorPanel != null)
            {
                _levelEditorPanel.TestPlayRequested += HandleTestPlayRequested;
                _levelEditorPanel.BackRequested += HandleEditorBack;
            }

            _mainMenuPanel.SettingsRequested += HandleSettingsRequested;
            _pausePanel.SettingsRequested += HandleSettingsRequested;
            _pausePanel.MainMenuRequested += HandleMainMenuFromPause;
            if (_settingsPanel != null)
            {
                _settingsPanel.CloseRequested += HandleSettingsClosed;
            }
        }

        private void Start()
        {
            ShowMainMenu();
        }

        private void OnDisable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _mainMenuPanel.StartRequested -= HandleStartRequested;
            _mainMenuPanel.QuitRequested -= HandleQuitRequested;
            _levelSelectPanel.BuiltInLevelChosen -= HandleBuiltInLevelChosen;
            _levelSelectPanel.CustomLevelChosen -= HandleCustomLevelChosen;
            _levelSelectPanel.CustomLevelEditRequested -= HandleCustomLevelEditRequested;
            _levelSelectPanel.CustomLevelDeleteRequested -= HandleCustomLevelDeleteRequested;
            _levelSelectPanel.BackRequested -= HandleLevelSelectBack;
            _hudPanel.PauseRequested -= HandlePauseToggle;
            _pausePanel.ResumeRequested -= HandleResumeRequested;
            _pausePanel.RestartRequested -= HandleRestartFromPause;
            _pausePanel.LevelSelectRequested -= HandleBackToLevelSelect;
            _resultPanel.NextRequested -= HandleNextRequested;
            _resultPanel.ReplayRequested -= HandleReplayRequested;
            _resultPanel.LevelSelectRequested -= HandleBackToLevelSelect;
            _inputReader.PauseRequested -= HandlePauseToggle;
            _levelSession.LevelCompleted -= HandleLevelCompleted;
            _mainMenuPanel.LevelEditorRequested -= HandleLevelEditorRequested;
            if (_levelEditorPanel != null)
            {
                _levelEditorPanel.TestPlayRequested -= HandleTestPlayRequested;
                _levelEditorPanel.BackRequested -= HandleEditorBack;
            }

            _mainMenuPanel.SettingsRequested -= HandleSettingsRequested;
            _pausePanel.SettingsRequested -= HandleSettingsRequested;
            _pausePanel.MainMenuRequested -= HandleMainMenuFromPause;
            if (_settingsPanel != null)
            {
                _settingsPanel.CloseRequested -= HandleSettingsClosed;
            }
        }

        private void ShowMainMenu()
        {
            _levelSession.EndLevel();
            _mainMenuPanel.SetLevelEditorAvailable(_levelEditorPanel != null);
            SetPanels(GameFlowState.MainMenu);
        }

        private void ShowLevelEditor()
        {
            _levelSession.EndLevel();
            _isTestPlay = false;
            SetPanels(GameFlowState.LevelEditor);
        }

        private void ShowLevelSelect()
        {
            _levelSession.EndLevel();
            RefreshLevelSelect();
            SetPanels(GameFlowState.LevelSelect);
        }

        private void RefreshLevelSelect()
        {
            RefreshCatalogIds();
            _builtInItems.Clear();
            bool hasNext = false;
            IReadOnlyList<LevelAsset> levels = _catalog.Levels;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == null)
                {
                    continue;
                }

                LevelData level = levels[i].Level;
                bool isLocked = !_progress.IsUnlocked(_catalogIds, i);
                int best = GetBestSteps(ProgressTracker.BuiltInKey(level.Id));

                // 第一个已解锁但未通关的关卡标为“下一关”
                bool isNext = !hasNext && !isLocked && best == LevelSelectItem.NoBestSteps;
                hasNext |= isNext;
                _builtInItems.Add(new LevelSelectItem(level, isLocked, best, isNext));
            }

            var problems = new List<string>();
            _customLevels = _saveService.LoadCustomLevels(problems);
            for (int i = 0; i < problems.Count; i++)
            {
                Debug.LogWarning($"[{nameof(GameFlow)}] 自建关卡已跳过：{problems[i]}", this);
            }

            _customItems.Clear();
            for (int i = 0; i < _customLevels.Count; i++)
            {
                LevelData level = _customLevels[i].Level;
                _customItems.Add(new LevelSelectItem(level, false, GetBestSteps(ProgressTracker.CustomKey(level.Id)), false));
            }

            _levelSelectPanel.Populate(_builtInItems, _customItems);
        }

        private void RefreshCatalogIds()
        {
            // 目录中的空项也占一个位置，保证下标与目录一致
            _catalogIds.Clear();
            IReadOnlyList<LevelAsset> levels = _catalog.Levels;
            for (int i = 0; i < levels.Count; i++)
            {
                _catalogIds.Add(levels[i] != null ? levels[i].Level.Id : string.Empty);
            }
        }

        private int GetBestSteps(string recordKey)
        {
            return _progress.TryGetBestSteps(recordKey, out int best) ? best : LevelSelectItem.NoBestSteps;
        }

        private void PlayBuiltInLevel(int catalogIndex)
        {
            RefreshCatalogIds();
            if (!_progress.IsUnlocked(_catalogIds, catalogIndex) || _catalog.Levels[catalogIndex] == null)
            {
                return;
            }

            LevelData level = _catalog.Levels[catalogIndex].Level;
            PlayLevel(level, ProgressTracker.BuiltInKey(level.Id), catalogIndex, false);
        }

        private void PlayLevel(LevelData level, string recordKey, int catalogIndex, bool isTestPlay)
        {
            if (!_levelSession.StartLevel(level))
            {
                // 具体原因已由 LevelSession 列出，这里回到来源界面
                if (isTestPlay)
                {
                    ShowLevelEditor();
                }
                else
                {
                    ShowLevelSelect();
                }

                return;
            }

            _currentLevel = level;
            _currentRecordKey = recordKey;
            _currentCatalogIndex = catalogIndex;
            _isTestPlay = isTestPlay;
            string exitLabel = isTestPlay ? UiText.BackToEditor : UiText.LevelSelect;
            _pausePanel.SetExitLabel(exitLabel);
            _resultPanel.SetExitLabel(exitLabel);
            SetPanels(GameFlowState.Playing);

            // 编辑器试玩是反复调整关卡的过程，不播放转场
            if (!isTestPlay)
            {
                BeginEntrance(false);
            }
        }

        /// <summary>
        /// 播放入场转场，期间关闭游戏输入，结束（或被跳过）后恢复。
        /// </summary>
        private void BeginEntrance(bool isShort)
        {
            if (_levelTransition == null)
            {
                return;
            }

            _levelSession.SetInputEnabled(false);
            _levelTransition.PlayEntrance(isShort, HandleEntranceFinished);
        }

        /// <summary>
        /// 淡出后再执行 <paramref name="next"/>（回选关、回主菜单、进下一关）。没有配置转场时直接执行。
        /// </summary>
        private void LeaveLevel(System.Action next)
        {
            if (_levelTransition == null)
            {
                next();
                return;
            }

            if (_levelTransition.IsPlayingExit)
            {
                return;
            }

            // 暂停和结算面板先收起，只留 HUD，画面淡出时不会有面板悬在上面
            _levelSession.SetInputEnabled(false);
            _pausePanel.SetVisible(false);
            _resultPanel.SetVisible(false);
            _levelTransition.PlayExit(next);
        }

        private void SetPanels(GameFlowState state)
        {
            // 入场途中暂停或离开时直接让入场结束，避免恢复游戏后转场还在播、输入却已打开
            if (state != GameFlowState.Playing && _levelTransition != null)
            {
                _levelTransition.CompleteEntrance();
            }

            _state = state;

            // 设置面板盖在打开它的界面上，原界面保持可见
            GameFlowState underlying = state == GameFlowState.Settings ? _settingsReturnState : state;
            bool isInLevel = underlying == GameFlowState.Playing || underlying == GameFlowState.Paused || underlying == GameFlowState.Result;
            _mainMenuPanel.SetVisible(underlying == GameFlowState.MainMenu);
            _levelSelectPanel.SetVisible(state == GameFlowState.LevelSelect);
            _hudPanel.SetVisible(isInLevel);
            _pausePanel.SetVisible(underlying == GameFlowState.Paused);
            if (_settingsPanel != null)
            {
                _settingsPanel.SetVisible(state == GameFlowState.Settings);
            }

            _resultPanel.SetVisible(state == GameFlowState.Result);
            if (_levelEditorPanel != null)
            {
                _levelEditorPanel.SetVisible(state == GameFlowState.LevelEditor);
            }

            _levelSession.SetInputEnabled(state == GameFlowState.Playing);
        }

        private bool HasNextBuiltInLevel()
        {
            return _currentCatalogIndex != NoCatalogIndex && _currentCatalogIndex + 1 < _catalog.Count;
        }

        private void SaveProgress()
        {
            try
            {
                _saveService.Save(_progress.Data);
            }
            catch (IOException exception)
            {
                // 写盘失败不影响继续游戏，但必须报告，否则玩家的进度会悄悄丢失
                Debug.LogError($"[{nameof(GameFlow)}] 保存存档失败：{exception.Message}（{_saveService.SaveFilePath}）", this);
            }
        }

        private void HandleStartRequested()
        {
            ShowLevelSelect();
        }

        private void HandleQuitRequested()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleBuiltInLevelChosen(int catalogIndex)
        {
            PlayBuiltInLevel(catalogIndex);
        }

        private void HandleCustomLevelChosen(int customIndex)
        {
            if (customIndex < 0 || customIndex >= _customLevels.Count)
            {
                return;
            }

            LevelData level = _customLevels[customIndex].Level;
            PlayLevel(level, ProgressTracker.CustomKey(level.Id), NoCatalogIndex, false);
        }

        private void HandleCustomLevelEditRequested(int customIndex)
        {
            if (_levelEditorPanel == null || customIndex < 0 || customIndex >= _customLevels.Count)
            {
                return;
            }

            if (_levelEditorPanel.OpenLevel(_customLevels[customIndex].Level))
            {
                ShowLevelEditor();
            }
        }

        /// <summary>删除自建关卡文件和它的通关记录，然后刷新列表。删除前的确认由选关页负责。</summary>
        private void HandleCustomLevelDeleteRequested(int customIndex)
        {
            if (customIndex < 0 || customIndex >= _customLevels.Count)
            {
                return;
            }

            CustomLevelEntry entry = _customLevels[customIndex];
            try
            {
                _saveService.DeleteCustomLevel(entry.FilePath);
            }
            catch (System.Exception exception) when (exception is System.IO.IOException || exception is System.UnauthorizedAccessException
                || exception is System.ArgumentException)
            {
                Debug.LogError($"[{nameof(GameFlow)}] 删除自建关卡失败（{entry.FilePath}）：{exception.Message}", this);
                return;
            }

            if (_progress.RemoveRecord(ProgressTracker.CustomKey(entry.Level.Id)))
            {
                SaveProgress();
            }

            RefreshLevelSelect();
        }

        private void HandleLevelSelectBack()
        {
            ShowMainMenu();
        }

        private void HandlePauseToggle()
        {
            // 淡出离场途中按 Esc 不应该回到游戏
            if (_levelTransition != null && _levelTransition.IsPlayingExit)
            {
                return;
            }

            if (_state == GameFlowState.Playing)
            {
                SetPanels(GameFlowState.Paused);
            }
            else if (_state == GameFlowState.Paused)
            {
                SetPanels(GameFlowState.Playing);
            }
            else if (_state == GameFlowState.Settings)
            {
                // Esc 在设置里表示返回，回到打开设置的界面，而不是直接回到游戏
                HandleSettingsClosed();
            }
        }

        private void HandleMainMenuFromPause()
        {
            // 关卡内进度不存档，直接结束关卡回主菜单
            if (_state != GameFlowState.Paused)
            {
                return;
            }

            if (_isTestPlay)
            {
                ShowMainMenu();
            }
            else
            {
                LeaveLevel(ShowMainMenu);
            }
        }

        private void HandleSettingsRequested()
        {
            if (_settingsPanel == null || (_state != GameFlowState.MainMenu && _state != GameFlowState.Paused))
            {
                return;
            }

            _settingsReturnState = _state;
            if (_uiThemeController != null)
            {
                _uiThemeController.SetSettingsFollowsWorld(_state == GameFlowState.Paused);
            }

            SetPanels(GameFlowState.Settings);
        }

        private void HandleSettingsClosed()
        {
            if (_state == GameFlowState.Settings)
            {
                SetPanels(_settingsReturnState);
            }
        }

        private void HandleResumeRequested()
        {
            if (_state == GameFlowState.Paused)
            {
                SetPanels(GameFlowState.Playing);
            }
        }

        private void HandleRestartFromPause()
        {
            _levelSession.RestartLevel();
            SetPanels(GameFlowState.Playing);
        }

        private void HandleBackToLevelSelect()
        {
            if (_isTestPlay)
            {
                ShowLevelEditor();
            }
            else
            {
                LeaveLevel(ShowLevelSelect);
            }
        }

        private void HandleEntranceFinished()
        {
            if (_state == GameFlowState.Playing)
            {
                _levelSession.SetInputEnabled(true);
            }
        }

        private void HandleLevelEditorRequested()
        {
            if (_levelEditorPanel != null)
            {
                ShowLevelEditor();
            }
        }

        private void HandleTestPlayRequested(LevelData level)
        {
            PlayLevel(level, null, NoCatalogIndex, true);
        }

        private void HandleEditorBack()
        {
            ShowMainMenu();
        }

        private void HandleLevelCompleted(int steps)
        {
            if (_state != GameFlowState.Playing && _state != GameFlowState.Paused)
            {
                return;
            }

            SetPanels(GameFlowState.Result);
            if (_isTestPlay)
            {
                // 试玩不记录成绩
                _resultPanel.ShowResult(steps, -1, false, false);
                return;
            }

            bool isNewBest = _progress.RecordCompletion(_currentRecordKey, steps);
            SaveProgress();
            _progress.TryGetBestSteps(_currentRecordKey, out int bestSteps);
            _resultPanel.ShowResult(steps, bestSteps, isNewBest, HasNextBuiltInLevel());
        }

        private void HandleNextRequested()
        {
            if (HasNextBuiltInLevel())
            {
                int nextIndex = _currentCatalogIndex + 1;
                LeaveLevel(() => PlayBuiltInLevel(nextIndex));
            }
        }

        private void HandleReplayRequested()
        {
            if (_currentLevel == null)
            {
                return;
            }

            _levelSession.RestartLevel();
            SetPanels(GameFlowState.Playing);
            if (!_isTestPlay)
            {
                BeginEntrance(true);
            }
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(GameFlow)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
