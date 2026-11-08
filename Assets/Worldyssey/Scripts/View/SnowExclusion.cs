using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 让这个物体（含子物体）不积雪：底座、终点、传送箱子、独有墙、目标箱子、玩家。
    /// 用 MaterialPropertyBlock 把 _SnowMask 设为 0，不修改共享材质。其他脚本之后写入颜色时会先读取再写回，不会覆盖这一项。
    /// </summary>
    public class SnowExclusion : MonoBehaviour
    {
        private static readonly int SnowMaskId = Shader.PropertyToID("_SnowMask");

        private void Awake()
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer target in GetComponentsInChildren<Renderer>(true))
            {
                // 精灵（配对符号等）不是卡通材质，不用处理
                if (target is SpriteRenderer)
                {
                    continue;
                }

                target.GetPropertyBlock(block);
                block.SetFloat(SnowMaskId, 0f);
                target.SetPropertyBlock(block);
            }
        }
    }
}
