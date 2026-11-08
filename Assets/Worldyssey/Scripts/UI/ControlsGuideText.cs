using System.Text;
using UnityEngine.InputSystem;
using Worldyssey.Gameplay;

namespace Worldyssey.UI
{
    /// <summary>
    /// 设置面板里的按键说明文字。
    /// </summary>
    public static class ControlsGuideText
    {
        // 按键提示的条目：显示名（英文原文，显示时翻译）和对应的输入动作
        private static readonly (string Label, string Action)[] ControlEntries =
        {
            (UiText.ControlMoveUp, InputActionNames.MoveUp),
            (UiText.ControlMoveDown, InputActionNames.MoveDown),
            (UiText.ControlMoveLeft, InputActionNames.MoveLeft),
            (UiText.ControlMoveRight, InputActionNames.MoveRight),
            (UiText.ControlTeleport, InputActionNames.Teleport),
            (UiText.ControlUndo, InputActionNames.Undo),
            (UiText.ControlRestart, InputActionNames.Restart),
            (UiText.ControlRotateLeft, InputActionNames.RotateViewLeft),
            (UiText.ControlRotateRight, InputActionNames.RotateViewRight),
            (UiText.ControlPause, InputActionNames.Pause),
        };

        /// <summary>
        /// 生成按键说明：游戏操作的按键从输入资源读取（改键后自动跟着变），编辑器操作用固定文字；按当前语言显示。
        /// </summary>
        public static string Build(InputActionAsset inputActions)
        {
            var text = new StringBuilder(Localization.Translate(UiText.ControlsHeader)).Append('\n');

            // 游戏操作的按键从输入资源读取，改键后这里自动跟着变
            InputActionMap map = inputActions != null ? inputActions.FindActionMap(InputActionNames.GameplayMap) : null;
            if (map != null)
            {
                foreach ((string label, string actionName) in ControlEntries)
                {
                    InputAction action = map.FindAction(actionName);
                    if (action != null)
                    {
                        text.Append('\n').AppendFormat(UiText.ControlLineFormat, Localization.Translate(label), action.GetBindingDisplayString());
                    }
                }
            }

            text.Append('\n').AppendFormat(UiText.ControlLineFormat,
                Localization.Translate(UiText.ControlPanCamera), Localization.Translate(UiText.ControlPanCameraBinding));
            text.Append("\n\n").Append(Localization.Translate(UiText.EditorControlsHeader)).Append('\n');
            foreach ((string label, string binding) in UiText.EditorControlEntries)
            {
                text.Append('\n').AppendFormat(UiText.ControlLineFormat, Localization.Translate(label), Localization.Translate(binding));
            }

            return text.ToString();
        }
    }
}
