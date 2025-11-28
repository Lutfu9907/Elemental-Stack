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
        if (isGameOver) return; // GameOver sonrası blok eklenmesini engelle

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

        // Önceki bloğa göre yeni bloğun genişliğini ayarla
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
        blockScript.elementType = (BlockElementType)Random.Range(0, 5); // None da dahil

        if (camFollow != null)
            camFollow.target = currentBlock.transform;

        isMoving = true;
        direction = 1f;
    }

    private void DropCurrentBlock()
    {
        isMoving = false;

        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb == null) rb = currentBlock.AddComponent<Rigidbody2D>();

        GameObject prevBlock = transform.childCount > 0
            ? transform.GetChild(transform.childCount - 1).gameObject
            : null;

        float originalWidth = blockPrefab.transform.localScale.x;

        // İlk blok → direkt yerleştir
        if (prevBlock == null)
        {
            PlaceBlock(currentBlock, Vector3.zero);
            return;
        }

        float deltaX = Mathf.Abs(currentBlock.transform.position.x - prevBlock.transform.position.x);
        float maxAllowOverlap = originalWidth; // %100 overlap limit

        if (deltaX >= maxAllowOverlap)
        {
            // Game Over
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f;
            rb.angularVelocity = Random.Range(-200f, 200f);
            currentBlock.transform.parent = null;

            StartCoroutine(TriggerGameOver());
            currentBlock = null;
            return;
        }

        // Üst üste → hizala & scale uygula
        Vector3 pos = currentBlock.transform.position;
        pos.x = prevBlock.transform.position.x;
        pos.y = prevBlock.transform.position.y + blockHeight;
        currentBlock.transform.position = pos;

        // -------------------- STABİL KÜÇÜLME --------------------
        float overlap = originalWidth - deltaX;

        // Eğer tam üst üste denk gelmişse küçülme yok
        if (deltaX < 0.05f)
            overlap = originalWidth;
        else
        {
            // Orantılı kırpma
            overlap = Mathf.Clamp(overlap, originalWidth * 0.5f, originalWidth);
        }

        Vector3 scale = currentBlock.transform.localScale;
        scale.x = overlap;
        currentBlock.transform.localScale = scale;
        // --------------------------------------------------------

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        currentBlock.transform.parent = transform;
        nextY = pos.y + blockHeight;

        score++;
        currentBlock = null;
    }
    private void PlaceBlock(GameObject block, Vector3 offset)
    {
        block.transform.position = new Vector3(0f, nextY, 0f) + offset;
        Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
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
