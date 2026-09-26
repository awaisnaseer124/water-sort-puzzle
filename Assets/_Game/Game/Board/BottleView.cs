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
    public sealed class BottleView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [Tooltip("Glass body behind the liquid; its shape also clips the liquid.")]
        [SerializeField] private Image _body;
        [Tooltip("Glass details in front of the liquid.")]
        [SerializeField] private Image _glass;

        [Tooltip("Parent for the generated layer images (inside the bottle's mask).")]
        [SerializeField] private RectTransform _liquidRoot;

        [Tooltip("Inactive, vertically filled image cloned once per layer.")]
        [SerializeField] private Image _layerTemplate;

        [SerializeField] private GameObject _completeMarker;

        [Header("Liquid geometry (local units)")]
        [SerializeField] private float _liquidBottom = -194f;
        [SerializeField] private float _liquidFullHeight = 304f;
        [Tooltip("Inside of the glass, used to keep the liquid level and its volume constant when tilted.")]
        [SerializeField] private float _interiorTop = 150f;
        [SerializeField] private float _interiorWidth = 94f;
        [Tooltip("Height of the bottle's mouth above its centre; the pour stream starts here.")]
        [SerializeField] private float _lipHeight = 170f;
        [Tooltip("Extra layer width so the liquid still fills the glass when it lies on its side.")]
        [SerializeField] private float _layerOverdraw = 500f;

        [Header("Slosh")]
        [SerializeField] private float _sloshDegrees = 6f;
        [SerializeField] private float _sloshSeconds = 0.8f;

        [Header("Feedback")]
        [SerializeField] private float _selectedLift = 30f;
        [SerializeField] private float _selectSeconds = 0.1f;
        [SerializeField] private float _shakeSeconds = 0.5f;
        [SerializeField] private float _shakeDegrees = 30f;
        [SerializeField] private float _hintScale = 1.12f;
        [SerializeField] private float _hintPulseSeconds = 0.45f;

        private readonly List<Image> _layers = new List<Image>();
        private ColorPalette _palette;
        private float _layerHeight;
        private float _tilt;

        public event Action<BottleView> Clicked;

        public int Index { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public Vector2 HomePosition { get; private set; }
        public float LipHeight => _lipHeight;

        // Height of the liquid surface above the bottle centre while upright, following fill animations.
        public float SurfaceHeight
        {
            get
            {
                float units = 0f;
                foreach (Image layer in _layers)
                    units += layer.fillAmount;
                return _liquidBottom + units * _layerHeight;
            }
        }

        private void Awake()
        {
            _button.onClick.AddListener(() => Clicked?.Invoke(this));
            _layerTemplate.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Rect.DOKill();
            DOTween.Kill(this);
            DOTween.Kill(_liquidRoot);
        }

        public void Initialize(int index, int capacity, ColorPalette palette)
        {
            Index = index;
            _palette = palette;

            _layerHeight = _liquidFullHeight / capacity;
            for (int i = 0; i < capacity; i++)
            {
                Image layer = Instantiate(_layerTemplate, _liquidRoot);
                layer.name = $"Layer {i}";
                layer.gameObject.SetActive(true);
                layer.rectTransform.sizeDelta = new Vector2(_layerTemplate.rectTransform.sizeDelta.x + _layerOverdraw, _layerHeight);
                _layers.Add(layer);
            }
            SetTilt(0f);
        }

        // Tilts the glass while the liquid stays level. The liquid root counter-rotates so the layers remain
        // horizontal, and each layer thins as the glass lies down so the volume looks constant.
        public void SetTilt(float degrees)
        {
            _tilt = degrees;
            Rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
            _liquidRoot.localRotation = Quaternion.Euler(0f, 0f, -degrees);

            LiquidGeometry geometry = LiquidGeometry.At(degrees, _liquidBottom, _interiorTop, _interiorWidth, _layerHeight);

            for (int i = 0; i < _layers.Count; i++)
            {
                RectTransform rect = _layers[i].rectTransform;
                rect.anchoredPosition = new Vector2(geometry.Centre.x, geometry.Lowest + i * geometry.LayerThickness);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, geometry.LayerThickness);
            }
        }

        // A short damped wobble of the liquid surface after a pour.
        public void Slosh()
        {
            DOTween.Kill(_liquidRoot);
            DOTween.To(() => 0f, t =>
                {
                    float angle = _sloshDegrees * Mathf.Exp(-4f * t) * Mathf.Sin(t * Mathf.PI * 6f);
                    _liquidRoot.localRotation = Quaternion.Euler(0f, 0f, angle - _tilt);
                }, 1f, _sloshSeconds)
                .SetEase(Ease.Linear)
                .SetTarget(_liquidRoot)
                .OnComplete(() => _liquidRoot.localRotation = Quaternion.Euler(0f, 0f, -_tilt));
        }

        public static Vector2 Rotate(Vector2 v, float degrees) => LiquidGeometry.Rotate(v, degrees);

        public void SetHomePosition(Vector2 position, bool animate, float seconds)
        {
            HomePosition = position;
            Rect.DOKill();
            if (animate)
                Rect.DOAnchorPosCore(position, seconds);
            else
                Rect.anchoredPosition = position;
        }

        public void Render(Bottle bottle)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                Image layer = _layers[i];
                DOTween.Kill(layer);
                bool filled = i < bottle.Count;
                layer.fillAmount = filled ? 1f : 0f;
                if (filled)
                    layer.color = _palette.GetColor(bottle[i]);
            }
            SetComplete(bottle.IsComplete);
            DOTween.Kill(_liquidRoot);
            SetTilt(0f);
        }

        public void SetDesign(BottleDesign design)
        {
            if (design == null)
                return;
            if (_body != null && design.Body != null)
                _body.sprite = design.Body;
            if (_glass != null)
            {
                _glass.sprite = design.Glass;
                _glass.enabled = design.Glass != null;
            }
        }

        public void SetComplete(bool complete)
        {
            if (_completeMarker != null)
                _completeMarker.SetActive(complete);
        }

        public void SetSelected(bool selected)
        {
            Rect.DOKill();
            Rect.DOAnchorPosCore(HomePosition + (selected ? new Vector2(0f, _selectedLift) : Vector2.zero), _selectSeconds);
        }

        public void SetHinted(bool hinted)
        {
            DOTween.Kill(this);
            transform.localScale = Vector3.one;
            if (!hinted)
                return;

            DOTween.To(() => transform.localScale, v => transform.localScale = v, Vector3.one * _hintScale, _hintPulseSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        public Tween Shake()
        {
            Rect.DOKill(complete: true);
            Rect.anchoredPosition = HomePosition;
            return Rect.DOShakeRotation(_shakeSeconds, new Vector3(0f, 0f, _shakeDegrees))
                .OnComplete(() => Rect.localRotation = Quaternion.identity);
        }

        public Tween Drain(int countBefore, int amount, float secondsPerUnit)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < amount; i++)
                sequence.Append(TweenFill(_layers[countBefore - 1 - i], 0f, secondsPerUnit));
            return sequence;
        }

        public Tween Fill(int countBefore, int amount, byte color, float secondsPerUnit)
        {
            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < amount; i++)
            {
                Image layer = _layers[countBefore + i];
                layer.color = _palette.GetColor(color);
                layer.fillAmount = 0f;
                sequence.Append(TweenFill(layer, 1f, secondsPerUnit));
            }
            return sequence;
        }

        private static Tween TweenFill(Image layer, float target, float seconds) =>
            DOTween.To(() => layer.fillAmount, v => layer.fillAmount = v, target, seconds)
                .SetEase(Ease.Linear)
                .SetTarget(layer);
    }

    internal static class RectTransformTweens
    {
        public static Tween DOAnchorPosCore(this RectTransform rect, Vector2 target, float seconds) =>
            DOTween.To(() => rect.anchoredPosition, v => rect.anchoredPosition = v, target, seconds)
                .SetEase(Ease.OutQuad)
                .SetTarget(rect);
    }
}
