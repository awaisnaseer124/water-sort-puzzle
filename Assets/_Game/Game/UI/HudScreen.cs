using System;
using ColorSort.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class HudScreen : UIScreen
    {
        [SerializeField] private Button _home;
        [SerializeField] private Button _restart;
        [SerializeField] private Button _extraBottle;
        [SerializeField] private Button _undo;
        [Tooltip("Hints cost a rewarded video.")]
        [SerializeField] private Button _hint;

        [SerializeField] private Text _levelNumber;
        [SerializeField] private GameObject _instruction;

        [Header("Moves")]
        [SerializeField] private GameObject _movesContainer;
        [SerializeField] private Text _moves;
        [Tooltip("Index 0 = first star (always earned), 2 = third star.")]
        [SerializeField] private Image[] _stars = new Image[3];
        [Tooltip("Move limit shown under each star; the first star has none.")]
        [SerializeField] private Text[] _starLimits = new Text[3];
        [SerializeField] private Sprite _starEarned;
        [SerializeField] private Sprite _starLost;

        [Header("Timer")]
        [SerializeField] private GameObject _timerContainer;
        [SerializeField] private Text _minutes;
        [SerializeField] private Text _seconds;
        [Tooltip("Where the timer row moves when the moves row is also shown (Hard mode).")]
        [SerializeField] private Vector2 _timerBelowMovesOffset = new Vector2(0f, -72f);

        private Vector2 _timerHome;

        public event Action HomeClicked;
        public event Action RestartClicked;
        public event Action ExtraBottleClicked;
        public event Action UndoClicked;
        public event Action HintClicked;

        public void Init()
        {
            Bind(_home, () => HomeClicked?.Invoke());
            Bind(_restart, () => RestartClicked?.Invoke());
            Bind(_extraBottle, () => ExtraBottleClicked?.Invoke());
            Bind(_undo, () => UndoClicked?.Invoke());
            Bind(_hint, () => HintClicked?.Invoke());
            _timerHome = ((RectTransform)_timerContainer.transform).anchoredPosition;
        }

        public void ShowLevel(int levelNumber, StarThresholds stars, bool showMoves, bool showTimer)
        {
            _levelNumber.text = levelNumber.ToString();
            _movesContainer.SetActive(showMoves);
            _timerContainer.SetActive(showTimer);
            ((RectTransform)_timerContainer.transform).anchoredPosition =
                _timerHome + (showMoves && showTimer ? _timerBelowMovesOffset : Vector2.zero);
            _instruction.SetActive(true);
            SetExtraBottleAvailable(true);
            SetUndoAvailable(false);

            SetLimit(0, "");
            SetLimit(1, stars.TwoStarMaxMoves.ToString());
            SetLimit(2, stars.ThreeStarMaxMoves.ToString());
            SetMoves(0, 3);
        }

        public void SetMoves(int moves, int rating)
        {
            _moves.text = moves.ToString();
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].sprite = i < rating ? _starEarned : _starLost;
        }

        public void SetTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            _minutes.text = (total / 60).ToString("00") + " :";
            _seconds.text = (total % 60).ToString("00");
        }

        public void HideInstruction() => _instruction.SetActive(false);

        public void SetExtraBottleAvailable(bool available) => _extraBottle.interactable = available;

        public void SetUndoAvailable(bool available) => _undo.interactable = available;

        private void SetLimit(int star, string text)
        {
            if (star < _starLimits.Length && _starLimits[star] != null)
                _starLimits[star].text = text;
        }
    }
}
