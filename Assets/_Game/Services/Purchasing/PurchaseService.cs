using System;

namespace ColorSort.Services.Purchasing
{
    public enum PurchaseResult
    {
        Success,
        Cancelled,
        Failed,
        Deferred,
        NotAvailable,
    }

    public interface IPurchaseService
    {
        void Initialize();
        bool IsReady { get; }

        string GetPriceText(string productId);

        void Buy(string productId, Action<PurchaseResult> onFinished);

        void RestorePurchases(Action<bool> onFinished);
    }

    public interface IPurchaseFulfiller
    {
        bool Fulfill(string productId, string transactionId);
    }

    public sealed class SimulatedPurchaseService : IPurchaseService
    {
        private readonly IPurchaseFulfiller _fulfiller;
        private int _transactions;

        public SimulatedPurchaseService(IPurchaseFulfiller fulfiller)
        {
            _fulfiller = fulfiller ?? throw new ArgumentNullException(nameof(fulfiller));
        }

        public bool IsReady => true;
        public PurchaseResult NextResult { get; set; } = PurchaseResult.Success;

        public void Initialize()
        {
        }

        public string GetPriceText(string productId) => "$0.99";

        public void Buy(string productId, Action<PurchaseResult> onFinished)
        {
            if (onFinished == null)
                throw new ArgumentNullException(nameof(onFinished));

            if (NextResult != PurchaseResult.Success)
            {
                onFinished(NextResult);
                return;
            }

            bool granted = _fulfiller.Fulfill(productId, "simulated-" + ++_transactions);
            onFinished(granted ? PurchaseResult.Success : PurchaseResult.NotAvailable);
        }

        public void RestorePurchases(Action<bool> onFinished) => onFinished?.Invoke(true);
    }
}
