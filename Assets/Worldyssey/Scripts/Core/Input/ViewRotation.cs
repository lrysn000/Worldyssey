namespace Worldyssey.Core
{
    /// <summary>
    /// 视角旋转与方向换算。玩家按的是“屏幕方向”，规则要的是“网格方向”，两者相差相机绕竖直轴转过的 90° 次数。
    /// </summary>
    /// <remarks>
    /// <c>cameraQuarterTurns</c> 为相机从默认朝向（看向网格 +y）起顺时针（俯视）转过的 90° 次数。
    /// 相机顺时针转时，画面里的场景看起来是逆时针转。
    /// </remarks>
    public static class ViewRotation
    {
        public const int QuarterTurnCount = 4;

        /// <summary>
        /// 把任意整数次数规整到 0~3。
        /// </summary>
        public static int Normalize(int cameraQuarterTurns)
        {
            return ((cameraQuarterTurns % QuarterTurnCount) + QuarterTurnCount) % QuarterTurnCount;
        }

        /// <summary>
        /// 屏幕方向换算为网格方向。例如相机顺时针转 90° 后，屏幕上方对应网格的右方。
        /// </summary>
        public static Direction ScreenToGrid(Direction screenDirection, int cameraQuarterTurns)
        {
            return (Direction)(((int)screenDirection + Normalize(cameraQuarterTurns)) % QuarterTurnCount);
        }
    }
}
