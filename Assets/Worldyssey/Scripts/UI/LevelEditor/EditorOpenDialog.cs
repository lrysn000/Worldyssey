using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器中“打开关卡”的列表对话框，列出全部自建关卡。
    /// </summary>
    public class EditorOpenDialog : UiPanel
    {
        [SerializeField] private LevelButton _itemPrefab;
        [SerializeField] private Transform _listRoot;
        [SerializeField] private Button _cancelButton;

        private readonly List<LevelButton> _items = new List<LevelButton>();

        /// <summary>选择了一个关卡，参数为传入列表中的下标。</summary>
        public event Action<int> LevelChosen;

        public event Action Cancelled;

        private void Awake()
        {
            CheckReference(_itemPrefab, nameof(_itemPrefab));
            CheckReference(_listRoot, nameof(_listRoot));
            CheckReference(_cancelButton, nameof(_cancelButton));
        }

        private void OnEnable()
        {
            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(HandleCancelClicked);
            }
        }

        private void OnDisable()
        {
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }
        }

        public void Show(IReadOnlyList<CustomLevelEntry> levels)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                Destroy(_items[i].gameObject);
            }

            _items.Clear();
            if (_itemPrefab != null && _listRoot != null)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelButton item = Instantiate(_itemPrefab, _listRoot);
                    item.Setup(levels[i].Level.DisplayName, levels[i].Level.Id, i, HandleItemClicked);
                    _items.Add(item);
                }
            }

            SetVisible(true);
        }

        private void HandleItemClicked(int index)
        {
            LevelChosen?.Invoke(index);
        }

        private void HandleCancelClicked()
        {
            Cancelled?.Invoke();
        }
    }
}
