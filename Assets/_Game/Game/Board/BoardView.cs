using System;
using System.Collections.Generic;
using ColorSort.Core.Board;
using ColorSort.Game.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.Board
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private BottleView _bottlePrefab;
        [SerializeField] private ColorPalette _palette;

        [Tooltip("Stream image shown while pouring; reparented under this board.")]
        [SerializeField] private Image _pourStream;

        [Header("Layout")]
        [Tooltip("Upper bound; the row size that makes bottles largest in the free area is chosen per level.")]
        [SerializeField, Min(1)] private int _maxPerRow = 7;
        [SerializeField] private Vector2 _spacing = new Vector2(130f, 533f);
        [SerializeField] private Vector2 _bottleSize = new Vector2(120f, 381f);
        [Tooltip("Share of the screen height kept clear for the top HUD.")]
        [SerializeField, Range(0f, 0.5f)] private float _topReserved = 0.26f;
        [Tooltip("Share of the screen height kept clear for the bottom buttons.")]
        [SerializeField, Range(0f, 0.5f)] private float _bottomReserved = 0.16f;
        [SerializeField, Min(0f)] private float _sideMargin = 20f;

        [Header("Pour")]
        [Tooltip("Gap between the pouring bottle's mouth and the target's mouth.")]
        [SerializeField] private float _lipGap = 30f;
        [Tooltip("Tilt when pouring from a full bottle; it tips further as it empties.")]
        [SerializeField, Range(0f, 180f)] private float _fullPourAngle = 55f;
        [SerializeField, Range(0f, 180f)] private float _emptyPourAngle = 110f;
        [SerializeField] private float _moveSeconds = 0.3f;
        [SerializeField] private float _secondsPerUnit = 0.3f;

        [Header("Pour effects")]
        [SerializeField] private Sprite _streamSprite;
        [SerializeField] private Sprite _dropletSprite;
        [SerializeField] private Sprite _rippleSprite;
        [SerializeField] private float _streamWidth = 18f;

        private readonly List<BottleView> _bottles = new List<BottleView>();
        private Vector2 _laidOutFor;
        private RectTransform _effectsLayer;
        private PourEffects _effects;
        private BottleDesign _bottleDesign;

        public event Action<int> BottleClicked;
        public event Action PourStarted;
        public event Action PourFinished;

        private RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            var layer = new GameObject("Pour Effects", typeof(RectTransform));
            _effectsLayer = (RectTransform)layer.transform;
            _effectsLayer.SetParent(transform, worldPositionStays: false);

            if (_pourStream != null)
                _pourStream.transform.SetParent(_effectsLayer, worldPositionStays: false);
            _effects = new PourEffects(_effectsLayer, _pourStream, _streamSprite, _dropletSprite, _rippleSprite, _streamWidth);
        }

        private void Update() => _effects.Tick();

        private void OnDestroy() => DOTween.Kill(this);

        private void LateUpdate()
        {
            if (_bottles.Count > 0 && ParentSize != _laidOutFor && !DOTween.IsTweening(this))
                Relayout(animate: false);
        }

        public void Build(IReadOnlyList<Bottle> bottles)
        {
            Clear();
            for (int i = 0; i < bottles.Count; i++)
                Spawn(bottles[i]);
            Relayout(animate: false);
        }

        public void AddBottle(Bottle bottle)
        {
            Spawn(bottle);
            Relayout(animate: true);
        }

        public void Refresh(IReadOnlyList<Bottle> bottles)
        {
            for (int i = 0; i < _bottles.Count; i++)
                _bottles[i].Render(bottles[i]);
        }

        // Applies a bottle design to every bottle, including ones added later.
        public void SetBottleDesign(BottleDesign design)
        {
            _bottleDesign = design;
            foreach (BottleView bottle in _bottles)
                bottle.SetDesign(design);
        }

        public void SetSelected(int index, bool selected) => _bottles[index].SetSelected(selected);

        public void ShowRejected(int index) => _bottles[index].Shake();

        public void ShowHint(int from, int to)
        {
            ClearHint();
            _bottles[from].SetHinted(true);
            _bottles[to].SetHinted(true);
        }

        public void ClearHint()
        {
            foreach (BottleView bottle in _bottles)
                bottle.SetHinted(false);
        }

        public Sequence PlayPour(Pour pour, IReadOnlyList<Bottle> bottles)
        {
            BottleView source = _bottles[pour.From];
            BottleView target = _bottles[pour.To];
            int sourceCountBefore = bottles[pour.From].Count + pour.Amount;
            int targetCountBefore = bottles[pour.To].Count - pour.Amount;
            float pourSeconds = pour.Amount * _secondsPerUnit;

            int capacity = bottles[pour.From].Capacity;
            Vector2 home = source.Rect.anchoredPosition;
            Vector2 mouth = target.HomePosition + new Vector2(0f, target.LipHeight + _lipGap);

            // Pour from the side facing the screen centre; negative angles tip clockwise (towards the right).
            float direction = target.HomePosition.x >= 0f ? -1f : 1f;
            float startAngle = direction * PourAngle(sourceCountBefore, capacity);
            float endAngle = direction * PourAngle(sourceCountBefore - pour.Amount, capacity);

            source.Rect.DOKill();
            source.transform.SetAsLastSibling();
            _effectsLayer.SetSiblingIndex(source.transform.GetSiblingIndex() - 1);

            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Append(Pose(source, t => Mathf.Lerp(0f, startAngle, t),
                (t, angle) => Vector2.Lerp(home, MouthOver(mouth, source, angle), t), _moveSeconds, Ease.InOutSine));
            sequence.AppendCallback(() =>
            {
                _effects.BeginStream(mouth, () => target.HomePosition.y + target.SurfaceHeight, _palette.GetColor(pour.Color));
                PourStarted?.Invoke();
            });
            sequence.Append(source.Drain(sourceCountBefore, pour.Amount, _secondsPerUnit));
            sequence.Join(target.Fill(targetCountBefore, pour.Amount, pour.Color, _secondsPerUnit));
            sequence.Join(Pose(source, t => Mathf.Lerp(startAngle, endAngle, t),
                (t, angle) => MouthOver(mouth, source, angle), pourSeconds, Ease.Linear));
            sequence.AppendCallback(() =>
            {
                _effects.EndStream();
                PourFinished?.Invoke();
                target.SetComplete(bottles[pour.To].IsComplete);
                target.Slosh();
            });
            sequence.Append(Pose(source, t => Mathf.Lerp(endAngle, 0f, t),
                (t, angle) => Vector2.Lerp(MouthOver(mouth, source, endAngle), source.HomePosition, t), _moveSeconds, Ease.InOutSine));
            sequence.AppendCallback(source.Slosh);
            return sequence;
        }

        private void Spawn(Bottle bottle)
        {
            BottleView view = Instantiate(_bottlePrefab, transform);
            view.name = $"Bottle {_bottles.Count}";
            view.Initialize(_bottles.Count, bottle.Capacity, _palette);
            view.SetDesign(_bottleDesign);
            view.Render(bottle);
            view.Clicked += OnBottleClicked;
            _bottles.Add(view);
        }

        private Vector2 ParentSize => ((RectTransform)transform.parent).rect.size;

        // Free space between the HUD bars, in the parent's units.
        private Vector2 AvailableSize
        {
            get
            {
                Vector2 parent = ParentSize;
                return new Vector2(parent.x - 2f * _sideMargin, parent.y * (1f - _topReserved - _bottomReserved));
            }
        }

        private void Relayout(bool animate)
        {
            _laidOutFor = ParentSize;
            Vector2 available = AvailableSize;

            int perRow = BoardLayout.BestPerRow(_bottles.Count, _maxPerRow, _spacing, _bottleSize, available);
            Vector2[] positions = BoardLayout.Arrange(_bottles.Count, perRow, _spacing);
            for (int i = 0; i < _bottles.Count; i++)
                _bottles[i].SetHomePosition(positions[i], animate, _moveSeconds);

            float scale = BoardLayout.FitScale(_bottles.Count, perRow, _spacing, _bottleSize, available);
            Rect.localScale = new Vector3(scale, scale, 1f);
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = new Vector2(0f, (_bottomReserved - _topReserved) * 0.5f * ParentSize.y);
        }

        // Fuller bottles pour at a shallower angle; an almost empty one has to tip past horizontal.
        private float PourAngle(int unitsInBottle, int capacity) =>
            Mathf.Lerp(_emptyPourAngle, _fullPourAngle, capacity > 0 ? (float)unitsInBottle / capacity : 0f);

        // Where the bottle's centre must be for its mouth to sit at the given point at this tilt.
        private static Vector2 MouthOver(Vector2 mouth, BottleView bottle, float angle) =>
            mouth - BottleView.Rotate(new Vector2(0f, bottle.LipHeight), angle);

        private static Tween Pose(BottleView bottle, Func<float, float> angleAt, Func<float, float, Vector2> positionAt, float seconds, Ease ease) =>
            DOTween.To(() => 0f, t =>
                {
                    float angle = angleAt(t);
                    bottle.SetTilt(angle);
                    bottle.Rect.anchoredPosition = positionAt(t, angle);
                }, 1f, seconds)
                .SetEase(ease)
                .SetTarget(bottle.Rect);

        private void Clear()
        {
            DOTween.Kill(this);
            foreach (BottleView view in _bottles)
            {
                view.Clicked -= OnBottleClicked;
                Destroy(view.gameObject);
            }
            _bottles.Clear();
            _effects.Clear();
        }

        private void OnBottleClicked(BottleView view) => BottleClicked?.Invoke(view.Index);
    }
}
