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
            RegisterStore(
                KongregateStore.STORE_NAME,
                (Application.platform == RuntimePlatform.WebGLPlayer && KongregateWeb.IsKongregateAPIAvailable()) ? new KongregateStore() : null);
        }

        // NOTE: Make the default constructor private so that users must use the
        // Instance() function.
        private KongregatePurchasingModule() { }
    }
}
