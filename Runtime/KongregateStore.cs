using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Kongregate.Web;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Kongregate.Purchasing
{
    public class KongregateStore : IStore
    {
        public const string STORE_NAME = "Kongregate";

        private IStoreCallback _callback;

        // Data tracking the initial request for items on startup.
        private ReadOnlyCollection<ProductDefinition> _pendingProducts;
        private Dictionary<string, StoreItem> _storeItems;
        private Dictionary<string, List<UserItem>> _userItems;

        // Data tracking the state of a pending purchase flow.
        private ProductDefinition _pendingPurchase;

        #region IStore
        public void Initialize(IStoreCallback callback)
        {
            _callback = callback;

            KongregateWeb.StoreItemsReceived += OnItemList;
            KongregateWeb.UserItemsReceived += OnUserItemsUpdated;
            KongregateWeb.PurchaseSucceeded += OnPurchaseSuccess;
            KongregateWeb.PurchaseFailed += OnPurchaseFail;
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
        {
            Debug.Assert(
                _pendingPurchase == null,
                "Attempting to update store products while purchase is in progress");

            _pendingProducts = products;

            // Wait for the API to become ready, then request the list of store items
            // and the user's items.
            //
            // NOTE: BecameReady is invoked immediately if the API is already ready, so
            // the callback will still be invoked even if the API is already ready.
            KongregateWeb.BecameReady += () =>
            {
                KongregateWeb.RequestItemList();
                KongregateWeb.RequestUserItemList();
            };
        }

        public void Purchase(ProductDefinition product, string developerPayload)
        {
            Debug.Assert(
                _pendingProducts == null,
                "Attempting to make a purchase while updating store products");

            _pendingPurchase = product;
            KongregateWeb.PurchaseItems(new string[] { product.id });
        }

        public void FinishTransaction(ProductDefinition product, string transactionId)
        {
            // Kongregate recommends consuming purchases on the server instead of the
            // client, so there's no client-side logic for finishing purchases by default.
            //
            // TODO: Optionally allow the client to consume items after a purchase.
        }
        #endregion

        #region KongJSGameObject Events
        void OnPurchaseSuccess(string[] items)
        {
            if (_pendingPurchase == null)
            {
                Debug.LogErrorFormat(
                    "Received purchase success notification when no purchase was in progress: {0}",
                    string.Join(", ", items));
                return;
            }

            // When the purchase succeeds, request an updated list of user items from
            // Kongregate. The purchase items flow doesn't return the metadata for the
            // purchased item, so we need to request the items ourself to finish the
            // purchase flow.
            KongregateWeb.RequestUserItemList();
        }

        void OnPurchaseFail(string[] items)
        {
            if (_pendingPurchase == null)
            {
                Debug.LogErrorFormat(
                    "Received purchase failure notification when no purchase was in progress: {0}",
                    string.Join(", ", items));
                return;
            }

            try
            {
                _callback.OnPurchaseFailed(new PurchaseFailureDescription(
                    _pendingPurchase.id,
                    PurchaseFailureReason.UserCancelled,
                    "User closed purchase dialog"));
            }
            finally
            {
                _pendingPurchase = null;
            }
        }

        void OnItemList(StoreItem[] itemList)
        {
            // Convert the list of store items to a dictionary with the product identifier
            // as the keys.
            _storeItems = itemList.ToDictionary(product => product.Identifier);

            // Wait to process the item list until we've received both the store items
            // and the user's items.
            if (_storeItems != null && _userItems != null)
            {
                ProcessStoreItems();
            }
        }

        void OnUserItemsUpdated(UserItem[] items)
        {
            // Delegate processing of user items depending on the reason why we requested
            // the updated list of items.
            //
            // TODO: Better handle the case where we attempt to update the store items
            // while a purchase is in progress. Currently the logic doesn't handle that
            // case correctly because both methods will attempt to finish the purchase
            // flow for the purchased items. It would be more correct to combine the two
            // methods and explicitly handle all the edge cases.

            if (_pendingPurchase != null)
            {
                OnUpdateUserItemsAfterPurchase(items);
            }

            if (_pendingProducts != null)
            {
                ProcessUserItems(items);
            }
        }

        /// <summary>
        /// Callback for when we initially request the list of user items on startup.
        /// </summary>
        void ProcessUserItems(UserItem[] items)
        {
            // Convert the list of user items to a dictionary with the product identifier
            // as the keys.
            _userItems = items
                .GroupBy(item => item.Identifier)
                .ToDictionary(group => group.Key, group => group.ToList());

            // Wait to process the item list until we've received both the store items
            // and the user's items.
            if (_storeItems != null && _userItems != null)
            {
                ProcessStoreItems();
            }
        }

        /// <summary>
        /// Callback for when we request the updated list of user items after making a purchase.
        /// </summary>
        void OnUpdateUserItemsAfterPurchase(UserItem[] items)
        {
            if (_pendingPurchase == null)
            {
                Debug.LogError("Updated items list when no purchase was pending");
                return;
            }

            try
            {
                var item = Array.Find(
                    items,
                    userItem => userItem.Identifier == _pendingPurchase.id);
                if (item == null)
                {
                    _callback.OnPurchaseFailed(new PurchaseFailureDescription(
                        _pendingPurchase.id,
                        PurchaseFailureReason.Unknown,
                        "Purchased product not found in user's items"));
                    return;
                }

                var receipt = new Receipt()
                {
                    AuthToken = KongregateWeb.GameAuthToken,
                    Items = new int[] { item.Id },
                };

                // NOTE: We use the item ID as the transaction ID because it's the only
                // unique value that Kongregate provides for the transaction.
                var transactionId = item.Id.ToString();

                _callback.OnPurchaseSucceeded(
                    item.Identifier,
                    JsonUtility.ToJson(receipt),
                    transactionId);
            }
            finally
            {
                _pendingPurchase = null;
            }
        }
        #endregion

        private void ProcessStoreItems()
        {
            Debug.Assert(
                _pendingProducts != null,
                "Trying to initialize without product definitions");

            try
            {
                // Use the item data to populate the price information for the produts.
                var products = _pendingProducts
                    .Where(product => _storeItems.ContainsKey(product.id))
                    .Select(product =>
                    {
                        var storeItem = _storeItems[product.id];
                        var metatdata = new ProductMetadata(
                            $"{storeItem.Price} Kreds",
                            storeItem.Name,
                            storeItem.Description,
                            "kreds",
                            storeItem.Price);

                    // If the product is present in the user's item list, provide
                    // the receipt and transaction ID so that the game code can
                    // restore the purchase.
                    if (_userItems.TryGetValue(product.id, out var ownedItems))
                        {
                            var receipt = new Receipt()
                            {
                                AuthToken = KongregateWeb.GameAuthToken,
                                Items = ownedItems.Select(item => item.Id).ToArray(),
                            };

                        // NOTE: We make up a transaction ID by joining the various item
                        // IDs since Kongregate doesn't directly provide a transaction ID.
                        // The item ID itself will be unique for the purchase so this
                        // should be fine when there's only a single item in the user's
                        // inventory. It might be weird if there are ever multiple items,
                        // though.
                        var transactionId = string.Join(",", ownedItems.Select(item => item.Id));

                            return new ProductDescription(
                                product.id,
                                metatdata,
                                JsonUtility.ToJson(receipt),
                                transactionId);
                        }
                        else
                        {
                            return new ProductDescription(product.id, metatdata);
                        }
                    })
                    .ToList();
                _callback.OnProductsRetrieved(products);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _callback.OnSetupFailed(InitializationFailureReason.NoProductsAvailable);
            }
            finally
            {
                // Clear variables used to track the intialization process.
                _pendingProducts = null;
                _storeItems = null;
                _userItems = null;
            }
        }

        [Serializable]
        public class Receipt
        {
            public string AuthToken;
            public int[] Items;
        }
    }
}
