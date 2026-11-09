using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 界面动效的可调数值，所有按钮共用一份，保证手感一致。
    /// </summary>
    [CreateAssetMenu(fileName = "UiMotionConfig", menuName = "Worldyssey/UI/Motion Config")]
    public class UiMotionConfig : ScriptableObject
    {
        [Header("鼠标悬停")]
        [Tooltip("悬停时放大到原尺寸的多少倍")]
        [Range(1f, 1.3f)]
        [SerializeField] private float _hoverScale = 1.06f;

        [Tooltip("放大和恢复的时长（秒）")]
        [Min(0.01f)]
        [SerializeField] private float _hoverDuration = 0.12f;

        [Header("世界指示器")]
        [Tooltip("切换世界时当前世界图标从多少倍缩回原大小")]
        [Range(1f, 2f)]
        [SerializeField] private float _worldPopScale = 1.3f;

        [Tooltip("图标弹跳的时长（秒）")]
        [Min(0.01f)]
        [SerializeField] private float _worldPopDuration = 0.3f;

        [Header("关卡编辑器")]
        [Tooltip("点击或拖动绘制时，格子从多少倍缩回原大小")]
        [Range(1f, 1.5f)]
        [SerializeField] private float _cellPopScale = 1.15f;

        [Tooltip("格子弹跳的时长（秒）")]
        [Min(0.01f)]
        [SerializeField] private float _cellPopDuration = 0.15f;

        [Tooltip("状态提示（保存成功等）停留的时长（秒），之后淡出")]
        [Min(0f)]
        [SerializeField] private float _toastHoldDuration = 2.5f;

        [Tooltip("状态提示淡出的时长（秒）")]
        [Min(0.01f)]
        [SerializeField] private float _toastFadeDuration = 0.4f;

        public float HoverScale => _hoverScale;
        public float HoverDuration => _hoverDuration;
        public float WorldPopScale => _worldPopScale;
        public float WorldPopDuration => _worldPopDuration;
        public float CellPopScale => _cellPopScale;
        public float CellPopDuration => _cellPopDuration;
        public float ToastHoldDuration => _toastHoldDuration;
        public float ToastFadeDuration => _toastFadeDuration;
    }
}
