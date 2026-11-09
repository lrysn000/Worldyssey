using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 角色模型的走路动画：<see cref="EntityView"/> 在格子之间移动时播放走路，停下后回到站立。
    /// 按住方向键连续走时两步之间有极短的空档，停下前稍等一下，避免动画在走和停之间闪动。
    /// </summary>
    [RequireComponent(typeof(EntityView))]
    public class CharacterWalkAnimator : MonoBehaviour
    {
        private static readonly int IsWalkingId = Animator.StringToHash("IsWalking");
        private static readonly int WalkSpeedId = Animator.StringToHash("WalkSpeed");

        [Tooltip("模型上的 Animator，控制器需有 IsWalking（Bool）和 WalkSpeed（Float）参数")]
        [SerializeField] private Animator _animator;

        [Tooltip("走路动画的播放倍速。动画一个循环约 1.4 秒，而走一格只要零点几秒，加快才不会脚底打滑")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _walkAnimationSpeed = 2f;

        [Tooltip("停止移动后多久切回站立（秒）")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _stopDelay = 0.1f;

        private EntityView _entity;
        private float _lastWalkTime = float.NegativeInfinity;
        private bool _isWalking;

        private void Awake()
        {
            _entity = GetComponent<EntityView>();
            if (_animator == null)
            {
                Debug.LogError($"[{nameof(CharacterWalkAnimator)}] {name} 缺少引用：{nameof(_animator)}", this);
                enabled = false;
                return;
            }

            _animator.applyRootMotion = false;
        }

        private void OnEnable()
        {
            if (_animator == null)
            {
                return;
            }

            // 每次启用（例如换关卡重建模型）都从站立开始
            _isWalking = false;
            _lastWalkTime = float.NegativeInfinity;
            _animator.SetFloat(WalkSpeedId, _walkAnimationSpeed);
            _animator.SetBool(IsWalkingId, false);
        }

        private void Update()
        {
            if (_entity.IsWalking)
            {
                _lastWalkTime = Time.time;
            }

            bool isWalking = Time.time - _lastWalkTime <= _stopDelay;
            if (isWalking == _isWalking)
            {
                return;
            }

            _isWalking = isWalking;
            _animator.SetBool(IsWalkingId, isWalking);
        }
    }
}
