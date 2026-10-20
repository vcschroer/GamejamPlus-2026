using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bomb : MonoBehaviour
{
    [SerializeField] private float bombDuration = 8f;
    [SerializeField] private float bombGravityScale = 4f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.2f, 0f);

    [SerializeField] private float groundSlowdownFactor = 0.35f;

    [SerializeField] private int shuffleCount = 10;
    [SerializeField] private float shuffleInterval = 0.3f;
    [SerializeField] private bool decelerateShuffle = true;
    [SerializeField] private float respawnDelay = 1f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject bombLightObject;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color middleColor = Color.yellow;
    [SerializeField] private Color endColor = Color.red;
    [SerializeField] private int bombSortingOrder = 10;

    [SerializeField] private GameObject explosionParticlesPrefab;

    [Min(0)]
    [SerializeField] private int explosionParticleCount = 1;

    [Min(0f)]
    [SerializeField] private float explosionDestroyDelay = 2f;

    private float currentTimer;
    private bool isHeld = false;
    private bool isThrown = false;
    private bool isRandomizing = false;
    private bool isExploded = false;
    private bool groundSoundPlayed = false;

    private Transform currentHolder;
    private Transform lastHolder = null;
    private Transform lastThrower = null;

    private Rigidbody2D rb;
    private Collider2D col;
    private Coroutine popUpCoroutine;
    private TrailRenderer bombTrail;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        bombTrail = GetComponentInChildren<TrailRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (bombLightObject == null && transform.childCount > 0)
        {
            Transform lightChild = transform.GetChild(0);

            if (lightChild != null)
                bombLightObject = lightChild.gameObject;
        }

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = bombSortingOrder;

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
        if (Time.timeScale == 0f)
            return;

        if (isRandomizing || isExploded)
            return;

        if (isHeld && currentHolder != null)
        {
            transform.position = currentHolder.position + headOffset;
        }

        HandleTimerAndVisuals();
    }

    private void SetVisualsActive(bool active)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = active;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (bombTrail != null && child == bombTrail.transform)
                continue;

            child.gameObject.SetActive(active);
        }
    }

    private void PlayCountdownSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("bomb coutdown");
        }
    }

    private IEnumerator RandomizeTargetRoutine()
    {
        isRandomizing = true;
        isExploded = false;
        isHeld = false;
        isThrown = false;
        groundSoundPlayed = false;
        lastThrower = null;

        rb.simulated = false;
        col.isTrigger = true;

        transform.localScale = Vector3.one;

        SetVisualsActive(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = startColor;
        }

        ToggleTrail(false);

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        while (players == null || players.Length < 2)
        {
            yield return new WaitForSecondsRealtime(0.1f);

            players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        }

        PlayerMovement[] alivePlayers = GetAlivePlayers();

        if (alivePlayers.Length == 1)
        {
            currentTimer = bombDuration;
            PlayCountdownSound();
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
                randomIndex = (randomIndex + 1) % activePlayers.Length;

            lastIndex = randomIndex;

            Transform targetTransform = activePlayers[randomIndex].transform;

            TriggerPopUp();

            float elapsed = 0f;

            while (elapsed < currentInterval)
            {
                while (Time.timeScale == 0f)
                    yield return null;

                if (targetTransform != null)
                    transform.position = targetTransform.position + headOffset;

                elapsed += Time.unscaledDeltaTime;
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
            yield return new WaitForSecondsRealtime(0.5f);
            StartCoroutine(RandomizeTargetRoutine());
            yield break;
        }

        int finalIndex = Random.Range(0, finalAlivePlayers.Length);
        Transform finalTarget = finalAlivePlayers[finalIndex].transform;

        TriggerPopUp();

        float finalElapsed = 0f;

        while (finalElapsed < currentInterval)
        {
            while (Time.timeScale == 0f)
                yield return null;

            if (finalTarget != null)
                transform.position = finalTarget.position + headOffset;

            finalElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        currentTimer = bombDuration;
        PlayCountdownSound();
        isRandomizing = false;

        PickUp(finalTarget);
    }

    private PlayerMovement[] GetAlivePlayers()
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        List<PlayerMovement> aliveList = new List<PlayerMovement>();

        foreach (PlayerMovement player in allPlayers)
        {
            if (player != null && !player.IsDead)
                aliveList.Add(player);
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
            transform.localScale = Vector3.one * scaleAmount;

        Color targetFlashColor;

        if (timeRatio > 0.5f)
        {
            float t = (timeRatio - 0.5f) * 2f;
            targetFlashColor = Color.Lerp(middleColor, startColor, t);
        }
        else
        {
            float t = timeRatio * 2f;
            targetFlashColor = Color.Lerp(endColor, middleColor, t);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetFlashColor;
        }

        if (currentTimer <= 0f && !isExploded)
            Explode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.timeScale == 0f)
            return;

        if (isRandomizing || isExploded)
            return;

        if (collision.collider.CompareTag("Projectile") || collision.collider.GetComponent("Projectile") != null)
        {
            return;
        }

        BossController boss = collision.collider.GetComponent<BossController>();

        if (boss == null)
            boss = collision.collider.GetComponentInParent<BossController>();

        if (boss != null)
        {
            if (boss.IsStaggered)
            {
                Explode(boss);
            }
            else
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySound("Bomba rebate boss");

                boss.OnBombBounce();

                if (collision.contactCount > 0)
                {
                    Vector2 normal = collision.contacts[0].normal;

                    Vector2 pushAwayDir = (transform.position - boss.transform.position).normalized;

                    if (pushAwayDir.sqrMagnitude < 0.1f)
                        pushAwayDir = Vector2.up;

                    rb.linearVelocity = (Vector2.Reflect(rb.linearVelocity, normal) + pushAwayDir * 5f) * 1.2f;
                }
            }

            return;
        }

        if (!isThrown)
            return;

        if (collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.contacts[0];

            if (contact.normal.y > 0.5f)
            {
                if (!groundSoundPlayed)
                {
                    if (SoundManager.Instance != null)
                        SoundManager.Instance.PlaySound("Bomba cai no cho");

                    groundSoundPlayed = true;
                }

                rb.linearVelocity *= groundSlowdownFactor;
            }
        }

        PlayerMovement hitPlayer = collision.collider.GetComponent<PlayerMovement>();

        if (hitPlayer == null)
            hitPlayer = collision.collider.GetComponentInParent<PlayerMovement>();

        if (hitPlayer != null)
        {
            if (hitPlayer.IsDead)
            {
                hitPlayer.Revive();
                PickUp(hitPlayer.transform);
                return;
            }

            PickUp(hitPlayer.transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Time.timeScale == 0f)
            return;

        if (isRandomizing || isExploded)
            return;

        if (collision.CompareTag("Projectile") || collision.GetComponent("Projectile") != null)
        {
            return;
        }

        BossController boss = collision.GetComponent<BossController>();

        if (boss == null)
            boss = collision.GetComponentInParent<BossController>();

        if (boss != null && boss.IsStaggered)
            Explode(boss);
    }

    public void PickUp(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        if (lastThrower != null && playerTransform != lastThrower)
        {
            FindAnyObjectByType<TutorialManager>()?.NotifyBombPassedBetweenPlayers();
        }

        if (lastHolder != null && playerTransform != lastHolder)
        {
            currentTimer = bombDuration;
            PlayCountdownSound();
        }

        if (currentHolder != null)
        {
            PlayerMovement previousPlayer = currentHolder.GetComponent<PlayerMovement>();

            if (previousPlayer != null)
                previousPlayer.DropBomb();
        }

        isHeld = true;
        isThrown = false;
        currentHolder = playerTransform;
        lastHolder = playerTransform;

        PlayerMovement newPlayer = playerTransform.GetComponent<PlayerMovement>();

        if (newPlayer != null)
            newPlayer.HoldBomb(this);

        rb.simulated = false;
        col.isTrigger = true;

        ToggleTrail(false);
        TriggerPopUp();
    }

    private void TriggerPopUp()
    {
        if (popUpCoroutine != null)
            StopCoroutine(popUpCoroutine);

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
            while (Time.timeScale == 0f)
                yield return null;

            elapsed += Time.unscaledDeltaTime;
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

        SetVisualsActive(false);

        ToggleTrail(false);
    }

    public void DropByDeadPlayerToward(Vector2 throwDirection, float speed)
    {
        isHeld = false;
        isThrown = true;

        groundSoundPlayed = false;
        currentHolder = null;

        transform.position = (Vector2)transform.position + new Vector2(0f, 0.5f);

        SetVisualsActive(true);

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = bombGravityScale;

        col.isTrigger = false;

        if (throwDirection == Vector2.zero)
            throwDirection = Vector2.right;

        rb.linearVelocity = throwDirection.normalized * speed;

        StartCoroutine(EnableTrailAfterDelay(0.1f));
    }

    public void Throw(Vector2 direction, Collider2D throwerCol, float customSpeed)
    {
        if (!isHeld || isRandomizing || isExploded)
            return;

        if (currentHolder != null)
        {
            lastThrower = currentHolder;
        }

        isHeld = false;
        isThrown = true;

        groundSoundPlayed = false;
        currentHolder = null;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = bombGravityScale;

        col.isTrigger = false;

        if (direction == Vector2.zero)
            direction = Vector2.right;

        rb.linearVelocity = direction.normalized * customSpeed;

        if (throwerCol != null)
            StartCoroutine(IgnoreThrowerTemporarily(throwerCol));

        StartCoroutine(EnableTrailAfterDelay(0.5f));
    }

    private IEnumerator EnableTrailAfterDelay(float delay)
    {
        ToggleTrail(false);
        yield return new WaitForSecondsRealtime(delay);
        ToggleTrail(true);
    }

    private void ToggleTrail(bool active)
    {
        if (bombTrail != null)
        {
            if (active)
            {
                bombTrail.Clear();
                bombTrail.enabled = true;
            }
            else
            {
                bombTrail.enabled = false;
            }
        }
    }

    private IEnumerator IgnoreThrowerTemporarily(Collider2D throwerCol)
    {
        Physics2D.IgnoreCollision(col, throwerCol, true);

        yield return new WaitForSecondsRealtime(0.25f);

        if (throwerCol != null)
            Physics2D.IgnoreCollision(col, throwerCol, false);
    }

    private void Explode(BossController directBoss = null)
    {
        if (isExploded)
            return;

        isExploded = true;
        ToggleTrail(false);

        if (explosionParticlesPrefab != null)
        {
            for (int i = 0; i < explosionParticleCount; i++)
            {
                GameObject explosion = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);

                if (explosion != null)
                    Destroy(explosion, explosionDestroyDelay);
            }
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("Bomba explode");

        if (GameEffectsManager.Instance != null)
        {
            GameEffectsManager.Instance.TriggerBombExplosionEffects(transform.position);
        }

        float explosionRadius = 1.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        bool hitBoss = false;

        if (directBoss != null && directBoss.IsStaggered)
        {
            directBoss.TakeDamage(1);
            FindAnyObjectByType<TutorialManager>()?.NotifyBossHitByBombInStagger();
            hitBoss = true;
        }
        else
        {
            foreach (Collider2D hit in hits)
            {
                BossController boss = hit.GetComponent<BossController>();

                if (boss == null)
                    boss = hit.GetComponentInParent<BossController>();

                if (boss != null && boss.IsStaggered)
                {
                    boss.TakeDamage(1);
                    FindAnyObjectByType<TutorialManager>()?.NotifyBossHitByBombInStagger();
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

                if (holder != null)
                    holder.DropBomb();
            }

            StartCoroutine(RespawnAndShuffleRoutine());
            return;
        }

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        HashSet<PlayerMovement> damagedPlayers = new HashSet<PlayerMovement>();

        foreach (PlayerMovement player in players)
        {
            if (player == null || player.IsDead)
                continue;

            if (damagedPlayers.Add(player))
                player.TakeDamage(1);
        }

        if (currentHolder != null)
        {
            PlayerMovement holder = currentHolder.GetComponent<PlayerMovement>();

            if (holder != null)
                holder.DropBomb();
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnAndShuffleRoutine()
    {
        isHeld = false;
        isThrown = false;
        currentHolder = null;
        lastThrower = null;

        rb.simulated = false;
        col.isTrigger = true;

        SetVisualsActive(false);

        yield return new WaitForSecondsRealtime(respawnDelay);

        StartCoroutine(RandomizeTargetRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isHeld = false;
        isThrown = false;
        currentHolder = null;
        lastThrower = null;

        rb.simulated = false;
        col.isTrigger = true;

        SetVisualsActive(false);

        yield return new WaitForSecondsRealtime(respawnDelay);

        StartCoroutine(RandomizeTargetRoutine());
    }
}