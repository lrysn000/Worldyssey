namespace Worldyssey.View
{
    /// <summary>
    /// <see cref="EntityView"/> 播放的表现动作，角色模型据此播放对应的骨骼动画（见 <see cref="CharacterActionAnimator"/>）。
    /// </summary>
    public enum EntityAction
    {
        /// <summary>钻进传送箱子。</summary>
        Vanish = 0,

        /// <summary>从伙伴箱子里走出来。</summary>
        Emerge = 1,

        /// <summary>过关庆祝。</summary>
        Celebrate = 2,

        /// <summary>效果被立即停止（撤销、重开、换关），动画应回到站立。</summary>
        Reset = 3,
    }
}
