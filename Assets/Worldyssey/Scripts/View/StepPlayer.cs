using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 播放一步的结果，播完前 <see cref="IsPlaying"/> 为 true。
    /// 普通步：转向、移动、推箱同时补间，推动的箱子挤压，到达终点的箱子弹起，撞墙时回弹。
    /// 传送步：淡出 → 切换世界并放好伙伴 → 淡入。通关步：移动播完后所有箱子依次弹起庆祝。
    /// 面提示也由这里刷新，保证提示变化与画面变化同时发生。
    /// </summary>
    public class StepPlayer : MonoBehaviour
    {
        [SerializeField] private LevelView _levelView;
        [SerializeField] private WorldTransition _worldTransition;
        [SerializeField] private GameplayConfig _gameplayConfig;

        private readonly List<EntityView> _animatingViews = new List<EntityView>(4);
        private readonly List<EntityView> _pendingPops = new List<EntityView>(2);
        private Coroutine _playRoutine;
        private LevelRules _pendingRules;
        private LevelState _pendingTeleportState;

        /// <summary>是否正在播放。播放期间新的命令会被缓存。撞墙回弹不算在内，不会挡住下一个输入。</summary>
        public bool IsPlaying => _playRoutine != null;

        private void Awake()
        {
            CheckReference(_levelView, nameof(_levelView));
            CheckReference(_worldTransition, nameof(_worldTransition));
            CheckReference(_gameplayConfig, nameof(_gameplayConfig));
        }

        private void OnDisable()
        {
            CompleteImmediately();
        }

        /// <summary>
        /// 播放一步。若上一步还没播完，先让它直接落到终点。
        /// <paramref name="allowBump"/> 为 false 时不播撞墙回弹（按住方向键连续尝试时）。
        /// </summary>
        public void Play(StepResult result, LevelRules rules, bool allowBump)
        {
            CompleteImmediately();
            if (_levelView == null || _gameplayConfig == null || _levelView.PlayerView == null)
            {
                return;
            }

            if (ContainsTeleport(result))
            {
                _pendingRules = rules;
                _pendingTeleportState = result.State;
                _playRoutine = StartCoroutine(TeleportRoutine());
                return;
            }

            PlayMovement(result, rules);
            if (allowBump && (result.Failure == StepFailure.Blocked || result.Failure == StepFailure.BoxBlocked))
            {
                _levelView.PlayerView.PlayBump(
                    _levelView.DirectionToLocal(result.State.PlayerFacing),
                    _gameplayConfig.BumpDistance * _levelView.CellSize,
                    _gameplayConfig.BumpDuration);
            }

            _levelView.UpdateFaceHints(rules, result.State);
        }

        /// <summary>
        /// 让正在播放的一步立即结束：实体落到终点，表现效果全部复原。用于撤销、重开等需要打断动画的操作。
        /// </summary>
        public void CompleteImmediately()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_pendingTeleportState != null)
            {
                ApplyTeleportedState();
            }

            if (_worldTransition != null)
            {
                _worldTransition.SetClear();
            }

            FinishTweens();
            _pendingPops.Clear();
            if (_levelView != null)
            {
                _levelView.StopAllEffects();
            }
        }

        private static bool ContainsTeleport(StepResult result)
        {
            IReadOnlyList<StepEvent> events = result.Events;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is TeleportedEvent)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlayMovement(StepResult result, LevelRules rules)
        {
            bool hasMoved = false;
            bool hasTurned = false;
            float duration = 0f;
            float moveDuration = _gameplayConfig.MoveDuration;
            AnimationCurve curve = _gameplayConfig.MoveCurve;

            // 用下标遍历，避免对接口集合 foreach 产生枚举器分配
            IReadOnlyList<StepEvent> events = result.Events;
            for (int i = 0; i < events.Count; i++)
            {
                switch (events[i])
                {
                    case PlayerMovedEvent _:
                        hasMoved = true;
                        break;
                    case PlayerTurnedEvent _:
                        hasTurned = true;
                        break;
                    case MappedBoxPushedEvent pushed:
                        EntityView boxView = _levelView.GetMappedBoxView(pushed.BoxIndex);
                        PushBox(boxView, pushed.To, result.State.PlayerFacing, moveDuration, curve);
                        if (rules.Map.IsGoal(pushed.To))
                        {
                            _pendingPops.Add(boxView);
                        }

                        duration = Mathf.Max(duration, moveDuration);
                        break;
                    case PortalBoxPushedEvent pushed:
                        PushBox(_levelView.GetPortalBoxView(pushed.PortalIndex), pushed.To, result.State.PlayerFacing, moveDuration, curve);
                        duration = Mathf.Max(duration, moveDuration);
                        break;
                }
            }

            if (hasMoved || hasTurned)
            {
                // 玩家的终点直接取结果状态，转向和移动合成一次补间
                float playerDuration = hasMoved ? moveDuration : _gameplayConfig.TurnDuration;
                EntityView playerView = _levelView.PlayerView;
                playerView.TweenTo(
                    _levelView.GridToLocal(result.State.PlayerPosition),
                    LevelVisualConfig.FacingToRotation(result.State.PlayerFacing),
                    playerDuration,
                    curve);
                _animatingViews.Add(playerView);
                duration = Mathf.Max(duration, playerDuration);
            }

            if (duration > 0f || result.IsLevelSolved)
            {
                _playRoutine = StartCoroutine(MovementRoutine(duration, result.IsLevelSolved));
            }
        }

        private void PushBox(EntityView view, GridPos to, Direction pushDirection, float duration, AnimationCurve curve)
        {
            view.TweenTo(_levelView.GridToLocal(to), Quaternion.identity, duration, curve);
            view.PlaySquash(_levelView.DirectionToLocal(pushDirection), _gameplayConfig.PushSquashAmount, duration);
            _animatingViews.Add(view);
        }

        private IEnumerator MovementRoutine(float moveDuration, bool isLevelSolved)
        {
            yield return Wait(moveDuration);

            // 各实体的补间与这里按同样的 deltaTime 推进，这里再统一落点一次，确保状态与表现一致
            FinishTweens();

            float popDuration = _gameplayConfig.PopDuration;
            float popHeight = _gameplayConfig.PopHeight * _levelView.CellSize;
            for (int i = 0; i < _pendingPops.Count; i++)
            {
                _pendingPops[i].PlayPop(popHeight, popDuration);
            }

            _pendingPops.Clear();

            if (isLevelSolved)
            {
                // 庆祝期间仍算播放中，LevelSession 会等它播完才弹结算
                int boxCount = _levelView.MappedBoxViewCount;
                for (int i = 0; i < boxCount; i++)
                {
                    _levelView.GetMappedBoxView(i).PlayPop(popHeight, popDuration);
                    yield return Wait(_gameplayConfig.CelebrationStagger);
                }

                yield return Wait(popDuration);
            }

            _playRoutine = null;
        }

        private static IEnumerator Wait(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator TeleportRoutine()
        {
            if (_worldTransition != null)
            {
                yield return _worldTransition.FadeTo(1f, _gameplayConfig.TeleportFadeOutDuration);
            }

            // 画面全黑时切换世界：伙伴生成或瞬移都直接落位，玩家看不到中间过程
            ApplyTeleportedState();

            if (_worldTransition != null)
            {
                yield return _worldTransition.FadeTo(0f, _gameplayConfig.TeleportFadeInDuration);
            }

            _playRoutine = null;
        }

        private void FinishTweens()
        {
            for (int i = 0; i < _animatingViews.Count; i++)
            {
                if (_animatingViews[i] != null)
                {
                    _animatingViews[i].CompleteTween();
                }
            }

            _animatingViews.Clear();
        }

        private void ApplyTeleportedState()
        {
            LevelState state = _pendingTeleportState;
            LevelRules rules = _pendingRules;
            _pendingTeleportState = null;
            _pendingRules = null;
            _levelView.Sync(state);
            _levelView.UpdateFaceHints(rules, state);
        }

        private void CheckReference(Object reference, string fieldName)
        {
            if (reference == null)
            {
                Debug.LogError($"[{nameof(StepPlayer)}] {name} 缺少引用：{fieldName}", this);
            }
        }
    }
}
