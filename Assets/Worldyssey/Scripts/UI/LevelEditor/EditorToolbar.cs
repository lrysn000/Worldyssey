using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器画笔工具栏。按钮顺序与 <see cref="EditorTool"/> 枚举一致，当前画笔高亮。
    /// 高亮通过切换按钮上 <see cref="ThemedGraphic"/> 的颜色角色实现，颜色仍由主题决定；
    /// 没有挂 ThemedGraphic 的旧按钮退回直接改色。
    /// </summary>
    public class EditorToolbar : MonoBehaviour
    {
        private static readonly Color FallbackNormalColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color FallbackSelectedColor = new Color(0.30f, 0.50f, 0.85f, 1f);

        [Tooltip("按 EditorTool 枚举顺序排列")]
        [SerializeField] private Button[] _toolButtons = new Button[0];

        private UnityAction[] _handlers;

        // 每个按钮上所有着色组件：底板、图标、文字。Awake 时取一次，切换画笔时只改角色
        private ThemedGraphic[][] _themedParts;

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

            CacheThemedParts();
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
            // 编辑器面板在 OnEnable 里就会调用这里，此时本组件的 Awake 可能还没执行（父物体先激活），所以按需缓存
            if (_themedParts == null)
            {
                CacheThemedParts();
            }

            for (int i = 0; i < _toolButtons.Length; i++)
            {
                Button button = _toolButtons[i];
                if (button == null)
                {
                    continue;
                }

                bool isSelected = i == (int)tool;
                ThemedGraphic[] parts = _themedParts[i];
                if (parts.Length == 0)
                {
                    if (button.image != null)
                    {
                        button.image.color = isSelected ? FallbackSelectedColor : FallbackNormalColor;
                    }

                    continue;
                }

                for (int p = 0; p < parts.Length; p++)
                {
                    bool isBackground = parts[p].gameObject == button.gameObject;
                    parts[p].SetRole(GetRole(isBackground, isSelected));
                }
            }
        }

        private void CacheThemedParts()
        {
            _themedParts = new ThemedGraphic[_toolButtons.Length][];
            for (int i = 0; i < _toolButtons.Length; i++)
            {
                _themedParts[i] = _toolButtons[i] != null ? _toolButtons[i].GetComponentsInChildren<ThemedGraphic>(true) : new ThemedGraphic[0];
            }
        }

        /// <summary>选中：强调色底 + 浅色内容；未选中：面板底色（比所在的白色卡片略深）+ 正文色内容。</summary>
        private static UiColorRole GetRole(bool isBackground, bool isSelected)
        {
            if (isBackground)
            {
                return isSelected ? UiColorRole.Accent : UiColorRole.PanelBackground;
            }

            return isSelected ? UiColorRole.HeaderText : UiColorRole.Text;
        }
    }
}
