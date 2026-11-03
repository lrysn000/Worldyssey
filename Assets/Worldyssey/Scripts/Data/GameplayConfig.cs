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
    }
}
