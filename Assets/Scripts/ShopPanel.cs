using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Başlangıç ekranından açılan mağaza: blok ve gökyüzü temaları.
// Sahip olunan temaya dokununca seçilir; olmayana dokununca coin yetiyorsa satın alınıp seçilir.
// Seçim anında arkadaki kuleye ve gökyüzüne uygulanır, böylece oyuncu sonucu hemen görür.
public class ShopPanel : MonoBehaviour
{
    public Transform blockGrid;
    public Transform skyGrid;
    public ShopItemView itemTemplate;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI messageText;
    public GameObject removeAdsButton;
    public TextMeshProUGUI removeAdsText;

    private readonly List<ShopItemView> items = new List<ShopItemView>();
    private Coroutine messageRoutine;

    void Awake()
    {
        itemTemplate.gameObject.SetActive(false);
        foreach (Skin skin in SkinCatalog.All)
        {
            Transform grid = skin.type == SkinType.Block ? blockGrid : skyGrid;
            ShopItemView item = Instantiate(itemTemplate, grid, false);
            item.gameObject.SetActive(true);
            item.name = "Item_" + skin.id;
            item.Bind(skin, OnItemClicked);
            items.Add(item);
        }
        messageText.alpha = 0f;
    }

    void OnEnable()
    {
        Localization.LanguageChanged += Refresh;
        PlayerData.CoinsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        Localization.LanguageChanged -= Refresh;
        PlayerData.CoinsChanged -= Refresh;
    }

    public void Open()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        gameObject.SetActive(false);
    }

    private void OnItemClicked(ShopItemView item)
    {
        Skin skin = item.Skin;

        if (!PlayerData.OwnsSkin(skin.id))
        {
            if (!PlayerData.SpendCoins(skin.price))
            {
                ShowMessage(Localization.Get("shop_not_enough"));
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                return;
            }
            PlayerData.UnlockSkin(skin.id);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButton();
        }

        PlayerData.SelectSkin(skin);
        SkinCatalog.NotifyChanged();
        Refresh();
    }

    private void Refresh()
    {
        coinsText.text = PlayerData.Coins.ToString("N0");
        foreach (ShopItemView item in items)
            item.Refresh();

        // "Reklamları kaldır" sadece mağaza hazırsa ve henüz alınmadıysa görünür
        if (removeAdsButton != null)
        {
            bool available = StoreService.RemoveAdsAvailable;
            removeAdsButton.SetActive(available);
            if (available && removeAdsText != null)
                removeAdsText.text = Localization.Get("remove_ads") + "  " + StoreService.RemoveAdsPrice;
        }
    }

    public void BuyRemoveAds()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
        StoreService.BuyRemoveAds(ok =>
        {
            if (ok)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
                ShowMessage(Localization.Get("ads_removed"));
            }
            Refresh();
        });
    }

    private void ShowMessage(string text)
    {
        messageText.text = text;
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine());
    }

    // Mesaj belirir, kısa süre kalır ve kaybolur (oyun duraklatılmışken de çalışır)
    private IEnumerator MessageRoutine()
    {
        messageText.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.4f);
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            messageText.alpha = 1f - t / 0.3f;
            yield return null;
        }
        messageText.alpha = 0f;
        messageRoutine = null;
    }
}
