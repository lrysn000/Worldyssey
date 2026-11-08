using UnityEngine;
using Worldyssey.Data;
using Worldyssey.Gameplay;

namespace Worldyssey.Audio
{
    /// <summary>
    /// 背景音乐和环境声各一条音轨，切换时交叉淡变。两者都受设置里的音乐音量控制，再乘以 <see cref="AudioLibrary"/> 里的相对音量。
    /// 播什么由 <see cref="GameplayAudio"/> 决定，这里只负责放。
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        private const float DefaultVolume = 1f;

        [SerializeField] private AudioLibrary _library;

        [Tooltip("可选：读取设置里的音乐音量。留空时按满音量播放")]
        [SerializeField] private GameSettingsController _settings;

        private AudioCrossfader _music;
        private AudioCrossfader _ambience;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = _library != null;
            if (!_hasReferences)
            {
                Debug.LogError($"[{nameof(MusicPlayer)}] {name} 缺少引用：{nameof(_library)}", this);
                enabled = false;
                return;
            }

            _music = new AudioCrossfader(gameObject);
            _ambience = new AudioCrossfader(gameObject);
        }

        private void Update()
        {
            // 每帧读设置音量：设置面板拖动滑条时立即听到变化，读取只是取字段，不分配内存
            float userVolume = _settings != null ? _settings.MusicVolume : DefaultVolume;
            float deltaTime = Time.unscaledDeltaTime;
            _music.Tick(deltaTime, userVolume * _library.MusicVolume);
            _ambience.Tick(deltaTime, userVolume * _library.AmbienceVolume);
        }

        /// <summary>切换背景音乐，null 为淡出到无声。</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (_hasReferences)
            {
                _music.Play(clip, _library.CrossfadeDuration);
            }
        }

        /// <summary>切换环境声，null 为淡出到无声。</summary>
        public void PlayAmbience(AudioClip clip)
        {
            if (_hasReferences)
            {
                _ambience.Play(clip, _library.CrossfadeDuration);
            }
        }
    }
}
