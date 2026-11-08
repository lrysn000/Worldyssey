using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.Audio
{
    /// <summary>
    /// 挂在 Canvas 根节点上，供子物体里的 <see cref="UiSoundButton"/> 播放界面音效。
    /// 按钮通过父级找到它，所以运行时生成的按钮（关卡卡片、列表项）不需要单独接线，预制体也不必引用场景物体。
    /// </summary>
    public class UiAudio : MonoBehaviour
    {
        [SerializeField] private AudioLibrary _library;
        [SerializeField] private SfxPlayer _sfxPlayer;

        private bool _hasReferences;

        private void Awake()
        {
            _hasReferences = CheckReference(_library, nameof(_library)) & CheckReference(_sfxPlayer, nameof(_sfxPlayer));
        }

        /// <summary>播放一类界面音效。</summary>
        public void Play(UiSoundKind kind)
        {
            if (_hasReferences)
            {
                _sfxPlayer.Play(kind == UiSoundKind.Back ? _library.UiBack : _library.UiClick);
            }
        }

        private bool CheckReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"[{nameof(UiAudio)}] {name} 缺少引用：{fieldName}", this);
            return false;
        }
    }
}
