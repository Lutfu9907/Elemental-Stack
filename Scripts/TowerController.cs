using System.Collections;
using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;
    public float blockHeight = 1f;

    [Header("Movement")]
    public float moveSpeed = 2f;            // Ana hız
    private float baseMoveSpeed;            // Ice tarafından değiştirilecek olan
    public float speedIncreasePerScore = 0.1f;
    public float moveRange = 3f;

    [Header("Difficulty")]
    public float missChance = 0.08f;        // %8 kayık spawn

    private GameObject currentBlock;
    private float nextY = 0f;
    private bool isMoving = false;
    private float direction = 1f;

    private CameraFollow camFollow;
    public int score = 0;

    void Start()
    {
        baseMoveSpeed = moveSpeed;

        nextY = transform.childCount > 0
            ? transform.GetChild(transform.childCount - 1).position.y + blockHeight
            : 0f;

        camFollow = Camera.main.GetComponent<CameraFollow>();
    }

    void Update()
    {
        if (currentBlock != null && isMoving)
        {
            float finalSpeed = moveSpeed + score * speedIncreasePerScore;

            currentBlock.transform.position += Vector3.right * direction * finalSpeed * Time.deltaTime;

            if (currentBlock.transform.position.x > moveRange) direction = -1f;
            if (currentBlock.transform.position.x < -moveRange) direction = 1f;
        }

        if (Input.GetMouseButtonDown(0))
        {
            AddOrDropBlock();
        }
    }

    public void AddOrDropBlock()
    {
        if (currentBlock == null)
            CreateNewBlock();
        else
        {
            DropCurrentBlock();
            CreateNewBlock();
        }
    }

    // ------------------ CREATE BLOCK ------------------
    private void CreateNewBlock()
    {
        float spawnX = 0f;

        // Eğer alt blok varsa, yeni blokun genişliği alt bloğa göre ayarlansın
        if (transform.childCount > 0)
        {
            GameObject prevBlock = transform.GetChild(transform.childCount - 1).gameObject;
            float prevWidth = prevBlock.transform.localScale.x;

            // Yeni blokun genişliği alt bloğa eşit
            Vector3 scale = blockPrefab.transform.localScale;
            scale.x = prevWidth;
            blockPrefab.transform.localScale = scale;

            // X pozisyonu hafif random kaydırma ile
            spawnX += Random.Range(-0.15f, 0.15f);
        }

        Vector3 spawnPos = new Vector3(spawnX, nextY, 0f);

        // %8 ihtimalle hafif kayık spawn = zorluk
        if (Random.value < missChance)
            spawnPos.x += Random.Range(-0.3f, 0.3f);

        currentBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // Element atama
        Block blockScript = currentBlock.GetComponent<Block>();
        blockScript.elementType = (BlockElementType)Random.Range(0, 4);
        ApplyElementDifficulty(blockScript);

        if (camFollow != null)
            camFollow.target = currentBlock.transform;

        isMoving = true;
        direction = 1f;
    }

    // ------------------ ELEMENT EFFECTS ------------------
    private void ApplyElementDifficulty(Block blockScript)
    {
        // Önce Ice varsa hızı resetle
        moveSpeed = baseMoveSpeed;

        switch (blockScript.elementType)
        {
            case BlockElementType.Fire:
                // Fire: spawn biraz dengesiz olsun
                currentBlock.transform.position += new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
                break;

            case BlockElementType.Ice:
                // Ice: hareket %20 hızlanır
                moveSpeed = baseMoveSpeed * 1.2f;
                break;

            case BlockElementType.Wind:
                // Wind: spawn pozisyonu sağ-sol kayar
                currentBlock.transform.position += new Vector3(Random.Range(-0.6f, 0.6f), 0f, 0f);
                break;

            case BlockElementType.Electric:
                // Electric: spawn anında küçük titreşim
                currentBlock.transform.position += new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0f);
                break;
        }
    }

    // ------------------ DROP BLOCK ------------------
    private void DropCurrentBlock()
    {
        isMoving = false;

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb == null) rb = currentBlock.AddComponent<Rigidbody2D>();

        GameObject prevBlock = transform.childCount > 0
            ? transform.GetChild(transform.childCount - 1).gameObject
            : null;

        float originalWidth = blockPrefab.transform.localScale.x;

        // İlk blok → direkt koy
        if (prevBlock == null)
        {
            currentBlock.transform.position = new Vector3(0f, nextY, 0f);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            currentBlock.transform.parent = transform;
            nextY += blockHeight;

            score++;

            currentBlock = null;
            return;
        }

        float deltaX = Mathf.Abs(currentBlock.transform.position.x - prevBlock.transform.position.x);
        float maxAllowOverlap = (originalWidth + originalWidth) / 2f;

        // Üst üste binmediyse → düşüş
        if (deltaX >= maxAllowOverlap)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f;
            rb.angularVelocity = Random.Range(-200f, 200f);
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            currentBlock.transform.parent = null;
            StartCoroutine(TriggerGameOver());

            currentBlock = null;
            return;
        }

        // Üst üste geldi → hizala & küçük ayar ile scale
        Vector3 pos = currentBlock.transform.position;
        pos.x = prevBlock.transform.position.x; // X ortala
        pos.y = prevBlock.transform.position.y + blockHeight; // Y kesin üst üste
        currentBlock.transform.position = pos;

        // Küçülme için eşik mantığı
        float minOverlapThreshold = 0.05f; // %5 tolerans
        float overlap = Mathf.Max(0, originalWidth - deltaX);
        if (overlap / originalWidth > 0.95f) // %95 veya daha fazlaysa neredeyse tam denk
            overlap = originalWidth; // küçültme yapma

        Vector3 scale = currentBlock.transform.localScale;
        scale.x = overlap;
        currentBlock.transform.localScale = scale;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        currentBlock.transform.parent = transform;
        nextY = pos.y + blockHeight;

        score++;

        currentBlock = null;
    }
    private IEnumerator TriggerGameOver()
    {
        yield return new WaitForSeconds(0.5f);
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.GameOver();
    }
}
