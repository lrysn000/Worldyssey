using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 按整数（或半整数）倍缩放界面：界面按 1080p 设计，素材 1 像素 = 4 单位、文字 1 像素 = 2~4 单位，
    /// 缩放倍数取 0.5 的整数倍时这些都能落在整数个屏幕像素上，像素素材和像素字在任何分辨率下都粗细一致。
    /// 代价是界面大小按档位变化（例如 1440p 下与 1080p 同样大小，四周留出更多空间）。
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class PixelCanvasScaler : MonoBehaviour
    {
        [Tooltip("界面的设计高度（像素）")]
        [Min(1f)]
        [SerializeField] private float _referenceHeight = 1080f;

        [Tooltip("缩放倍数的档位间隔。0.5 时 2 单位的像素总是整数个屏幕像素")]
        [Range(0.25f, 1f)]
        [SerializeField] private float _step = 0.5f;

        [Tooltip("最小缩放倍数，屏幕很小时不再继续缩小")]
        [Min(0.25f)]
        [SerializeField] private float _minScale = 0.5f;

        private CanvasScaler _scaler;
        private int _lastScreenHeight = -1;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        private void OnEnable()
        {
            _lastScreenHeight = -1;
        }

        private void Update()
        {
            // 只在分辨率变化时重新计算，平时只比较一个整数
            if (Screen.height == _lastScreenHeight)
            {
                return;
            }

            _lastScreenHeight = Screen.height;
            float steps = Mathf.Floor(Screen.height / _referenceHeight / _step);
            _scaler.scaleFactor = Mathf.Max(_minScale, steps * _step);
        }
    }
}
