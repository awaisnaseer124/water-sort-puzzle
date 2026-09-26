using System;
using ColorSort.Core.Progression;
using ColorSort.Game.Board;
using ColorSort.Game.Data;
using UnityEngine;

namespace ColorSort.Game.Presentation
{
    public sealed class BottleSkinView : MonoBehaviour
    {
        [SerializeField] private BoardView _board;

        private PlayerProfile _profile;
        private SkinCatalog _skins;
        private int _applied = -1;

        public void Init(PlayerProfile profile, SkinCatalog skins)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _skins = skins ? skins : throw new ArgumentNullException(nameof(skins));
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
            int selected = _profile.BottleSkins.Selected;
            if (selected == _applied)
                return;
            _applied = selected;
            _board.SetBottleDesign(_skins.Bottle(selected));
        }
    }
}
