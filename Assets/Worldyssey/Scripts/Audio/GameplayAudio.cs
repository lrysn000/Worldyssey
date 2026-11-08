using UnityEngine;
using Worldyssey.Core;
using Worldyssey.Data;
using Worldyssey.Gameplay;
using Worldyssey.View;

namespace Worldyssey.Audio
{
    /// <summary>
    /// 把游戏里发生的事换成声音：每一步的事件（走、推、落位、撞墙、传送）、撤销、重开、通关，
    /// 以及背景音乐（有关卡时播关卡音乐，否则播菜单音乐）和随显示世界切换的环境声。
    /// 只监听现有事件，不改动玩法逻辑。
    /// </summary>
    public class GameplayAudio : MonoBehaviour
    {
        [SerializeField] private AudioLibrary _library;
        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private LevelView _levelView;
        [SerializeField] private SfxPlayer _sfxPlayer;
        [SerializeField] private MusicPlayer _musicPlayer;

        private PlaySession _session;
        private bool _hasLevel;
        private bool _hasReferences;
        private float _lastBlockedTime = float.NegativeInfinity;

        private void Awake()
        {
            _hasReferences = CheckReference(_library, nameof(_library))
                & CheckReference(_levelSession, nameof(_levelSession))
                & CheckReference(_levelView, nameof(_levelView))
                & CheckReference(_sfxPlayer, nameof(_sfxPlayer))
                & CheckReference(_musicPlayer, nameof(_musicPlayer));
            if (!_hasReferences)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _levelSession.StateChanged += HandleStateChanged;
            _levelSession.Undone += HandleUndone;
            _levelSession.Restarted += HandleRestarted;
            _levelSession.LevelCompleted += HandleLevelCompleted;
            _levelView.ShownWorldChanged += HandleShownWorldChanged;
            BindSession(_levelSession.Session);
        }

        private void Start()
        {
            if (_hasReferences)
            {
                _hasLevel = _levelSession.HasLevel;
                RefreshMusic();
            }
        }

        private void OnDisable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _levelSession.StateChanged -= HandleStateChanged;
            _levelSession.Undone -= HandleUndone;
            _levelSession.Restarted -= HandleRestarted;
            _levelSession.LevelCompleted -= HandleLevelCompleted;
            _levelView.ShownWorldChanged -= HandleShownWorldChanged;
            BindSession(null);
        }

        /// <summary>
        /// 每次加载关卡都会换一个新的 <see cref="PlaySession"/>，这里跟着换订阅对象，旧对局的事件不再收听。
        /// </summary>
        private void BindSession(PlaySession session)
        {
            if (session == _session)
            {
                return;
            }

            if (_session != null)
            {
                _session.StepApplied -= HandleStepApplied;
            }

            _session = session;
            if (_session != null)
            {
                _session.StepApplied += HandleStepApplied;
            }
        }

        private void RefreshMusic()
        {
            _musicPlayer.PlayMusic(_hasLevel ? _library.LevelMusic : _library.MenuMusic);
            _musicPlayer.PlayAmbience(_hasLevel ? GetAmbience(_levelView.ShownWorld) : null);
        }

        private AudioClip GetAmbience(int world)
        {
            return _library.GetAmbience(_levelView.GetWorldThemeIndex(world));
        }

        private void HandleStateChanged()
        {
            BindSession(_levelSession.Session);
            bool hasLevel = _levelSession.HasLevel;
            if (hasLevel != _hasLevel)
            {
                _hasLevel = hasLevel;
                RefreshMusic();
            }
        }

        private void HandleShownWorldChanged(int world)
        {
            // 加载关卡时画面先建好、关卡状态后通知，此时 _hasLevel 还是 false，由 HandleStateChanged 统一处理
            if (_hasLevel)
            {
                _musicPlayer.PlayAmbience(GetAmbience(world));
            }
        }

        private void HandleStepApplied(StepResult result)
        {
            if (!result.IsStepTaken)
            {
                PlayBlocked(result.Failure);
                return;
            }

            LevelMap map = _session.Rules.Map;
            int season = _levelView.GetWorldThemeIndex(result.State.PlayerWorld);
            for (int i = 0; i < result.Events.Count; i++)
            {
                StepEvent stepEvent = result.Events[i];
                if (stepEvent is PlayerMovedEvent)
                {
                    _sfxPlayer.Play(_library.GetFootstep(season));
                }
                else if (stepEvent is MappedBoxPushedEvent mapped)
                {
                    _sfxPlayer.Play(_library.BoxPush);
                    if (map.IsGoal(mapped.To) && !map.IsGoal(mapped.From))
                    {
                        _sfxPlayer.Play(_library.BoxOnGoal);
                    }
                }
                else if (stepEvent is PortalBoxPushedEvent portal)
                {
                    _sfxPlayer.Play(_library.BoxPush);
                    if (map.IsPedestal(portal.To) && !map.IsPedestal(portal.From))
                    {
                        _sfxPlayer.Play(_library.BoxOnPedestal);
                    }
                }
                else if (stepEvent is TeleportedEvent)
                {
                    _sfxPlayer.Play(_library.Teleport);
                }
            }
        }

        private void PlayBlocked(StepFailure failure)
        {
            if (failure != StepFailure.Blocked && failure != StepFailure.BoxBlocked)
            {
                return;
            }

            // 按住方向键顶墙时每帧都会失败一次，只在间隔足够长（即重新按下）时播放
            float now = Time.unscaledTime;
            bool isNewPress = now - _lastBlockedTime > _library.BlockedHoldGap;
            _lastBlockedTime = now;
            if (isNewPress)
            {
                _sfxPlayer.Play(_library.Blocked);
            }
        }

        private void HandleUndone()
        {
            _sfxPlayer.Play(_library.Undo);
        }

        private void HandleRestarted()
        {
            _sfxPlayer.Play(_library.Restart);
        }

        private void HandleLevelCompleted(int stepCount)
        {
            _sfxPlayer.Play(_library.LevelComplete);
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(GameplayAudio)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
