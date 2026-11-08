namespace Worldyssey.Data
{
    /// <summary>
    /// 教学步骤的开始或完成条件。由 TutorialDirector 根据每一步的结果判断。
    /// </summary>
    /// <remarks>值会被序列化到资源里，新增条件只能加在末尾，不能调整已有的数值。</remarks>
    public enum TutorialTrigger
    {
        /// <summary>作为开始条件时表示上一步完成后立即开始；作为完成条件时表示不会自动完成。</summary>
        None = 0,
        PlayerMoved = 1,
        PlayerTurned = 2,
        /// <summary>玩家移动或转身（任意方向键生效）。</summary>
        MovedOrTurned = 3,
        /// <summary>对着箱子按方向键，这一下只转身。</summary>
        TurnedToFaceBox = 4,
        MappedBoxPushed = 5,
        /// <summary>目标箱子被推到终点上。</summary>
        BoxOnGoal = 6,
        PortalBoxPushed = 7,
        /// <summary>传送箱子被推上底座。</summary>
        PortalOnPedestal = 8,
        Teleported = 9,
        /// <summary>传送后回程箱子在目标世界生成或被拉过来。</summary>
        PartnerArrived = 10,
        /// <summary>有世界从正常变为冲突。</summary>
        WorldLocked = 11,
        /// <summary>有世界从冲突恢复正常。</summary>
        WorldUnlocked = 12,
        Undone = 13,
        Restarted = 14,
        /// <summary>只用作完成条件：显示满 <see cref="TutorialStep.Seconds"/> 秒后完成。</summary>
        AfterSeconds = 15,
    }
}
