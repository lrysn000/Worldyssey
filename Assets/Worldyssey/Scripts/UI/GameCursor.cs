using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 游戏鼠标指针：按下左键时显示按下样式，指向可点击的界面元素（按钮、输入框、编辑器网格等）时显示悬停样式，其余为普通样式。
    /// 只在状态变化时调用 <see cref="Cursor.SetCursor(Texture2D, Vector2, CursorMode)"/>。
    /// </summary>
    public class GameCursor : MonoBehaviour
    {
        private enum CursorState
        {
            None,
            Normal,
            Hover,
            Pressed,
        }

        [SerializeField] private CursorSet _cursorSet;

        private readonly List<RaycastResult> _hits = new List<RaycastResult>(8);
        private PointerEventData _pointerData;
        private EventSystem _pointerDataOwner;
        private CursorState _state = CursorState.None;

        // 上一次命中的可点击物体及它身上的 Selectable，命中物体不变时不再重新获取组件
        private GameObject _lastHandler;
        private Selectable _lastSelectable;

        private void Awake()
        {
            if (_cursorSet == null)
            {
                Debug.LogError($"[{nameof(GameCursor)}] {name} 缺少引用：{nameof(_cursorSet)}", this);
            }
        }

        private void OnEnable()
        {
            Apply(CursorState.Normal);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (_cursorSet == null || mouse == null)
            {
                return;
            }

            CursorState next;
            if (mouse.leftButton.isPressed)
            {
                next = CursorState.Pressed;
            }
            else
            {
                next = IsOverClickable(mouse.position.ReadValue()) ? CursorState.Hover : CursorState.Normal;
            }

            Apply(next);
        }

        private void OnDisable()
        {
            // 恢复系统指针，避免组件关闭后停在某个样式上
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _state = CursorState.None;
        }

        private bool IsOverClickable(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerDataOwner != eventSystem)
            {
                _pointerData = new PointerEventData(eventSystem);
                _pointerDataOwner = eventSystem;
            }

            _pointerData.position = screenPosition;
            _hits.Clear();
            eventSystem.RaycastAll(_pointerData, _hits);
            if (_hits.Count == 0)
            {
                return false;
            }

            // 能响应点击或按下的才算可点击：按钮、开关、输入框是点击，编辑器网格是按下
            GameObject hit = _hits[0].gameObject;
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            if (handler == null)
            {
                handler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit);
            }

            if (handler == null)
            {
                return false;
            }

            if (handler != _lastHandler)
            {
                _lastHandler = handler;
                _lastSelectable = handler.GetComponent<Selectable>();
            }

            // 禁用的按钮不显示悬停样式
            return _lastSelectable == null || _lastSelectable.IsInteractable();
        }

        private void Apply(CursorState state)
        {
            if (state == _state || _cursorSet == null)
            {
                return;
            }

            _state = state;
            Cursor.SetCursor(GetTexture(state), _cursorSet.Hotspot, _cursorSet.Mode);
        }

        private Texture2D GetTexture(CursorState state)
        {
            switch (state)
            {
                case CursorState.Hover:
                    return _cursorSet.Hover != null ? _cursorSet.Hover : _cursorSet.Normal;
                case CursorState.Pressed:
                    return _cursorSet.Pressed != null ? _cursorSet.Pressed : _cursorSet.Normal;
                default:
                    return _cursorSet.Normal;
            }
        }
    }
}
