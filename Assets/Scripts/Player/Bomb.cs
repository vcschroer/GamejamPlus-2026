using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bomb : MonoBehaviour
{
    [Header("Configurações da Bomba")]
    [SerializeField] private float bombDuration = 6f; 
    [SerializeField] private float throwSpeed = 40f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f); 

    [Header("Efeitos Visuais")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color flashColor = Color.red;

    private float currentTimer;
    private bool isHeld = false;
    private bool isThrown = false;
    private Transform currentHolder;
    private Transform lastHolder = null; 
    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        currentTimer = bombDuration;

        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false;
        isThrown = true;
    }

    private void Update()
    {
        if (isHeld)
        {
            if (currentHolder != null)
            {
                transform.position = currentHolder.position + headOffset;
            }
        }

        HandleTimerAndVisuals();
    }

    private void HandleTimerAndVisuals()
    {
        currentTimer -= Time.deltaTime;

        float timeRatio = Mathf.Clamp01(currentTimer / bombDuration);

        float currentPeriod = Mathf.Lerp(0.2f, 1.0f, timeRatio);
        float speed = 1f / currentPeriod;

        float scaleAmount = Mathf.Sin(Time.time * (speed * Mathf.PI * 2f)) * 0.15f + 1f;
        transform.localScale = Vector3.one * scaleAmount;

        float pingPong = Mathf.PingPong(Time.time * speed, 1f);
        spriteRenderer.color = Color.Lerp(normalColor, flashColor, pingPong);

        if (currentTimer <= 0f)
        {
            Explode();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isThrown) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PickUp(collision.transform);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            rb.linearVelocity *= 0.3f;
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            rb.linearVelocity *= 0.7f;
        }
    }

    public void PickUp(Transform playerTransform)
    {
        if (playerTransform != lastHolder)
        {
            currentTimer = bombDuration;
        }

        if (currentHolder != null)
        {
            PlayerMovement prevPlayer = currentHolder.GetComponent<PlayerMovement>();
            if (prevPlayer != null) prevPlayer.DropBomb();
        }

        isHeld = true;
        isThrown = false;
        currentHolder = playerTransform;
        lastHolder = playerTransform; 

        PlayerMovement newPlayer = playerTransform.GetComponent<PlayerMovement>();
        if (newPlayer != null) newPlayer.HoldBomb(this);

        rb.simulated = false; 
        col.isTrigger = true; 
    }

    public void Throw(Vector2 direction, Collider2D throwerCol, float customSpeed)
    {
        if (!isHeld) return;

        isHeld = false;
        isThrown = true;
        currentHolder = null;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.isTrigger = false; 

        if (direction == Vector2.zero)
            direction = Vector2.right;

        rb.linearVelocity = direction.normalized * customSpeed;

        if (throwerCol != null)
        {
            StartCoroutine(IgnoreThrowerTemporarily(throwerCol));
        }
    }

    public void Throw(Vector2 direction, Collider2D throwerCol)
    {
        Throw(direction, throwerCol, throwSpeed);
    }

    private IEnumerator IgnoreThrowerTemporarily(Collider2D throwerCol)
    {
        Physics2D.IgnoreCollision(col, throwerCol, true);
        yield return new WaitForSeconds(0.25f); 
        if (throwerCol != null)
        {
            Physics2D.IgnoreCollision(col, throwerCol, false);
        }
    }

    private void Explode()
    {
        Debug.Log("BOOM! A bomba explodiu!");

        if (currentHolder != null)
        {
            PlayerMovement player = currentHolder.GetComponent<PlayerMovement>();
            if (player != null) player.DropBomb();
        }

        Destroy(gameObject);
    }
}