using System;

namespace Worldyssey.Core
{
    /// <summary>
    /// 关卡编辑器的编辑状态与操作：草稿、撤销栈、当前画笔、当前世界、目标世界和选中的配对。
    /// 界面只把用户操作转给这里，再按返回的 <see cref="EditorFeedback"/> 刷新显示和提示，因此编辑逻辑可以单元测试。
    /// </summary>
    public sealed class LevelEditorModel
    {
        public const int NoPair = -1;

        private readonly DraftHistory _history = new DraftHistory();
        private bool _isStrokeErase;
        private bool _hasStrokeChanged;

        public LevelEditorModel(LevelDraft draft)
        {
            Load(draft ?? throw new ArgumentNullException(nameof(draft)));
        }

        public LevelDraft Draft { get; private set; }
        public EditorTool Tool { get; private set; } = EditorTool.SharedWall;
        public int CurrentWorld { get; private set; }

        /// <summary>新增配对时与当前世界相连的另一个世界。</summary>
        public int TargetWorld { get; private set; }

        public int SelectedPair { get; private set; } = NoPair;

        /// <summary>当前草稿的校验结果，每次变化后更新。</summary>
        public LevelValidationReport Report { get; private set; }

        public int UndoCount => _history.Count;

        /// <summary>
        /// 换成另一份草稿（新建或打开），清空撤销历史，切到起始世界。
        /// </summary>
        public void Load(LevelDraft draft)
        {
            Draft = draft ?? throw new ArgumentNullException(nameof(draft));
            _history.Clear();
            CurrentWorld = draft.StartWorld;
            TargetWorld = NextWorld(CurrentWorld);
            SelectedPair = NoPair;
            Revalidate();
        }

        public EditorFeedback SelectTool(EditorTool tool)
        {
            Tool = tool;
            return EditorFeedback.AllChanged;
        }

        public EditorFeedback BeginStroke(GridPos pos, bool isErase)
        {
            _history.Push(Draft);
            _isStrokeErase = isErase;
            _hasStrokeChanged = false;
            return ApplyStroke(pos, true);
        }

        public EditorFeedback ContinueStroke(GridPos pos)
        {
            return ApplyStroke(pos, false);
        }

        public void EndStroke()
        {
            // 什么都没改的一笔不留撤销记录
            if (!_hasStrokeChanged)
            {
                _history.TryUndo(out _);
            }
        }

        public EditorFeedback SetDisplayName(string displayName)
        {
            Draft.DisplayName = displayName ?? string.Empty;
            Revalidate();
            return EditorFeedback.GridChanged;
        }

        public EditorFeedback SelectWorld(int world)
        {
            if (world < 0 || world >= Draft.WorldCount)
            {
                return EditorFeedback.None;
            }

            CurrentWorld = world;
            if (TargetWorld == CurrentWorld)
            {
                TargetWorld = NextWorld(CurrentWorld);
            }

            return EditorFeedback.AllChanged;
        }

        public EditorFeedback CycleTargetWorld()
        {
            TargetWorld = NextWorld(TargetWorld);
            if (TargetWorld == CurrentWorld)
            {
                TargetWorld = NextWorld(TargetWorld);
            }

            return EditorFeedback.AllChanged;
        }

        /// <summary>
        /// 选中一个配对，并切换到传送箱子画笔，方便紧接着摆放。
        /// </summary>
        public EditorFeedback SelectPair(int pair)
        {
            if (pair < 0 || pair >= Draft.Pairs.Count)
            {
                return EditorFeedback.None;
            }

            SelectedPair = pair;
            Tool = EditorTool.PortalBox;
            return EditorFeedback.AllChanged;
        }

        public EditorFeedback Resize(int deltaWidth, int deltaHeight)
        {
            return EditStructure(() => Draft.Resize(Draft.Width + deltaWidth, Draft.Height + deltaHeight));
        }

        public EditorFeedback AddWorld()
        {
            return EditStructure(() =>
            {
                int world = Draft.AddWorld();
                if (world < 0)
                {
                    return false;
                }

                CurrentWorld = world;
                TargetWorld = NextWorld(world);
                return true;
            });
        }

