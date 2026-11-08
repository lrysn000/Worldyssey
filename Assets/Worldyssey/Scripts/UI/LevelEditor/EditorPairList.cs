using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Worldyssey.Core;
using Worldyssey.Data;

namespace Worldyssey.UI
{
    /// <summary>
    /// 编辑器的配对列表：显示所有配对并高亮选中项，提供新增（当前世界 ↔ 目标世界）、删除、切换目标世界。
    /// </summary>
    public class EditorPairList : MonoBehaviour
    {
        [SerializeField] private LevelButton _itemPrefab;
        [SerializeField] private Transform _listRoot;

        [Tooltip("可选：列表所在的滚动区域。设置后选中项（例如刚新增的配对）会自动滚进可见范围")]
        [SerializeField] private ScrollRect _scrollRect;
        [Tooltip("可选：配对符号，显示在每行左侧，与箱子顶面和网格上的符号一致")]
        [SerializeField] private PairSymbolSet _pairSymbols;

        [SerializeField] private Button _targetButton;
        [SerializeField] private TMP_Text _targetLabel;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _removeButton;

        private readonly List<LevelButton> _items = new List<LevelButton>();

        public event Action<int> PairSelected;
        public event Action TargetCycleRequested;
        public event Action AddRequested;
        public event Action RemoveRequested;

        private void Awake()
        {
            if (_itemPrefab == null || _listRoot == null || _targetButton == null || _targetLabel == null
                || _addButton == null || _removeButton == null)
            {
                Debug.LogError($"[{nameof(EditorPairList)}] {name} 引用未配置完整", this);
            }
        }

        private void OnEnable()
        {
            if (_targetButton != null)
            {
                _targetButton.onClick.AddListener(HandleTargetClicked);
            }

            if (_addButton != null)
            {
                _addButton.onClick.AddListener(HandleAddClicked);
            }

            if (_removeButton != null)
            {
                _removeButton.onClick.AddListener(HandleRemoveClicked);
            }
        }

        private void OnDisable()
        {
            if (_targetButton != null)
            {
                _targetButton.onClick.RemoveListener(HandleTargetClicked);
            }

            if (_addButton != null)
            {
                _addButton.onClick.RemoveListener(HandleAddClicked);
            }

            if (_removeButton != null)
            {
                _removeButton.onClick.RemoveListener(HandleRemoveClicked);
            }
        }

        /// <summary>
        /// 重建列表。只在配对或选中项变化时调用，绘制格子时不调用。
        /// </summary>
        public void Render(LevelDraft draft, int selectedPair, int targetWorld)
        {
            if (_itemPrefab == null || _listRoot == null)
            {
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                // Destroy 要到帧末才生效，先隐藏，下面立即重排布局时旧项不再占位
                _items[i].gameObject.SetActive(false);
                Destroy(_items[i].gameObject);
            }

            _items.Clear();
            IReadOnlyList<DraftPair> pairs = draft.Pairs;
            for (int i = 0; i < pairs.Count; i++)
            {
                LevelButton item = Instantiate(_itemPrefab, _listRoot);
                string detail = string.Format(UiText.PairDetailFormat, draft.GetWorldName(pairs[i].WorldA), draft.GetWorldName(pairs[i].WorldB));
                item.Setup(string.Format(UiText.PairTitleFormat, i), detail, i, HandleItemClicked);
                item.SetIcon(_pairSymbols != null ? _pairSymbols.GetSymbol(i) : null);
                item.SetHighlighted(i == selectedPair);
                _items.Add(item);
            }

            if (_targetLabel != null)
            {
                _targetLabel.text = targetWorld >= 0 && targetWorld < draft.WorldCount
                    ? string.Format(UiText.TargetWorldFormat, draft.GetWorldName(targetWorld))
                    : string.Format(UiText.TargetWorldFormat, "-");
            }

            if (_removeButton != null)
            {
                _removeButton.interactable = selectedPair >= 0;
            }

            ScrollToItem(selectedPair);
        }

        /// <summary>把第 <paramref name="index"/> 项滚进可见范围，已经可见时不动。</summary>
        private void ScrollToItem(int index)
        {
            if (_scrollRect == null || index < 0 || index >= _items.Count)
            {
                return;
            }

            RectTransform content = _scrollRect.content;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float viewHeight = _scrollRect.viewport.rect.height;
            if (content.rect.height <= viewHeight)
            {
                return;
            }

            // 内容区以顶边为原点向下排列，anchoredPosition.y 就是当前滚过的距离
            var item = (RectTransform)_items[index].transform;
            float itemTop = -(item.localPosition.y + item.rect.height * (1f - item.pivot.y));
            float itemBottom = itemTop + item.rect.height;
            float offset = content.anchoredPosition.y;
            if (itemTop < offset)
            {
                offset = itemTop;
            }
            else if (itemBottom > offset + viewHeight)
            {
                offset = itemBottom - viewHeight;
            }

            content.anchoredPosition = new Vector2(content.anchoredPosition.x, offset);
        }

        private void HandleItemClicked(int pair)
        {
            PairSelected?.Invoke(pair);
        }

        private void HandleTargetClicked()
        {
            TargetCycleRequested?.Invoke();
        }

        private void HandleAddClicked()
        {
            AddRequested?.Invoke();
        }

        private void HandleRemoveClicked()
        {
            RemoveRequested?.Invoke();
        }
    }
}
