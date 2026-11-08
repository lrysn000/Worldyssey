using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Worldyssey.Gameplay;

namespace Worldyssey.UI
{
    /// <summary>
    /// 设置面板：全屏、分辨率、主音量 / 音乐 / 音效、界面语言，以及按键提示。改动立即生效，关闭面板时保存。
    /// 按键提示直接读输入资源里的实际绑定，改了按键会自动跟着变。所有设置项都是可选引用，缺了的那项不显示也不报错。
    /// </summary>
    public class SettingsPanel : UiPanel
    {
        [SerializeField] private Button _closeButton;

        [Header("设置项（可选）")]
        [SerializeField] private GameSettingsController _settings;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TMP_Dropdown _languageDropdown;

        [Header("按键提示（可选）")]
        [SerializeField] private TMP_Text _controlsText;
        [SerializeField] private InputActionAsset _inputActions;

        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
        private readonly List<string> _resolutionLabels = new List<string>();
        private readonly List<string> _languageLabels = new List<string>();

        /// <summary>点击了关闭按钮。</summary>
        public event Action CloseRequested;

        private void Awake()
        {
            CheckReference(_closeButton, nameof(_closeButton));
            bool hasSettingControls = _fullscreenToggle != null || _resolutionDropdown != null || _languageDropdown != null
                || _masterSlider != null || _musicSlider != null || _sfxSlider != null;
            if (hasSettingControls)
            {
                CheckReference(_settings, nameof(_settings));
            }
        }

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            Localization.LanguageChanged += HandleLanguageChanged;
            Refresh();
            if (_settings == null)
            {
                return;
            }

            AddListener(_languageDropdown, HandleLanguageSelected);
            AddListener(_fullscreenToggle, HandleFullscreenChanged);
            AddListener(_resolutionDropdown, HandleResolutionChanged);
            AddListener(_masterSlider, _settings.SetMasterVolume);
            AddListener(_musicSlider, _settings.SetMusicVolume);
            AddListener(_sfxSlider, _settings.SetSfxVolume);
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            Localization.LanguageChanged -= HandleLanguageChanged;
            if (_settings == null)
            {
                return;
            }

            RemoveListener(_languageDropdown, HandleLanguageSelected);
            RemoveListener(_fullscreenToggle, HandleFullscreenChanged);
            RemoveListener(_resolutionDropdown, HandleResolutionChanged);
            RemoveListener(_masterSlider, _settings.SetMasterVolume);
            RemoveListener(_musicSlider, _settings.SetMusicVolume);
            RemoveListener(_sfxSlider, _settings.SetSfxVolume);

            // 拖滑条时不反复写盘，面板关闭时统一保存
            _settings.SaveIfDirty();
        }

        /// <summary>
        /// 按当前设置刷新各控件，不触发修改事件。
        /// </summary>
        private void Refresh()
        {
            if (_settings != null)
            {
                SetSlider(_masterSlider, _settings.MasterVolume);
                SetSlider(_musicSlider, _settings.MusicVolume);
                SetSlider(_sfxSlider, _settings.SfxVolume);
                if (_fullscreenToggle != null)
                {
                    _fullscreenToggle.SetIsOnWithoutNotify(_settings.IsFullscreen);
                }

                RefreshResolutions();
                RefreshLanguages();
            }

            RefreshControlsText();
        }

        private void RefreshControlsText()
        {
            if (_controlsText != null)
            {
                _controlsText.text = ControlsGuideText.Build(_inputActions);
            }
        }

        private void RefreshResolutions()
        {
            if (_resolutionDropdown == null)
            {
                return;
            }

            _resolutions.Clear();
            _resolutions.AddRange(GameSettingsController.GetResolutionOptions());
            Vector2Int current = _settings.GetCurrentResolution();
            if (!_resolutions.Contains(current))
            {
                // 当前分辨率不在列表里（例如编辑器的 Game 窗口）也要能显示出来
                _resolutions.Add(current);
            }

            _resolutionLabels.Clear();
            for (int i = 0; i < _resolutions.Count; i++)
            {
                _resolutionLabels.Add(string.Format(UiText.ResolutionFormat, _resolutions[i].x, _resolutions[i].y));
            }

            _resolutionDropdown.ClearOptions();
            _resolutionDropdown.AddOptions(_resolutionLabels);
            _resolutionDropdown.SetValueWithoutNotify(_resolutions.IndexOf(current));
        }

        private void RefreshLanguages()
        {
            if (_languageDropdown == null)
            {
                return;
            }

            // 语言名用各自的语言书写，切换界面语言后也不变，选错了也能认出来切回去
            _languageLabels.Clear();
            for (int i = 0; i < Localization.LanguageCount; i++)
            {
                _languageLabels.Add(Localization.GetNativeName((GameLanguage)i));
            }

            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(_languageLabels);
            _languageDropdown.SetValueWithoutNotify((int)_settings.Language);
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleLanguageSelected(int index)
        {
            if (index >= 0 && index < Localization.LanguageCount)
            {
                _settings.SetLanguage((GameLanguage)index);
            }
        }

        private void HandleLanguageChanged()
        {
            RefreshControlsText();
        }

        private void HandleFullscreenChanged(bool isOn)
        {
            _settings.SetFullscreen(isOn);
        }

        private void HandleResolutionChanged(int index)
        {
            if (index >= 0 && index < _resolutions.Count)
            {
                _settings.SetResolution(_resolutions[index].x, _resolutions[index].y);
            }
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private static void AddListener(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            if (slider != null)
            {
                slider.onValueChanged.AddListener(action);
            }
        }

        private static void RemoveListener(Slider slider, UnityEngine.Events.UnityAction<float> action)
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(action);
            }
        }

        private static void AddListener(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(action);
            }
        }

        private static void RemoveListener(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(action);
            }
        }

        private static void AddListener(TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> action)
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(action);
            }
        }

        private static void RemoveListener(TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> action)
        {
            if (dropdown != null)
            {
                dropdown.onValueChanged.RemoveListener(action);
            }
        }
    }
}
