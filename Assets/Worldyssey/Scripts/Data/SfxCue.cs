using System;
using System.Collections.Generic;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 一个音效条目：若干变体随机播放，带音量、音高浮动和最短间隔。由 <see cref="AudioLibrary"/> 持有，运行时只读。
    /// </summary>
    [Serializable]
    public sealed class SfxCue
    {
        [Tooltip("变体，播放时随机选一个且不连续重复同一个")]
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];

        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;

        [Tooltip("每次播放的音高在此范围内随机，让重复的声音不机械。x 为下限，y 为上限")]
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

        [Tooltip("两次播放的最短间隔（秒），同一帧多次触发时不叠音")]
        [Min(0f)]
        [SerializeField] private float _minInterval = 0.05f;

        public IReadOnlyList<AudioClip> Clips => _clips;
        public float Volume => _volume;
        public Vector2 PitchRange => _pitchRange;
        public float MinInterval => _minInterval;
        public bool HasClips => _clips != null && _clips.Length > 0;
    }
}
