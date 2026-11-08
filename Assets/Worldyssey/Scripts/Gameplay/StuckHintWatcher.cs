using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 判断什么时候该给卡关提示：想进被锁住的世界（每次都提示）、箱子推进墙角、短时间内反复撤销（每次进关各一次）。
    /// 只负责判断和生成文字，显示由 <see cref="TutorialDirector"/> 负责。
    /// </summary>
    public sealed class StuckHintWatcher
    {
        private readonly TutorialLibrary _library;
        private bool _hasShownCornerHint;
        private bool _hasShownUndoHint;
        private int _undoCount;
        private float _undoWindowStart;

        public StuckHintWatcher(TutorialLibrary library)
        {
            _library = library;
        }

        /// <summary>进入新关卡时清空计数。</summary>
        public void Reset()
        {
            _hasShownCornerHint = false;
            _hasShownUndoHint = false;
            _undoCount = 0;
            _undoWindowStart = float.NegativeInfinity;
        }

        /// <summary>这一步有箱子被推进墙角（且不在终点上）时返回 true。</summary>
        public bool TryGetCornerHint(StepResult result, LevelMap map, out string message, out TutorialTarget target)
        {
            message = null;
            target = TutorialTarget.None;
            if (_hasShownCornerHint)
            {
                return false;
            }

            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is MappedBoxPushedEvent pushed && TutorialQueries.IsCorneredOffGoal(map, pushed.To))
                {
                    _hasShownCornerHint = true;
                    message = _library.CornerText;
                    target = TutorialTarget.MappedBox(pushed.BoxIndex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>传送因目标世界冲突失败时，说明是哪个世界、指出压住它的箱子。</summary>
        public bool TryGetLockedWorldHint(PlaySession session, out string message, out TutorialTarget target)
        {
            message = null;
            target = TutorialTarget.None;
            int world = TutorialQueries.GetFacedPortalWorld(session.Rules, session.State);
            if (world < 0)
            {
                return false;
            }

            int box = TutorialQueries.FindConflictingBox(session.Rules, session.State, world);
            if (box >= 0)
            {
                target = TutorialTarget.MappedBox(box);
            }

            message = string.Format(_library.LockedWorldFormat, session.Rules.Map.GetWorldName(world));
            return true;
        }

        /// <summary>记一次撤销，时间窗内次数达到阈值时返回 true。</summary>
        public bool TryGetUndoHint(float now, out string message)
        {
            message = null;
            if (now - _undoWindowStart > _library.UndoBurstWindow)
            {
                _undoWindowStart = now;
                _undoCount = 0;
            }

            _undoCount++;
            if (_hasShownUndoHint || _undoCount < _library.UndoBurstCount)
            {
                return false;
            }

            _hasShownUndoHint = true;
            message = _library.UndoBurstText;
            return true;
        }
    }
}
