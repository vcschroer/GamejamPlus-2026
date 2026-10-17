using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Movimentação & Área de Voo (Collider Bounds)")]
    [SerializeField] private Collider2D flightAreaCollider;
    [SerializeField] private float minFlySpeed = 3f;
    [SerializeField] private float maxFlySpeed = 8f;
    [SerializeField] private float waypointReachDistance = 0.8f;

    [Header("Cooldown de Movimento")]
    [SerializeField] private float maxMoveCooldown = 2.0f;
    [SerializeField] private float minMoveCooldown = 0.4f;
    private float moveCooldownTimer = 0f;

    [Header("Efeito Visual de Flutuação (Sine Wave)")]
    [SerializeField] private float hoverFrequency = 2f;
    [SerializeField] private float hoverAmountX = 0.25f;
    [SerializeField] private float hoverAmountY = 0.4f;

    [Header("Comportamento de Perseguição")]
    [Range(0f, 1f)]
    [SerializeField] private float chasePlayerChance = 0.5f;

    private Vector2 currentBaseTarget;
    private float hoverTimer;
    private Rigidbody2D rb;

    [Header("Configurações de Vida do Boss")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Controle do Dash Queen Bee / Stagger")]
    [SerializeField] private int normalAttacksBeforeDash = 5;
    private int normalAttackCounter = 0;

    [Header("Sistema de Stagger (Tonto / Vulnerável)")]
    [SerializeField] private bool isStaggered = false;
    public bool IsStaggered => isStaggered;
    [SerializeField] private float staggerDuration = 3.5f;
    [SerializeField] private float staggerKnockbackForce = 1.5f;
    [SerializeField] private float staggerKnockbackDuration = 0.35f;
    [SerializeField] private float staggerKnockbackHeight = 0.5f;
    [SerializeField] private float staggerWobbleAngle = 6f;
    [SerializeField] private float staggerWobbleSpeed = 10f;

    [Header("Debug / Teste de Stagger")]
    [SerializeField] private bool forceStaggerTest = false;

    [Header("Configurações de Pausa dos Ataques (Escala com HP)")]
    [SerializeField] private float maxTimeBetweenAttacks = 3.5f;
    [SerializeField] private float minTimeBetweenAttacks = 1.0f;

    [SerializeField] private float arcAttackDuration = 1.2f;
    [SerializeField] private float burstAttackDuration = 1.5f;
    [SerializeField] private float circleAttackDuration = 1.5f;
    private bool isActing = false;
    public bool IsActing => isActing;

    [Header("Ataque 1: Arco de Projéteis")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int minArcProjectileCount = 5;
    [SerializeField] private int maxArcProjectileCount = 9;
    [SerializeField] private float arcAngle = 60f;

    [Header("Ataque 2: Disparo Sequencial (Burst)")]
    [SerializeField] private int minBurstShotCount = 4;
    [SerializeField] private int maxBurstShotCount = 8;
    [SerializeField] private float timeBetweenBurstShots = 0.25f;

    [Header("Ataque 3: Círculo Completo (Bullet Hell)")]
    [SerializeField] private int minCircleProjectileCount = 12;
    [SerializeField] private int maxCircleProjectileCount = 24;
    [SerializeField] private float circleProjectileSpeed = 7f;

    [Header("Ataque 4: Queen Bee Dash")]
    [SerializeField] private float minQueenBeeDashSpeed = 14f;
    [SerializeField] private float maxQueenBeeDashSpeed = 24f;

    [Header("Referências Visuais")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BossEffects bossEffects;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        if (bossEffects == null)
            bossEffects = GetComponent<BossEffects>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (flightAreaCollider != null)
        {
            ChooseNextFlightTarget();
        }

        StartCoroutine(BossBehaviorLoop());
    }

    private void Update()
    {
        if (forceStaggerTest && !isStaggered)
        {
            forceStaggerTest = false;
            StartCoroutine(StaggerRoutine());
        }
    }

    private void FixedUpdate()
    {
        hoverTimer += Time.fixedDeltaTime;

        if (isStaggered)
        {
            float wobble = Mathf.Sin(Time.time * staggerWobbleSpeed) * staggerWobbleAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, wobble);
            return;
        }

        if (!isActing)
        {
            FlyInArea();
        }
        else
        {
            ApplyHoverOnly();
        }
    }

    public void OnBombBounce()
    {
        if (bossEffects != null)
            bossEffects.TriggerImpact();
    }

    private Vector2 GetCurrentHoverOffset(float freqMultiplier = 1f, float amountMultiplier = 1f)
    {
        float freq = hoverFrequency * freqMultiplier;
        float offsetX = Mathf.Sin(hoverTimer * freq * 0.8f) * (hoverAmountX * amountMultiplier);
        float offsetY = Mathf.Cos(hoverTimer * freq) * (hoverAmountY * amountMultiplier);
        return new Vector2(offsetX, offsetY);
    }

    private void FlyInArea()
    {
        if (flightAreaCollider == null) return;

        Bounds bounds = flightAreaCollider.bounds;

        float healthRatio = (float)currentHealth / maxHealth;
        float rageFactor = 1f - healthRatio;

        float currentSpeed = Mathf.Lerp(minFlySpeed, maxFlySpeed, rageFactor);
        float currentHoverFreq = Mathf.Lerp(1f, 1.8f, rageFactor);

        Vector2 hoverOffset = GetCurrentHoverOffset(currentHoverFreq, 1f);
        Vector2 targetWithHover = currentBaseTarget + hoverOffset;

        targetWithHover.x = Mathf.Clamp(targetWithHover.x, bounds.min.x, bounds.max.x);
        targetWithHover.y = Mathf.Clamp(targetWithHover.y, bounds.min.y, bounds.max.y);

        Vector2 smoothPos = Vector2.MoveTowards(rb.position, targetWithHover, currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(smoothPos);

        Transform nearestPlayer = GetNearestPlayerTransform();
        if (spriteRenderer != null && nearestPlayer != null)
        {
            if (nearestPlayer.position.x > transform.position.x) spriteRenderer.flipX = false;
            else if (nearestPlayer.position.x < transform.position.x) spriteRenderer.flipX = true;
        }

        moveCooldownTimer -= Time.fixedDeltaTime;
        if (Vector2.Distance(rb.position, currentBaseTarget) < waypointReachDistance && moveCooldownTimer <= 0f)
        {
            ChooseNextFlightTarget();
            moveCooldownTimer = Mathf.Lerp(maxMoveCooldown, minMoveCooldown, rageFactor);
        }
    }

    private void ApplyHoverOnly()
    {
        Vector2 hoverOffset = GetCurrentHoverOffset(1f, 0.5f);
        Vector2 smoothPos = Vector2.Lerp(rb.position, rb.position + hoverOffset, Time.fixedDeltaTime * 2f);
        rb.MovePosition(smoothPos);
    }

    private void ChooseNextFlightTarget()
    {
        if (flightAreaCollider == null) return;

        Bounds bounds = flightAreaCollider.bounds;

        System.Collections.Generic.List<PlayerMovement> alivePlayers = GetAlivePlayersList();

        float healthRatio = (float)currentHealth / maxHealth;
        float adjustedChaseChance = Mathf.Lerp(chasePlayerChance * 1.5f, chasePlayerChance, healthRatio);

        bool shouldChasePlayer = (alivePlayers.Count > 0) && (Random.value < adjustedChaseChance);

        if (shouldChasePlayer)
        {
            PlayerMovement targetPlayer = GetNearestPlayer();
            if (targetPlayer == null) targetPlayer = alivePlayers[Random.Range(0, alivePlayers.Count)];

            Vector2 playerPos = targetPlayer.transform.position;

            float offsetHeadY = Random.Range(1.5f, 3.5f);
            float offsetHeadX = Random.Range(-2f, 2f);
            Vector2 desiredPos = playerPos + new Vector2(offsetHeadX, offsetHeadY);

            float clampedX = Mathf.Clamp(desiredPos.x, bounds.min.x, bounds.max.x);
            float clampedY = Mathf.Clamp(desiredPos.y, bounds.min.y, bounds.max.y);

            currentBaseTarget = new Vector2(clampedX, clampedY);
        }
        else
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            currentBaseTarget = new Vector2(randomX, randomY);
        }
    }

    private System.Collections.Generic.List<PlayerMovement> GetAlivePlayersList()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        System.Collections.Generic.List<PlayerMovement> aliveList = new System.Collections.Generic.List<PlayerMovement>();

        foreach (PlayerMovement p in players)
        {
            if (p != null && !p.IsDead) aliveList.Add(p);
        }
        return aliveList;
    }

    private PlayerMovement GetNearestPlayer()
    {
        System.Collections.Generic.List<PlayerMovement> alivePlayers = GetAlivePlayersList();
        if (alivePlayers.Count == 0) return null;

        PlayerMovement nearest = alivePlayers[0];
        float minDst = Vector2.Distance(transform.position, nearest.transform.position);

        for (int i = 1; i < alivePlayers.Count; i++)
        {
            float dst = Vector2.Distance(transform.position, alivePlayers[i].transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                nearest = alivePlayers[i];
            }
        }
        return nearest;
    }

    private Transform GetNearestPlayerTransform()
    {
        PlayerMovement nearest = GetNearestPlayer();
        return nearest != null ? nearest.transform : null;
    }

    public void TakeDamage(int damageAmount)
    {
        if (!isStaggered)
        {
            Debug.Log("O Boss está protegido! Aguarde o Stagger para causar dano.");
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);
        Debug.Log("DANO CONFIRMADO NO STAGGER! Vida restante do Boss: " + currentHealth);

        if (bossEffects != null)
            bossEffects.TriggerDamage();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("Boss derrotado!");
        Destroy(gameObject);
    }

    private IEnumerator BossBehaviorLoop()
    {
        yield return new WaitForSeconds(2f);
        while (true)
        {
            if (isStaggered)
            {
                yield return null;
                continue;
            }

            isActing = true;

            if (normalAttackCounter >= normalAttacksBeforeDash)
            {
                normalAttackCounter = 0;
                yield return StartCoroutine(QueenBeeDashAttack());
            }
            else
            {
                int chosenAttack = Random.Range(0, 3);

                if (bossEffects != null)
                    bossEffects.TriggerAttack();

                if (chosenAttack == 0)
                {
                    yield return StartCoroutine(ArcProjectileAttack());
                }
                else if (chosenAttack == 1)
                {
                    yield return StartCoroutine(TargetedBurstAttack());
                }
                else
                {
                    yield return StartCoroutine(CircleRingAttack());
                }

                normalAttackCounter++;
            }

            isActing = false;
            ChooseNextFlightTarget();

            float healthRatio = (float)currentHealth / maxHealth;
            float rageFactor = 1f - healthRatio;
            float currentAttackCooldown = Mathf.Lerp(maxTimeBetweenAttacks, minTimeBetweenAttacks, rageFactor);

            yield return new WaitForSeconds(currentAttackCooldown);
        }
    }

    private IEnumerator ArcProjectileAttack()
    {
        yield return new WaitForSeconds(0.2f);
        if (projectilePrefab != null && firePoint != null)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            float rageFactor = 1f - healthRatio;
            int count = Mathf.RoundToInt(Mathf.Lerp(minArcProjectileCount, maxArcProjectileCount, rageFactor));

            Transform nearestPlayer = GetNearestPlayerTransform();
            Vector2 baseDir = nearestPlayer != null ? ((Vector2)(nearestPlayer.position - firePoint.position)).normalized : (spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right);

            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float startAngle = -arcAngle / 2f;
            float angleStep = count > 1 ? arcAngle / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float currentAngleOffset = startAngle + angleStep * i;
                float finalAngle = baseAngle + currentAngleOffset;

                Quaternion rotation = Quaternion.Euler(0, 0, finalAngle);
                Vector2 direction = rotation * Vector2.right;

                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null) rbProj.linearVelocity = direction * 10f;
            }
        }
        yield return new WaitForSeconds(arcAttackDuration);
    }

    private IEnumerator TargetedBurstAttack()
    {
        yield return new WaitForSeconds(0.2f);

        float healthRatio = (float)currentHealth / maxHealth;
        float rageFactor = 1f - healthRatio;
        int burstCount = Mathf.RoundToInt(Mathf.Lerp(minBurstShotCount, maxBurstShotCount, rageFactor));

        for (int i = 0; i < burstCount; i++)
        {
            Transform nearestPlayer = GetNearestPlayerTransform();
            if (nearestPlayer != null && firePoint != null && projectilePrefab != null)
            {
                Vector2 dirToPlayer = (nearestPlayer.position - firePoint.position).normalized;
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null) rbProj.linearVelocity = dirToPlayer * 12f;
            }
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
        yield return new WaitForSeconds(burstAttackDuration);
    }

    private IEnumerator CircleRingAttack()
    {
        yield return new WaitForSeconds(0.2f);
        if (projectilePrefab != null && firePoint != null)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            float rageFactor = 1f - healthRatio;
            int circleCount = Mathf.RoundToInt(Mathf.Lerp(minCircleProjectileCount, maxCircleProjectileCount, rageFactor));

            float angleStep = 360f / circleCount;

            for (int i = 0; i < circleCount; i++)
            {
                float angle = i * angleStep;
                Quaternion rotation = Quaternion.Euler(0, 0, angle);
                Vector2 direction = rotation * Vector2.right;

                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null)
                {
                    rbProj.linearVelocity = direction * circleProjectileSpeed;
                }
            }
        }
        yield return new WaitForSeconds(circleAttackDuration);
    }

    private IEnumerator QueenBeeDashAttack()
    {
        if (flightAreaCollider == null) yield break;

        Bounds bounds = flightAreaCollider.bounds;
        float padding = 1.0f;

        float[] yHeights = new float[] {
            bounds.min.y + 3.0f,
            bounds.min.y + 2.0f,
            bounds.min.y + 1.0f
        };

        bool startFromLeft = true;

        float healthRatio = (float)currentHealth / maxHealth;
        float rageFactor = 1f - healthRatio;
        float currentDashSpeed = Mathf.Lerp(minQueenBeeDashSpeed, maxQueenBeeDashSpeed, rageFactor);

        for (int i = 0; i < yHeights.Length; i++)
        {
            float currentY = yHeights[i];

            float startX = startFromLeft ? bounds.min.x + padding : bounds.max.x - padding;
            float targetX = startFromLeft ? bounds.max.x - padding : bounds.min.x + padding;

            Vector2 startPos = new Vector2(startX, currentY);
            Vector2 targetPos = new Vector2(targetX, currentY);

            while (Vector2.Distance(transform.position, startPos) > 0.1f)
            {
                Vector3 nextPos = Vector3.MoveTowards(transform.position, startPos, maxFlySpeed * Time.deltaTime);
                Vector2 hover = GetCurrentHoverOffset(1f, 0.4f);
                transform.position = nextPos + (Vector3)hover * Time.deltaTime;

                if (spriteRenderer != null)
                    spriteRenderer.flipX = (startPos.x > transform.position.x);

                yield return null;
            }
            transform.position = startPos;

            if (spriteRenderer != null)
                spriteRenderer.flipX = !startFromLeft;

            if (bossEffects != null)
                bossEffects.TriggerDash();

            yield return new WaitForSeconds(0.15f);

            while (Vector2.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, currentDashSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;

            startFromLeft = !startFromLeft;
        }

        if (bossEffects != null)
            bossEffects.TriggerImpact();

        Vector2 knockbackDir = startFromLeft ? Vector2.right : Vector2.left;
        Vector3 initialPos = transform.position;
        Vector3 targetKnockbackPos = initialPos + (Vector3)(knockbackDir * staggerKnockbackForce);

        targetKnockbackPos.x = Mathf.Clamp(targetKnockbackPos.x, bounds.min.x + padding, bounds.max.x - padding);
        targetKnockbackPos.y = Mathf.Clamp(targetKnockbackPos.y, bounds.min.y + padding, bounds.max.y - padding);

        float knockbackElapsed = 0f;
        while (knockbackElapsed < staggerKnockbackDuration)
        {
            knockbackElapsed += Time.deltaTime;
            float t = knockbackElapsed / staggerKnockbackDuration;

            Vector3 currentPos = Vector3.Lerp(initialPos, targetKnockbackPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * staggerKnockbackHeight;

            transform.position = currentPos;
            yield return null;
        }
        transform.position = targetKnockbackPos;

        yield return StartCoroutine(StaggerRoutine());
    }

    private IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        Debug.Log("BOSS FICOU TONTO! (STAGGER ATIVO - ATAQUE A BOMBA AGORA)");

        yield return new WaitForSeconds(staggerDuration);

        isStaggered = false;
        transform.rotation = Quaternion.identity;
        Debug.Log("Boss se recuperou do Stagger!");
    }

    private void OnDrawGizmosSelected()
    {
        if (flightAreaCollider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(flightAreaCollider.bounds.center, flightAreaCollider.bounds.size);
        }
    }
}