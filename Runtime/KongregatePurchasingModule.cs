using System;
using Kongregate.Web;
using UnityEngine;
using UnityEngine.Purchasing.Extension;

namespace Kongregate.Purchasing
{
    public class KongregatePurchasingModule : AbstractPurchasingModule
    {
        public static KongregatePurchasingModule Instance()
        {
            return new KongregatePurchasingModule();
        }

        public override void Configure()
        {
            if (KongregateWeb.Status == ApiStatus.Uninitialized)
            {
                throw new Exception($"You must first intialize {typeof(KongregateWeb).Name} before trying to register the store.");
            }

            RegisterStore(
                KongregateStore.STORE_NAME,
                (Application.platform == RuntimePlatform.WebGLPlayer && KongregateWeb.Status != ApiStatus.Unavailable) ? new KongregateStore() : null);
        }

        // NOTE: Make the default constructor private so that users must use the
        // Instance() function.
        private KongregatePurchasingModule() { }
    }
}