        /// <summary>
        /// 设置当前世界的主题；被其他世界占用时两者交换。可以撤销。
        /// </summary>
        public EditorFeedback SetCurrentWorldTheme(WorldTheme theme)
        {
            return EditStructure(() => Draft.SetWorldTheme(CurrentWorld, theme));
        }

        public EditorFeedback RemoveCurrentWorld()
        {
            return EditStructure(() =>
            {
                if (!Draft.RemoveWorld(CurrentWorld))
                {
                    return false;
                }

                CurrentWorld = Math.Min(CurrentWorld, Draft.WorldCount - 1);
                TargetWorld = NextWorld(CurrentWorld);
                SelectedPair = NoPair;
                return true;
            });
        }

        /// <summary>
        /// 新增连接当前世界与目标世界的配对，并选中它。
        /// </summary>
        public EditorFeedback AddPair()
        {
            if (TargetWorld == CurrentWorld || Draft.WorldCount < 2)
            {
                return EditorFeedback.PairRejected;
            }

            return EditStructure(() =>
            {
                int pair = Draft.AddPair(CurrentWorld, TargetWorld);
                if (pair < 0)
                {
                    return false;
                }

                SelectedPair = pair;
                Tool = EditorTool.PortalBox;
                return true;
            });
        }

        public EditorFeedback RemoveSelectedPair()
        {
            return EditStructure(() =>
            {
                if (!Draft.RemovePair(SelectedPair))
                {
                    return false;
                }

                SelectedPair = NoPair;
                return true;
            });
        }

        public EditorFeedback Undo()
        {
            if (!_history.TryUndo(out LevelDraft previous))
            {
                return EditorFeedback.NothingToUndo;
            }

            Draft = previous;
            CurrentWorld = Math.Min(CurrentWorld, Draft.WorldCount - 1);
            if (TargetWorld >= Draft.WorldCount || TargetWorld == CurrentWorld)
            {
                TargetWorld = NextWorld(CurrentWorld);
            }

            if (SelectedPair >= Draft.Pairs.Count)
            {
                SelectedPair = NoPair;
            }

            Revalidate();
            return EditorFeedback.AllChanged;
        }

        private EditorFeedback ApplyStroke(GridPos pos, bool isFirstCell)
        {
            EditorTool tool = _isStrokeErase ? EditorTool.Eraser : Tool;

            // 玩家和传送箱子是“放置一次”的操作，拖动经过其他格子时不重复执行
            bool isSingleShot = tool == EditorTool.Player || tool == EditorTool.PortalBox;
            if (isSingleShot && !isFirstCell)
            {
                return EditorFeedback.None;
            }

            if (tool == EditorTool.PortalBox && SelectedPair == NoPair)
            {
                return EditorFeedback.SelectPairFirst;
            }

            if (Draft.Apply(tool, CurrentWorld, pos, SelectedPair))
            {
                _hasStrokeChanged = true;
                Revalidate();

                // 放玩家会改变起始世界，世界标签上的标记也要更新
                return tool == EditorTool.Player ? EditorFeedback.AllChanged : EditorFeedback.GridChanged;
            }

            return tool == EditorTool.PortalBox ? EditorFeedback.PortalRejected : EditorFeedback.None;
        }

        /// <summary>
        /// 记录撤销快照后执行一次结构性编辑；没有变化时丢掉这份快照。
        /// </summary>
        private EditorFeedback EditStructure(Func<bool> edit)
        {
            _history.Push(Draft);
            if (!edit())
            {
                _history.TryUndo(out _);
                return EditorFeedback.None;
            }

            Revalidate();
            return EditorFeedback.AllChanged;
        }

        private void Revalidate()
        {
            Report = LevelValidator.Validate(Draft.ToLevelData());
        }

        private int NextWorld(int world)
        {
            return Draft.WorldCount == 0 ? 0 : (world + 1) % Draft.WorldCount;
        }
    }
}
