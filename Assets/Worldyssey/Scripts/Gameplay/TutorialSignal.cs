using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 一次可能推进教学的动作：一步操作的结果，或者撤销、重开。判断它满足哪些 <see cref="TutorialTrigger"/>。
    /// </summary>
    public readonly struct TutorialSignal
    {
        private readonly StepResult _result;
        private readonly LevelMap _map;
        private readonly int _lockedWorlds;
        private readonly int _unlockedWorlds;
        private readonly TutorialTrigger _directTrigger;

        private TutorialSignal(StepResult result, LevelMap map, int lockedWorlds, int unlockedWorlds, TutorialTrigger directTrigger)
        {
            _result = result;
            _map = map;
            _lockedWorlds = lockedWorlds;
            _unlockedWorlds = unlockedWorlds;
            _directTrigger = directTrigger;
        }

        /// <summary>
        /// 一步操作。<paramref name="conflictsBefore"/> 和 <paramref name="conflictsAfter"/> 是按世界编号排列的冲突位掩码。
        /// </summary>
        public static TutorialSignal FromStep(StepResult result, LevelMap map, int conflictsBefore, int conflictsAfter)
        {
            return new TutorialSignal(result, map, conflictsAfter & ~conflictsBefore, conflictsBefore & ~conflictsAfter, TutorialTrigger.None);
        }

        /// <summary>撤销、重开这类没有操作结果的信号。</summary>
        public static TutorialSignal Direct(TutorialTrigger trigger)
        {
            return new TutorialSignal(null, null, 0, 0, trigger);
        }

        /// <summary>这个信号是否满足 <paramref name="trigger"/>。None 和 AfterSeconds 永远返回 false，由调用方处理。</summary>
        public bool Matches(TutorialTrigger trigger)
        {
            if (trigger == TutorialTrigger.None || trigger == TutorialTrigger.AfterSeconds)
            {
                return false;
            }

            if (_result == null)
            {
                return trigger == _directTrigger;
            }

            switch (trigger)
            {
                case TutorialTrigger.TurnedToFaceBox:
                    return _result.Failure == StepFailure.TurnedToFaceBox;
                case TutorialTrigger.WorldLocked:
                    return _lockedWorlds != 0;
                case TutorialTrigger.WorldUnlocked:
                    return _unlockedWorlds != 0;
                default:
                    return HasMatchingEvent(trigger);
            }
        }

        private bool HasMatchingEvent(TutorialTrigger trigger)
        {
            for (int i = 0; i < _result.Events.Count; i++)
            {
                if (EventMatches(_result.Events[i], trigger))
                {
                    return true;
                }
            }

            return false;
        }

        private bool EventMatches(StepEvent stepEvent, TutorialTrigger trigger)
        {
            switch (trigger)
            {
                case TutorialTrigger.PlayerMoved:
                    return stepEvent is PlayerMovedEvent;
                case TutorialTrigger.PlayerTurned:
                    return stepEvent is PlayerTurnedEvent;
                case TutorialTrigger.MovedOrTurned:
                    return stepEvent is PlayerMovedEvent || stepEvent is PlayerTurnedEvent;
                case TutorialTrigger.MappedBoxPushed:
                    return stepEvent is MappedBoxPushedEvent;
                case TutorialTrigger.BoxOnGoal:
                    return stepEvent is MappedBoxPushedEvent mapped && _map.IsGoal(mapped.To);
                case TutorialTrigger.PortalBoxPushed:
                    return stepEvent is PortalBoxPushedEvent;
                case TutorialTrigger.PortalOnPedestal:
                    return stepEvent is PortalBoxPushedEvent portal && _map.IsPedestal(portal.To);
                case TutorialTrigger.Teleported:
                    return stepEvent is TeleportedEvent;
                case TutorialTrigger.PartnerArrived:
                    return stepEvent is PartnerSpawnedEvent || stepEvent is PartnerMovedEvent;
                default:
                    return false;
            }
        }
    }
}
