using System.Collections;
using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;
    public float blockHeight = 1f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    private float baseMoveSpeed;
    public float speedIncreasePerScore = 0.1f;
    public float moveRange = 3f;

    [Header("Difficulty")]
    public float missChance = 0.08f;

    private GameObject currentBlock;
    private float nextY = 0f;
    private bool isMoving = false;
    private float direction = 1f;

    private CameraFollow camFollow;
    public int score = 0;
    private bool gameActive = true; // ← restart sonrası tıklamaları kontrol için

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
        if (!gameActive) return; // oyun aktif değilse tıklama ve hareket yok

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
        if (!gameActive) return;

        if (currentBlock == null)
            CreateNewBlock();
        else
        {
            DropCurrentBlock();
            CreateNewBlock();
        }
    }

    private void CreateNewBlock()
    {
        GameObject prevBlock = transform.childCount > 0 ? transform.GetChild(transform.childCount - 1).gameObject : null;

        float spawnX = 0f;
        Vector3 spawnScale = blockPrefab.transform.localScale;

        if (prevBlock != null)
        {
            // Yeni blok, alt blokla aynı genişlikte başlar
            spawnScale.x = prevBlock.transform.localScale.x;
            spawnX = prevBlock.transform.position.x; // Ortala
        }

        Vector3 spawnPos = new Vector3(spawnX, nextY, 0f);

        // %8 ihtimalle hafif kayık spawn
        if (Random.value < missChance)
            spawnPos.x += Random.Range(-0.3f, 0.3f);

        currentBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
        currentBlock.transform.localScale = spawnScale;

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

    private void ApplyElementDifficulty(Block blockScript)
    {
        moveSpeed = baseMoveSpeed;

        switch (blockScript.elementType)
        {
            case BlockElementType.Fire:
                currentBlock.transform.position += new Vector3(Random.Range(-0.2f, 0.2f), 0f, 0f);
                break;
            case BlockElementType.Ice:
                moveSpeed = baseMoveSpeed * 1.2f;
                break;
            case BlockElementType.Wind:
                currentBlock.transform.position += new Vector3(Random.Range(-0.6f, 0.6f), 0f, 0f);
                break;
            case BlockElementType.Electric:
                currentBlock.transform.position += new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0f);
                break;
        }
    }

    private void DropCurrentBlock()
    {
        isMoving = false;

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb == null) rb = currentBlock.AddComponent<Rigidbody2D>();

        GameObject prevBlock = transform.childCount > 0 ? transform.GetChild(transform.childCount - 1).gameObject : null;

        if (prevBlock == null)
        {
            // İlk blok → direkt koy
            currentBlock.transform.position = new Vector3(0f, nextY, 0f);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            currentBlock.transform.parent = transform;
            nextY += blockHeight;
            score++;
            currentBlock = null;
            return;
        }

        float deltaX = currentBlock.transform.position.x - prevBlock.transform.position.x;
        float prevWidth = prevBlock.transform.localScale.x;

        // Eğer overlap yok → düşüş
        if (Mathf.Abs(deltaX) >= prevWidth)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f;
            rb.angularVelocity = Random.Range(-200f, 200f);
            currentBlock.transform.parent = null;
            StartCoroutine(TriggerGameOver());
            currentBlock = null;
            return;
        }

        // Üst üste geldi → scale sadece gerçek overlap kadar
        float overlap = prevWidth - Mathf.Abs(deltaX);
        overlap = Mathf.Max(overlap, prevWidth * 0.3f); // minimum %30 genişlik

        Vector3 scale = currentBlock.transform.localScale;
        scale.x = overlap;
        currentBlock.transform.localScale = scale;

        Vector3 pos = currentBlock.transform.position;
        pos.x = prevBlock.transform.position.x + Mathf.Clamp(deltaX, -prevWidth / 2 + overlap / 2, prevWidth / 2 - overlap / 2);
        pos.y = prevBlock.transform.position.y + blockHeight;
        currentBlock.transform.position = pos;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        currentBlock.transform.parent = transform;

        nextY = pos.y + blockHeight;
        score++;
        currentBlock = null;
    }

    private IEnumerator TriggerGameOver()
    {
        gameActive = false; // ← artık tıklamalar bloke
        yield return new WaitForSeconds(0.5f);
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.GameOver();
    }

    // -------- PUBLIC: Restart --------
    public void RestartGame()
    {
        gameActive = true;
    }
}
