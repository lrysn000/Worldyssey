using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Worldyssey.Core;
using Worldyssey.View;

namespace Worldyssey.Gameplay
{
    /// <summary>
    /// 按住鼠标左键拖动画面平移相机，用来观察大地图。游戏和编辑器试玩共用。
    /// 只有在关卡中、游戏输入开启、相机处于正常取景、按下时不在界面元素上，才会开始拖动；
    /// 拖动中途遇到传送或暂停等情况立即停止。平移的边界由 <see cref="CameraRig"/> 按关卡大小限制。
    /// 玩家做出游戏操作（移动、传送、撤销、重开、旋转视角）时，相机平滑回到中心；暂停和其他按键不影响。
    /// 操作来自 <see cref="PlayerInputReader"/> 的事件，所以改键后依然有效。
    /// </summary>
    public class CameraPanController : MonoBehaviour
    {
        [SerializeField] private LevelSession _levelSession;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private PlayerInputReader _inputReader;

        private bool _isDragging;
        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_levelSession, nameof(_levelSession)) & CheckReference(_cameraRig, nameof(_cameraRig))
                & CheckReference(_inputReader, nameof(_inputReader));
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _inputReader.DirectionPressed += HandleDirectionPressed;
            _inputReader.TeleportRequested += HandleGameAction;
            _inputReader.UndoRequested += HandleGameAction;
            _inputReader.RestartRequested += HandleGameAction;
            _inputReader.RotateViewRequested += HandleRotateViewRequested;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (!_hasReferences || mouse == null)
            {
                return;
            }

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _isDragging = CanPan() && !IsPointerOverUi();
                if (_isDragging)
                {
                    _cameraRig.BeginPan(position);
                }

                return;
            }

            if (!_isDragging)
            {
                return;
            }

            if (!mouse.leftButton.isPressed || !CanPan())
            {
                _isDragging = false;
                return;
            }

            _cameraRig.UpdatePan(position);
        }

        private void OnDisable()
        {
            _isDragging = false;
            if (!_hasReferences)
            {
                return;
            }

            _inputReader.DirectionPressed -= HandleDirectionPressed;
            _inputReader.TeleportRequested -= HandleGameAction;
            _inputReader.UndoRequested -= HandleGameAction;
            _inputReader.RestartRequested -= HandleGameAction;
            _inputReader.RotateViewRequested -= HandleRotateViewRequested;
        }

        private void HandleDirectionPressed(Direction direction)
        {
            HandleGameAction();
        }

        private void HandleRotateViewRequested(bool sceneClockwise)
        {
            HandleGameAction();
        }

        /// <summary>游戏操作：结束拖动并让相机回到中心。</summary>
        private void HandleGameAction()
        {
            if (!_levelSession.HasLevel)
            {
                return;
            }

            _isDragging = false;
            _cameraRig.ReturnPanToCenter();
        }

        private bool CanPan()
        {
            return _levelSession.HasLevel && _levelSession.IsInputEnabled && _cameraRig.CanPan;
        }

        private static bool IsPointerOverUi()
        {
            // HUD 按钮、暂停面板等界面挡在前面时，点击属于界面，不拖动相机
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(CameraPanController)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
