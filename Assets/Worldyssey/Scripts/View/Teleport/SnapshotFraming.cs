using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 计算截图转场的起始机位（正交相机）：朝向固定、画面正中对准目标时，正交尺寸多小才能让目标铺满整个画面。
    /// </summary>
    public static class SnapshotFraming
    {
        private const int SearchSteps = 24;
        private const float MinSize = 0.01f;
        private static readonly Vector2[] CornerSigns = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };

        /// <summary>
        /// 返回画面四个角的视线（正交相机下互相平行）都能打在 <paramref name="target"/> 上的最大正交尺寸（半高）。
        /// </summary>
        public static float FindCoveringOrthoSize(Bounds target, Quaternion rotation, Camera camera)
        {
            // 尺寸越小越容易盖满，所以“能盖满”对尺寸是单调的，可以二分；正中那条视线一定打在目标上
            float low = MinSize;
            float high = Mathf.Max(MinSize, target.size.magnitude);
            for (int i = 0; i < SearchSteps; i++)
            {
                float middle = (low + high) * 0.5f;
                if (Covers(target, rotation, camera.aspect, middle))
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private static bool Covers(Bounds target, Quaternion rotation, float aspect, float size)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 origin = target.center - forward * (target.size.magnitude + 1f);
            for (int i = 0; i < CornerSigns.Length; i++)
            {
                Vector3 offset = rotation * new Vector3(CornerSigns[i].x * size * aspect, CornerSigns[i].y * size, 0f);
                if (!target.IntersectRay(new Ray(origin + offset, forward)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
