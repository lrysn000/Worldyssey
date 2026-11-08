using UnityEngine;

namespace Worldyssey.Audio
{
    /// <summary>
    /// 一条可循环的音轨：两个 AudioSource 交替使用，换曲时新曲淡入、旧曲淡出。音量每帧由调用方给出，设置里改音量立即生效。
    /// </summary>
    internal sealed class AudioCrossfader
    {
        private readonly AudioSource[] _sources = new AudioSource[2];
        private AudioClip _targetClip;
        private int _active;
        private float _fade = 1f;
        private float _fadeOutStart;
        private float _duration;

        public AudioCrossfader(GameObject host)
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                _sources[i] = source;
            }
        }

        /// <summary>
        /// 换到 <paramref name="clip"/>，为 null 时淡出到无声。与当前目标相同时什么都不做，切换世界来回时不会重新开始。
        /// </summary>
        public void Play(AudioClip clip, float duration)
        {
            if (clip == _targetClip)
            {
                return;
            }

            // 旧曲从它此刻的响度开始淡出，淡变途中再次切换也不会跳变
            _fadeOutStart = _fade;
            _targetClip = clip;
            _active = 1 - _active;
            _duration = duration;
            _fade = duration > 0f ? 0f : 1f;

            AudioSource incoming = _sources[_active];
            incoming.Stop();
            incoming.clip = clip;
            incoming.volume = 0f;
            if (clip != null)
            {
                incoming.Play();
            }
        }

        /// <summary>推进淡变并应用音量。<paramref name="volume"/> 为满响度时的音量。</summary>
        public void Tick(float deltaTime, float volume)
        {
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + deltaTime / _duration);
            }

            AudioSource incoming = _sources[_active];
            AudioSource outgoing = _sources[1 - _active];
            incoming.volume = volume * _fade;
            if (!outgoing.isPlaying)
            {
                return;
            }

            outgoing.volume = volume * _fadeOutStart * (1f - _fade);
            if (_fade >= 1f)
            {
                outgoing.Stop();
                outgoing.clip = null;
            }
        }
    }
}
