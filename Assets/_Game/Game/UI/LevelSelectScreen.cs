using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class LevelSelectScreen : UIScreen
    {
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _content;
        [Tooltip("Inactive item cloned per level.")]
        [SerializeField] private Button _template;
        [SerializeField] private Button _close;

        private readonly List<Button> _items = new List<Button>();

        public event Action<int> LevelChosen;
        public event Action Closed;

        public void Init()
        {
            _template.gameObject.SetActive(false);
            Bind(_close, () => Closed?.Invoke());
        }

        public void Show(int levelCount, Func<int, bool> isUnlocked, int currentLevel)
        {
            EnsureItems(levelCount);
            for (int i = 0; i < _items.Count; i++)
            {
                Button item = _items[i];
                bool unlocked = isUnlocked(i);
                item.interactable = unlocked;
                item.transform.GetChild(0).GetComponent<Text>().text = (i + 1).ToString();
                item.transform.GetChild(1).gameObject.SetActive(unlocked && i == currentLevel);
                item.transform.GetChild(2).gameObject.SetActive(!unlocked);
            }

            base.Show();
            ScrollTo(currentLevel);
        }

        private void EnsureItems(int count)
        {
            while (_items.Count < count)
            {
                int index = _items.Count;
                Button item = Instantiate(_template, _content);
                item.name = $"Level {index + 1}";
                item.gameObject.SetActive(true);
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => LevelChosen?.Invoke(index));
                _items.Add(item);
            }
            for (int i = 0; i < _items.Count; i++)
                _items[i].gameObject.SetActive(i < count);
        }

        private void ScrollTo(int index)
        {
            if (_scroll == null || _items.Count == 0)
                return;

            Canvas.ForceUpdateCanvases();
            float contentHeight = _content.rect.height;
            float viewportHeight = _scroll.viewport != null ? _scroll.viewport.rect.height : ((RectTransform)_scroll.transform).rect.height;
            if (contentHeight <= viewportHeight)
            {
                _scroll.verticalNormalizedPosition = 1f;
                return;
            }

            var item = (RectTransform)_items[Mathf.Clamp(index, 0, _items.Count - 1)].transform;
            float itemCentreFromTop = -item.anchoredPosition.y;
            float scrolled = Mathf.Clamp01((itemCentreFromTop - viewportHeight / 2f) / (contentHeight - viewportHeight));
            _scroll.verticalNormalizedPosition = 1f - scrolled;
        }
    }
}
