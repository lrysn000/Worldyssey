namespace Worldyssey.Core
{
    /// <summary>
    /// 会进入输入缓存的命令种类。撤销和重开会打断动画立即执行，不在此列。
    /// </summary>
    public enum InputCommandType
    {
        Move = 0,
        Teleport = 1,
    }
}
