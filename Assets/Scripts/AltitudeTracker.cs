using UnityEngine;

// Yükseklik hedefleri ve rekor çizgisi:
//  - 25 Bulutlar, 50 Yüksek gökyüzü, 75 Kutup ışıkları, 100 Uzay, 150 Ay: ulaşınca kutlama yazısı,
//    renkli patlama, ses ve coin ödülü; gökyüzünde bölgeye özel bir değişiklik (bulut geçişi, yıldızlar...).
//  - Oyuncunun en yüksek katında kuleyi çevreleyen ince bir çizgi ve yanında "Rekor" etiketi;
//    geçince "Yeni rekor!" yazar.
public class AltitudeTracker : MonoBehaviour
{
    public TowerController tower;
    public ElementBanner banner;
    public SkyDecor skyDecor;
    public GameObject celebrationBurst;

    [Header("Record line")]
    public Material lineMaterial;
    public RectTransform recordLabel;   // HUD'da çizginin yanında duran "Rekor" etiketi
    public float lineMargin = 0.35f;

    public int BonusCoins { get; private set; }   // bu oyunda bölgelerden kazanılan coin

    private int recordFloor;
    private bool recordBroken;
    private LineRenderer recordLine;
    private Camera cam;
    private Canvas canvas;

    void Start()
    {
        cam = Camera.main;
        if (recordLabel != null)
            canvas = recordLabel.GetComponentInParent<Canvas>();

        recordFloor = PlayerData.BestFloor;
        if (recordFloor > 0)
            CreateRecordLine();
        else if (recordLabel != null)
            recordLabel.gameObject.SetActive(false);

        if (tower != null)
            tower.FloorPlaced += OnFloorPlaced;
    }

    void OnDestroy()
    {
        if (tower != null)
            tower.FloorPlaced -= OnFloorPlaced;
    }

    void LateUpdate()
    {
        if (recordLine == null) return;

        // Çizgi hafifçe nabız gibi atar
        Color c = recordLine.startColor;
        c.a = 0.45f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
        recordLine.startColor = c;
        recordLine.endColor = c;

        // Etiket, çizginin sağ ucunun ekrandaki yerinde durur
        if (recordLabel != null && cam != null && canvas != null)
        {
            Vector3 world = recordLine.GetPosition(2);
            Vector3 screen = cam.WorldToScreenPoint(world);
            bool visible = screen.z > 0f && screen.y > 0f && screen.y < Screen.height;
            recordLabel.gameObject.SetActive(visible);
            if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)recordLabel.parent, screen, canvas.worldCamera, out Vector2 local))
                recordLabel.anchoredPosition = local + new Vector2(24f, 0f);
        }
    }

    private void OnFloorPlaced(int floor, Transform block)
    {
        int zone = AltitudeZones.IndexReachedAt(floor);
        if (zone >= 0)
            Celebrate(zone, block);
        else if (!recordBroken && recordFloor > 0 && floor == recordFloor + 1)
            BreakRecord();
    }

    private void Celebrate(int index, Transform block)
    {
        AltitudeZones.Zone zone = AltitudeZones.All[index];
        BonusCoins += zone.reward;

        if (banner != null)
            banner.ShowText(Localization.Get("zone_" + zone.key + "_reached"), "+" + zone.reward + " " + Localization.Get("coins"));
        if (celebrationBurst != null)
            Instantiate(celebrationBurst, block.position, Quaternion.identity).transform.localScale = block.localScale;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPerfect(8);
            AudioManager.Instance.PlayCoin();
        }
        Haptics.Medium();
        if (skyDecor != null)
            skyDecor.OnZoneReached(index);

        // Bölge ödülü aynı zamanda rekoru geçtiyse onu da işaretle (yazı bölgeninki kalır)
        if (!recordBroken && recordFloor > 0 && block != null && tower.floors > recordFloor)
            BreakRecord(false);
    }

    private void BreakRecord(bool announce = true)
    {
        recordBroken = true;
        if (announce && banner != null)
            banner.ShowText(Localization.Get("new_best"), null);
        if (recordLine != null)
            Destroy(recordLine.gameObject);
        recordLine = null;
        if (recordLabel != null)
            recordLabel.gameObject.SetActive(false);
    }

    // Rekor katının üst yüzeyi hizasında kuleyi çevreleyen kare
    private void CreateRecordLine()
    {
        float y = recordFloor * tower.blockHeight;
        // Kule henüz başlamadıysa ölçüyü doğrudan taban bloğundan al
        Vector3 size = tower.FullScale;
        if (size == Vector3.zero && tower.transform.childCount > 0)
            size = tower.transform.GetChild(0).localScale;
        float hx = size.x * 0.5f + lineMargin;
        float hz = size.z * 0.5f + lineMargin;

        var go = new GameObject("RecordLine");
        recordLine = go.AddComponent<LineRenderer>();
        recordLine.useWorldSpace = true;
        recordLine.loop = true;
        recordLine.positionCount = 4;
        recordLine.SetPosition(0, new Vector3(-hx, y, -hz));
        recordLine.SetPosition(1, new Vector3(-hx, y, hz));
        recordLine.SetPosition(2, new Vector3(hx, y, hz));
        recordLine.SetPosition(3, new Vector3(hx, y, -hz));
        recordLine.widthMultiplier = 0.05f;
        recordLine.numCornerVertices = 2;
        recordLine.sharedMaterial = lineMaterial;
        recordLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        recordLine.receiveShadows = false;
        recordLine.startColor = new Color(1f, 1f, 1f, 0.6f);
        recordLine.endColor = recordLine.startColor;
    }
}
