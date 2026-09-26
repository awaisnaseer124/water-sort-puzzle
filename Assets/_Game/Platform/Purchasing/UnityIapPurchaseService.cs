using System;
using System.Collections.Generic;
using System.Linq;
using ColorSort.Core.Store;
using ColorSort.Services.Purchasing;
using UnityEngine.Purchasing;
using IPurchaseService = ColorSort.Services.Purchasing.IPurchaseService;
using PurchaseResult = ColorSort.Services.Purchasing.PurchaseResult;

namespace ColorSort.Platform.Purchasing
{
    public sealed class UnityIapPurchaseService : IPurchaseService
    {
        private readonly IPurchaseFulfiller _fulfiller;
        private readonly Action<string> _warn;
        private readonly Dictionary<string, Action<PurchaseResult>> _waiting = new Dictionary<string, Action<PurchaseResult>>();

        private StoreController _store;
        private bool _productsFetched;

        public UnityIapPurchaseService(IPurchaseFulfiller fulfiller, Action<string> warn)
        {
            _fulfiller = fulfiller ?? throw new ArgumentNullException(nameof(fulfiller));
            _warn = warn ?? (_ => { });
        }

        public bool IsReady => _store != null && _productsFetched;

        public async void Initialize()
        {
            if (_store != null)
                return;

            _store = UnityIAPServices.StoreController();
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += failure => _warn($"Store disconnected: {failure.Message}");
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += failure => _warn($"Fetching products failed: {failure.FailureReason}");
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += order => Finish(ProductId(order), PurchaseResult.Deferred);

            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                _warn($"Store connection failed: {e.Message}");
            }
        }

        public string GetPriceText(string productId) =>
            IsReady ? _store.GetProductById(productId)?.metadata.localizedPriceString : null;

        public void Buy(string productId, Action<PurchaseResult> onFinished)
        {
            if (onFinished == null)
                throw new ArgumentNullException(nameof(onFinished));

            if (!IsReady || _store.GetProductById(productId) == null)
            {
                onFinished(PurchaseResult.NotAvailable);
                return;
            }
            if (_waiting.ContainsKey(productId))
            {
                onFinished(PurchaseResult.Failed); // a purchase of this product is already in flight
                return;
            }

            _waiting[productId] = onFinished;
            _store.PurchaseProduct(productId);
        }

        public void RestorePurchases(Action<bool> onFinished)
        {
            if (!IsReady)
            {
                onFinished?.Invoke(false);
                return;
            }
            _store.RestoreTransactions((success, error) =>
            {
                if (!success)
                    _warn($"Restore failed: {error}");
                onFinished?.Invoke(success);
            });
        }

        private void OnStoreConnected()
        {
            var definitions = StoreCatalog.Products
                .Select(p => new ProductDefinition(p.Id, p.Kind == ProductKind.Consumable ? ProductType.Consumable : ProductType.NonConsumable))
                .ToList();
            _store.FetchProducts(definitions);
        }

        private void OnProductsFetched(List<Product> products)
        {
            _productsFetched = true;
            _store.FetchPurchases(); // delivers anything paid but not yet confirmed, and owned non-consumables
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string productId = ProductId(order);
            if (productId == null)
            {
                _warn("Pending order without a product; not confirmed.");
                return;
            }

            if (!_fulfiller.Fulfill(productId, order.Info.TransactionID))
                _warn($"Purchased unknown product '{productId}'; confirming so the order does not stay pending.");

            _store.ConfirmPurchase(order);
            Finish(productId, PurchaseResult.Success);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            PurchaseResult result = order.FailureReason == PurchaseFailureReason.UserCancelled
                ? PurchaseResult.Cancelled
                : PurchaseResult.Failed;
            if (result == PurchaseResult.Failed)
                _warn($"Purchase failed: {order.FailureReason} {order.Details}");
            Finish(ProductId(order), result);
        }

        private void Finish(string productId, PurchaseResult result)
        {
            if (productId == null || !_waiting.TryGetValue(productId, out Action<PurchaseResult> callback))
                return; // e.g. a re-delivered purchase nobody is waiting for
            _waiting.Remove(productId);
            callback(result);
        }

        private static string ProductId(Order order) =>
            order?.CartOrdered?.Items().FirstOrDefault()?.Product?.definition.id;
    }
}
