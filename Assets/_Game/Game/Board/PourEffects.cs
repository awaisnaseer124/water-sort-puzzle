using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.Board
{
    // The falling stream and the splash where it lands. Splash images are pooled and each tween targets its own image,
    // so restarting the stream never freezes droplets that are still in the air.
    public sealed class PourEffects
    {
        private const float GrowSeconds = 0.08f;
        private const float DetachSeconds = 0.14f;
        private const float SplashInterval = 0.12f;
        private const float Gravity = -1400f;

        private readonly RectTransform _container;
        private readonly Image _stream;
        private readonly Sprite _droplet;
        private readonly Sprite _ripple;
        private readonly float _streamWidth;
        private readonly List<Image> _pool = new List<Image>();

        private Func<float> _surfaceY;
        private Vector2 _top;
        private Color _color;
        private bool _attached;
        private float _nextSplash;

        public PourEffects(RectTransform container, Image stream, Sprite streamSprite, Sprite droplet, Sprite ripple, float streamWidth)
        {
            _container = container;
            _stream = stream;
            _droplet = droplet;
            _ripple = ripple;
            _streamWidth = streamWidth;

            if (_stream == null)
                return;
            if (streamSprite != null)
                _stream.sprite = streamSprite;
            _stream.type = Image.Type.Simple;
            _stream.raycastTarget = false;
            _stream.rectTransform.pivot = new Vector2(0.5f, 1f);
            _stream.gameObject.SetActive(false);
        }

        public void BeginStream(Vector2 top, Func<float> surfaceY, Color color)
        {
            if (_stream == null)
                return;

            DOTween.Kill(_stream);
            _top = top;
            _surfaceY = surfaceY;
            _color = color;
            _attached = true;
            _nextSplash = Time.time + GrowSeconds;

            _stream.color = color;
            _stream.rectTransform.anchoredPosition = top;
            _stream.rectTransform.sizeDelta = new Vector2(_streamWidth, 0f);
            _stream.gameObject.SetActive(true);

            DOTween.To(() => 0f, h => _stream.rectTransform.sizeDelta = new Vector2(_streamWidth, h), Length(), GrowSeconds)
                .SetEase(Ease.InQuad)
                .SetTarget(_stream)
                .OnComplete(() => Splash(Impact(), 5, 1f));
        }

        public void Tick()
        {
            if (!_attached || !_stream.gameObject.activeSelf)
                return;

            if (!DOTween.IsTweening(_stream))
                _stream.rectTransform.sizeDelta = new Vector2(_streamWidth, Length());

            if (Time.time >= _nextSplash)
            {
                _nextSplash = Time.time + SplashInterval;
                Splash(Impact(), 2, 0.6f);
            }
        }

        // The tail leaves the lip and falls into the target.
        public void EndStream()
        {
            if (_stream == null || !_attached)
                return;

            _attached = false;
            RectTransform rect = _stream.rectTransform;
            float bottom = _surfaceY();
            float startTop = rect.anchoredPosition.y;
            DOTween.To(() => 0f, t =>
                {
                    float top = Mathf.Lerp(startTop, bottom, t);
                    rect.anchoredPosition = new Vector2(_top.x, top);
                    rect.sizeDelta = new Vector2(_streamWidth * (1f - 0.4f * t), Mathf.Max(0f, top - bottom));
                }, 1f, DetachSeconds)
                .SetEase(Ease.InQuad)
                .SetTarget(_stream)
                .OnComplete(() => _stream.gameObject.SetActive(false));
        }

        public void Clear()
        {
            _attached = false;
            if (_stream != null)
            {
                DOTween.Kill(_stream);
                _stream.gameObject.SetActive(false);
            }
            foreach (Image image in _pool)
            {
                DOTween.Kill(image);
                image.gameObject.SetActive(false);
            }
        }

        private float Length() => Mathf.Max(0f, _top.y - _surfaceY());

        private Vector2 Impact() => new Vector2(_top.x, _surfaceY());

        private void Splash(Vector2 at, int drops, float strength)
        {
            if (_ripple != null)
            {
                Image ring = Take(_ripple, new Vector2(70f, 22f));
                ring.rectTransform.anchoredPosition = at;
                Animate(ring, 0.35f, t =>
                {
                    ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, t);
                    ring.color = WithAlpha(_color, 0.8f * (1f - t));
                });
            }

            if (_droplet == null)
                return;

            for (int i = 0; i < drops; i++)
            {
                Image drop = Take(_droplet, Vector2.one * UnityEngine.Random.Range(8f, 14f));
                var velocity = new Vector2(UnityEngine.Random.Range(-70f, 70f), UnityEngine.Random.Range(110f, 200f)) * strength;
                float seconds = UnityEngine.Random.Range(0.25f, 0.4f);
                Animate(drop, seconds, t =>
                {
                    float time = t * seconds;
                    drop.rectTransform.anchoredPosition = at + velocity * time + new Vector2(0f, 0.5f * Gravity * time * time);
                    drop.color = WithAlpha(_color, t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
                });
            }
        }

        private void Animate(Image image, float seconds, Action<float> step)
        {
            step(0f);
            DOTween.To(() => 0f, v => step(v), 1f, seconds)
                .SetEase(Ease.Linear)
                .SetTarget(image)
                .OnComplete(() => image.gameObject.SetActive(false));
        }

        private Image Take(Sprite sprite, Vector2 size)
        {
            Image image = _pool.Find(i => !i.gameObject.activeSelf);
            if (image == null)
            {
                var go = new GameObject("Splash", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_container, worldPositionStays: false);
                image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _pool.Add(image);
            }

            image.sprite = sprite;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localScale = Vector3.one;
            image.gameObject.SetActive(true);
            return image;
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);
    }
}
