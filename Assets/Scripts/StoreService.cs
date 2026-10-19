using System;
using UnityEngine;

// Uygulama içi satın alma (şimdilik tek ürün: "Reklamları kaldır").
//   Editör                 → SimulatedStore (satın alma anında başarılı olur, denemek için)
//   IAP_ENABLED tanımlıysa → UnityIapStore (Unity In-App Purchasing paketi kurulunca)
//   Diğer                  → NoStore (ürün gösterilmez)
// Kurulum: docs/RELEASE.md → "Reklamsız sürüm".
public interface IStoreProvider
{
    void Initialize();
    bool RemoveAdsAvailable { get; }
    string RemoveAdsPrice { get; }
    void BuyRemoveAds(Action<bool> done);
    void Restore(Action<bool> done);
}

public static class StoreService
{
    public const string RemoveAdsProductId = "remove_ads";

    private static IStoreProvider provider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        provider = null;
    }

    private static IStoreProvider Provider
    {
        get
        {
            if (provider == null)
            {
#if UNITY_EDITOR
                provider = new SimulatedStore();
#elif IAP_ENABLED
                provider = new UnityIapStore();
#else
                provider = new NoStore();
#endif
                provider.Initialize();
            }
            return provider;
        }
    }

    public static bool RemoveAdsAvailable => !PlayerData.AdsRemoved && Provider.RemoveAdsAvailable;
    public static string RemoveAdsPrice => Provider.RemoveAdsPrice;

    public static void BuyRemoveAds(Action<bool> done)
    {
        Provider.BuyRemoveAds(ok =>
        {
            if (ok) PlayerData.SetAdsRemoved(true);
            done?.Invoke(ok);
        });
    }

    public static void Restore(Action<bool> done)
    {
        Provider.Restore(ok =>
        {
            if (ok) PlayerData.SetAdsRemoved(true);
            done?.Invoke(ok);
        });
    }
}

public class NoStore : IStoreProvider
{
    public void Initialize() { }
    public bool RemoveAdsAvailable => false;
    public string RemoveAdsPrice => "";
    public void BuyRemoveAds(Action<bool> done) => done?.Invoke(false);
    public void Restore(Action<bool> done) => done?.Invoke(false);
}

public class SimulatedStore : IStoreProvider
{
    public void Initialize() { }
    public bool RemoveAdsAvailable => true;
    public string RemoveAdsPrice => "$1.99";

    public void BuyRemoveAds(Action<bool> done)
    {
        Debug.Log("StoreService (editör): 'Reklamları kaldır' satın alındı sayıldı.");
        done?.Invoke(true);
    }

    public void Restore(Action<bool> done) => done?.Invoke(PlayerData.AdsRemoved);
}

#if IAP_ENABLED
// Unity In-App Purchasing (4.x API). Google Play'de "remove_ads" adlı, tüketilmeyen (non-consumable) bir ürün açılmalı.
// Android'de daha önce alınmış ürün, başlatmada ProcessPurchase ile kendiliğinden geri gelir.
public class UnityIapStore : IStoreProvider, UnityEngine.Purchasing.IDetailedStoreListener
{
    private UnityEngine.Purchasing.IStoreController controller;
    private UnityEngine.Purchasing.IExtensionProvider extensions;
    private Action<bool> pendingPurchase;

    public void Initialize()
    {
        var builder = UnityEngine.Purchasing.ConfigurationBuilder.Instance(UnityEngine.Purchasing.StandardPurchasingModule.Instance());
        builder.AddProduct(StoreService.RemoveAdsProductId, UnityEngine.Purchasing.ProductType.NonConsumable);
        UnityEngine.Purchasing.UnityPurchasing.Initialize(this, builder);
    }

    public bool RemoveAdsAvailable => controller != null && controller.products.WithID(StoreService.RemoveAdsProductId) != null;

    public string RemoveAdsPrice =>
        controller?.products.WithID(StoreService.RemoveAdsProductId)?.metadata.localizedPriceString ?? "";

    public void BuyRemoveAds(Action<bool> done)
    {
        if (controller == null) { done?.Invoke(false); return; }
        pendingPurchase = done;
        controller.InitiatePurchase(StoreService.RemoveAdsProductId);
    }

    public void Restore(Action<bool> done)
    {
        var product = controller?.products.WithID(StoreService.RemoveAdsProductId);
        done?.Invoke(product != null && product.hasReceipt);
    }

    public void OnInitialized(UnityEngine.Purchasing.IStoreController controller, UnityEngine.Purchasing.IExtensionProvider extensions)
    {
        this.controller = controller;
        this.extensions = extensions;
        var product = controller.products.WithID(StoreService.RemoveAdsProductId);
        if (product != null && product.hasReceipt)
            PlayerData.SetAdsRemoved(true);
    }

    public void OnInitializeFailed(UnityEngine.Purchasing.InitializationFailureReason error) =>
        Debug.LogWarning("IAP init failed: " + error);

    public void OnInitializeFailed(UnityEngine.Purchasing.InitializationFailureReason error, string message) =>
        Debug.LogWarning("IAP init failed: " + error + " " + message);

    public UnityEngine.Purchasing.PurchaseProcessingResult ProcessPurchase(UnityEngine.Purchasing.PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == StoreService.RemoveAdsProductId)
        {
            PlayerData.SetAdsRemoved(true);
            pendingPurchase?.Invoke(true);
            pendingPurchase = null;
        }
        return UnityEngine.Purchasing.PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, UnityEngine.Purchasing.PurchaseFailureReason reason)
    {
        pendingPurchase?.Invoke(false);
        pendingPurchase = null;
    }

    public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, UnityEngine.Purchasing.Extension.PurchaseFailureDescription description)
    {
        pendingPurchase?.Invoke(false);
        pendingPurchase = null;
    }
}
#endif
