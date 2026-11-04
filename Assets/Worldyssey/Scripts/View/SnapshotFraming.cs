using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 计算截图转场的起始机位：朝向固定时，相机离目标多近才能让目标铺满整个画面。
    /// </summary>
    public static class SnapshotFraming
    {
        private const int SearchSteps = 24;
        private const float MaxDistanceFactor = 20f;
        private static readonly Vector2[] CornerSigns = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };

        /// <summary>
        /// 返回画面四个角的视线都能打在 <paramref name="target"/> 上的最远距离（从目标中心沿视线反方向量）。
        /// 相机至少在目标外面；离到最近也盖不满时返回最近距离，此时画面边缘会露出一点新世界。
        /// </summary>
        public static float FindCoveringDistance(Bounds target, Quaternion rotation, Camera camera)
        {
            float tanVertical = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanHorizontal = tanVertical * camera.aspect;
            float near = target.extents.magnitude + camera.nearClipPlane;
            if (!Covers(target, rotation, tanHorizontal, tanVertical, near))
            {
                return near;
            }

            // 离得越近越容易盖满，所以“能盖满”对距离是单调的，可以二分
            float low = near;
            float high = near + target.size.magnitude * MaxDistanceFactor;
            for (int i = 0; i < SearchSteps; i++)
            {
                float middle = (low + high) * 0.5f;
                if (Covers(target, rotation, tanHorizontal, tanVertical, middle))
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

        private static bool Covers(Bounds target, Quaternion rotation, float tanHorizontal, float tanVertical, float distance)
        {
            Vector3 origin = target.center - rotation * Vector3.forward * distance;
            for (int i = 0; i < CornerSigns.Length; i++)
            {
                Vector3 direction = rotation * new Vector3(CornerSigns[i].x * tanHorizontal, CornerSigns[i].y * tanVertical, 1f);
                if (!target.IntersectRay(new Ray(origin, direction)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
