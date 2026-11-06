using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 面板角上的装饰：所属界面用季节主题时显示对应季节的角饰，用中性主题时显示中性装饰（为空则隐藏）。
    /// 跟随 <see cref="UiThemeScope"/> 的主题，所以和界面换色同一时刻切换，设置面板这种主题会变的界面也适用。
    /// 尺寸按素材像素 × 像素比例设置；左右、上下镜像由物体的缩放符号决定（只用左上角的素材）。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SeasonOrnament : MonoBehaviour
    {
        private const int Unresolved = int.MinValue;

        [Tooltip("留空时自动使用最近的父级作用域")]
        [SerializeField] private UiThemeScope _scope;
        [SerializeField] private UiThemeSet _themeSet;

        [Tooltip("按 春、夏、秋、冬 排列；某一项为空时该季节不显示")]
        [SerializeField] private Sprite[] _seasonSprites = new Sprite[4];

        [Tooltip("中性主题时的装饰，为空时不显示")]
        [SerializeField] private Sprite _neutralSprite;

        [Tooltip("素材的 1 个像素显示为多少界面单位")]
        [Min(1)]
        [SerializeField] private int _pixelScale = 4;

        [Tooltip("季节角饰离角落的距离（素材像素）")]
        [SerializeField] private Vector2 _seasonInset = Vector2.zero;

        [Tooltip("中性装饰离角落的距离（素材像素）")]
        [SerializeField] private Vector2 _neutralInset = new Vector2(3f, 3f);

        private Image _image;
        private RectTransform _rect;
        private int _shownSeason = Unresolved;
        private bool _hasReferences;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
            if (_scope == null)
            {
                _scope = GetComponentInParent<UiThemeScope>(true);
            }

            _hasReferences = _scope != null && _themeSet != null;
            if (!_hasReferences)
            {
                Debug.LogError($"[{nameof(SeasonOrnament)}] {name} 缺少主题作用域或主题组引用", this);
            }
        }

        private void OnEnable()
        {
            if (!_hasReferences)
            {
                return;
            }

            _scope.ColorsChanged += HandleColorsChanged;
            Apply();
        }

        private void OnDisable()
        {
            if (_hasReferences)
            {
                _scope.ColorsChanged -= HandleColorsChanged;
            }
        }

        private void HandleColorsChanged()
        {
            Apply();
        }

        private void Apply()
        {
            // 渐变期间每帧都会通知，季节没变就不重复设置
            int season = _themeSet.IndexOfSeasonTheme(_scope.Theme);
            if (season == _shownSeason)
            {
                return;
            }

            _shownSeason = season;
            Sprite sprite = season >= 0 ? (season < _seasonSprites.Length ? _seasonSprites[season] : null) : _neutralSprite;
            _image.enabled = sprite != null;
            if (sprite == null)
            {
                return;
            }

            _image.sprite = sprite;
            _rect.sizeDelta = sprite.rect.size * _pixelScale;

            // 轴心在素材左上角；镜像后仍从角落向面板内部延伸，所以内缩方向跟着缩放符号走
            Vector2 inset = (season >= 0 ? _seasonInset : _neutralInset) * _pixelScale;
            float signX = Mathf.Sign(_rect.localScale.x);
            float signY = Mathf.Sign(_rect.localScale.y);
            _rect.anchoredPosition = new Vector2(inset.x * signX, -inset.y * signY);
        }
    }
}
