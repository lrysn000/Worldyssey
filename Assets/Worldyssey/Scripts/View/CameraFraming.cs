using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 相机取景的纯计算：给定朝向和包围盒，求恰好完整看到它所需的距离。
    /// </summary>
    public static class CameraFraming
    {
        // 包围盒 8 个角的符号组合，用于逐角计算所需距离
        private static readonly Vector3[] CornerSigns =
        {
            new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f),
            new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(-1f, 1f, 1f), new Vector3(1f, 1f, 1f),
        };

        /// <summary>
        /// 让包围盒（四周加 <paramref name="margin"/>）的 8 个角都落在视锥内所需的最小距离。
        /// 对每个角：横向偏移 / tan(半水平视角) 与纵向偏移 / tan(半垂直视角) 取大者，再减去该角在视线方向上的深度。
        /// 画面顶部 <paramref name="topReserve"/> 比例的区域留给 HUD，纵向可用的半视角相应缩小。
        /// </summary>
        public static float FitDistance(Camera camera, Quaternion rotation, Vector3 boundsExtents, float margin, float topReserve)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;
            float tanHalfVertical = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanHalfHorizontal = tanHalfVertical * camera.aspect;
            tanHalfVertical *= 1f - topReserve;

            Vector3 extents = boundsExtents + new Vector3(margin, 0f, margin);
            float required = 0f;
            for (int i = 0; i < CornerSigns.Length; i++)
            {
                Vector3 offset = Vector3.Scale(CornerSigns[i], extents);
                float x = Mathf.Abs(Vector3.Dot(offset, right));
                float y = Mathf.Abs(Vector3.Dot(offset, up));
                float depth = Vector3.Dot(offset, forward);
                float needed = Mathf.Max(x / tanHalfHorizontal, y / tanHalfVertical) - depth;
                required = Mathf.Max(required, needed);
            }

            // 不能贴近到近裁剪面以内
            return Mathf.Max(required, camera.nearClipPlane + extents.magnitude);
        }

        /// <summary>鼠标射线与高度为 <paramref name="groundY"/> 的水平面的交点。射线不朝向地面时返回 false。</summary>
        public static bool TryGetGroundPoint(Camera camera, Vector2 screenPosition, float groundY, out Vector3 point)
        {
            point = default;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            var ground = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            if (!ground.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }
    }
}
