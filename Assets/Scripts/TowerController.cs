using System.Collections;
using UnityEngine;
using TMPro;

// 3D kule mekaniği: blok sırayla X ve Z ekseninde kayar, dokununca durur;
// alttaki bloğun dışında kalan kısım kesilip fizikle düşer.
public class TowerController : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;
    public float blockHeight = 0.5f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float speedIncreasePerScore = 0.05f;
    public float maxMoveSpeed = 6f;     // elementlerin hız etkisi bu tavanın üstüne eklenir
    public float moveRange = 2.6f;

    [Header("Elements")]
    public GameObject fireEffect;
    public GameObject iceEffect;
    public GameObject windEffect;
    public GameObject electricEffect;
    public ElementBanner elementBanner;
    [Tooltip("Kayan element bloğunun üstünde süzülen simgeler ve arkalarındaki beyaz rozet")]
    public Sprite fireIcon;
    public Sprite iceIcon;
    public Sprite windIcon;
    public Sprite electricIcon;
    public Sprite iconBadge;
    [Tooltip("Simgeler için ışıksız, saydam materyal")]
    public Material iconMaterial;
    [Tooltip("Element çerçevesi için parlayan (bloom) materyal")]
    public Material frameMaterial;
    [Tooltip("Kesilen parça bloğun bu oranından büyükse yerleştirme \"kötü\" sayılır (yardım eli için)")]
    public float badCutFraction = 0.25f;
    [Tooltip("Blok tabanın bu oranından küçüldüyse oyuncu zorlanıyor sayılır")]
    public float smallBlockAreaFraction = 0.45f;

    [Header("Perfect")]
    [Tooltip("Bu mesafeden (dünya birimi) az kayma Perfect sayılır")]
    public float perfectTolerance = 0.12f;
    [Tooltip("Bu kadar perfect üst üste gelince blok her perfect'te biraz büyür (ödül)")]
    public int perfectGrowStreak = 5;
    public float perfectGrowStep = 0.15f;
    public Material rippleMaterial;
    private int combo = 0;

    [Header("Falling Pieces")]
    public float pieceLifetime = 3f;

    [HideInInspector] public bool gameStarted = false;
    public TextMeshProUGUI scoreText;
    public int score = 0;     // puan (rüzgâr bonusu ile kat sayısından fazla olabilir)
    public int floors = 0;    // yerleşmiş kat sayısı; eksen, hız, element ve arka plan buna göre
    public int perfectCount = 0;   // bu oyundaki toplam perfect (coin hesabı için)

    private GameManager gm;
    private ScorePunchEffect scorePunch;
    private CameraFollow camFollow;

    private Transform topBlock;     // kulenin en üstündeki yerleşmiş blok
    private float topSurfaceY;      // topBlock'un üst yüzeyi
    private Vector3 topScale;       // topBlock'un gerçek ölçüsü (punch efektinden etkilenmez)
    private Vector3 fullScale;      // taban bloğunun ölçüsü; perfect ödülü bloğu en fazla buna kadar büyütür
    private Transform currentBlock; // kayan blok
    private bool moveOnX = true;
    private float direction = 1f;
    private bool isGameOver = false;

    private Block.BlockElementType currentElement = Block.BlockElementType.None;
    private float currentBlockAge;
    private GameObject currentEffect;
    private ElementVisual currentVisual;
    private int badPlacementsInRow;

    void Start()
    {
        // Sahne kurucu kulenin altına bir taban bloğu koyar
        if (transform.childCount > 0)
        {
            topBlock = transform.GetChild(transform.childCount - 1);
            topScale = topBlock.localScale;
            fullScale = topScale;
            topSurfaceY = topBlock.position.y + topScale.y * 0.5f;
        }
        else
        {
            Debug.LogError("TowerController: taban bloğu yok. Tower Stack > Build Game Scene ile sahneyi yeniden kur.");
        }

        // CameraFollow kameranın kendisinde ya da kameranın bağlı olduğu "rig" objesinde olabilir
        if (Camera.main != null)
            camFollow = Camera.main.GetComponentInParent<CameraFollow>();
        gm = FindFirstObjectByType<GameManager>();

        if (scoreText != null)
            scorePunch = scoreText.GetComponent<ScorePunchEffect>();
    }

    void Update()
    {
        if (!gameStarted || isGameOver || currentBlock == null) return;

        currentBlockAge += Time.deltaTime;
        float speed = Mathf.Min(maxMoveSpeed, moveSpeed + floors * speedIncreasePerScore)
                      * ElementRules.SpeedMultiplier(currentElement, currentBlockAge);
        Vector3 pos = currentBlock.position;
        float center = AxisValue(topBlock.position);
        float offset = AxisValue(pos) - center + direction * speed * Time.deltaTime;

        if (offset > moveRange) { offset = moveRange; direction = -1f; }
        else if (offset < -moveRange) { offset = -moveRange; direction = 1f; }

        SetAxis(ref pos, center + offset);
        currentBlock.position = pos;
        // Dokunmalar TapInputArea üzerinden AddOrDropBlock'u çağırır
    }

    public void StartGame()
    {
        gameStarted = true;
    }

    public void StartTower()
    {
        gameStarted = true;
        CreateNewBlock();
    }

    public void AddOrDropBlock()
    {
        if (!gameStarted || isGameOver || topBlock == null) return;

        if (currentBlock != null)
            DropCurrentBlock();

        if (!isGameOver)
            CreateNewBlock();
    }

    private void CreateNewBlock()
    {
        // Eksen her katta değişir: X, Z, X, Z...
        moveOnX = floors % 2 == 0;
        direction = Random.value < 0.5f ? -1f : 1f;

        int floor = floors + 1;
        currentElement = ElementRules.Pick(floor, IsStruggling(), out bool isIntroduction);
        currentBlockAge = 0f;

        Vector3 pos = topBlock.position;
        pos.y = topSurfaceY + blockHeight * 0.5f;
        SetAxis(ref pos, AxisValue(pos) - direction * moveRange);

        GameObject go = Instantiate(blockPrefab, pos, Quaternion.identity);
        go.transform.localScale = new Vector3(topScale.x, blockHeight, topScale.z);

        Block blockScript = go.GetComponent<Block>();
        if (blockScript != null)
        {
            blockScript.elementType = currentElement;
            blockScript.floor = floor;
            blockScript.ApplyElementColor();
        }

        // Element bloğu kayarken kendi parçacıklarını saçar
        GameObject effectPrefab = EffectFor(currentElement);
        currentEffect = effectPrefab != null ? Instantiate(effectPrefab, go.transform, false) : null;

        // ...ve parlar, etrafında çerçeve, üstünde simgesi olur
        if (currentElement != Block.BlockElementType.None && blockScript != null)
            currentVisual = ElementVisual.Attach(blockScript, IconFor(currentElement), iconBadge, iconMaterial, frameMaterial);

        if (isIntroduction && elementBanner != null)
            elementBanner.Show(currentElement);

        currentBlock = go.transform;

        if (camFollow != null)
            camFollow.target = currentBlock;
    }

    private void DropCurrentBlock()
    {
        Transform block = currentBlock;
        currentBlock = null;
        Block.BlockElementType placedElement = currentElement;
        StopCurrentEffect(true);
        if (currentVisual != null)
        {
            currentVisual.Placed();
            currentVisual = null;
        }

        float delta = AxisValue(block.position) - AxisValue(topBlock.position);
        float topSize = AxisValue(topScale);
        float overlap = topSize - Mathf.Abs(delta);
        int bonusPoints = 0;

        if (overlap <= 0f)
        {
            // Tamamen ıskaladı: blok düşer, oyun biter
            MakeFalling(block.gameObject, delta);
            StartCoroutine(TriggerGameOver());
            return;
        }

        Vector3 pos = block.position;
        Vector3 scale = block.localScale;
        float tolerance = perfectTolerance * ElementRules.PerfectToleranceMultiplier(currentElement);
        bool isPerfect = Mathf.Abs(delta) < tolerance;

        if (isPerfect)
        {
            SetAxis(ref pos, AxisValue(topBlock.position));
            block.position = pos;

            combo++;
            perfectCount++;
            badPlacementsInRow = 0;
            bonusPoints = ElementRules.PerfectBonusPoints(currentElement);

            // Uzun perfect serisi ve elektrik bloğu ödülü: blok, taban boyutunu geçmeden büyür
            float grow = ElementRules.PerfectGrowAmount(currentElement);
            if (combo >= perfectGrowStreak)
                grow += perfectGrowStep;
            if (grow > 0f)
            {
                scale.x = Mathf.Min(scale.x + grow, fullScale.x);
                scale.z = Mathf.Min(scale.z + grow, fullScale.z);
                block.localScale = scale;
            }

            AudioManager.Instance.PlayPerfect(combo);
            Haptics.Medium();
            PerfectPopup.Spawn(pos + Vector3.up * 0.6f, combo, bonusPoints);
            CameraShake.Instance.Shake(0.03f, 0.08f);
            ComboEffectManager.Instance.PlayPerfectComboEffect(combo);

            Vector3 rippleCenter = new Vector3(pos.x, pos.y - blockHeight * 0.5f + 0.01f, pos.z);
            Vector2 rippleSize = new Vector2(scale.x, scale.z);
            PerfectRipple.Spawn(rippleCenter, rippleSize, rippleMaterial, Color.white);
            if (combo >= 3)
                PerfectRipple.Spawn(rippleCenter, rippleSize, rippleMaterial, Color.white, 0.12f);
        }
        else
        {
            combo = 0;
            ComboEffectManager.Instance.ResetComboTier();
            AudioManager.Instance.PlaySound(AudioManager.Instance.sliceSound);
            Haptics.Light();

            float newCenter = AxisValue(topBlock.position) + delta * 0.5f;
            float pieceSize = Mathf.Abs(delta);
            badPlacementsInRow = pieceSize / topSize > badCutFraction ? badPlacementsInRow + 1 : 0;
            float pieceCenter = newCenter + Mathf.Sign(delta) * (overlap + pieceSize) * 0.5f;
            SpawnPiece(block, pieceCenter, pieceSize, delta);

            SetAxis(ref pos, newCenter);
            SetAxis(ref scale, overlap);
            block.position = pos;
            block.localScale = scale;
        }

        AudioManager.Instance.PlaySound(AudioManager.Instance.placeBlockSound);

        // Blok temasının yerleşme efekti (şeker serpintisi, talaş, altın tozu, neon halka)
        Block placedBlock = block.GetComponent<Block>();
        if (placedBlock != null && SkinVisuals.Instance != null)
            SkinVisuals.Instance.OnBlockPlaced(placedBlock, blockHeight);

        // Element bloğu yerleşince kendi renginde bir halka açılır
        if (placedElement != Block.BlockElementType.None)
        {
            Color ring = Color.Lerp(Block.ElementColor(placedElement), Color.white, 0.25f);
            Vector3 ringCenter = new Vector3(pos.x, pos.y - blockHeight * 0.5f + 0.01f, pos.z);
            PerfectRipple.Spawn(ringCenter, new Vector2(scale.x, scale.z), rippleMaterial, ring, 0.05f);
        }

        block.SetParent(transform, true);
        topBlock = block;
        topScale = scale;
        topSurfaceY += blockHeight;

        if (isPerfect)
            StartCoroutine(PunchScale(block));

        floors++;
        score += 1 + bonusPoints;
        if (gm != null)
            gm.UpdateScore(score);

        if (scorePunch != null)
        {
            float punchStrength = Mathf.Clamp(combo * 0.15f, 0.15f, 0.6f);
            scorePunch.Punch(punchStrength);
        }
    }

    private void SpawnPiece(Transform source, float center, float size, float delta)
    {
        if (size <= 0.001f) return;

        GameObject piece = Instantiate(blockPrefab);

        // Düşen parça, kesildiği bloğun rengini taşısın
        Block pieceBlock = piece.GetComponent<Block>();
        Block sourceBlock = source.GetComponent<Block>();
        if (pieceBlock != null && sourceBlock != null)
        {
            pieceBlock.elementType = sourceBlock.elementType;
            pieceBlock.floor = sourceBlock.floor;
            pieceBlock.ApplyElementColor();
        }

        Vector3 pos = source.position;
        Vector3 scale = source.localScale;
        SetAxis(ref pos, center);
        SetAxis(ref scale, size);
        piece.transform.position = pos;
        piece.transform.localScale = scale;

        MakeFalling(piece, delta);
        Destroy(piece, pieceLifetime);
    }

    private void MakeFalling(GameObject go, float delta)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Kesildiği yönde hafifçe itilip dönerek düşsün
        Vector3 push = moveOnX ? Vector3.right : Vector3.forward;
        rb.linearVelocity = push * Mathf.Sign(delta) * 0.8f;
        rb.angularVelocity = Random.insideUnitSphere * 2f;

        go.transform.SetParent(null, true);
    }

    private IEnumerator PunchScale(Transform block)
    {
        Vector3 original = block.localScale;
        block.localScale = new Vector3(original.x * 1.06f, original.y, original.z * 1.06f);
        yield return new WaitForSeconds(0.06f);
        block.localScale = original;
    }

    // Oyuncu zorlanıyor mu: üst üste kötü kesim ya da iyice daralmış blok. O zaman yardımcı elementler sıklaşır.
    private bool IsStruggling()
    {
        if (badPlacementsInRow >= 2) return true;
        float area = topScale.x * topScale.z;
        float fullArea = fullScale.x * fullScale.z;
        return fullArea > 0f && area < fullArea * smallBlockAreaFraction;
    }

    private GameObject EffectFor(Block.BlockElementType element)
    {
        switch (element)
        {
            case Block.BlockElementType.Fire: return fireEffect;
            case Block.BlockElementType.Ice: return iceEffect;
            case Block.BlockElementType.Wind: return windEffect;
            case Block.BlockElementType.Electric: return electricEffect;
            default: return null;
        }
    }

    private Sprite IconFor(Block.BlockElementType element)
    {
        switch (element)
        {
            case Block.BlockElementType.Fire: return fireIcon;
            case Block.BlockElementType.Ice: return iceIcon;
            case Block.BlockElementType.Wind: return windIcon;
            case Block.BlockElementType.Electric: return electricIcon;
            default: return null;
        }
    }

    // Blok yerleşince parçacıklar kesilir (istenirse son bir patlama yapar);
    // havadakiler kendi ömrünü tamamlayıp kaybolur
    private void StopCurrentEffect(bool burst = false)
    {
        if (currentEffect == null) return;
        foreach (ParticleSystem ps in currentEffect.GetComponentsInChildren<ParticleSystem>())
        {
            if (burst)
                ps.Emit(24);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        Destroy(currentEffect, 2f);
        currentEffect = null;
    }

    private float AxisValue(Vector3 v)
    {
        return moveOnX ? v.x : v.z;
    }

    private void SetAxis(ref Vector3 v, float value)
    {
        if (moveOnX) v.x = value;
        else v.z = value;
    }

    private IEnumerator TriggerGameOver()
    {
        isGameOver = true;

        AudioManager.Instance.PlaySound(AudioManager.Instance.gameOverSound);
        Haptics.Heavy();

        yield return new WaitForSeconds(0.5f);
        CameraShake.Instance.Shake(0.1f, 0.15f);

        if (gm == null)
            gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
            gm.GameOver();
    }

    public void RestartGame()
    {
        isGameOver = false;
        currentBlock = null;
    }
}
