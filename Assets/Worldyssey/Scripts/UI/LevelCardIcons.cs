using UnityEngine;

namespace Worldyssey.UI
{
    /// <summary>
    /// 关卡卡片右上角的三种状态图标，由选关面板统一持有后传给每张卡片。
    /// </summary>
    public readonly struct LevelCardIcons
    {
        public LevelCardIcons(Sprite cleared, Sprite next, Sprite locked)
        {
            Cleared = cleared;
            Next = next;
            Locked = locked;
        }

        public Sprite Cleared { get; }
        public Sprite Next { get; }
        public Sprite Locked { get; }
    }
}
