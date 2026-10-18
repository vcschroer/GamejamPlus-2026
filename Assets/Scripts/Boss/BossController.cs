using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    public enum BossAttackType
    {
        ArcProjectile = 0,
        TargetedBurst = 1,
        CircleRing = 2,
        ArcWave = 3,
        CircleWave = 4
    }

    public enum BossStaggerAttackType
    {
        QueenBeeDash = 0,
        FivePointJump = 1
    }

    [Header("Introdução / Início do Jogo")]
    [SerializeField] private float introDelay = 2f;
    [SerializeField] private Vector2 startIntroPosition = new Vector2(0f, 10f);
    [SerializeField] private Vector2 endIntroPosition = new Vector2(0f, 5f);
    [SerializeField] private float introMoveDuration = 1.5f;
    [SerializeField] private float introShakeDuration = 0.4f;
    [SerializeField] private float introShakeIntensity = 0.2f;

    private bool isIntroFinished = false;
    public bool IsIntroFinished => isIntroFinished;

    [Header("Estágios de Ataques Normais por Vida")]
    [SerializeField]
    private List<BossAttackType> attacksStage3Of3 = new List<BossAttackType>
    {
        BossAttackType.ArcProjectile,
        BossAttackType.TargetedBurst,
        BossAttackType.CircleRing,
        BossAttackType.ArcWave,
        BossAttackType.CircleWave
    };

    [SerializeField]
    private List<BossAttackType> attacksStage2Of3 = new List<BossAttackType>
    {
        BossAttackType.ArcProjectile,
        BossAttackType.TargetedBurst,
        BossAttackType.CircleRing,
        BossAttackType.ArcWave,
        BossAttackType.CircleWave
    };

    [SerializeField]
    private List<BossAttackType> attacksStage1Of3 = new List<BossAttackType>
    {
        BossAttackType.ArcProjectile,
        BossAttackType.TargetedBurst,
        BossAttackType.CircleRing,
        BossAttackType.ArcWave,
        BossAttackType.CircleWave
    };

    [Header("Estágios de Ataques de Stagger por Vida")]
    [SerializeField]
    private List<BossStaggerAttackType> staggerAttacksStage3Of3 = new List<BossStaggerAttackType>
    {
        BossStaggerAttackType.QueenBeeDash,
        BossStaggerAttackType.FivePointJump
    };

    [SerializeField]
    private List<BossStaggerAttackType> staggerAttacksStage2Of3 = new List<BossStaggerAttackType>
    {
        BossStaggerAttackType.QueenBeeDash,
        BossStaggerAttackType.FivePointJump
    };

    [SerializeField]
    private List<BossStaggerAttackType> staggerAttacksStage1Of3 = new List<BossStaggerAttackType>
    {
        BossStaggerAttackType.QueenBeeDash,
        BossStaggerAttackType.FivePointJump
    };

    [Header("Movimentação & Área de Voo")]
    [SerializeField] private Collider2D flightAreaCollider;
    [SerializeField] private float minFlySpeed = 3f;
    [SerializeField] private float maxFlySpeed = 8f;
    [SerializeField] private float waypointReachDistance = 0.8f;

    [Header("Referência do Chão")]
    [SerializeField] private Transform groundPoint;

    [SerializeField] private LayerMask groundLayers = ~0;

    [SerializeField] private string groundTag = "Ground";

    [SerializeField] private float groundRaycastDistance = 100f;

    [SerializeField] private float groundImpactHeight = 0.8f;

    [Header("Cooldown de Movimento")]
    [SerializeField] private float maxMoveCooldown = 2f;
    [SerializeField] private float minMoveCooldown = 0.4f;
    private float moveCooldownTimer;

    [Header("Efeito Visual de Flutuação")]
    [SerializeField] private float hoverFrequency = 2f;
    [SerializeField] private float hoverAmountX = 0.25f;
    [SerializeField] private float hoverAmountY = 0.4f;

    [Header("Comportamento de Perseguição")]
    [Range(0f, 1f)]
    [SerializeField] private float chasePlayerChance = 0.5f;

    private Vector2 currentBaseTarget;
    private float hoverTimer;
    private Rigidbody2D rb;

    [Header("Configurações de Vida")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Controle dos Ataques de Stagger")]
    [SerializeField] private int normalAttacksBeforeDash = 5;
    private int normalAttackCounter;

    [Header("Sistema de Stagger")]
    [SerializeField] private bool isStaggered;
    public bool IsStaggered => isStaggered;

    [SerializeField] private float staggerChargeDuration = 1.2f;

    [SerializeField] private float staggerChargeShakeIntensity = 0.15f;

    [SerializeField] private float staggerDuration = 3.5f;
    [SerializeField] private float staggerKnockbackForce = 1.5f;
    [SerializeField] private float staggerKnockbackDuration = 0.35f;
    [SerializeField] private float staggerKnockbackHeight = 0.5f;
    [SerializeField] private float staggerWobbleAngle = 6f;
    [SerializeField] private float staggerWobbleSpeed = 10f;

    [Header("Debug / Teste de Stagger")]
    [SerializeField] private bool forceStaggerTest;

    [Header("Intervalo Entre Ataques")]
    [SerializeField] private float maxTimeBetweenAttacks = 3.5f;
    [SerializeField] private float minTimeBetweenAttacks = 1f;
    [SerializeField] private float arcAttackDuration = 1.2f;
    [SerializeField] private float burstAttackDuration = 1.5f;
    [SerializeField] private float circleAttackDuration = 1.5f;

    private bool isActing;
    public bool IsActing => isActing;

    [Header("Referências dos Projéteis")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Ataque 1: Arco")]
    [SerializeField] private int minArcProjectileCount = 5;
    [SerializeField] private int maxArcProjectileCount = 9;
    [SerializeField] private float arcAngle = 60f;
    [SerializeField] private float arcProjectileSpeed = 10f;

    [Header("Ataque 2: Disparo Sequencial")]
    [SerializeField] private int minBurstShotCount = 4;
    [SerializeField] private int maxBurstShotCount = 8;
    [SerializeField] private float timeBetweenBurstShots = 0.25f;
    [SerializeField] private float burstProjectileSpeed = 12f;

    [Header("Ataque 3: Círculo Completo")]
    [SerializeField] private int minCircleProjectileCount = 12;
    [SerializeField] private int maxCircleProjectileCount = 24;
    [SerializeField] private float circleProjectileSpeed = 7f;

    [Header("Ataque 4: Arco em Waves")]
    [SerializeField] private int arcWaveCount = 3;
    [SerializeField] private float arcWaveInterval = 0.45f;
    [SerializeField] private float arcWaveAngleOffset = 8f;

    [Header("Ataque 5: Círculo em Waves")]
    [SerializeField] private int circleWaveCount = 3;
    [SerializeField] private float circleWaveInterval = 0.45f;
    [SerializeField] private float circleWaveRotationOffset = 10f;

    [Header("Ataque de Stagger 1: Queen Bee Dash")]
    [SerializeField] private float minQueenBeeDashSpeed = 14f;
    [SerializeField] private float maxQueenBeeDashSpeed = 24f;

    [Header("Ataque de Stagger 2: Saltos e Mergulho")]
    [SerializeField] private int jumpPointCount = 5;

    [SerializeField] private float jumpDuration = 0.45f;

    [SerializeField] private float jumpHeight = 2.5f;

    [SerializeField] private float jumpPause = 0.12f;

    [SerializeField] private float finalDivePreparationHeight = 2.5f;

    [SerializeField] private float finalDivePreparationDuration = 0.3f;

    [SerializeField] private float finalDiveDuration = 0.18f;

    [SerializeField] private float groundImpactShakeDuration = 0.35f;

    [Header("Referências Visuais & Partículas de Dano")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BossEffects bossEffects;
    [SerializeField] private ParticleSystem damageParticleSystemPrefab; 

    [Header("Piscar Conforme a Vida")]
    [SerializeField] private Color lowHealthBlinkColor = Color.red;
    [SerializeField, Min(0.01f)] private float mediumHealthBlinkInterval = 0.65f;
    [SerializeField, Min(0.01f)] private float lowHealthBlinkInterval = 0.15f;

    [Header("Inclinação Visual e Cordas")]
    [SerializeField] private PuppetStringsController puppetStrings;
    [SerializeField] private float leftLeanAngle = 8f;
    [SerializeField] private float staggerWobbleMultiplier = 1f;

    private Coroutine behaviorCoroutine;
    private Coroutine healthBlinkCoroutine;
    private Color originalSpriteColor = Color.white;
    private Transform spriteVisualTransform;
    private Quaternion spriteBaseLocalRotation = Quaternion.identity;
    private float currentLeanAngle;
    private int lastHealthForVisuals = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        if (bossEffects == null)
            bossEffects = GetComponent<BossEffects>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
            spriteVisualTransform = spriteRenderer.transform;
            spriteBaseLocalRotation = spriteVisualTransform.localRotation;
        }

        if (puppetStrings == null)
            puppetStrings = GetComponent<PuppetStringsController>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isIntroFinished = false;
        UpdateHealthVisuals(forceUpdate: true);

        transform.position = startIntroPosition;

        StartCoroutine(BossIntroRoutine());
    }

    private IEnumerator BossIntroRoutine()
    {
        yield return new WaitForSeconds(introDelay);

        float elapsed = 0f;
        while (elapsed < introMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / introMoveDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector2.Lerp(startIntroPosition, endIntroPosition, smoothT);
            yield return null;
        }
        transform.position = endIntroPosition;

        if (bossEffects != null)
        {
            bossEffects.TriggerImpact();
        }

        if (GameEffectsManager.Instance != null)
        {
            GameEffectsManager.Instance.TriggerCameraShake(introShakeDuration, introShakeIntensity);
        }

        Vector3 originalPos = transform.position;
        float shakeElapsed = 0f;
        while (shakeElapsed < introShakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            Vector2 shakeOffset = Random.insideUnitCircle * introShakeIntensity;
            transform.position = (Vector2)originalPos + shakeOffset;
            yield return null;
        }
        transform.position = originalPos;

        isIntroFinished = true;

        if (flightAreaCollider != null)
            ChooseNextFlightTarget();

        behaviorCoroutine = StartCoroutine(BossBehaviorLoop());
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
        if (!isIntroFinished)
            return;

        hoverTimer += Time.fixedDeltaTime;

        UpdateSpriteRotation();

        if (isStaggered)
            return;

        if (!isActing)
            FlyInArea();
        else
            ApplyHoverOnly();
    }

    public void OnBombBounce()
    {
        if (bossEffects != null)
            bossEffects.TriggerImpact();
    }

    private void FlyInArea()
    {
        if (flightAreaCollider == null || rb == null)
            return;

        Bounds bounds = flightAreaCollider.bounds;
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        float rageFactor = 1f - healthRatio;

        float currentSpeed = Mathf.Lerp(minFlySpeed, maxFlySpeed, rageFactor);
        float currentHoverFreq = Mathf.Lerp(1f, 1.8f, rageFactor);

        Vector2 hoverOffset = GetCurrentHoverOffset(currentHoverFreq, 1f);
        Vector2 targetWithHover = currentBaseTarget + hoverOffset;

        targetWithHover.x = Mathf.Clamp(targetWithHover.x, bounds.min.x, bounds.max.x);
        targetWithHover.y = Mathf.Clamp(targetWithHover.y, bounds.min.y, bounds.max.y);

        Vector2 smoothPos = Vector2.Lerp(rb.position, targetWithHover, currentSpeed * Time.fixedDeltaTime * 2.5f);
        rb.MovePosition(smoothPos);

        Transform nearestPlayer = GetNearestPlayerTransform();
        if (spriteRenderer != null && nearestPlayer != null)
        {
            spriteRenderer.flipX = nearestPlayer.position.x < transform.position.x;
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
        if (rb == null) return;

        Vector2 hoverOffset = GetCurrentHoverOffset(1f, 0.5f);
        Vector2 smoothPos = Vector2.Lerp(rb.position, rb.position + hoverOffset, Time.fixedDeltaTime * 2f);
        rb.MovePosition(smoothPos);
    }

    private Vector2 GetCurrentHoverOffset(float freqMultiplier = 1f, float amountMultiplier = 1f)
    {
        float freq = hoverFrequency * freqMultiplier;
        float offsetX = Mathf.Sin(hoverTimer * freq * 0.8f) * hoverAmountX * amountMultiplier;
        float offsetY = Mathf.Cos(hoverTimer * freq) * hoverAmountY * amountMultiplier;
        return new Vector2(offsetX, offsetY);
    }

    private void ChooseNextFlightTarget()
    {
        if (flightAreaCollider == null)
            return;

        Bounds bounds = flightAreaCollider.bounds;
        List<PlayerMovement> alivePlayers = GetAlivePlayersList();

        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        float adjustedChaseChance = Mathf.Lerp(chasePlayerChance * 1.5f, chasePlayerChance, healthRatio);

        bool shouldChasePlayer = alivePlayers.Count > 0 && Random.value < adjustedChaseChance;

        if (shouldChasePlayer)
        {
            PlayerMovement targetPlayer = GetNearestPlayer();
            if (targetPlayer == null)
                targetPlayer = alivePlayers[Random.Range(0, alivePlayers.Count)];

            Vector2 playerPos = targetPlayer.transform.position;
            Vector2 desiredPos = playerPos + new Vector2(Random.Range(-2f, 2f), Random.Range(1.5f, 3.5f));

            currentBaseTarget = new Vector2(
                Mathf.Clamp(desiredPos.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(desiredPos.y, bounds.min.y, bounds.max.y));
        }
        else
        {
            currentBaseTarget = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y));
        }
    }

    private List<PlayerMovement> GetAlivePlayersList()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        List<PlayerMovement> aliveList = new List<PlayerMovement>();

        foreach (PlayerMovement p in players)
        {
            if (p != null && !p.IsDead)
                aliveList.Add(p);
        }
        return aliveList;
    }

    private PlayerMovement GetNearestPlayer()
    {
        List<PlayerMovement> alivePlayers = GetAlivePlayersList();
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

        if (bossEffects != null)
            bossEffects.TriggerDamage();

        SpawnDamageParticleSystemRandomlyOnSprite();

        UpdateHealthVisuals();

        if (currentHealth <= 0)
            Die();
    }

    private void SpawnDamageParticleSystemRandomlyOnSprite()
    {
        if (damageParticleSystemPrefab != null)
        {
            Vector3 spawnPos = transform.position;

            if (spriteRenderer != null)
            {
                Bounds spriteBounds = spriteRenderer.bounds;

                float randomX = Random.Range(spriteBounds.min.x, spriteBounds.max.x);
                float randomY = Random.Range(spriteBounds.min.y, spriteBounds.max.y);

                spawnPos = new Vector3(randomX, randomY, transform.position.z);
            }

            ParticleSystem ps = Instantiate(damageParticleSystemPrefab, spawnPos, Quaternion.identity);
            ps.Play();

            float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(ps.gameObject, totalDuration);
        }
    }

    private void UpdateHealthVisuals(bool forceUpdate = false)
    {
        int safeMaxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, safeMaxHealth);

        if (puppetStrings != null)
        {
            puppetStrings.SetHealth(currentHealth, safeMaxHealth);
            currentLeanAngle = puppetStrings.LeftStringLost ? leftLeanAngle : 0f;
        }
        else
        {
            currentLeanAngle = 0f;
        }

        if (!forceUpdate && lastHealthForVisuals == currentHealth)
            return;

        lastHealthForVisuals = currentHealth;

        if (healthBlinkCoroutine != null)
        {
            StopCoroutine(healthBlinkCoroutine);
            healthBlinkCoroutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        if (currentHealth > 0 && currentHealth < safeMaxHealth)
            healthBlinkCoroutine = StartCoroutine(HealthBlinkRoutine());
    }

    private IEnumerator HealthBlinkRoutine()
    {
        float ratio = (float)currentHealth / Mathf.Max(1, maxHealth);
        float interval = ratio > (1f / 3f) ? mediumHealthBlinkInterval : lowHealthBlinkInterval;
        bool useBlinkColor = false;

        while (currentHealth > 0)
        {
            if (spriteRenderer != null)
            {
                useBlinkColor = !useBlinkColor;
                spriteRenderer.color = useBlinkColor ? lowHealthBlinkColor : originalSpriteColor;
            }
            yield return new WaitForSeconds(interval);
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        healthBlinkCoroutine = null;
    }

    private void UpdateSpriteRotation()
    {
        if (spriteVisualTransform == null || spriteVisualTransform == transform)
            return;

        float staggerWobble = isStaggered
            ? Mathf.Sin(Time.time * staggerWobbleSpeed) * staggerWobbleAngle * staggerWobbleMultiplier
            : 0f;

        spriteVisualTransform.localRotation = spriteBaseLocalRotation * Quaternion.Euler(0f, 0f, currentLeanAngle + staggerWobble);
    }

    private void Die()
    {
        if (healthBlinkCoroutine != null)
        {
            StopCoroutine(healthBlinkCoroutine);
            healthBlinkCoroutine = null;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rbBody = GetComponent<Rigidbody2D>();
        if (rbBody != null) rbBody.simulated = false;

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        StopAllCoroutines();
        Destroy(gameObject);
    }

    private List<BossAttackType> GetCurrentStageAttacks()
    {
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        if (healthRatio > 0.66f) return attacksStage3Of3;
        else if (healthRatio > 0.33f) return attacksStage2Of3;
        else return attacksStage1Of3;
    }

    private List<BossStaggerAttackType> GetCurrentStageStaggerAttacks()
    {
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        if (healthRatio > 0.66f) return staggerAttacksStage3Of3;
        else if (healthRatio > 0.33f) return staggerAttacksStage2Of3;
        else return staggerAttacksStage1Of3;
    }

    private IEnumerator BossBehaviorLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (isStaggered)
            {
                isActing = false;
                yield return null;
                continue;
            }

            isActing = true;

            if (normalAttackCounter >= normalAttacksBeforeDash)
            {
                normalAttackCounter = 0;
                yield return StartCoroutine(StaggerChargeRoutine());

                List<BossStaggerAttackType> availableStaggerAttacks = GetCurrentStageStaggerAttacks();
                if (availableStaggerAttacks == null || availableStaggerAttacks.Count == 0)
                    availableStaggerAttacks = new List<BossStaggerAttackType> { BossStaggerAttackType.QueenBeeDash };

                BossStaggerAttackType chosenStaggerAttack = availableStaggerAttacks[Random.Range(0, availableStaggerAttacks.Count)];

                if (bossEffects != null)
                    bossEffects.TriggerAttack();

                if (chosenStaggerAttack == BossStaggerAttackType.QueenBeeDash)
                    yield return StartCoroutine(QueenBeeDashAttack());
                else
                    yield return StartCoroutine(FivePointJumpAttack());
            }
            else
            {
                List<BossAttackType> availableAttacks = GetCurrentStageAttacks();
                if (availableAttacks == null || availableAttacks.Count == 0)
                    availableAttacks = new List<BossAttackType> { BossAttackType.ArcProjectile };

                BossAttackType chosenAttack = availableAttacks[Random.Range(0, availableAttacks.Count)];

                if (bossEffects != null)
                    bossEffects.TriggerAttack();

                switch (chosenAttack)
                {
                    case BossAttackType.ArcProjectile: yield return StartCoroutine(ArcProjectileAttack()); break;
                    case BossAttackType.TargetedBurst: yield return StartCoroutine(TargetedBurstAttack()); break;
                    case BossAttackType.CircleRing: yield return StartCoroutine(CircleRingAttack()); break;
                    case BossAttackType.ArcWave: yield return StartCoroutine(ArcWaveAttack()); break;
                    case BossAttackType.CircleWave: yield return StartCoroutine(CircleWaveAttack()); break;
                }

                normalAttackCounter++;
            }

            isActing = false;

            if (currentHealth <= 0)
                yield break;

            if (isStaggered)
            {
                yield return new WaitUntil(() => !isStaggered || currentHealth <= 0);
                if (currentHealth <= 0) yield break;
            }

            ChooseNextFlightTarget();

            float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
            float rageFactor = 1f - healthRatio;
            float currentAttackCooldown = Mathf.Lerp(maxTimeBetweenAttacks, minTimeBetweenAttacks, rageFactor);

            yield return new WaitForSeconds(currentAttackCooldown);
        }
    }

    private IEnumerator StaggerChargeRoutine()
    {
        if (staggerChargeDuration <= 0f) yield break;

        if (bossEffects != null)
            bossEffects.TriggerAttack();

        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < staggerChargeDuration)
        {
            if (currentHealth <= 0) yield break;
            elapsed += Time.deltaTime;
            transform.position = (Vector2)originalPos + (Random.insideUnitCircle * staggerChargeShakeIntensity);
            yield return null;
        }
        transform.position = originalPos;
    }

    private IEnumerator ArcProjectileAttack()
    {
        yield return new WaitForSeconds(0.2f);
        FireArcWave(0f);
        yield return new WaitForSeconds(arcAttackDuration);
    }

    private IEnumerator ArcWaveAttack()
    {
        yield return new WaitForSeconds(0.2f);
        int waves = Mathf.Max(1, arcWaveCount);

        for (int wave = 0; wave < waves; wave++)
        {
            if (isStaggered || currentHealth <= 0) yield break;
            float angleOffset = wave * arcWaveAngleOffset - ((waves - 1) * arcWaveAngleOffset * 0.5f);
            FireArcWave(angleOffset);
            if (wave < waves - 1) yield return new WaitForSeconds(arcWaveInterval);
        }
        yield return new WaitForSeconds(arcAttackDuration);
    }

    private void FireArcWave(float extraAngle)
    {
        if (projectilePrefab == null || firePoint == null) return;

        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(minArcProjectileCount, maxArcProjectileCount, 1f - healthRatio)));

        Transform nearestPlayer = GetNearestPlayerTransform();
        Vector2 baseDir = nearestPlayer != null ? ((Vector2)(nearestPlayer.position - firePoint.position)).normalized : Vector2.right;

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float startAngle = -arcAngle / 2f;
        float angleStep = count > 1 ? arcAngle / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float finalAngle = baseAngle + startAngle + angleStep * i + extraAngle;
            Vector2 direction = Quaternion.Euler(0f, 0f, finalAngle) * Vector2.right;

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null) rbProj.linearVelocity = direction * arcProjectileSpeed;
        }
    }

    private IEnumerator TargetedBurstAttack()
    {
        yield return new WaitForSeconds(0.2f);
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        int burstCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(minBurstShotCount, maxBurstShotCount, 1f - healthRatio)));

        for (int i = 0; i < burstCount; i++)
        {
            if (isStaggered || currentHealth <= 0) yield break;
            Transform nearestPlayer = GetNearestPlayerTransform();

            if (nearestPlayer != null && firePoint != null && projectilePrefab != null)
            {
                Vector2 dirToPlayer = ((Vector2)nearestPlayer.position - (Vector2)firePoint.position).normalized;
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null) rbProj.linearVelocity = dirToPlayer * burstProjectileSpeed;
            }
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
        yield return new WaitForSeconds(burstAttackDuration);
    }

    private IEnumerator CircleRingAttack()
    {
        yield return new WaitForSeconds(0.2f);
        FireCircleWave(0f);
        yield return new WaitForSeconds(circleAttackDuration);
    }

    private IEnumerator CircleWaveAttack()
    {
        yield return new WaitForSeconds(0.2f);
        int waves = Mathf.Max(1, circleWaveCount);

        for (int wave = 0; wave < waves; wave++)
        {
            if (isStaggered || currentHealth <= 0) yield break;
            FireCircleWave(wave * circleWaveRotationOffset);
            if (wave < waves - 1) yield return new WaitForSeconds(circleWaveInterval);
        }
        yield return new WaitForSeconds(circleAttackDuration);
    }

    private void FireCircleWave(float rotationOffset)
    {
        if (projectilePrefab == null || firePoint == null) return;
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        int count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(minCircleProjectileCount, maxCircleProjectileCount, 1f - healthRatio)));
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep + rotationOffset;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
            if (rbProj != null) rbProj.linearVelocity = direction * circleProjectileSpeed;
        }
    }

    private IEnumerator QueenBeeDashAttack()
    {
        if (flightAreaCollider == null) yield break;
        Bounds bounds = flightAreaCollider.bounds;
        float padding = 1f;

        float[] yHeights = { bounds.min.y + 3f, bounds.min.y + 2f, bounds.min.y + 1f };
        bool startFromLeft = true;
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        float baseDashSpeed = Mathf.Lerp(minQueenBeeDashSpeed, maxQueenBeeDashSpeed, 1f - healthRatio);

        for (int i = 0; i < yHeights.Length; i++)
        {
            if (isStaggered || currentHealth <= 0) yield break;

            float currentY = Mathf.Clamp(yHeights[i], bounds.min.y + padding, bounds.max.y - padding);
            float startX = startFromLeft ? bounds.min.x + padding : bounds.max.x - padding;
            float targetX = startFromLeft ? bounds.max.x - padding : bounds.min.x + padding;

            yield return MoveSmoothlyToPosition(new Vector2(startX, currentY), 0.4f);

            if (spriteRenderer != null) spriteRenderer.flipX = !startFromLeft;
            if (bossEffects != null) bossEffects.TriggerDash();
            yield return new WaitForSeconds(0.12f);

            float currentDashSpeed = Mathf.Lerp(baseDashSpeed, maxQueenBeeDashSpeed, (float)i / (yHeights.Length - 1));
            yield return MoveSmoothlyToPositionWithSpeed(new Vector2(targetX, currentY), currentDashSpeed);

            startFromLeft = !startFromLeft;
        }

        if (bossEffects != null) bossEffects.TriggerImpact();

        Vector2 knockbackDir = startFromLeft ? Vector2.right : Vector2.left;
        Vector3 initialPos = transform.position;
        Vector3 targetKnockbackPos = initialPos + (Vector3)(knockbackDir * staggerKnockbackForce);

        targetKnockbackPos.x = Mathf.Clamp(targetKnockbackPos.x, bounds.min.x + padding, bounds.max.x - padding);
        targetKnockbackPos.y = Mathf.Clamp(targetKnockbackPos.y, bounds.min.y + padding, bounds.max.y - padding);

        float elapsed = 0f;
        while (elapsed < staggerKnockbackDuration)
        {
            if (currentHealth <= 0) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, staggerKnockbackDuration));
            Vector3 pos = Vector3.Lerp(initialPos, targetKnockbackPos, Mathf.SmoothStep(0f, 1f, t));
            pos.y += Mathf.Sin(t * Mathf.PI) * staggerKnockbackHeight;
            transform.position = pos;
            yield return null;
        }
        transform.position = targetKnockbackPos;
        yield return StartCoroutine(StaggerRoutine());
    }

    private IEnumerator FivePointJumpAttack()
    {
        int totalPoints = Mathf.Max(1, jumpPointCount);
        yield return new WaitForSeconds(0.35f);

        for (int i = 0; i < totalPoints; i++)
        {
            if (currentHealth <= 0 || isStaggered) yield break;
            bool isFinalDive = i == totalPoints - 1;

            PlayerMovement targetPlayer = GetNearestPlayer();
            float targetX = targetPlayer != null ? targetPlayer.transform.position.x : transform.position.x;

            if (flightAreaCollider != null)
                targetX = Mathf.Clamp(targetX, flightAreaCollider.bounds.min.x + 0.5f, flightAreaCollider.bounds.max.x - 0.5f);

            float landingY = GetGroundLandingY(targetX);
            if (spriteRenderer != null) spriteRenderer.flipX = targetX < transform.position.x;
            if (bossEffects != null) bossEffects.TriggerDash();

            float apexY = landingY + Mathf.Max(1f, isFinalDive ? finalDivePreparationHeight : jumpHeight);
            if (flightAreaCollider != null) apexY = Mathf.Min(apexY, flightAreaCollider.bounds.max.y - 0.5f);

            float progressFactor = (float)i / Mathf.Max(1, totalPoints - 1);
            float ascentDuration = isFinalDive ? finalDivePreparationDuration : Mathf.Lerp(jumpDuration, jumpDuration * 0.45f, progressFactor);

            yield return MoveBossToPosition(new Vector2(targetX, apexY), ascentDuration);
            if (currentHealth <= 0) yield break;

            float diveDuration = isFinalDive ? finalDiveDuration : Mathf.Max(0.12f, Mathf.Lerp(jumpDuration * 0.65f, jumpDuration * 0.3f, progressFactor));
            yield return DiveBossToGround(new Vector2(targetX, landingY), diveDuration);
            if (currentHealth <= 0) yield break;

            if (bossEffects != null) bossEffects.TriggerImpact();

            if (isFinalDive)
            {
                yield return new WaitForSeconds(groundImpactShakeDuration);
                yield return StartCoroutine(StaggerRoutine());
                yield break;
            }
            yield return new WaitForSeconds(Mathf.Lerp(jumpPause, jumpPause * 0.4f, progressFactor));
        }
    }

    private float GetGroundLandingY(float targetX)
    {
        if (groundPoint != null) return groundPoint.position.y + groundImpactHeight;

        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(targetX, transform.position.y + groundRaycastDistance * 0.5f), Vector2.down, groundRaycastDistance, groundLayers);
        float closestGroundY = float.NegativeInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (!string.IsNullOrEmpty(groundTag) && !hit.collider.CompareTag(groundTag)) continue;
            if (hit.point.y > closestGroundY) closestGroundY = hit.point.y;
        }

        if (!float.IsNegativeInfinity(closestGroundY)) return closestGroundY + groundImpactHeight;
        if (flightAreaCollider != null) return flightAreaCollider.bounds.min.y + groundImpactHeight;
        return transform.position.y;
    }

    private IEnumerator MoveBossToPosition(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (currentHealth <= 0) yield break;
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        transform.position = targetPosition;
    }

    private IEnumerator MoveSmoothlyToPosition(Vector2 targetPos, float duration)
    {
        yield return MoveBossToPosition(targetPos, duration);
    }

    private IEnumerator MoveSmoothlyToPositionWithSpeed(Vector2 targetPosition, float speed)
    {
        while (Vector2.Distance(transform.position, targetPosition) > 0.05f)
        {
            if (isStaggered || currentHealth <= 0) yield break;
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }

    private IEnumerator DiveBossToGround(Vector2 landingPosition, float duration)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (currentHealth <= 0) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float fallT = t * t * (3f - 2f * t);
            transform.position = new Vector2(landingPosition.x, Mathf.Lerp(startPosition.y, landingPosition.y, fallT));
            yield return null;
        }
        transform.position = landingPosition;
    }

    private IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        yield return new WaitForSeconds(staggerDuration);
        isStaggered = false;
        transform.rotation = Quaternion.identity;
        UpdateSpriteRotation();
    }
}