using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mağazadaki tek bir tema kartı: küçük renk önizlemesi, adı ve fiyatı ya da durumu (Seçili / Sahip).
public class ShopItemView : MonoBehaviour
{
    public Image background;
    public Image preview;             // temanın küçük resmi (SkinVisuals'tan)
    public Image[] swatches;          // resim yoksa yedek: renk şeritleri, üstten alta
    public TextMeshProUGUI nameText;
    public GameObject priceRow;       // coin simgesi + fiyat
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI statusText;
    public Button button;

    private static readonly Color NormalColor = new Color(0.95f, 0.96f, 0.98f);
    private static readonly Color SelectedColor = new Color(1f, 0.93f, 0.68f);

    public Skin Skin { get; private set; }

    public void Bind(Skin skin, Action<ShopItemView> onClick)
    {
        Skin = skin;
        Sprite picture = SkinVisuals.Instance != null ? SkinVisuals.Instance.Preview(skin.id) : null;
        if (preview != null)
        {
            preview.sprite = picture;
            preview.gameObject.SetActive(picture != null);
        }

        Color[] colors = SkinCatalog.PreviewColors(skin);
        if (swatches != null)
        {
            for (int i = 0; i < swatches.Length; i++)
            {
                swatches[i].color = colors[Mathf.Min(i, colors.Length - 1)];
                swatches[i].gameObject.SetActive(picture == null);
            }
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(this));
        Refresh();
    }

    public void Refresh()
    {
        if (Skin == null) return;

        bool owned = PlayerData.OwnsSkin(Skin.id);
        bool selected = Skin.type == SkinType.Block
            ? PlayerData.SelectedBlockSkin == Skin.id
            : PlayerData.SelectedSkySkin == Skin.id;

        nameText.text = Localization.Get(Skin.nameKey);
        background.color = selected ? SelectedColor : NormalColor;

        priceRow.SetActive(!owned);
        priceText.text = Skin.price.ToString("N0");
        statusText.gameObject.SetActive(owned);
        statusText.text = Localization.Get(selected ? "shop_selected" : "shop_owned");
    }
}
