using System;
using UnityEngine;

namespace Worldyssey.Core
{
    /// <summary>
    /// 玩家设置：音量、显示模式和界面语言。与关卡进度分开存放，删除进度不会丢设置。
    /// </summary>
    /// <remarks>字段名即 JSON 键名，因此不加下划线。</remarks>
    [Serializable]
    public class SettingsData
    {
        /// <summary>当前设置格式版本。格式变化时递增。</summary>
        public const int CurrentVersion = 1;

        /// <summary>音乐和音效的默认音量，略低于满，给主音量留出余地。</summary>
        public const float DefaultChannelVolume = 0.8f;

        /// <summary>默认语言代码。</summary>
        public const string DefaultLanguage = "en";

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = DefaultChannelVolume;
        [SerializeField] private float sfxVolume = DefaultChannelVolume;
        [SerializeField] private bool isFullscreen = true;

        // 0 表示还没选过分辨率，沿用系统当前分辨率
        [SerializeField] private int resolutionWidth;
        [SerializeField] private int resolutionHeight;

        // 语言代码（如 en、zh-Hans）。旧设置文件没有这个键时保持默认英文
        [SerializeField] private string language = DefaultLanguage;

        public int Version => version;
        public float MasterVolume => Mathf.Clamp01(masterVolume);
        public float MusicVolume => Mathf.Clamp01(musicVolume);
        public float SfxVolume => Mathf.Clamp01(sfxVolume);
        public bool IsFullscreen => isFullscreen;
        public int ResolutionWidth => Mathf.Max(0, resolutionWidth);
        public int ResolutionHeight => Mathf.Max(0, resolutionHeight);

        /// <summary>是否选过分辨率。</summary>
        public bool HasResolution => ResolutionWidth > 0 && ResolutionHeight > 0;

        /// <summary>界面语言代码，未设置时为 <see cref="DefaultLanguage"/>。</summary>
        public string Language => string.IsNullOrEmpty(language) ? DefaultLanguage : language;

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        public void SetLanguage(string code)
        {
            language = string.IsNullOrEmpty(code) ? DefaultLanguage : code;
        }

        public void SetFullscreen(bool value)
        {
            isFullscreen = value;
        }

        /// <summary>
        /// 设置分辨率。宽或高不大于 0 时视为“沿用系统分辨率”。
        /// </summary>
        public void SetResolution(int width, int height)
        {
            bool isValid = width > 0 && height > 0;
            resolutionWidth = isValid ? width : 0;
            resolutionHeight = isValid ? height : 0;
        }
    }
}
