using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.View
{
    /// <summary>
    /// 播放一步的结果，播完前 <see cref="IsPlaying"/> 为 true。
    /// 普通步：转向、移动、推箱同时补间。传送步：淡出 → 切换世界并放好伙伴 → 淡入。
    /// 面提示也由这里刷新，保证提示变化与画面变化同时发生。
    /// </summary>
    public class StepPlayer : MonoBehaviour
    {
        [SerializeField] private LevelView _levelView;
        [SerializeField] private WorldTransition _worldTransition;
        [SerializeField] private GameplayConfig _gameplayConfig;

        private readonly List<EntityView> _animatingViews = new List<EntityView>(4);
        private Coroutine _playRoutine;
        private LevelRules _pendingRules;
        private LevelState _pendingTeleportState;

        /// <summary>是否正在播放。播放期间新的命令会被缓存。</summary>
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
        /// </summary>
        public void Play(StepResult result, LevelRules rules)
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

            PlayMovement(result);
            _levelView.UpdateFaceHints(rules, result.State);
        }

        /// <summary>
        /// 让正在播放的一步立即结束，所有相关实体落到终点。用于撤销、重开等需要打断动画的操作。
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

            for (int i = 0; i < _animatingViews.Count; i++)
            {
                if (_animatingViews[i] != null)
                {
                    _animatingViews[i].CompleteTween();
                }
            }

            _animatingViews.Clear();
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

        private void PlayMovement(StepResult result)
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
                        StartTween(_levelView.GetMappedBoxView(pushed.BoxIndex), pushed.To, moveDuration, curve);
                        duration = Mathf.Max(duration, moveDuration);
                        break;
                    case PortalBoxPushedEvent pushed:
                        StartTween(_levelView.GetPortalBoxView(pushed.PortalIndex), pushed.To, moveDuration, curve);
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

            if (duration > 0f)
            {
                _playRoutine = StartCoroutine(WaitRoutine(duration));
            }
        }

        private void StartTween(EntityView view, GridPos to, float duration, AnimationCurve curve)
        {
            view.TweenTo(_levelView.GridToLocal(to), Quaternion.identity, duration, curve);
            _animatingViews.Add(view);
        }

        private IEnumerator WaitRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 各实体的补间与这里按同样的 deltaTime 推进，这里再统一落点一次，确保状态与表现一致
            _playRoutine = null;
            CompleteImmediately();
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
