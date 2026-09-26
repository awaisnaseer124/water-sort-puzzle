using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class SkinShopItem : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [Tooltip("Optional: set from the catalog so the shop always shows the real art.")]
        [SerializeField] private Image _preview;
        [SerializeField] private GameObject _equippedMarker;
        [SerializeField] private GameObject _lockedOverlay;

        public void Init(Sprite preview, Action onClick)
        {
            if (_preview != null && preview != null)
                _preview.sprite = preview;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick());
        }

        public void Show(bool owned, bool equipped)
        {
            _button.interactable = owned;
            _equippedMarker.SetActive(owned && equipped);
            _lockedOverlay.SetActive(!owned);
        }
    }
}
