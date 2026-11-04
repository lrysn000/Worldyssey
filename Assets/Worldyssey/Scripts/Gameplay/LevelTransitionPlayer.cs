using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Worldyssey.Data;
using Worldyssey.View;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 关卡进出场转场。入场：整座岛从云海里升起，相机从远处推到正常机位，箱子依次落下，玩家最后落地；按任意键跳到结束。
    /// 离场：画面淡出成当前季节的背景色。由 <see cref="GameFlow"/> 决定什么时候播放，这里不碰任何 UI。
    /// </summary>
    public class LevelTransitionPlayer : MonoBehaviour
    {
        // 物件在岛升到这个比例时开始落下，和岛到位的最后一段重叠，衔接更紧凑
        private const float DropStartRatio = 0.85f;

        [SerializeField] private LevelView _levelView;
        [SerializeField] private GameplayConfig _gameplayConfig;

        [Tooltip("全屏遮罩，用来淡出和淡入（与传送的颜色遮罩共用）")]
        [SerializeField] private WorldTransition _overlay;

        [Tooltip("可选：入场时从远处推近的相机。留空则相机不动")]
        [SerializeField] private CameraRig _cameraRig;

        private Coroutine _entrance;
        private Coroutine _exit;
        private Coroutine _cameraRoutine;
        private Coroutine _fadeRoutine;
        private Action _onEntranceComplete;
        private int _entranceStartFrame;
        private bool _hasReferences;

        /// <summary>正在淡出离场。</summary>
        public bool IsPlayingExit => _exit != null;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_gameplayConfig, nameof(_gameplayConfig))
                & CheckReference(_overlay, nameof(_overlay));
        }

        private void Update()
        {
            // 只在入场动画期间检查跳过；开始那一帧不算，避免点“开始关卡”的那次点击直接把转场跳过
            if (_entrance != null && Time.frameCount > _entranceStartFrame && IsSkipPressed())
            {
                CompleteEntrance();
            }
        }

        private void OnDisable()
        {
            CompleteEntrance();
        }

        /// <summary>
        /// 播放入场。<paramref name="isShort"/> 为 true 时只升起岛、物件落下，不推镜头，用于重玩。
        /// 结束（包括被跳过、被打断）时调用 <paramref name="onComplete"/>。
        /// </summary>
        public void PlayEntrance(bool isShort, Action onComplete)
        {
            CompleteEntrance();
            if (!_hasReferences || !isActiveAndEnabled || _levelView.PlayerView == null)
            {
                onComplete?.Invoke();
                return;
            }

            _onEntranceComplete = onComplete;
            _entranceStartFrame = Time.frameCount;
            _entrance = StartCoroutine(EntranceRoutine(isShort));
        }

        /// <summary>
        /// 立即结束入场：岛和物件落到正常位置、相机复位、遮罩清空，并通知完成。没有在入场时什么都不做。
        /// </summary>
        public void CompleteEntrance()
        {
            if (_entrance == null)
            {
                return;
            }

            StopRoutine(ref _entrance);
            StopRoutine(ref _cameraRoutine);
            StopRoutine(ref _fadeRoutine);
            _levelView.SetIslandHeight(0f);
            if (_cameraRig != null)
            {
                _cameraRig.ResetZoom();
            }

            _overlay.SetClear();
            StopEntityEffects();
            FinishEntrance();
        }

        /// <summary>
        /// 淡出成当前季节的背景色后调用 <paramref name="onFaded"/>（切换到下一个界面或下一关）。
        /// 之后没有开始新的入场，就把遮罩清掉，交给新界面盖住画面。
        /// </summary>
        public void PlayExit(Action onFaded)
        {
            if (!_hasReferences || !isActiveAndEnabled || _exit != null)
            {
                onFaded?.Invoke();
                return;
            }

            CompleteEntrance();
            _exit = StartCoroutine(ExitRoutine(onFaded));
        }

        private IEnumerator EntranceRoutine(bool isShort)
        {
            float riseDuration = isShort ? _gameplayConfig.LevelShortRiseDuration : _gameplayConfig.LevelRiseDuration;
            float depth = _gameplayConfig.LevelRiseDepth;
            _levelView.SetIslandHeight(-depth);
            if (!isShort && _cameraRig != null)
            {
                _cameraRig.SetZoomImmediate(_gameplayConfig.LevelCameraStartZoom);
                _cameraRoutine = StartCoroutine(_cameraRig.ZoomBack(riseDuration));
            }

            // 从下一关切过来时遮罩还是不透明的，随岛升起淡回来；从选关进来时遮罩本来就是透明的
            _fadeRoutine = StartCoroutine(_overlay.FadeTo(0f, _gameplayConfig.LevelFadeDuration));
            float totalDuration = Mathf.Max(riseDuration, ScheduleDrops(riseDuration * DropStartRatio));

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / riseDuration);

                // 缓出：一开始升得快，快到位时慢下来，像浮上来停稳
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                _levelView.SetIslandHeight(-depth * (1f - eased));
                yield return null;
            }

            _levelView.SetIslandHeight(0f);
            _entrance = null;
            _cameraRoutine = null;
            _fadeRoutine = null;
            FinishEntrance();
        }

        /// <summary>
        /// 安排箱子依次落下、玩家最后落地，返回最后一个落地的时间。落下前物件保持隐藏。
        /// </summary>
        private float ScheduleDrops(float startTime)
        {
            Vector3 fromAbove = Vector3.up * _gameplayConfig.LevelDropHeight;
            float duration = _gameplayConfig.LevelDropDuration;
            float stagger = _gameplayConfig.LevelDropStagger;
            float delay = startTime;
            for (int i = 0; i < _levelView.MappedBoxViewCount; i++)
            {
                _levelView.GetMappedBoxView(i).PlayEmerge(fromAbove, duration, delay);
                delay += stagger;
            }

            for (int i = 0; i < _levelView.PortalBoxViewCount; i++)
            {
                PortalBoxView portal = _levelView.GetPortalBoxView(i);
                if (portal.gameObject.activeInHierarchy)
                {
                    portal.PlayEmerge(fromAbove, duration, delay);
                    delay += stagger;
                }
            }

            _levelView.PlayerView.PlayEmerge(fromAbove, duration, delay);
            return delay + duration;
        }

        private IEnumerator ExitRoutine(Action onFaded)
        {
            Camera viewCamera = _cameraRig != null ? _cameraRig.ViewCamera : null;
            if (viewCamera != null)
            {
                _overlay.SetColor(viewCamera.backgroundColor);
            }

            yield return _overlay.FadeTo(1f, _gameplayConfig.LevelFadeDuration);
            _exit = null;
            onFaded?.Invoke();

            // 进下一关时入场会负责淡回来；回到菜单、选关时由菜单界面盖住画面，遮罩直接清掉
            if (_entrance == null)
            {
                _overlay.SetClear();
            }
        }

        private void StopEntityEffects()
        {
            if (_levelView.PlayerView == null)
            {
                return;
            }

            _levelView.PlayerView.StopEffects();
            for (int i = 0; i < _levelView.MappedBoxViewCount; i++)
            {
                _levelView.GetMappedBoxView(i).StopEffects();
            }

            for (int i = 0; i < _levelView.PortalBoxViewCount; i++)
            {
                _levelView.GetPortalBoxView(i).StopEffects();
            }
        }

        private void FinishEntrance()
        {
            Action callback = _onEntranceComplete;
            _onEntranceComplete = null;
            callback?.Invoke();
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        private static bool IsSkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        private bool CheckReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(LevelTransitionPlayer)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
