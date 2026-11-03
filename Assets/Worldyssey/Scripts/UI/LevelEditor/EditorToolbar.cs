using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器画笔工具栏。按钮顺序与 <see cref="EditorTool"/> 枚举一致，当前画笔高亮。
    /// </summary>
    public class EditorToolbar : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color SelectedColor = new Color(0.30f, 0.50f, 0.85f, 1f);

        [Tooltip("按 EditorTool 枚举顺序排列")]
        [SerializeField] private Button[] _toolButtons = new Button[0];

        private UnityAction[] _handlers;

        public event Action<EditorTool> ToolSelected;

        private void Awake()
        {
            int toolCount = Enum.GetValues(typeof(EditorTool)).Length;
            if (_toolButtons == null || _toolButtons.Length != toolCount)
            {
                Debug.LogError($"[{nameof(EditorToolbar)}] {name} 需要 {toolCount} 个画笔按钮", this);
                _toolButtons = new Button[0];
            }

            // 每个按钮一个固定的处理方法实例，才能在 OnDisable 里正确退订
            _handlers = new UnityAction[_toolButtons.Length];
            for (int i = 0; i < _toolButtons.Length; i++)
            {
                var tool = (EditorTool)i;
                _handlers[i] = () => ToolSelected?.Invoke(tool);
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < _toolButtons.Length; i++)
            {
                if (_toolButtons[i] != null)
                {
                    _toolButtons[i].onClick.AddListener(_handlers[i]);
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _toolButtons.Length; i++)
            {
                if (_toolButtons[i] != null)
                {
                    _toolButtons[i].onClick.RemoveListener(_handlers[i]);
                }
            }
        }

        public void SetSelected(EditorTool tool)
        {
            for (int i = 0; i < _toolButtons.Length; i++)
            {
                if (_toolButtons[i] != null && _toolButtons[i].image != null)
                {
                    _toolButtons[i].image.color = i == (int)tool ? SelectedColor : NormalColor;
                }
            }
        }
    }
}
