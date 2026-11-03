using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器顶部栏：关卡名、尺寸调整、世界标签与增删世界。只发出请求事件，不直接修改草稿。
    /// </summary>
    public class EditorHeaderBar : MonoBehaviour
    {
        private static readonly Color TabNormalColor = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color TabSelectedColor = new Color(0.30f, 0.50f, 0.85f, 1f);

        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private TMP_Text _sizeText;
        [SerializeField] private Button _widthMinusButton;
        [SerializeField] private Button _widthPlusButton;
        [SerializeField] private Button _heightMinusButton;
        [SerializeField] private Button _heightPlusButton;

        [Tooltip("最多 4 个世界标签，按世界编号排列")]
        [SerializeField] private Button[] _worldTabs = new Button[0];
        [SerializeField] private Button _addWorldButton;
        [SerializeField] private Button _removeWorldButton;

        private readonly EditorButtonBindings _bindings = new EditorButtonBindings();

        public event Action<string> NameEdited;

        /// <summary>请求改变尺寸，参数为宽、高的增量。</summary>
        public event Action<int, int> ResizeRequested;

        public event Action<int> WorldSelected;
        public event Action AddWorldRequested;
        public event Action RemoveWorldRequested;

        private void Awake()
        {
            if (_nameInput == null || _sizeText == null || _worldTabs.Length == 0)
            {
                Debug.LogError($"[{nameof(EditorHeaderBar)}] {name} 引用未配置完整", this);
            }

            _bindings.Add(_widthMinusButton, () => ResizeRequested?.Invoke(-1, 0));
            _bindings.Add(_widthPlusButton, () => ResizeRequested?.Invoke(1, 0));
            _bindings.Add(_heightMinusButton, () => ResizeRequested?.Invoke(0, -1));
            _bindings.Add(_heightPlusButton, () => ResizeRequested?.Invoke(0, 1));
            _bindings.Add(_addWorldButton, () => AddWorldRequested?.Invoke());
            _bindings.Add(_removeWorldButton, () => RemoveWorldRequested?.Invoke());
            for (int i = 0; i < _worldTabs.Length; i++)
            {
                int world = i;
                _bindings.Add(_worldTabs[i], () => WorldSelected?.Invoke(world));
            }
        }

        private void OnEnable()
        {
            _bindings.Subscribe();
            if (_nameInput != null)
            {
                _nameInput.onEndEdit.AddListener(HandleNameEndEdit);
            }
        }

        private void OnDisable()
        {
            _bindings.Unsubscribe();
            if (_nameInput != null)
            {
                _nameInput.onEndEdit.RemoveListener(HandleNameEndEdit);
            }
        }

        public void Render(LevelDraft draft, int currentWorld)
        {
            if (_nameInput != null)
            {
                _nameInput.SetTextWithoutNotify(draft.DisplayName);
            }

            if (_sizeText != null)
            {
                _sizeText.text = string.Format(UiText.SizeFormat, draft.Width, draft.Height);
            }

            for (int i = 0; i < _worldTabs.Length; i++)
            {
                Button tab = _worldTabs[i];
                if (tab == null)
                {
                    continue;
                }

                bool exists = i < draft.WorldCount;
                tab.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                tab.image.color = i == currentWorld ? TabSelectedColor : TabNormalColor;
                TMP_Text label = tab.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = draft.GetWorldName(i) + (i == draft.StartWorld ? UiText.StartWorldMark : string.Empty);
                }
            }
        }

        private void HandleNameEndEdit(string text)
        {
            NameEdited?.Invoke(text.Trim());
        }
    }
}
