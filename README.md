# Unity IAP Store for Kongregate

This package provides a [Unity IAP](https://docs.unity3d.com/Manual/UnityIAP.html) store implementation for the Kongregate website. This allows you to easily add support in-app purchases through the Kongregate website if your game is setup to use Unity IAP.

## Setup

To include kongregate-iap as a Unity package, you'll need to be on Unity 2018.3 or later. Open `Packages/manifest.json` in your project and add "com.kongregate.kongregate-iap" to the "dependencies" object:

```json
{
  "dependencies": {
    "com.kongregate.kongregate-iap": "https://github.com/randomPoison/kongregate-iap.git"
  }
}
```

> NOTE: You'll need to have Git installed on your development machine for Unity to be able to download the dependency. See https://git-scm.com/ for more information.

> NOTE: If you're using an older version of Unity, you can still use kongregate-iap by copying the contents of `Scripts` into your project's `Assets` folder.

kongregate-iap depends on the [kongregate-web](https://github.com/randomPoison/kongregate-web) package, which requires additional setup. If you're not already using kongregate-web directly, follow the [kongregate-web setup instructions](https://github.com/randomPoison/kongregate-web#setup) in order to get started.

## Usage

When initializing Unity IAP, provide `KongregatePurchasingModule.Instance()` to `ConfigurationBuilder.Instance()` in order to setup the Kongregate store on WebGL:

```c#
var builder = ConfigurationBuilder.Instance(
    KongregatePurchasingModule.Instance(),
    StandardPurchasingModule.Instance());
```

## Custom Purchasing Module

The basic setup described will always configure your game to use the Kongregate store when building a WebGL player. However, if you need more control over when you use the Kongregate store on WebGL, you can create a [custom store module](https://docs.unity3d.com/Manual/UnityIAPModules.html) to do so. In your custom module's `Configure()` method, you should register `KongregateStore` as follows:

```c#
public override void Configure()
{
    RegisterStore(
        KongregateStore.STORE_NAME,
        UseKongregateStore ? new KongregateStore() : null);

    // Register any other stores...
}
```

## Receipt Data

When using the Kongregate store, the `Store` field in the [purchase receipt](https://docs.unity3d.com/Manual/UnityIAPPurchaseReceipts.html) is "Kongregate". The `Payload` field  is a JSON-encoded string containing the following fields:

* `AuthToken: string` - The authorization token for the user who made the purchase.
* `Items: int[]` - An array of unique item IDs.

You can use `UnityEngine.JsonUtility.FromJson()` to deserialize `Payload` into an instance of `KongregateStore.Receipt` if you need to inspect its contents.

