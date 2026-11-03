using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 一组按钮与处理方法的绑定，统一在 OnEnable 订阅、OnDisable 退订，保证成对出现。
    /// </summary>
    public sealed class EditorButtonBindings
    {
        private readonly List<(Button Button, UnityAction Action)> _bindings = new List<(Button, UnityAction)>();

        /// <summary>登记一个绑定。按钮为空时忽略，缺失引用由调用方在 Awake 中报告。</summary>
        public void Add(Button button, UnityAction action)
        {
            if (button != null)
            {
                _bindings.Add((button, action));
            }
        }

        public void Subscribe()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Button.onClick.AddListener(_bindings[i].Action);
            }
        }

        public void Unsubscribe()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                _bindings[i].Button.onClick.RemoveListener(_bindings[i].Action);
            }
        }
    }
}
