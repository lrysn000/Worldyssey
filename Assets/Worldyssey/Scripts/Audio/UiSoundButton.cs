using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Worldyssey.Audio
{
    /// <summary>
    /// 按钮被点击时播放界面音效。音效由父级 Canvas 上的 <see cref="UiAudio"/> 播放。
    /// 不可交互的按钮不会触发点击，也就不响。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UiSoundButton : MonoBehaviour
    {
        [SerializeField] private UiSoundKind _kind = UiSoundKind.Click;

        private Button _button;
        private UiAudio _uiAudio;
        private UnityAction _clickedAction;
        private bool _hasWarned;

        private void Awake()
        {
            _button = GetComponent<Button>();
            // 委托只建一次，反复显示隐藏面板时不产生垃圾
            _clickedAction = HandleClicked;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(_clickedAction);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(_clickedAction);
        }

        private void HandleClicked()
        {
            // 第一次点击时再找：生成的按钮可能在 Awake 之后才被放到 Canvas 下面
            if (_uiAudio == null)
            {
                _uiAudio = GetComponentInParent<UiAudio>(true);
            }

            if (_uiAudio != null)
            {
                _uiAudio.Play(_kind);
            }
            else if (!_hasWarned)
            {
                _hasWarned = true;
                Debug.LogWarning($"[{nameof(UiSoundButton)}] {name} 的父级里没有 {nameof(UiAudio)}，点击不会发声。运行 Worldyssey/Setup/Build UI Sounds", this);
            }
        }
    }
}
