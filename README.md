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

> TODO: Provide a store module for registering the store instance.