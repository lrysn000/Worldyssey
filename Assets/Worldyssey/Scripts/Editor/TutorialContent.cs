using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 第 1~3 章的默认教学文字。只教操作和规则（先转身再推、传送、回程箱子、选面进入、冲突），
    /// 推箱顺序、换世界推、先后规划这类策略留给玩家自己发现。坐标以左下角为原点，与关卡 JSON 一致。
    /// </summary>
    internal static class TutorialContent
    {
        private const int Summer = 1;

        internal static List<LevelTutorial> Build()
        {
            return new List<LevelTutorial>
            {
                Level("season-1-1",
                    Step("Use [W][A][S][D] or the arrow keys to move.", TutorialTrigger.None, TutorialTrigger.MovedOrTurned, Target(TutorialTargetKind.Player)),
                    Step("Walk into the box to push it. If you aren't facing it, the first press only turns you around.", TutorialTrigger.None, TutorialTrigger.MappedBoxPushed, TutorialTarget.MappedBox(0)),
                    Step("Push the box onto the goal to clear the level.", TutorialTrigger.None, TutorialTrigger.BoxOnGoal, Target(TutorialTargetKind.Goal))),

                Level("season-2-1",
                    Step("This glowing box is a portal. Push it onto the pedestal.", TutorialTrigger.None, TutorialTrigger.PortalOnPedestal, Target(TutorialTargetKind.PortalBox)),
                    Step("Face the portal and press [F] to travel to the world of its color.", TutorialTrigger.None, TutorialTrigger.Teleported, Target(TutorialTargetKind.PortalBox)),
                    Step("You are in Summer now. The panel at the top shows which world you are in.", TutorialTrigger.None, TutorialTrigger.AfterSeconds, Target(TutorialTargetKind.WorldIcon, Summer), 5f),
                    Step("Each world has its own walls. The hedge only grows in Spring, so the way is clear here.", TutorialTrigger.None, TutorialTrigger.BoxOnGoal, Target(TutorialTargetKind.Goal))),

                Level("season-2-2",
                    Step("Every world shares the same wooden boxes. Push one here and it moves in all of them.", TutorialTrigger.Teleported, TutorialTrigger.MappedBoxPushed, TutorialTarget.MappedBox(0))),

                Level("season-2-3",
                    Step("You arrive where you stand. Enter from a different side of the pedestal to land somewhere else.", TutorialTrigger.PortalOnPedestal, TutorialTrigger.Teleported, Target(TutorialTargetKind.PortalBox)),
                    Step("A return portal formed on your pedestal. Push it onto another pedestal to travel back from there.", TutorialTrigger.PartnerArrived, TutorialTrigger.Teleported, Target(TutorialTargetKind.PortalBox, 1))),

                Level("season-3-1",
                    Step("Summer is locked: a box is sitting where Summer has a wall, so its portal won't let you in.", TutorialTrigger.None, TutorialTrigger.AfterSeconds, Target(TutorialTargetKind.WorldIcon, Summer), 6f),
                    Step("Push the box off that spot to unlock Summer.", TutorialTrigger.None, TutorialTrigger.WorldUnlocked, TutorialTarget.MappedBox(0)),
                    Step("Boxes can pass through other worlds' walls, but that world stays locked while a box is inside one.", TutorialTrigger.WorldLocked, TutorialTrigger.AfterSeconds, TutorialTarget.None, 6f)),
            };
        }

        private static LevelTutorial Level(string levelId, params TutorialStep[] steps)
        {
            return new LevelTutorial(levelId, new List<TutorialStep>(steps));
        }

        private static TutorialStep Step(string text, TutorialTrigger startOn, TutorialTrigger completeOn, TutorialTarget target, float seconds = 4f)
        {
            return new TutorialStep(text, startOn, completeOn, target, seconds);
        }

        private static TutorialTarget Target(TutorialTargetKind kind, int index = 0)
        {
            return new TutorialTarget(kind, index, Vector2Int.zero);
        }
    }
}
