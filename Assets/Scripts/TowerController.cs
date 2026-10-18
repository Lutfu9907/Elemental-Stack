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
    public float maxMoveSpeed = 7f;
    public float moveRange = 2.6f;

    [Header("Perfect")]
    [Tooltip("Bu mesafeden (dünya birimi) az kayma Perfect sayılır")]
    public float perfectTolerance = 0.12f;
    private int combo = 0;

    [Header("Falling Pieces")]
    public float pieceLifetime = 3f;

    [HideInInspector] public bool gameStarted = false;
    public TextMeshProUGUI scoreText;
    public int score = 0;

    private GameManager gm;
    private ScorePunchEffect scorePunch;
    private CameraFollow camFollow;

    private Transform topBlock;     // kulenin en üstündeki yerleşmiş blok
    private float topSurfaceY;      // topBlock'un üst yüzeyi
    private Vector3 topScale;       // topBlock'un gerçek ölçüsü (punch efektinden etkilenmez)
    private Transform currentBlock; // kayan blok
    private bool moveOnX = true;
    private float direction = 1f;
    private bool isGameOver = false;

    void Start()
    {
        // Sahne kurucu kulenin altına bir taban bloğu koyar
        if (transform.childCount > 0)
        {
            topBlock = transform.GetChild(transform.childCount - 1);
            topScale = topBlock.localScale;
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

        float speed = Mathf.Min(maxMoveSpeed, moveSpeed + score * speedIncreasePerScore);
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
        moveOnX = score % 2 == 0;
        direction = Random.value < 0.5f ? -1f : 1f;

        Vector3 pos = topBlock.position;
        pos.y = topSurfaceY + blockHeight * 0.5f;
        SetAxis(ref pos, AxisValue(pos) - direction * moveRange);

        GameObject go = Instantiate(blockPrefab, pos, Quaternion.identity);
        go.transform.localScale = new Vector3(topScale.x, blockHeight, topScale.z);

        Block blockScript = go.GetComponent<Block>();
        if (blockScript != null)
        {
            blockScript.elementType = (Block.BlockElementType)Random.Range(0, 4);
            blockScript.ApplyElementColor();
        }

        currentBlock = go.transform;

        if (camFollow != null)
            camFollow.target = currentBlock;
    }

    private void DropCurrentBlock()
    {
        Transform block = currentBlock;
        currentBlock = null;

        float delta = AxisValue(block.position) - AxisValue(topBlock.position);
        float topSize = AxisValue(topScale);
        float overlap = topSize - Mathf.Abs(delta);

        if (overlap <= 0f)
        {
            // Tamamen ıskaladı: blok düşer, oyun biter
            MakeFalling(block.gameObject, delta);
            StartCoroutine(TriggerGameOver());
            return;
        }

        Vector3 pos = block.position;
        Vector3 scale = block.localScale;
        bool isPerfect = Mathf.Abs(delta) < perfectTolerance;

        if (isPerfect)
        {
            SetAxis(ref pos, AxisValue(topBlock.position));
            block.position = pos;

            AudioManager.Instance.PlaySound(AudioManager.Instance.sliceSound);

            combo++;
            PerfectPopup.Spawn(pos + Vector3.up * 0.6f);
            ComboPopup.Spawn(pos + Vector3.up * 0.9f, combo);
            CameraShake.Instance.Shake(0.03f, 0.08f);
            ComboEffectManager.Instance.PlayPerfectComboEffect(combo);
        }
        else
        {
            combo = 0;
            ComboEffectManager.Instance.ResetComboTier();

            float newCenter = AxisValue(topBlock.position) + delta * 0.5f;
            float pieceSize = Mathf.Abs(delta);
            float pieceCenter = newCenter + Mathf.Sign(delta) * (overlap + pieceSize) * 0.5f;
            SpawnPiece(block, pieceCenter, pieceSize, delta);

            SetAxis(ref pos, newCenter);
            SetAxis(ref scale, overlap);
            block.position = pos;
            block.localScale = scale;
        }

        AudioManager.Instance.PlaySound(AudioManager.Instance.placeBlockSound);

        block.SetParent(transform, true);
        topBlock = block;
        topScale = scale;
        topSurfaceY += blockHeight;

        if (isPerfect)
            StartCoroutine(PunchScale(block));

        score++;
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
