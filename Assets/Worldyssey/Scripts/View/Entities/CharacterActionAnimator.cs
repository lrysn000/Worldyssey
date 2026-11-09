using UnityEngine;

namespace Worldyssey.View
{
    /// <summary>
    /// 角色模型的动作动画：钻进传送箱子、从伙伴箱子走出（以及开局落地出现）、过关庆祝时播放对应的骨骼动画，播完自动回到站立。
    /// 动作来自同一物体上 <see cref="EntityView"/> 的 <see cref="EntityView.ActionStarted"/>；
    /// 控制器里没有对应状态的动作直接忽略，所以换成没有这些动画的角色也不会出错。
    /// </summary>
    [RequireComponent(typeof(EntityView))]
    public class CharacterActionAnimator : MonoBehaviour
    {
        /// <summary>动画控制器里各动作状态的名字，<c>CharacterPrefabBuilder</c> 生成控制器时使用同样的名字。</summary>
        public const string EnterStateName = "Enter";
        public const string ArriveStateName = "Arrive";
        public const string CelebrateStateName = "Celebrate";
        public const string IdleStateName = "Idle";

        private const int BaseLayer = 0;
        private static readonly int EnterStateId = Animator.StringToHash(EnterStateName);
        private static readonly int ArriveStateId = Animator.StringToHash(ArriveStateName);
        private static readonly int CelebrateStateId = Animator.StringToHash(CelebrateStateName);
        private static readonly int IdleStateId = Animator.StringToHash(IdleStateName);

        [Tooltip("模型上的 Animator，控制器由 CharacterPrefabBuilder 生成")]
        [SerializeField] private Animator _animator;

        [Tooltip("切入动作动画的过渡时间（秒）")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _crossFadeDuration = 0.08f;

        private EntityView _entity;
        private bool _hasCheckedStates;
        private bool _hasEnter;
        private bool _hasArrive;
        private bool _hasCelebrate;

        // 走出箱子前有一段等待（例如开局玩家从天而降时依次出现），等到真正出现时再开始动作
        private float _pendingArriveTime = float.PositiveInfinity;

        private void Awake()
        {
            _entity = GetComponent<EntityView>();
            if (_animator == null)
            {
                Debug.LogError($"[{nameof(CharacterActionAnimator)}] {name} 缺少引用：{nameof(_animator)}", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_entity != null)
            {
                _entity.ActionStarted += HandleActionStarted;
            }
        }

        private void OnDisable()
        {
            if (_entity != null)
            {
                _entity.ActionStarted -= HandleActionStarted;
            }

            _pendingArriveTime = float.PositiveInfinity;
        }

        private void Update()
        {
            if (Time.time < _pendingArriveTime)
            {
                return;
            }

            _pendingArriveTime = float.PositiveInfinity;
            if (_animator.isActiveAndEnabled)
            {
                PlayIfPresent(_hasArrive, ArriveStateId);
            }
        }

        private void HandleActionStarted(EntityAction action, float delay)
        {
            if (!_animator.isActiveAndEnabled)
            {
                return;
            }

            CheckStatesOnce();
            _pendingArriveTime = float.PositiveInfinity;
            switch (action)
            {
                case EntityAction.Vanish:
                    PlayIfPresent(_hasEnter, EnterStateId);
                    break;
                case EntityAction.Emerge:
                    // 等待期间角色缩着看不见，动作从真正出现时开始播
                    if (delay > 0f)
                    {
                        _pendingArriveTime = Time.time + delay;
                    }
                    else
                    {
                        PlayIfPresent(_hasArrive, ArriveStateId);
                    }

                    break;
                case EntityAction.Celebrate:
                    PlayIfPresent(_hasCelebrate, CelebrateStateId);
                    break;
                case EntityAction.Reset:
                    // 撤销、重开时画面要立刻对齐状态，动作动画直接切回站立
                    if (IsPlayingAction())
                    {
                        _animator.Play(IdleStateId, BaseLayer, 0f);
                    }

                    break;
            }
        }

        private void CheckStatesOnce()
        {
            // 第一次播放动作时 Animator 一定已经初始化；在 Awake 里查可能早于子物体上 Animator 的初始化
            if (_hasCheckedStates)
            {
                return;
            }

            _hasCheckedStates = true;
            _hasEnter = _animator.HasState(BaseLayer, EnterStateId);
            _hasArrive = _animator.HasState(BaseLayer, ArriveStateId);
            _hasCelebrate = _animator.HasState(BaseLayer, CelebrateStateId);
        }

        private void PlayIfPresent(bool hasState, int stateId)
        {
            if (hasState)
            {
                _animator.CrossFadeInFixedTime(stateId, _crossFadeDuration, BaseLayer, 0f);
            }
        }

        private bool IsPlayingAction()
        {
            int current = _animator.GetCurrentAnimatorStateInfo(BaseLayer).shortNameHash;
            int next = _animator.GetNextAnimatorStateInfo(BaseLayer).shortNameHash;
            return IsActionState(current) || IsActionState(next);
        }

        private static bool IsActionState(int stateId)
        {
            return stateId == EnterStateId || stateId == ArriveStateId || stateId == CelebrateStateId;
        }
    }
}
