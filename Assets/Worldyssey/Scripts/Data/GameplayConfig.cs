using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 玩法手感相关的可调数值。策划直接在资源上调参，不改代码。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Worldyssey/Gameplay Config")]
    public class GameplayConfig : ScriptableObject
    {
        [Header("动画")]
        [Tooltip("移动或推箱一格的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _moveDuration = 0.12f;

        [Tooltip("原地转向（撞墙）的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _turnDuration = 0.08f;

        [Tooltip("补间缓动曲线，横轴为归一化时间，纵轴为归一化进度")]
        [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("世界切换")]
        [Tooltip("传送时画面淡出的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _teleportFadeOutDuration = 0.15f;

        [Tooltip("传送时画面淡入的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _teleportFadeInDuration = 0.15f;

        [Header("提示")]
        [Tooltip("传送失败等提示在 HUD 上停留的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _hintDuration = 1.5f;

        [Header("手感反馈")]
        [Tooltip("撞墙回弹时向前顶出的距离（格）")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _bumpDistance = 0.15f;

        [Tooltip("撞墙回弹的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _bumpDuration = 0.12f;

        [Tooltip("箱子被推时沿推动方向压扁的比例")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pushSquashAmount = 0.12f;

        [Tooltip("箱子到达终点、通关庆祝时弹起的高度（格）")]
        [Range(0f, 1f)]
        [SerializeField] private float _popHeight = 0.25f;

        [Tooltip("弹起一次的时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _popDuration = 0.2f;

        [Tooltip("通关庆祝时相邻两个箱子开始弹起的间隔（秒）")]
        [Min(0f)]
        [SerializeField] private float _celebrationStagger = 0.08f;

        [Header("相机")]
        [Tooltip("相机俯角（度），90 为正上方俯视")]
        [Range(20f, 90f)]
        [SerializeField] private float _cameraPitch = 55f;

        [Tooltip("关卡四周在画面里留出的边距（格）")]
        [Min(0f)]
        [SerializeField] private float _cameraMargin = 1f;

        [Tooltip("旋转视角 90° 的时长（秒），期间不能操作")]
        [Min(0f)]
        [SerializeField] private float _viewRotateDuration = 0.25f;

        [Header("输入")]
        [Tooltip("按住方向键多久后开始连续移动（秒）")]
        [Min(0f)]
        [SerializeField] private float _holdRepeatDelay = 0.18f;

        public float MoveDuration => _moveDuration;
        public float TurnDuration => _turnDuration;
        public AnimationCurve MoveCurve => _moveCurve;
        public float HoldRepeatDelay => _holdRepeatDelay;
        public float TeleportFadeOutDuration => _teleportFadeOutDuration;
        public float TeleportFadeInDuration => _teleportFadeInDuration;
        public float HintDuration => _hintDuration;
        public float BumpDistance => _bumpDistance;
        public float BumpDuration => _bumpDuration;
        public float PushSquashAmount => _pushSquashAmount;
        public float PopHeight => _popHeight;
        public float PopDuration => _popDuration;
        public float CelebrationStagger => _celebrationStagger;
        public float CameraPitch => _cameraPitch;
        public float CameraMargin => _cameraMargin;
        public float ViewRotateDuration => _viewRotateDuration;
    }
}
