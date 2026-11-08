using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using Worldyssey.Core;
using Worldyssey.UI;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 持有并应用玩家设置：启动时读取 settings.json 并应用音量、显示模式和界面语言；设置面板修改时立即生效，关闭面板或退出游戏时保存。
    /// 音乐、音效音量通过混音器的暴露参数生效；没有混音器时主音量直接作用于 <see cref="AudioListener.volume"/>，
    /// 音乐、音效音量先保存下来，供以后的音频系统读取（<see cref="VolumesChanged"/>）。
    /// </summary>
    public class GameSettingsController : MonoBehaviour
    {
        /// <summary>混音器里需要暴露的参数名。</summary>
        public const string MasterVolumeParameter = "MasterVolume";
        public const string MusicVolumeParameter = "MusicVolume";
        public const string SfxVolumeParameter = "SfxVolume";

        // 线性音量 0 对应的分贝值：混音器的下限，等同于静音
        private const float SilentDecibels = -80f;

        [Tooltip("可选：音频混音器，需要暴露 MasterVolume、MusicVolume、SfxVolume 三个参数。留空时主音量直接作用于 AudioListener")]
        [SerializeField] private AudioMixer _mixer;

        [Tooltip("翻译表（CSV，首列英文原文，之后每列一种语言）。留空时界面只显示英文")]
        [SerializeField] private TextAsset _translationTable;

        private SaveService _saveService;
        private SettingsData _data = new SettingsData();
        private bool _isDirty;

        public float MasterVolume => _data.MasterVolume;
        public float MusicVolume => _data.MusicVolume;
        public float SfxVolume => _data.SfxVolume;
        public bool IsFullscreen => _data.IsFullscreen;
        public GameLanguage Language => Localization.FromCode(_data.Language);

        /// <summary>音量变化了。以后的音乐、音效播放器订阅它来更新自己的音量。</summary>
        public event Action VolumesChanged;

        private void Awake()
        {
            _saveService = new SaveService(Application.persistentDataPath);
            _data = _saveService.LoadSettingsOrDefault(out string error);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[{nameof(GameSettingsController)}] {error}（{_saveService.SettingsFilePath}）", this);
            }

            ApplyAudio();
            ApplyScreen();

            // 尽早加载翻译表，界面启用时就能拿到当前语言的文字；晚于界面的部分靠 LanguageChanged 补刷新
            Localization.LoadTable(_translationTable);
            Localization.SetLanguage(Language);
        }

        private void OnApplicationQuit()
        {
            SaveIfDirty();
        }

        public void SetMasterVolume(float value)
        {
            _data.SetMasterVolume(value);
            MarkDirtyAndApplyAudio();
        }

        public void SetMusicVolume(float value)
        {
            _data.SetMusicVolume(value);
            MarkDirtyAndApplyAudio();
        }

        public void SetSfxVolume(float value)
        {
            _data.SetSfxVolume(value);
            MarkDirtyAndApplyAudio();
        }

        /// <summary>切换界面语言，立即生效，关闭设置面板时保存。</summary>
        public void SetLanguage(GameLanguage language)
        {
            _data.SetLanguage(Localization.ToCode(language));
            _isDirty = true;
            Localization.SetLanguage(language);
        }

        public void SetFullscreen(bool isFullscreen)
        {
            _data.SetFullscreen(isFullscreen);
            _isDirty = true;
            ApplyScreen();
        }

        public void SetResolution(int width, int height)
        {
            _data.SetResolution(width, height);
            _isDirty = true;
            ApplyScreen();
        }

        /// <summary>
        /// 当前使用的分辨率：选过就用选的，没选过就用系统当前分辨率。
        /// </summary>
        public Vector2Int GetCurrentResolution()
        {
            return _data.HasResolution
                ? new Vector2Int(_data.ResolutionWidth, _data.ResolutionHeight)
                : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        /// <summary>
        /// 可选的分辨率（去掉只有刷新率不同的重复项），从小到大排列。
        /// </summary>
        public static List<Vector2Int> GetResolutionOptions()
        {
            var options = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!options.Contains(size))
                {
                    options.Add(size);
                }
            }

            options.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return options;
        }

        /// <summary>
        /// 有修改时写入设置文件。拖滑条时不每次都写，关闭设置面板或退出游戏时保存一次。
        /// </summary>
        public void SaveIfDirty()
        {
            if (!_isDirty || _saveService == null)
            {
                return;
            }

            try
            {
                _saveService.SaveSettings(_data);
                _isDirty = false;
            }
            catch (IOException exception)
            {
                Debug.LogError($"[{nameof(GameSettingsController)}] 保存设置失败：{exception.Message}（{_saveService.SettingsFilePath}）", this);
            }
        }

        private void MarkDirtyAndApplyAudio()
        {
            _isDirty = true;
            ApplyAudio();
        }

        private void ApplyAudio()
        {
            if (_mixer != null)
            {
                _mixer.SetFloat(MasterVolumeParameter, ToDecibels(_data.MasterVolume));
                _mixer.SetFloat(MusicVolumeParameter, ToDecibels(_data.MusicVolume));
                _mixer.SetFloat(SfxVolumeParameter, ToDecibels(_data.SfxVolume));
            }
            else
            {
                AudioListener.volume = _data.MasterVolume;
            }

            VolumesChanged?.Invoke();
        }

        private void ApplyScreen()
        {
            FullScreenMode mode = _data.IsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (_data.HasResolution)
            {
                Screen.SetResolution(_data.ResolutionWidth, _data.ResolutionHeight, mode);
            }
            else
            {
                // 没选过分辨率时只切换窗口模式，不改分辨率
                Screen.fullScreenMode = mode;
            }
        }

        /// <summary>
        /// 线性音量换算成分贝：人耳对响度的感受接近对数，直接线性设置时滑条前半段几乎听不出变化。
        /// </summary>
        private static float ToDecibels(float volume)
        {
            return volume <= 0.0001f ? SilentDecibels : Mathf.Log10(volume) * 20f;
        }
    }
}
