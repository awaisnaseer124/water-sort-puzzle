using System;
using ColorSort.Core.Progression;
using ColorSort.Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.Presentation
{
    public sealed class BackgroundView : MonoBehaviour
    {
        [SerializeField] private Image _image;

        private PlayerProfile _profile;
        private SkinCatalog _skins;

        public void Init(PlayerProfile profile, SkinCatalog skins)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _skins = skins ?? throw new ArgumentNullException(nameof(skins));
            _profile.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (_profile != null)
                _profile.Changed -= Apply;
        }

        private void Apply()
        {
            Sprite sprite = _skins.Background(_profile.Backgrounds.Selected);
            if (sprite != null && _image.sprite != sprite)
                _image.sprite = sprite;
        }
    }
}
