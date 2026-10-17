using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bomb : MonoBehaviour
{
    [Header("Configurações da Bomba")]
    [SerializeField] private float bombDuration = 8f;
    [SerializeField] private float bombGravityScale = 4f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Comportamento ao Bater no Chão")]
    [SerializeField] private float groundSlowdownFactor = 0.35f;

    [Header("Configurações da Roleta / Sorteio")]
    [SerializeField] private int shuffleCount = 10;
    [SerializeField] private float shuffleInterval = 0.3f;
    [SerializeField] private bool decelerateShuffle = true;
    [SerializeField] private float respawnDelay = 1f;

    [Header("Efeitos Visuais & Ordem na Camada")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private int bombSortingOrder = 10;

    private float currentTimer;
    private bool isHeld = false;
    private bool isThrown = false;
    private bool isRandomizing = false;
    private bool isExploded = false;
    private Transform currentHolder;
    private Transform lastHolder = null;
    private Rigidbody2D rb;
    private Collider2D col;
    private Coroutine popUpCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = bombSortingOrder;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = bombGravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col.isTrigger = false;
    }

    private void Start()
    {
        StartCoroutine(RandomizeTargetRoutine());
    }

    private void Update()
    {
        if (isRandomizing || isExploded) return;

        if (isHeld)
        {
            if (currentHolder != null)
            {
                transform.position = currentHolder.position + headOffset;
            }
        }

        HandleTimerAndVisuals();
    }

    private IEnumerator RandomizeTargetRoutine()
    {
        isRandomizing = true;
        isExploded = false;
        isHeld = false;
        isThrown = false;
        rb.simulated = false;
        col.isTrigger = true;

        transform.localScale = Vector3.one;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = normalColor;
        }

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        while (players == null || players.Length < 2)
        {
            yield return new WaitForSeconds(0.1f);
            players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        }

        PlayerMovement[] alivePlayers = GetAlivePlayers();

        if (alivePlayers.Length == 1)
        {
            currentTimer = bombDuration;
            isRandomizing = false;
            PickUp(alivePlayers[0].transform);
            yield break;
        }

        int lastIndex = -1;
        float currentInterval = shuffleInterval;

        for (int i = 0; i < shuffleCount; i++)
        {
            PlayerMovement[] activePlayers = GetAlivePlayers();
            if (activePlayers == null || activePlayers.Length == 0)
            {
                activePlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            }

            if (activePlayers == null || activePlayers.Length == 0)
            {
                yield return null;
                continue;
            }

            int randomIndex = Random.Range(0, activePlayers.Length);
            if (activePlayers.Length > 1 && randomIndex == lastIndex)
            {
                randomIndex = (randomIndex + 1) % activePlayers.Length;
            }
            lastIndex = randomIndex;
            Transform targetTransform = activePlayers[randomIndex].transform;

            TriggerPopUp();

            float elapsed = 0f;
            while (elapsed < currentInterval)
            {
                if (targetTransform != null)
                {
                    transform.position = targetTransform.position + headOffset;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (decelerateShuffle)
            {
                currentInterval = Mathf.Lerp(shuffleInterval, shuffleInterval * 2f, (float)i / shuffleCount);
            }
        }

        PlayerMovement[] finalAlivePlayers = GetAlivePlayers();
        if (finalAlivePlayers == null || finalAlivePlayers.Length == 0)
        {
            finalAlivePlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        }

        if (finalAlivePlayers == null || finalAlivePlayers.Length == 0)
        {
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(RandomizeTargetRoutine());
            yield break;
        }

        int finalIndex = Random.Range(0, finalAlivePlayers.Length);
        Transform finalTarget = finalAlivePlayers[finalIndex].transform;

        TriggerPopUp();

        float finalElapsed = 0f;
        while (finalElapsed < currentInterval)
        {
            if (finalTarget != null)
            {
                transform.position = finalTarget.position + headOffset;
            }
            finalElapsed += Time.deltaTime;
            yield return null;
        }

        currentTimer = bombDuration;
        isRandomizing = false;

        PickUp(finalTarget);
    }

    private PlayerMovement[] GetAlivePlayers()
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        System.Collections.Generic.List<PlayerMovement> aliveList = new System.Collections.Generic.List<PlayerMovement>();

        foreach (PlayerMovement p in allPlayers)
        {
            if (p != null && !p.IsDead)
            {
                aliveList.Add(p);
            }
        }
        return aliveList.ToArray();
    }

    private void HandleTimerAndVisuals()
    {
        currentTimer -= Time.deltaTime;

        float timeRatio = Mathf.Clamp01(currentTimer / bombDuration);
        float currentPeriod = Mathf.Lerp(0.2f, 1.0f, timeRatio);
        float speed = 1f / currentPeriod;

        float scaleAmount = Mathf.Sin(Time.time * (speed * Mathf.PI * 2f)) * 0.15f + 1f;

        if (popUpCoroutine == null)
        {
            transform.localScale = Vector3.one * scaleAmount;
        }

        float pingPong = Mathf.PingPong(Time.time * speed, 1f);
        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(normalColor, flashColor, pingPong);

        if (currentTimer <= 0f && !isExploded)
        {
            Explode();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isRandomizing || isExploded) return;

        if (collision.collider.CompareTag("Projectile") || collision.collider.GetComponent("Projectile") != null)
        {
            return;
        }

        BossController boss = collision.collider.GetComponent<BossController>();
        if (boss == null) boss = collision.collider.GetComponentInParent<BossController>();

        if (boss != null)
        {
            if (boss.IsStaggered)
            {
                Explode(boss);
            }
            else
            {
                boss.OnBombBounce();

                if (collision.contactCount > 0)
                {
                    Vector2 normal = collision.contacts[0].normal;
                    rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal) * 0.8f;
                }
            }
            return;
        }

        if (!isThrown) return;

        PlayerMovement hitPlayer = collision.collider.GetComponent<PlayerMovement>();
        if (hitPlayer == null) hitPlayer = collision.collider.GetComponentInParent<PlayerMovement>();

        if (hitPlayer != null)
        {
            if (hitPlayer.IsDead)
            {
                hitPlayer.Revive();
                PickUp(hitPlayer.transform);
                return;
            }

            PickUp(hitPlayer.transform);
            return;
        }

        if (collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.contacts[0];
            if (contact.normal.y > 0.5f)
                rb.linearVelocity *= groundSlowdownFactor;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isRandomizing || isExploded) return;

        if (collision.CompareTag("Projectile") || collision.GetComponent("Projectile") != null)
        {
            return;
        }

        BossController boss = collision.GetComponent<BossController>();
        if (boss == null) boss = collision.GetComponentInParent<BossController>();

        if (boss != null)
        {
            if (boss.IsStaggered)
            {
                Explode(boss);
            }
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

        TriggerPopUp();
    }

    private void TriggerPopUp()
    {
        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        popUpCoroutine = StartCoroutine(AnimatePopUp());
    }

    private IEnumerator AnimatePopUp()
    {
        float duration = 0.18f;
        float elapsed = 0f;

        Vector3 startScale = Vector3.one * 0.7f;
        Vector3 maxScale = Vector3.one * 1.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, maxScale, Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        transform.localScale = Vector3.one;
        popUpCoroutine = null;
    }

    public void DropByDeadPlayer()
    {
        isHeld = false;
        currentHolder = null;
        rb.simulated = false;
        col.isTrigger = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    public void Throw(Vector2 direction, Collider2D throwerCol, float customSpeed)
    {
        if (!isHeld || isRandomizing || isExploded) return;

        isHeld = false;
        isThrown = true;
        currentHolder = null;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = bombGravityScale;
        col.isTrigger = false;

        if (direction == Vector2.zero)
            direction = Vector2.right;

        rb.linearVelocity = direction.normalized * customSpeed;

        if (throwerCol != null)
        {
            StartCoroutine(IgnoreThrowerTemporarily(throwerCol));
        }
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

    private void Explode(BossController directBoss = null)
    {
        if (isExploded) return;
        isExploded = true;

        if (GameEffectsManager.Instance != null)
        {
            GameEffectsManager.Instance.TriggerBombExplosionEffects(transform.position);
        }

        bool hitBoss = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        if (directBoss != null)
        {
            if (directBoss.IsStaggered)
            {
                directBoss.TakeDamage(1);
                hitBoss = true;
            }
        }
        else
        {
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Boss")) continue;

                BossController boss = hit.GetComponent<BossController>();
                if (boss == null) boss = hit.GetComponentInParent<BossController>();

                if (boss != null && boss.IsStaggered)
                {
                    boss.TakeDamage(1);
                    hitBoss = true;
                    break;
                }
            }
        }

        if (hitBoss)
        {
            if (currentHolder != null)
            {
                PlayerMovement holder = currentHolder.GetComponent<PlayerMovement>();
                if (holder != null) holder.DropBomb();
            }

            StartCoroutine(RespawnAndShuffleRoutine());
            return;
        }
        else
        {
            if (currentHolder != null)
            {
                PlayerMovement holder = currentHolder.GetComponent<PlayerMovement>();
                if (holder != null)
                {
                    holder.DropBomb();
                    holder.TakeDamage(1);
                }
            }
            else
            {
                foreach (Collider2D hit in hits)
                {
                    if (!hit.CompareTag("Player")) continue;

                    PlayerMovement player = hit.GetComponent<PlayerMovement>();
                    if (player != null && !player.IsDead)
                        player.TakeDamage(1);
                }
            }
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnAndShuffleRoutine()
    {
        isHeld = false;
        isThrown = false;
        currentHolder = null;
        rb.simulated = false;
        col.isTrigger = true;

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        StartCoroutine(RandomizeTargetRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isHeld = false;
        isThrown = false;
        currentHolder = null;
        rb.simulated = false;
        col.isTrigger = true;

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        StartCoroutine(RandomizeTargetRoutine());
    }
}