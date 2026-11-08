using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 全部音效、环境声和音乐的配置。季节相关的数组按 WorldTheme 的数值排列：0 春、1 夏、2 秋、3 冬。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Worldyssey/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        public const int SeasonCount = 4;

        [Header("玩家与箱子")]
        [Tooltip("四季脚步声，按 春、夏、秋、冬 排列；玩家所在世界的季节决定用哪一组")]
        [SerializeField] private SfxCue[] _footsteps = new SfxCue[SeasonCount];

        [SerializeField] private SfxCue _boxPush = new SfxCue();

        [Tooltip("箱子被推到目标点上")]
        [SerializeField] private SfxCue _boxOnGoal = new SfxCue();

        [Tooltip("传送箱子被推到底座上")]
        [SerializeField] private SfxCue _boxOnPedestal = new SfxCue();

        [Tooltip("撞墙或推不动")]
        [SerializeField] private SfxCue _blocked = new SfxCue();

        [Tooltip("按住方向键顶着墙时每帧都会尝试移动。两次撞墙间隔小于此值（秒）视为同一次按住，不重复播放")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _blockedHoldGap = 0.15f;

        [Header("世界")]
        [Tooltip("进入或离开世界箱子，两个方向共用")]
        [SerializeField] private SfxCue _teleport = new SfxCue();

        [Header("关卡流程")]
        [SerializeField] private SfxCue _undo = new SfxCue();
        [SerializeField] private SfxCue _restart = new SfxCue();
        [SerializeField] private SfxCue _levelComplete = new SfxCue();

        [Header("界面")]
        [SerializeField] private SfxCue _uiClick = new SfxCue();
        [SerializeField] private SfxCue _uiBack = new SfxCue();

        [Header("音乐与环境声")]
        [Tooltip("主菜单、选关、关卡编辑器的背景音乐")]
        [SerializeField] private AudioClip _menuMusic;

        [Tooltip("关卡内（包括编辑器试玩）的背景音乐")]
        [SerializeField] private AudioClip _levelMusic;

        [Tooltip("四季环境声，按 春、夏、秋、冬 排列，在关卡内随显示的世界切换")]
        [SerializeField] private AudioClip[] _ambience = new AudioClip[SeasonCount];

        [Tooltip("音乐相对音量，再乘以设置里的音乐音量")]
        [Range(0f, 1f)]
        [SerializeField] private float _musicVolume = 0.6f;

        [Tooltip("环境声相对音量，再乘以设置里的音乐音量。关卡内与音乐同时播放，所以默认偏低")]
        [Range(0f, 1f)]
        [SerializeField] private float _ambienceVolume = 0.4f;

        [Tooltip("音乐、环境声切换时的交叉淡变时长（秒）")]
        [Min(0f)]
        [SerializeField] private float _crossfadeDuration = 1.5f;

        public SfxCue BoxPush => _boxPush;
        public SfxCue BoxOnGoal => _boxOnGoal;
        public SfxCue BoxOnPedestal => _boxOnPedestal;
        public SfxCue Blocked => _blocked;
        public float BlockedHoldGap => _blockedHoldGap;
        public SfxCue Teleport => _teleport;
        public SfxCue Undo => _undo;
        public SfxCue Restart => _restart;
        public SfxCue LevelComplete => _levelComplete;
        public SfxCue UiClick => _uiClick;
        public SfxCue UiBack => _uiBack;
        public AudioClip MenuMusic => _menuMusic;
        public AudioClip LevelMusic => _levelMusic;
        public float MusicVolume => _musicVolume;
        public float AmbienceVolume => _ambienceVolume;
        public float CrossfadeDuration => _crossfadeDuration;

        /// <summary>季节对应的脚步声，编号超出范围时返回 null。</summary>
        public SfxCue GetFootstep(int season)
        {
            return season >= 0 && season < _footsteps.Length ? _footsteps[season] : null;
        }

        /// <summary>季节对应的环境声，编号超出范围（例如没有关卡时）返回 null。</summary>
        public AudioClip GetAmbience(int season)
        {
            return season >= 0 && season < _ambience.Length ? _ambience[season] : null;
        }
    }
}
