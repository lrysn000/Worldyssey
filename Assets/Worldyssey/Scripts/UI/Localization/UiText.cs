namespace Worldyssey.UI
{
    /// <summary>
    /// 全部界面文案的英文原文，也是翻译表（Localization/Strings.csv）的查找键。
    /// 显示前一律经 <see cref="Localization.Translate"/> 换成当前语言；改了这里的英文，翻译表第一列要同步改。
    /// </summary>
    public static class UiText
    {
        public const string GameTitle = "WORLDYSSEY";

        // 主菜单
        public const string StartGame = "Start";
        public const string Quit = "Quit";
        public const string Settings = "Settings";

        // 设置
        public const string SettingsTitle = "Settings";
        public const string SettingsPlaceholder = "More options coming soon";
        public const string Close = "Close";
        public const string Fullscreen = "Fullscreen";
        public const string Resolution = "Resolution";
        public const string ResolutionFormat = "{0} x {1}";
        public const string MasterVolume = "Master Volume";
        public const string MusicVolume = "Music";
        public const string SfxVolume = "Sound Effects";
        public const string Language = "Language";
        public const string ControlsHeader = "Controls";
        public const string ControlLineFormat = "{0}    {1}";

        // 按键提示的条目名，顺序与 SettingsPanel 里的动作列表一致
        public const string ControlMoveUp = "Move Up";
        public const string ControlMoveDown = "Move Down";
        public const string ControlMoveLeft = "Move Left";
        public const string ControlMoveRight = "Move Right";
        public const string ControlTeleport = "Enter Portal";
        public const string ControlUndo = "Undo";
        public const string ControlRestart = "Restart";
        public const string ControlRotateLeft = "Rotate View Left";
        public const string ControlRotateRight = "Rotate View Right";
        public const string ControlPause = "Pause";
        public const string ControlPanCamera = "Pan Camera";
        public const string ControlPanCameraBinding = "Drag Mouse";

        // 关卡编辑器的操作写在代码里（不走输入资源），按键文字直接列出
        public const string EditorControlsHeader = "Level Editor";
        public static readonly (string Label, string Binding)[] EditorControlEntries =
        {
            ("Paint", "Left Click / Drag"),
            ("Erase", "Right Click / Drag"),
            ("Straight Line", "Shift + Drag"),
            ("Next World", "Tab"),
            ("Previous World", "Shift + Tab"),
            ("Previous / Next Pair", "Up / Down"),
            ("Save", "Ctrl + S"),
        };

        // 选关
        public const string SelectLevelTitle = "Select Level";
        public const string BuiltInLevelsHeader = "Levels";
        public const string CustomLevelsHeader = "Custom Levels";
        public const string NoCustomLevels = "No custom levels yet";
        public const string Back = "Back";
        public const string Locked = "Locked";
        public const string NotCleared = "Not cleared";
        public const string BestStepsFormat = "Personal Best: {0}";
        public const string NextLevelTag = "NEW";
        public const string NoLevels = "No levels";
        public const string LockedHint = "Clear the previous level to unlock";
        public const string ClearedProgressFormat = "Cleared {0} / {1}";
        public const string PlayLevel = "Play";
        public const string EditLevel = "Edit";
        public const string DeleteLevel = "Delete";
        public const string DeleteLevelConfirmFormat = "Delete \"{0}\"?\nThis cannot be undone.";

        // HUD
        public const string StepCountFormat = "Steps: {0}";
        public const string Undo = "Undo (Z)";
        public const string Restart = "Restart (R)";
        public const string Pause = "Pause (Esc)";
        public const string WorldConflictedHint = "Target world is in conflict";
        public const string ExitBlockedHint = "Exit is blocked on this side";
        public const string PedestalOccupiedHint = "Target pedestal is occupied";

        // 暂停
        public const string PausedTitle = "Paused";
        public const string Resume = "Resume";
        public const string LevelSelect = "Level Select";
        public const string MainMenu = "Main Menu";

        // 结算
        public const string LevelCompleteTitle = "Level Complete";
        public const string ResultStepsFormat = "Steps: {0}";
        public const string ResultBestFormat = "Personal Best: {0}";
        public const string NewBest = "New Personal Best!";
        public const string NextLevel = "Next Level";
        public const string Replay = "Replay";

        // 关卡编辑器
        public const string LevelEditor = "Level Editor";
        public const string BackToEditor = "Back to Editor";
        public const string ToolsHeader = "Tools";
        public const string PairsHeader = "Portal Pairs";
        public const string IssuesHeader = "Issues";
        public const string NamePlaceholder = "Level name";
        public const string SizeFormat = "{0} x {1}";
        public const string WorldTabFormat = "World {0}";
        public const string StartWorldMark = " *";
        public const string TargetWorldFormat = "Target: {0}";
        public const string AddPair = "Add Pair";
        public const string RemovePair = "Remove Pair";
        public const string PairTitleFormat = "Pair {0}";
        public const string PairDetailFormat = "{0} <-> {1}";
        public const string NewLevel = "New";
        public const string OpenLevel = "Open";
        public const string SaveLevel = "Save";
        public const string TestPlay = "Test Play";
        public const string OpenLevelTitle = "Open Level";
        public const string Cancel = "Cancel";
        public const string NoIssues = "No issues";
        public const string IssuesCountFormat = "Issues ({0})";
        public const string EditorCoordinateFormat = "X {0}  Y {1}";
        public const string EditorLineFormat = "X {0}  Y {1}  Len {2}";
        public const string ToolGroupTerrain = "Terrain";
        public const string ToolGroupWorld = "This World";
        public const string ToolGroupObjects = "Objects";
        public const string ErrorPrefix = "E: ";
        public const string WarningPrefix = "W: ";
        public const string MoreIssuesFormat = "... and {0} more";
        public const string StatusSavedFormat = "Saved: {0}";
        public const string StatusSaveFailed = "Save failed, see console";
        public const string StatusFixErrors = "Fix errors first";
        public const string StatusUnsolvable = "This level can't be solved, so it wasn't saved";
        public const string StatusSavedUncheckedFormat = "Saved: {0} (too complex to check if it can be solved)";
        public const string StatusSelectPair = "Select a pair first";
        public const string StatusPortalRejected = "This world is not in the pair, or the cell is taken";
        public const string StatusPairRejected = "Need two different worlds";
        public const string StatusPairLimitFormat = "At most {0} pairs";
        public const string StatusOpenFailed = "Could not open level, see console";
        public const string StatusNothingToUndo = "Nothing to undo";
        public const string StatusNewLevel = "New level";
        public const string StatusOpenedFormat = "Opened: {0}";
        public const string StatusNoCustomLevels = "No custom levels to open";

        // 编辑器画笔，顺序与 EditorTool 枚举一致
        public static readonly string[] EditorToolNames =
        {
            "Floor", "Wall", "Pedestal", "Goal", "Unique Wall", "Box", "Player", "Portal", "Eraser",
        };
    }
}
