using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Worldyssey.UI
{
    /// <summary>
    /// 选关界面：内置关卡列表和玩家自建关卡列表。每次打开时按最新数据重建按钮。
    /// </summary>
    public class LevelSelectPanel : UiPanel
    {
        [SerializeField] private LevelButton _levelButtonPrefab;
        [SerializeField] private Transform _builtInListRoot;
        [SerializeField] private Transform _customListRoot;
        [SerializeField] private TMP_Text _noCustomLevelsText;
        [SerializeField] private Button _backButton;

        private readonly List<LevelButton> _spawnedButtons = new List<LevelButton>();

        /// <summary>选择了内置关卡，参数为关卡目录中的下标。</summary>
        public event Action<int> BuiltInLevelChosen;

        /// <summary>选择了自建关卡，参数为自建列表中的下标。</summary>
        public event Action<int> CustomLevelChosen;

        public event Action BackRequested;

        private void Awake()
        {
            CheckReference(_levelButtonPrefab, nameof(_levelButtonPrefab));
            CheckReference(_builtInListRoot, nameof(_builtInListRoot));
            CheckReference(_customListRoot, nameof(_customListRoot));
            CheckReference(_noCustomLevelsText, nameof(_noCustomLevelsText));
            CheckReference(_backButton, nameof(_backButton));
        }

        private void OnEnable()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void OnDisable()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        /// <summary>
        /// 用最新数据重建两个列表。选关界面打开频率低、按钮少，直接销毁重建，不做对象池。
        /// </summary>
        public void Populate(IReadOnlyList<LevelSelectItem> builtInLevels, IReadOnlyList<LevelSelectItem> customLevels)
        {
            ClearButtons();
            if (_levelButtonPrefab == null || _builtInListRoot == null || _customListRoot == null)
            {
                return;
            }

            for (int i = 0; i < builtInLevels.Count; i++)
            {
                SpawnButton(_builtInListRoot, builtInLevels[i], i, HandleBuiltInClicked);
            }

            for (int i = 0; i < customLevels.Count; i++)
            {
                SpawnButton(_customListRoot, customLevels[i], i, HandleCustomClicked);
            }

            if (_noCustomLevelsText != null)
            {
                _noCustomLevelsText.gameObject.SetActive(customLevels.Count == 0);
            }
        }

        private void SpawnButton(Transform parent, LevelSelectItem item, int index, Action<int> onClicked)
        {
            LevelButton button = Instantiate(_levelButtonPrefab, parent);
            button.Setup(item, index, onClicked);
            _spawnedButtons.Add(button);
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null)
                {
                    Destroy(_spawnedButtons[i].gameObject);
                }
            }

            _spawnedButtons.Clear();
        }

        private void HandleBuiltInClicked(int index)
        {
            BuiltInLevelChosen?.Invoke(index);
        }

        private void HandleCustomClicked(int index)
        {
            CustomLevelChosen?.Invoke(index);
        }

        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
