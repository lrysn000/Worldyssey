using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器操作栏：校验问题列表、状态提示，以及新建、打开、保存、试玩、返回按钮。
    /// 设置了 <see cref="_statusGroup"/> 时，状态提示显示一段时间后自动淡出。
    /// </summary>
    public class EditorActionBar : MonoBehaviour
    {
        // 问题列表可以滚动，这里只防止极端情况下文字过长
        private const int MaxIssueLines = 40;

        [SerializeField] private TMP_Text _issuesText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _newButton;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _testPlayButton;
        [SerializeField] private Button _backButton;

        [Header("可选")]
        [Tooltip("问题列表标题，显示问题数量")]
        [SerializeField] private TMP_Text _issuesHeaderText;

        [Tooltip("状态提示（含底板）的透明度控制，有提示时淡入，停留后淡出")]
        [SerializeField] private CanvasGroup _statusGroup;

        [Tooltip("状态提示停留和淡出的时长")]
        [SerializeField] private UiMotionConfig _motionConfig;

        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();
        private readonly StringBuilder _issuesBuilder = new StringBuilder(512);
        private Coroutine _toastRoutine;

        public event Action NewRequested;
        public event Action OpenRequested;
        public event Action SaveRequested;
        public event Action TestPlayRequested;
        public event Action BackRequested;

        private void Awake()
        {
            if (_issuesText == null || _statusText == null)
            {
                Debug.LogError($"[{nameof(EditorActionBar)}] {name} 缺少问题列表或状态文本引用", this);
            }

            _bindings.Add(_newButton, () => NewRequested?.Invoke());
            _bindings.Add(_openButton, () => OpenRequested?.Invoke());
            _bindings.Add(_saveButton, () => SaveRequested?.Invoke());
            _bindings.Add(_testPlayButton, () => TestPlayRequested?.Invoke());
            _bindings.Add(_backButton, () => BackRequested?.Invoke());
        }

        private void OnEnable()
        {
            _bindings.Subscribe();

            // 编辑器面板激活时可能先于本组件调用 SetStatus，那时还不能启动协程，这里补上淡出
            if (_statusGroup != null && _statusGroup.alpha > 0f && _toastRoutine == null && _motionConfig != null)
            {
                _toastRoutine = StartCoroutine(ToastRoutine(_motionConfig.ToastHoldDuration, _motionConfig.ToastFadeDuration));
            }
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();

            // 离开编辑器时清掉提示，下次进来不会看到上次的残留
            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
                _toastRoutine = null;
            }

            if (_statusGroup != null)
            {
                _statusGroup.alpha = 0f;
            }
        }

        public void RenderIssues(LevelValidationReport report)
        {
            if (_issuesText == null)
            {
                return;
            }

            IReadOnlyList<LevelIssue> issues = report.Issues;
            if (_issuesHeaderText != null)
            {
                _issuesHeaderText.text = issues.Count == 0 ? Localization.Translate(UiText.IssuesHeader) : string.Format(Localization.Translate(UiText.IssuesCountFormat), issues.Count);
            }

            if (issues.Count == 0)
            {
                _issuesText.text = Localization.Translate(UiText.NoIssues);
                return;
            }

            _issuesBuilder.Clear();
            int shown = Mathf.Min(issues.Count, MaxIssueLines);
            for (int i = 0; i < shown; i++)
            {
                _issuesBuilder.Append(issues[i].IsError ? Localization.Translate(UiText.ErrorPrefix) : Localization.Translate(UiText.WarningPrefix));
                _issuesBuilder.AppendLine(issues[i].Message);
            }

            if (issues.Count > shown)
            {
                _issuesBuilder.AppendFormat(Localization.Translate(UiText.MoreIssuesFormat), issues.Count - shown);
            }

            _issuesText.text = _issuesBuilder.ToString();
        }

        public void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }

            if (_statusGroup == null)
            {
                return;
            }

            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
                _toastRoutine = null;
            }

            bool hasMessage = !string.IsNullOrEmpty(message);
            _statusGroup.alpha = hasMessage ? 1f : 0f;
            if (hasMessage && _motionConfig != null && isActiveAndEnabled)
            {
                _toastRoutine = StartCoroutine(ToastRoutine(_motionConfig.ToastHoldDuration, _motionConfig.ToastFadeDuration));
            }
        }

        private IEnumerator ToastRoutine(float hold, float fade)
        {
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                _statusGroup.alpha = 1f - Mathf.Clamp01(elapsed / fade);
                yield return null;
            }

            _statusGroup.alpha = 0f;
            _toastRoutine = null;
        }
    }
}
