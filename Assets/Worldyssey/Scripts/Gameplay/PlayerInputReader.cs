using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Worldyssey.Core;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 把 Input System 的按键动作转换为游戏命令事件。按键绑定在 .inputactions 资源中配置，不写在代码里。
    /// 方向键分别发出按下和松开事件，按住连续移动由 <see cref="MoveInputScheduler"/> 处理。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        private const string GameplayMapName = "Gameplay";
        private const string MoveUpActionName = "MoveUp";
        private const string MoveDownActionName = "MoveDown";
        private const string MoveLeftActionName = "MoveLeft";
        private const string MoveRightActionName = "MoveRight";
        private const string TeleportActionName = "Teleport";
        private const string UndoActionName = "Undo";
        private const string RestartActionName = "Restart";

        [Tooltip("包含 Gameplay 动作表的输入资源")]
        [SerializeField] private InputActionAsset _inputActions;

        private readonly Dictionary<InputAction, Direction> _directionByAction = new Dictionary<InputAction, Direction>(4);
        private InputActionMap _gameplayMap;
        private InputAction _teleportAction;
        private InputAction _undoAction;
        private InputAction _restartAction;
        private bool _isReady;

        public event Action<Direction> DirectionPressed;
        public event Action<Direction> DirectionReleased;
        public event Action TeleportRequested;
        public event Action UndoRequested;
        public event Action RestartRequested;

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] {name} 未设置 Input Actions", this);
                return;
            }

            _gameplayMap = _inputActions.FindActionMap(GameplayMapName);
            if (_gameplayMap == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] {_inputActions.name} 中找不到动作表 {GameplayMapName}", this);
                return;
            }

            bool hasAllActions = TryAddDirection(MoveUpActionName, Direction.Up)
                & TryAddDirection(MoveDownActionName, Direction.Down)
                & TryAddDirection(MoveLeftActionName, Direction.Left)
                & TryAddDirection(MoveRightActionName, Direction.Right);
            _teleportAction = FindAction(TeleportActionName);
            _undoAction = FindAction(UndoActionName);
            _restartAction = FindAction(RestartActionName);
            _isReady = hasAllActions && _teleportAction != null && _undoAction != null && _restartAction != null;
        }

        private void OnEnable()
        {
            if (!_isReady)
            {
                return;
            }

            foreach (InputAction action in _directionByAction.Keys)
            {
                action.performed += HandleDirectionPerformed;
                action.canceled += HandleDirectionCanceled;
            }

            _teleportAction.performed += HandleTeleportPerformed;
            _undoAction.performed += HandleUndoPerformed;
            _restartAction.performed += HandleRestartPerformed;
            _gameplayMap.Enable();
        }

        private void OnDisable()
        {
            if (!_isReady)
            {
                return;
            }

            // 先停用再退订：停用时按住的键会触发 canceled，让订阅方收到松开
            _gameplayMap.Disable();
            foreach (InputAction action in _directionByAction.Keys)
            {
                action.performed -= HandleDirectionPerformed;
                action.canceled -= HandleDirectionCanceled;
            }

            _teleportAction.performed -= HandleTeleportPerformed;
            _undoAction.performed -= HandleUndoPerformed;
            _restartAction.performed -= HandleRestartPerformed;
        }

        private bool TryAddDirection(string actionName, Direction direction)
        {
            InputAction action = FindAction(actionName);
            if (action == null)
            {
                return false;
            }

            _directionByAction[action] = direction;
            return true;
        }

        private InputAction FindAction(string actionName)
        {
            InputAction action = _gameplayMap.FindAction(actionName);
            if (action == null)
            {
                Debug.LogError($"[{nameof(PlayerInputReader)}] 动作表 {GameplayMapName} 中找不到动作 {actionName}", this);
            }

            return action;
        }

        private void HandleDirectionPerformed(InputAction.CallbackContext context)
        {
            if (_directionByAction.TryGetValue(context.action, out Direction direction))
            {
                DirectionPressed?.Invoke(direction);
            }
        }

        private void HandleDirectionCanceled(InputAction.CallbackContext context)
        {
            if (_directionByAction.TryGetValue(context.action, out Direction direction))
            {
                DirectionReleased?.Invoke(direction);
            }
        }

        private void HandleTeleportPerformed(InputAction.CallbackContext context)
        {
            TeleportRequested?.Invoke();
        }

        private void HandleUndoPerformed(InputAction.CallbackContext context)
        {
            UndoRequested?.Invoke();
        }

        private void HandleRestartPerformed(InputAction.CallbackContext context)
        {
            RestartRequested?.Invoke();
        }
    }
}
