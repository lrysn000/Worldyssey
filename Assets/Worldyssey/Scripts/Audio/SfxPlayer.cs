using System.Collections.Generic;
using UnityEngine;
using Worldyssey.Data;
using Worldyssey.Gameplay;

namespace Worldyssey.Audio
{
    /// <summary>
    /// 播放 <see cref="SfxCue"/>：一组复用的 2D AudioSource 轮流发声，每个条目随机选变体且不连续重复，并限制最短间隔。
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private const float DefaultVolume = 1f;

        [Tooltip("同时发声的最大数量，超出时打断最早的一个")]
        [Range(2, 32)]
        [SerializeField] private int _voiceCount = 8;

        [Tooltip("可选：读取设置里的音效音量。留空时按满音量播放")]
        [SerializeField] private GameSettingsController _settings;

        private readonly Dictionary<SfxCue, CueRecord> _records = new Dictionary<SfxCue, CueRecord>();
        private AudioSource[] _voices;
        private int _nextVoice;

        private struct CueRecord
        {
            public int LastIndex;
            public float LastTime;
        }

        private void Awake()
        {
            // 发声源在启动时一次建好，播放时不再创建组件
            _voices = new AudioSource[_voiceCount];
            for (int i = 0; i < _voiceCount; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 0f;
                _voices[i] = voice;
            }
        }

        /// <summary>播放一个音效条目。条目为空、没有素材或距上次播放太近时不播放。</summary>
        public void Play(SfxCue cue)
        {
            if (cue == null || !cue.HasClips || _voices == null)
            {
                return;
            }

            // 用不受时间缩放影响的时间，暂停菜单里的按钮音也正常
            float now = Time.unscaledTime;
            bool hasRecord = _records.TryGetValue(cue, out CueRecord record);
            if (hasRecord && now - record.LastTime < cue.MinInterval)
            {
                return;
            }

            int index = PickIndex(cue.Clips.Count, hasRecord ? record.LastIndex : -1);
            AudioClip clip = cue.Clips[index];
            if (clip == null)
            {
                Debug.LogError($"[{nameof(SfxPlayer)}] 音效条目的第 {index} 个素材为空，检查 {nameof(AudioLibrary)}", this);
                return;
            }

            _records[cue] = new CueRecord { LastIndex = index, LastTime = now };
            AudioSource voice = TakeVoice();
            Vector2 pitch = cue.PitchRange;
            voice.clip = clip;
            voice.pitch = Random.Range(pitch.x, pitch.y);
            voice.volume = cue.Volume * (_settings != null ? _settings.SfxVolume : DefaultVolume);
            voice.Play();
        }

        private static int PickIndex(int count, int lastIndex)
        {
            if (count <= 1)
            {
                return 0;
            }

            // 从除上一次以外的变体里等概率选一个
            int index = Random.Range(0, count - 1);
            return lastIndex >= 0 && index >= lastIndex ? index + 1 : index;
        }

        private AudioSource TakeVoice()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                int candidate = (_nextVoice + i) % _voices.Length;
                if (!_voices[candidate].isPlaying)
                {
                    _nextVoice = (candidate + 1) % _voices.Length;
                    return _voices[candidate];
                }
            }

            // 全部在响时打断轮到的那一个，它通常是最早开始的
            AudioSource stolen = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return stolen;
        }
    }
}
