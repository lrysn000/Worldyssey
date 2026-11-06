using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 正交相机取景的纯计算：恰好看全包围盒所需的正交尺寸、相机距离、像素网格对齐、鼠标射线与地面的交点。
    /// </summary>
    public static class CameraFraming
    {
        // 正交相机的远近只影响裁剪，不影响画面大小；留足余量，拖动和旋转时关卡不会被近裁剪面切掉
        private const float ExtraViewDistance = 20f;

        private static readonly Vector3[] CornerSigns =
        {
            new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f),
            new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(-1f, 1f, 1f), new Vector3(1f, 1f, 1f),
        };

        /// <summary>
        /// 让包围盒（四周加 <paramref name="margin"/>）的 8 个角都落在画面内的最小正交尺寸（半高）。
        /// 画面顶部 <paramref name="topReserve"/> 比例留给 HUD：相机上移 尺寸×topReserve，关卡可用的半高缩小为 尺寸×(1-topReserve)。
        /// </summary>
        public static float FitOrthoSize(float aspect, Quaternion rotation, Vector3 boundsExtents, float margin, float topReserve)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 extents = boundsExtents + new Vector3(margin, 0f, margin);
            float usableHeight = Mathf.Max(0.01f, 1f - topReserve);
            float required = 0.01f;
            for (int i = 0; i < CornerSigns.Length; i++)
            {
                Vector3 offset = Vector3.Scale(CornerSigns[i], extents);
                float x = Mathf.Abs(Vector3.Dot(offset, right));
                float y = Mathf.Abs(Vector3.Dot(offset, up));
                required = Mathf.Max(required, Mathf.Max(x / aspect, y / usableHeight));
            }

            return required;
        }

        /// <summary>相机到关卡中心的距离：保证整个关卡（含拖动范围）在近裁剪面之外。</summary>
        public static float ViewDistance(Camera camera, Vector3 boundsExtents)
        {
            return boundsExtents.magnitude * 2f + camera.nearClipPlane + ExtraViewDistance;
        }

        /// <summary>
        /// 把相机位置在自身的左右、上下方向上对齐到像素网格（每格 <paramref name="pixelWorldSize"/> 世界单位）。
        /// 静止物体因此每帧落在同样的像素上，移动时不会闪烁。<paramref name="offsetInPixels"/> 为对齐丢掉的零头（单位：像素），
        /// 放大到屏幕时按它反向偏移画面，相机运动依然平滑。
        /// </summary>
        public static Vector3 SnapToPixelGrid(Vector3 position, Quaternion rotation, float pixelWorldSize, out Vector2 offsetInPixels)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            float x = Vector3.Dot(position, right) / pixelWorldSize;
            float y = Vector3.Dot(position, up) / pixelWorldSize;
            float snappedX = Mathf.Round(x);
            float snappedY = Mathf.Round(y);
            offsetInPixels = new Vector2(x - snappedX, y - snappedY);
            return position + (right * (snappedX - x) + up * (snappedY - y)) * pixelWorldSize;
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
