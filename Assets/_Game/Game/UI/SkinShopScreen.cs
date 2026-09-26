using System;
using System.Collections.Generic;
using ColorSort.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    // A cosmetics shop (backgrounds, bottle designs): tap an owned item to equip it, buy the next one with coins,
    // or watch a video for coins. Slots beyond the catalog are hidden.
    public sealed class SkinShopScreen : UIScreen
    {
        [SerializeField] private SkinShopItem[] _items = Array.Empty<SkinShopItem>();
        [SerializeField] private Button _buy;
        [SerializeField] private Button _watchVideo;
        [SerializeField] private Button _close;

        private CosmeticSlot _slot;
        private Wallet _wallet;
        private int _itemCount;
        private int _price;
        private Action<string> _notify;

        public event Action WatchVideoClicked;
        public event Action Closed;

        public void Init(CosmeticSlot slot, Wallet wallet, IReadOnlyList<Sprite> catalog, int price, Action<string> notify)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _itemCount = Mathf.Min(catalog.Count, _items.Length);
            _price = price;
            _notify = notify ?? (_ => { });

            for (int i = 0; i < _items.Length; i++)
            {
                int id = i;
                bool exists = i < _itemCount;
                _items[i].gameObject.SetActive(exists);
                if (exists)
                    _items[i].Init(catalog[i], () => Equip(id));
            }
            Bind(_buy, BuyNext);
            Bind(_watchVideo, () => WatchVideoClicked?.Invoke());
            Bind(_close, () => Closed?.Invoke());
        }

        public override void Show()
        {
            Refresh();
            base.Show();
        }

        private void Equip(int id)
        {
            if (!_slot.IsUnlocked(id))
                return;
            _slot.Select(id);
            Refresh();
        }

        private void BuyNext()
        {
            switch (CosmeticShop.TryBuyNext(_slot, _wallet, _itemCount, _price, out int bought))
            {
                case ShopResult.Bought:
                    _slot.Select(bought);
                    break;
                case ShopResult.NotEnoughCoins:
                    _notify("Not enough coins");
                    break;
                case ShopResult.AllOwned:
                    _notify("Everything is unlocked");
                    break;
            }
            Refresh();
        }

        private void Refresh()
        {
            for (int id = 0; id < _itemCount; id++)
                _items[id].Show(_slot.IsUnlocked(id), _slot.Selected == id);
        }
    }
}
