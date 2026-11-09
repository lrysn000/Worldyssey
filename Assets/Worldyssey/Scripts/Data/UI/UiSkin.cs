using TMPro;
using UnityEngine;

namespace Worldyssey.Data
{
    /// <summary>
    /// 界面套件：所有界面共用的素材和字体（像素风）。只在编辑器生成、换皮界面时读取，颜色仍由主题决定，所以素材都用白色或浅色、可着色的。
    /// 换素材或字体后重新运行 Worldyssey/Art/UI/Apply Pixel UI Skin 即可，布局不变。
    /// </summary>
    [CreateAssetMenu(fileName = "UiSkin", menuName = "Worldyssey/UI/Skin")]
    public class UiSkin : ScriptableObject
    {
        [Header("素材")]
        [Tooltip("面板、卡片、标题栏等大块底板（九宫格）")]
        [SerializeField] private Sprite _panel;

        [Tooltip("按钮、页签等可点击的底板（九宫格）")]
        [SerializeField] private Sprite _button;

        [Tooltip("滑条的轨道（九宫格）")]
        [SerializeField] private Sprite _sliderTrack;

        [Tooltip("滑条的把手")]
        [SerializeField] private Sprite _sliderHandle;

        [SerializeField] private Sprite _checkIcon;
        [SerializeField] private Sprite _crossIcon;
        [SerializeField] private Sprite _arrowDownIcon;

        [Header("装饰（灰阶素材随主题着色）")]
        [Tooltip("装饰面板的外框：双层边框，中间透明，盖在面板底上")]
        [SerializeField] private Sprite _frame;

        [Tooltip("面板内部的平铺底纹（白色点阵），按下面的颜色叠在面板底上，不随主题变色")]
        [SerializeField] private Sprite _pattern;

        [Tooltip("底纹颜色：淡淡的浅灰，不透明度为 0 时不加底纹")]
        [SerializeField] private Color _patternColor = new Color(0.8f, 0.8f, 0.8f, 0.35f);

        [Tooltip("标题栏、顶栏")]
        [SerializeField] private Sprite _titleBar;

        [Tooltip("凹进去的小框，用于图标槽")]
        [SerializeField] private Sprite _inset;

        [Tooltip("按钮按下时的样子（整体下沉一格）")]
        [SerializeField] private Sprite _buttonPressed;

        [Tooltip("选中时四个角的括号（左上角的样子，其余三个角镜像）")]
        [SerializeField] private Sprite _selectCorner;

        [SerializeField] private Sprite _pipOn;
        [SerializeField] private Sprite _pipOff;

        [Header("角饰（彩色，不着色）")]
        [Tooltip("按 春、夏、秋、冬 排列，放在面板左上和右上角（右边镜像）")]
        [SerializeField] private Sprite[] _seasonOrnaments = new Sprite[4];

        [Tooltip("中性界面四个角的装饰")]
        [SerializeField] private Sprite _neutralOrnament;

        [Header("字体")]
        [Tooltip("标题、按钮文字等原本加粗的文字")]
        [SerializeField] private TMP_FontAsset _titleFont;

        [Tooltip("正文")]
        [SerializeField] private TMP_FontAsset _bodyFont;

        [Tooltip("SDF 字体（非点阵）整体放大的倍数，写入字体资源的 Face Info Scale，所有用它的文字一起放大。" +
            "当前界面字号都是 11 的倍数；Fusion Pixel（multilan_85）的一个字形像素约为 1/14.81 字号，" +
            "取 14.81 ÷ 11 ≈ 1.347 时每个字形像素正好落在整数个屏幕像素上，笔画最整齐")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _sdfFontScale = 1.347f;

        [Tooltip("把字号取整到字体设计尺寸的整数倍（像素字体只有这样笔画才粗细一致），并关闭自动缩放字号")]
        [SerializeField] private bool _isFontSizeSnapped = true;

        [Tooltip("文字至少放大到设计尺寸的几倍。1 倍的像素字太小，小字建议至少 2 倍")]
        [Range(1, 4)]
        [SerializeField] private int _minTextScale = 2;

        [Header("像素比例")]
        [Tooltip("1080p 下素材的 1 个像素放大成多少个屏幕像素。与 3D 画面的像素大小接近时风格最统一（1080 ÷ 场景像素高度）")]
        [Range(1, 8)]
        [SerializeField] private int _uiPixelScale = 4;

        [Tooltip("编辑器网格格子这类小元素用的像素比例，太大会让边框占满格子")]
        [Range(1, 8)]
        [SerializeField] private int _smallPixelScale = 2;

        // 默认值版本：素材方案更新时，生成工具据此决定是否覆盖旧的默认素材
        [SerializeField, HideInInspector] private int _defaultsVersion;

        public Sprite Panel => _panel;
        public Sprite Frame => _frame;
        public Sprite Pattern => _pattern;
        public Color PatternColor => _patternColor;
        public bool HasPattern => _pattern != null && _patternColor.a > 0f;
        public Sprite TitleBar => _titleBar;
        public Sprite Inset => _inset;
        public Sprite ButtonPressed => _buttonPressed;
        public Sprite SelectCorner => _selectCorner;
        public Sprite PipOn => _pipOn;
        public Sprite PipOff => _pipOff;
        public Sprite[] SeasonOrnaments => _seasonOrnaments;
        public Sprite NeutralOrnament => _neutralOrnament;
        public Sprite Button => _button;
        public Sprite SliderTrack => _sliderTrack;
        public Sprite SliderHandle => _sliderHandle;
        public Sprite CheckIcon => _checkIcon;
        public Sprite CrossIcon => _crossIcon;
        public Sprite ArrowDownIcon => _arrowDownIcon;
        public TMP_FontAsset TitleFont => _titleFont;
        public TMP_FontAsset BodyFont => _bodyFont;
        public bool IsFontSizeSnapped => _isFontSizeSnapped;
        public int MinTextScale => _minTextScale;
        public float SdfFontScale => _sdfFontScale;
        public int UiPixelScale => _uiPixelScale;
        public int SmallPixelScale => _smallPixelScale;
    }
}
