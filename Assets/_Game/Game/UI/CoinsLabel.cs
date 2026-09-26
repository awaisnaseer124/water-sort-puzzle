using System;
using ColorSort.Core.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.Game.UI
{
    public sealed class CoinsLabel : MonoBehaviour
    {
        [SerializeField] private Text _text;

        private Wallet _wallet;

        public void Init(Wallet wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _wallet.BalanceChanged += Show;
            Show(_wallet.Balance);
        }

        private void OnDestroy()
        {
            if (_wallet != null)
                _wallet.BalanceChanged -= Show;
        }

        private void Show(int balance) => _text.text = balance.ToString();
    }
}
