using System.Collections;
using UnityEngine;
using TMPro;

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

    [Header("Perfect Popup")]
    public float perfectToleranceFactor = 0.03f;
    private int combo = 0;

    [Header("References")]
    public Transform spawnPoint;

    [HideInInspector] public bool gameStarted = false;

    private GameManager gm;
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
        gm = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (!gameStarted || isGameOver) return;

        if (currentBlock != null && isMoving)
        {
            currentBlock.transform.position +=
                Vector3.right * direction *
                (moveSpeed + score * speedIncreasePerScore) * Time.deltaTime;

            if (currentBlock.transform.position.x > moveRange) direction = -1f;
            if (currentBlock.transform.position.x < -moveRange) direction = 1f;
        }

        if (Input.GetMouseButtonDown(0))
        {
            AddOrDropBlock();
        }
    }

    public void StartGame()
    {
        gameStarted = true;
    }

    public void AddOrDropBlock()
    {
        if (!gameStarted || isGameOver) return;

        if (currentBlock == null)
        {
            CreateNewBlock();
        }
        else
        {
            DropCurrentBlock();
            CreateNewBlock();
        }
    }

    public void StartTower()
    {
        gameStarted = true;
        CreateNewBlock();
    }

    private void CreateNewBlock()
    {
        float spawnX = 0f;

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
            blockScript.elementType = (Block.BlockElementType)Random.Range(0, 4);
            blockScript.ApplyElementColor();
        }

        if (camFollow != null)
            camFollow.target = currentBlock.transform;

        isMoving = true;
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
            AudioManager.Instance.PlaySound(AudioManager.Instance.placeBlockSound);
            PlaceFirstBlock();
            return;
        }

        float prevWidth = prevBlock.transform.localScale.x;
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

        float tolerance = perfectToleranceFactor * prevWidth;
        bool isPerfect = absOverhang < tolerance;

        if (isPerfect)
        {
            // PERFECT LOGIC
            overlapWidth = prevWidth;
            overhang = 0f;

            AudioManager.Instance.PlaySound(AudioManager.Instance.sliceSound);

            PerfectPopup.Spawn(currentBlock.transform.position + Vector3.up * 0.6f);
            CameraShake.Instance.Shake(0.015f, 0.08f);

            ComboPopup.Spawn(currentBlock.transform.position + Vector3.up * 1.2f, combo);


            combo++;

            StartCoroutine(PunchScale(currentBlock));
        }
        else
        {
            if (overlapWidth > 0f)
            {
                combo++;

                if (combo >= 2)
                {
                    ComboPopup.Spawn(
                        currentBlock.transform.position + Vector3.up * 1.2f,
                        combo
                    );
                }
            }
            else
            {
                combo = 0;
            }
        }
        Debug.Log($"perfect={isPerfect}, overlap={overlapWidth}, combo={combo}");


        float newCenterX = prevBlock.transform.position.x + (overhang / 2f);
        Vector3 newPos = currentBlock.transform.position;
        newPos.x = newCenterX;
        newPos.y = prevBlock.transform.position.y + blockHeight;
        currentBlock.transform.position = newPos;

        Vector3 newScale = currentBlock.transform.localScale;
        newScale.x = overlapWidth;
        currentBlock.transform.localScale = newScale;

        AudioManager.Instance.PlaySound(AudioManager.Instance.placeBlockSound);

        SpawnFallingPieces(prevBlock, newCenterX, overlapWidth);

        Rigidbody2D rbBlock = currentBlock.GetComponent<Rigidbody2D>();
        if (rbBlock == null) rbBlock = currentBlock.AddComponent<Rigidbody2D>();
        rbBlock.bodyType = RigidbodyType2D.Kinematic;
        rbBlock.gravityScale = 0f;

        currentBlock.transform.parent = transform;

        nextY = newPos.y + blockHeight;

        score++;
        if (gm != null)
            gm.UpdateScore(score);

        currentBlock = null;
    }

    private IEnumerator PunchScale(GameObject obj)
    {
        Vector3 original = obj.transform.localScale;

        obj.transform.localScale = original * 1.15f;
        yield return new WaitForSeconds(0.05f);

        obj.transform.localScale = original;
    }

    private void PlaceFirstBlock()
    {
        Rigidbody2D rb = currentBlock.GetComponent<Rigidbody2D>();
        if (rb == null) rb = currentBlock.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        currentBlock.transform.parent = transform;

        nextY = currentBlock.transform.position.y + blockHeight;
        score++;
        gm.UpdateScore(score);

        currentBlock = null;
    }

    private void SpawnFallingPieces(GameObject prevBlock, float newCenterX, float overlapWidth)
    {
        float prevWidth = prevBlock.transform.localScale.x;
        float halfPrev = prevWidth * 0.5f;

        float prevLeft = prevBlock.transform.position.x - halfPrev;
        float prevRight = prevBlock.transform.position.x + halfPrev;

        float overlapLeft = newCenterX - (overlapWidth * 0.5f);
        float overlapRight = newCenterX + (overlapWidth * 0.5f);

        if (overlapLeft > prevLeft + 0.001f)
        {
            float leftWidth = overlapLeft - prevLeft;
            float leftCenter = (prevLeft + overlapLeft) * 0.5f;
            SpawnPiece(leftCenter, leftWidth);
        }

        if (overlapRight < prevRight - 0.001f)
        {
            float rightWidth = prevRight - overlapRight;
            float rightCenter = (overlapRight + prevRight) * 0.5f;
            SpawnPiece(rightCenter, rightWidth);
        }
    }

    private void SpawnPiece(float centerX, float width)
    {
        if (width <= 0f) return;

        GameObject piece = Instantiate(blockPrefab);
        piece.transform.localScale = new Vector3(width, blockPrefab.transform.localScale.y, blockPrefab.transform.localScale.z);
        piece.transform.position = new Vector3(centerX, currentBlock.transform.position.y + 0.6f, 0f);

        Rigidbody2D rb = piece.GetComponent<Rigidbody2D>();
        if (rb == null) rb = piece.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.angularVelocity = Random.Range(-200f, 200f);

        BoxCollider2D col = piece.GetComponent<BoxCollider2D>();
        if (col == null) col = piece.AddComponent<BoxCollider2D>();

        foreach (Transform child in transform)
        {
            Collider2D childCol = child.GetComponent<Collider2D>();
            if (childCol != null)
                Physics2D.IgnoreCollision(col, childCol, true);
        }

        Destroy(piece, 4f);
    }

    private IEnumerator TriggerGameOver()
    {
        isGameOver = true;

        AudioManager.Instance.PlaySound(AudioManager.Instance.gameOverSound);

        yield return new WaitForSeconds(0.5f);
        CameraShake.Instance.Shake(0.07f, 0.15f);

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
            gm.GameOver();
    }

    public void RestartGame()
    {
        isGameOver = false;
        currentBlock = null;
    }
}
