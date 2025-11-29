using System.Collections;
using UnityEngine;

public class TowerController : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;
    public float blockHeight = 1f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float speedIncreasePerScore = 0.1f;
    public float moveRange = 3f;

    [Header("Difficulty")]
    public float missChance = 0.08f;

    private GameObject currentBlock;
    private float nextY = 0f;
    private bool isMoving = false;
    private float direction = 1f;
    private bool isGameOver = false;

    private CameraFollow camFollow;
    public int score = 0;

    void Start()
    {
        nextY = transform.childCount > 0
            ? transform.GetChild(transform.childCount - 1).position.y + blockHeight
            : 0f;

        camFollow = Camera.main.GetComponent<CameraFollow>();
    }

    void Update()
    {
        if (isGameOver) return;

        if (currentBlock != null && isMoving)
        {
            currentBlock.transform.position += Vector3.right * direction * (moveSpeed + score * speedIncreasePerScore) * Time.deltaTime;

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
        if (isGameOver) return;

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
        float spawnX = 0f;

        // Önceki bloğa göre genişlik ayarı
        if (transform.childCount > 0)
        {
            GameObject prevBlock = transform.GetChild(transform.childCount - 1).gameObject;
            float prevWidth = prevBlock.transform.localScale.x;

            Vector3 scale = blockPrefab.transform.localScale;
            scale.x = prevWidth;
            blockPrefab.transform.localScale = scale;

            spawnX += Random.Range(-0.15f, 0.15f);
        }

        Vector3 spawnPos = new Vector3(spawnX, nextY, 0f);

        if (Random.value < missChance)
            spawnPos.x += Random.Range(-0.3f, 0.3f);

        currentBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        Block blockScript = currentBlock.GetComponent<Block>();
        if (blockScript != null)
        {
            // Rastgele tip ata ve renk uygula
            blockScript.elementType = (Block.BlockElementType)Random.Range(0, 4); // 0-3 Fire,Ice,Wind,Electric, None yok
            blockScript.ApplyElementColor();
        }

        if (camFollow != null)
            camFollow.target = currentBlock.transform;

        // Blok hareketini başlat
        isMoving = true;

        // Yönü rastgele belirle (sağ veya sol)
        direction = Random.value < 0.5f ? -1f : 1f;
    }

    private void DropCurrentBlock()
    {
        if (!isMoving || currentBlock == null) return;
        isMoving = false;

        GameObject prevBlock = transform.childCount > 0
            ? transform.GetChild(transform.childCount - 1).gameObject
            : null;

        if (prevBlock == null)
        {
            PlaceBlock(currentBlock, Vector3.zero);
            return;
        }

        float prevWidth = prevBlock.transform.localScale.x;
        float halfPrev = prevWidth * 0.5f;

        float overhang = currentBlock.transform.position.x - prevBlock.transform.position.x;
        float absOverhang = Mathf.Abs(overhang);

        float overlapWidth = Mathf.Max(0f, prevWidth - absOverhang);

        if (overlapWidth <= 0f)
        {
            Rigidbody2D rbMiss = currentBlock.GetComponent<Rigidbody2D>();
            if (rbMiss == null) rbMiss = currentBlock.AddComponent<Rigidbody2D>();
            rbMiss.bodyType = RigidbodyType2D.Dynamic;
            rbMiss.gravityScale = 3f;
            rbMiss.angularVelocity = Random.Range(-200f, 200f);
            currentBlock.transform.parent = null;

            StartCoroutine(TriggerGameOver());
            currentBlock = null;
            return;
        }

        float tolerance = 0.05f * prevWidth;
        if (absOverhang < tolerance)
        {
            overlapWidth = prevWidth;
            overhang = 0f;
        }
        else
        {
            float minAllowed = prevWidth * 0.5f;
            overlapWidth = Mathf.Clamp(overlapWidth, minAllowed, prevWidth);
        }

        float newCenterX = prevBlock.transform.position.x + (overhang / 2f);
        Vector3 newPos = currentBlock.transform.position;
        newPos.x = newCenterX;
        newPos.y = prevBlock.transform.position.y + blockHeight;
        currentBlock.transform.position = newPos;

        Vector3 newScale = currentBlock.transform.localScale;
        newScale.x = overlapWidth;
        currentBlock.transform.localScale = newScale;

        // BLOK RENGİ
        SpriteRenderer currentSR = currentBlock.GetComponentInChildren<SpriteRenderer>();
        Color blockColor = currentSR != null ? currentSR.color : Color.gray;

        // Kırpılan parçalar
        float prevLeft = prevBlock.transform.position.x - halfPrev;
        float prevRight = prevBlock.transform.position.x + halfPrev;

        float overlapLeft = newCenterX - (overlapWidth * 0.5f);
        float overlapRight = newCenterX + (overlapWidth * 0.5f);

        if (overlapLeft > prevLeft + 0.001f)
        {
            float leftWidth = overlapLeft - prevLeft;
            float leftCenter = (prevLeft + overlapLeft) * 0.5f;
            SpawnFallingPiece(leftCenter, leftWidth, currentBlock.transform.position.y + blockHeight, blockColor);
        }

        if (overlapRight < prevRight - 0.001f)
        {
            float rightWidth = prevRight - overlapRight;
            float rightCenter = (prevRight + overlapRight) * 0.5f;
            SpawnFallingPiece(rightCenter, rightWidth, currentBlock.transform.position.y + blockHeight, blockColor);
        }

        // Ana bloğu sabitle
        Rigidbody2D rbBlock = currentBlock.GetComponent<Rigidbody2D>();
        if (rbBlock == null) rbBlock = currentBlock.AddComponent<Rigidbody2D>();
        rbBlock.bodyType = RigidbodyType2D.Kinematic;
        rbBlock.gravityScale = 0f;
        currentBlock.transform.parent = transform;
        nextY = newPos.y + blockHeight;

        score++;
        currentBlock = null;
    }
    private void SpawnFallingPiece(float centerX, float width, float startY, Color color)
    {
        if (width <= 0f) return;

        GameObject piece = Instantiate(blockPrefab);
        piece.transform.localScale = new Vector3(width, blockPrefab.transform.localScale.y, blockPrefab.transform.localScale.z);
        piece.transform.position = new Vector3(centerX, startY + 0.5f, 0f);

        Block blockScript = piece.GetComponent<Block>();
        if (blockScript != null)
            blockScript.blockColor = color;

        SpriteRenderer sr = piece.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = color;

        Rigidbody2D rb = piece.GetComponent<Rigidbody2D>();
        if (rb == null) rb = piece.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.angularVelocity = Random.Range(-200f, 200f);
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = piece.GetComponent<BoxCollider2D>();
        if (col == null) col = piece.AddComponent<BoxCollider2D>();

        // Alt bloğa çarpmaz
        foreach (Transform child in transform)
        {
            Collider2D childCol = child.GetComponent<Collider2D>();
            if (childCol != null)
                Physics2D.IgnoreCollision(rb.GetComponent<Collider2D>(), childCol, true);
        }

        Destroy(piece, 5f);
    }


    private void PlaceBlock(GameObject block, Vector3 offset)
    {
        block.transform.position = new Vector3(0f, nextY, 0f) + offset;
        Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
        if (rb == null) rb = block.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        block.transform.parent = transform;
        nextY += blockHeight;

        score++;
        currentBlock = null;
    }

    private IEnumerator TriggerGameOver()
    {
        isGameOver = true;
        yield return new WaitForSeconds(0.5f);
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.GameOver();
    }

    public void RestartGame()
    {
        isGameOver = false;
        currentBlock = null;
    }
}
