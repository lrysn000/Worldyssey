using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器底部栏：校验问题列表、状态提示，以及新建、打开、保存、试玩、返回按钮。
    /// </summary>
    public class EditorActionBar : MonoBehaviour
    {
        private const int MaxIssueLines = 8;

        [SerializeField] private TMP_Text _issuesText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _newButton;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _testPlayButton;
        [SerializeField] private Button _backButton;

        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();
        private readonly StringBuilder _issuesBuilder = new StringBuilder(512);

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
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();
        }

        public void RenderIssues(LevelValidationReport report)
        {
            if (_issuesText == null)
            {
                return;
            }

            IReadOnlyList<LevelIssue> issues = report.Issues;
            if (issues.Count == 0)
            {
                _issuesText.text = UiText.NoIssues;
                return;
            }

            _issuesBuilder.Clear();
            int shown = Mathf.Min(issues.Count, MaxIssueLines);
            for (int i = 0; i < shown; i++)
            {
                _issuesBuilder.Append(issues[i].IsError ? UiText.ErrorPrefix : UiText.WarningPrefix);
                _issuesBuilder.AppendLine(issues[i].Message);
            }

            if (issues.Count > shown)
            {
                _issuesBuilder.AppendFormat(UiText.MoreIssuesFormat, issues.Count - shown);
            }

            _issuesText.text = _issuesBuilder.ToString();
        }

        public void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }
    }
}
