namespace Worldyssey.UI
{
    /// <summary>
    /// 全部界面文案。集中在这里，以后改语言或做本地化只需改这一处。
    /// </summary>
    /// <remarks>暂用英文：TextMeshPro 默认字体没有中文字形，中文字体后续统一处理。</remarks>
    public static class UiText
    {
        public const string GameTitle = "WORLDYSSEY";

        // 主菜单
        public const string StartGame = "Start";
        public const string Quit = "Quit";

        // 选关
        public const string SelectLevelTitle = "Select Level";
        public const string BuiltInLevelsHeader = "Levels";
        public const string CustomLevelsHeader = "Custom Levels";
        public const string NoCustomLevels = "No custom levels yet";
        public const string Back = "Back";
        public const string Locked = "Locked";
        public const string NotCleared = "Not cleared";
        public const string BestStepsFormat = "Best: {0}";

        // HUD
        public const string StepCountFormat = "Steps: {0}";
        public const string Solved = "Solved!";
        public const string ConflictPrefix = "Conflict: ";
        public const string ConflictSeparator = ", ";
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

        // 结算
        public const string LevelCompleteTitle = "Level Complete";
        public const string ResultStepsFormat = "Steps: {0}";
        public const string ResultBestFormat = "Best: {0}";
        public const string NewBest = "New best!";
        public const string NextLevel = "Next Level";
        public const string Replay = "Replay";
    }
}
