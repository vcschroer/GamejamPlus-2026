using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private float introScreamDuration = 1.5f;
    [SerializeField] private float introShakeDuration = 0.4f;
    [SerializeField] private float introShakeIntensity = 0.2f;

    private bool isIntroFinished = false;
    public bool IsIntroFinished => isIntroFinished;
    private Coroutine introRoutineInstance;

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

    [Header("Colliders Específicos de Ataque")]
    [SerializeField] private Collider2D defaultCollider;
    [SerializeField] private Collider2D dashCollider;
    [SerializeField] private Collider2D jumpCollider;

    [Header("Materiais de Outline")]
    [SerializeField] private Material normalOutlineMaterial;
    [SerializeField] private Material thinOutlineMaterial;

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

    [SerializeField] public float staggerChargeDuration = 1.2f;
    [SerializeField] private float staggerChargeShakeIntensity = 0.05f;
    [SerializeField] private Color staggerChargeBlinkColor = Color.red;
    [SerializeField, Min(0.001f)] private float staggerChargeBlinkInterval = 0.05f;
    [SerializeField] private float staggerDuration = 3.5f;
    [SerializeField] private float staggerKnockbackForce = 3f;
    [SerializeField] private float staggerKnockbackDuration = 0.35f;
    [SerializeField] private float staggerKnockbackHeight = 0.5f;
    [SerializeField] private float staggerWobbleAngle = 5f;
    [SerializeField] private float staggerWobbleSpeed = 5f;

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
    [SerializeField] private TrailRenderer dashTrailRenderer;
    [SerializeField] private float dashWaveAmplitude = 0.6f;
    [SerializeField] private ParticleSystem dashImpactParticlesPrefab;
    [SerializeField] private int dashImpactParticleCount = 3;
    [SerializeField] private float dashParticleVerticalOffset = 0.5f;
    [SerializeField] private float dashParticleLineSpacing = 0.6f;

    [Header("Ataque de Stagger 2: Saltos e Mergulho")]
    [SerializeField] private int jumpPointCount = 5;
    [SerializeField] private float jumpDuration = 0.45f;
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float jumpPause = 0.12f;
    [SerializeField] private float finalDivePreparationHeight = 2.5f;
    [SerializeField] private float finalDivePreparationDuration = 0.3f;
    [SerializeField] private float finalDiveDuration = 0.18f;
    [SerializeField] private float groundImpactShakeDuration = 0.35f;
    [SerializeField] private ParticleSystem jumpImpactParticlesPrefab;
    [SerializeField] private int jumpImpactParticleCount = 3;
    [SerializeField] private float jumpParticleVerticalOffset = 0.5f;
    [SerializeField] private float jumpParticleLineSpacing = 0.6f;

    [Header("Referências Visuais & Partículas")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private BossEffects bossEffects;
    [SerializeField] private ParticleSystem damageParticleSystemPrefab;
    [SerializeField] private int damageParticleCount = 1;
    [SerializeField] private ParticleSystem deathBatParticlesPrefab;
    [SerializeField] private int deathBatParticleCount = 5;
    [SerializeField] private float particleSpawnRadius = 1.5f;

    [Header("Tela de Vitória")]
    [SerializeField] private Image victoryFadePanel;
    [SerializeField] private float victoryFadeDuration = 1.5f;
    [SerializeField] private string victorySceneName = "VictoryScene";

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
    private Coroutine staggerRoutineInstance;
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

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
            spriteVisualTransform = spriteRenderer.transform;
            spriteBaseLocalRotation = spriteVisualTransform.localRotation;
            if (normalOutlineMaterial == null) normalOutlineMaterial = spriteRenderer.material;
        }

        if (puppetStrings == null)
            puppetStrings = GetComponent<PuppetStringsController>();

        if (victoryFadePanel != null)
        {
            Color c = victoryFadePanel.color;
            c.a = 0f;
            victoryFadePanel.color = c;
            victoryFadePanel.gameObject.SetActive(false);
        }

        SetActiveCollider(defaultCollider);
        if (dashTrailRenderer != null) dashTrailRenderer.enabled = false;
    }

    private void SetActiveCollider(Collider2D targetCollider)
    {
        if (defaultCollider != null) defaultCollider.enabled = (defaultCollider == targetCollider);
        if (dashCollider != null) dashCollider.enabled = (dashCollider == targetCollider);
        if (jumpCollider != null) jumpCollider.enabled = (jumpCollider == targetCollider);
    }

    private void SetSpriteMaterial(Material targetMaterial)
    {
        if (spriteRenderer != null && targetMaterial != null)
        {
            spriteRenderer.material = targetMaterial;
        }
    }

    private void SpawnImpactParticlesLine(ParticleSystem prefab, int count, Vector3 centerPos, float verticalOffset, float spacing)
    {
        if (prefab == null || count <= 0) return;

        float totalWidth = (count - 1) * spacing;
        float startX = centerPos.x - (totalWidth * 0.5f);
        float spawnY = centerPos.y - verticalOffset;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(startX + (i * spacing), spawnY, centerPos.z);
            ParticleSystem ps = Instantiate(prefab, spawnPos, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 2f);
        }
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isIntroFinished = false;
        UpdateHealthVisuals(forceUpdate: true);

        transform.position = startIntroPosition;

        if (introRoutineInstance != null)
            StopCoroutine(introRoutineInstance);

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        introRoutineInstance = StartCoroutine(BossIntroRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator BossIntroRoutine()
    {
        if (animator != null)
            animator.SetBool("IsIntro", true);

        float safetyTimer = 0f;
        while (FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Length < 2 && safetyTimer < 3f)
        {
            safetyTimer += Time.deltaTime;
            yield return null;
        }

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

        if (animator != null)
        {
            animator.SetBool("IsIntro", false);
            animator.SetBool("IsRangedAttacking", true);
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("boss morte");

        if (GameEffectsManager.Instance != null)
        {
            GameEffectsManager.Instance.TriggerCameraShake(introShakeDuration, introShakeIntensity);
        }

        if (bossEffects != null)
        {
            bossEffects.TriggerImpact();
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

        yield return new WaitForSeconds(introScreamDuration - introShakeDuration);

        if (animator != null)
        {
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsIdle", true);
        }

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
            if (staggerRoutineInstance != null)
                StopCoroutine(staggerRoutineInstance);
            staggerRoutineInstance = StartCoroutine(StaggerRoutine());
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
        {
            if (animator != null)
                animator.SetBool("IsIdle", true);

            FlyInArea();
        }
        else
        {
            if (animator != null)
                animator.SetBool("IsIdle", false);

            ApplyHoverOnly();
        }
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

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("boss dano");

        if (bossEffects != null)
            bossEffects.TriggerDamage();

        SpawnDamageParticleSystemRandomlyOnSprite();

        UpdateHealthVisuals();

        if (staggerRoutineInstance != null)
        {
            StopCoroutine(staggerRoutineInstance);
            staggerRoutineInstance = null;
        }

        isStaggered = false;

        if (animator != null)
            animator.SetBool("IsStaggered", false);

        transform.rotation = Quaternion.identity;
        UpdateSpriteRotation();

        if (currentHealth <= 0)
            Die();
    }

    private void SpawnDamageParticleSystemRandomlyOnSprite()
    {
        if (damageParticleSystemPrefab != null)
        {
            for (int i = 0; i < damageParticleCount; i++)
            {
                Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * particleSpawnRadius);

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
    }

    private void UpdateHealthVisuals(bool forceUpdate = false)
    {
        int safeMaxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, safeMaxHealth);

        if (puppetStrings != null)
        {
            puppetStrings.SetHealth(currentHealth, safeMaxHealth);

            if (puppetStrings.LeftStringLost)
            {
                currentLeanAngle = leftLeanAngle * 2f;
            }
            else
            {
                currentLeanAngle = 0f;
            }
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
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("boss morte");

        if (deathBatParticlesPrefab != null)
        {
            for (int i = 0; i < deathBatParticleCount; i++)
            {
                Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * particleSpawnRadius);

                if (spriteRenderer != null)
                {
                    Bounds spriteBounds = spriteRenderer.bounds;
                    float randomX = Random.Range(spriteBounds.min.x, spriteBounds.max.x);
                    float randomY = Random.Range(spriteBounds.min.y, spriteBounds.max.y);
                    spawnPos = new Vector3(randomX, randomY, transform.position.z);
                }

                ParticleSystem batParticles = Instantiate(deathBatParticlesPrefab, spawnPos, Quaternion.identity);
                batParticles.Play();
                float totalDuration = batParticles.main.duration + batParticles.main.startLifetime.constantMax;
                Destroy(batParticles.gameObject, totalDuration);
            }
        }

        if (SceneManager.GetActiveScene().name == "Tutorial Game")
        {
            Destroy(gameObject);
            return;
        }

        if (animator != null)
        {
            animator.SetBool("IsIntro", false);
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsDashAttacking", false);
            animator.SetBool("IsJumpAttacking", false);
            animator.SetBool("IsStaggered", false);
            animator.SetBool("IsStaggerCharging", false);
        }

        if (healthBlinkCoroutine != null)
        {
            StopCoroutine(healthBlinkCoroutine);
            healthBlinkCoroutine = null;
        }

        SetActiveCollider(null);
        if (dashTrailRenderer != null) dashTrailRenderer.enabled = false;
        SetSpriteMaterial(normalOutlineMaterial);

        Rigidbody2D rbBody = GetComponent<Rigidbody2D>();
        if (rbBody != null) rbBody.simulated = false;

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        StopAllCoroutines();
        StartCoroutine(VictoryFadeAndLoadSceneRoutine());
    }

    private IEnumerator VictoryFadeAndLoadSceneRoutine()
    {
        if (victoryFadePanel != null)
        {
            victoryFadePanel.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = victoryFadePanel.color;

            while (elapsed < victoryFadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / victoryFadeDuration);
                victoryFadePanel.color = c;
                yield return null;
            }

            c.a = 1f;
            victoryFadePanel.color = c;
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        SceneManager.LoadScene(victorySceneName);
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

                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySound("boss meelee");

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

        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsStaggerCharging", true);
        }

        if (bossEffects != null)
            bossEffects.TriggerAttack();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("boss morte");

        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        float blinkTimer = 0f;
        bool useBlink = false;

        while (elapsed < staggerChargeDuration)
        {
            if (currentHealth <= 0) yield break;

            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            blinkTimer += deltaTime;

            transform.position = (Vector2)originalPos + (Random.insideUnitCircle * staggerChargeShakeIntensity);

            if (blinkTimer >= staggerChargeBlinkInterval)
            {
                blinkTimer = 0f;
                useBlink = !useBlink;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = useBlink ? staggerChargeBlinkColor : originalSpriteColor;
                }
            }

            yield return null;
        }

        transform.position = originalPos;

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        if (animator != null)
        {
            animator.SetBool("IsStaggerCharging", false);
        }
    }

    private IEnumerator ArcProjectileAttack()
    {
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRangedAttacking", true);
        }

        yield return new WaitForSeconds(0.2f);
        FireArcWave(0f);

        if (animator != null)
        {
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsIdle", true);
        }

        yield return new WaitForSeconds(arcAttackDuration);
    }

    private IEnumerator ArcWaveAttack()
    {
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRangedAttacking", true);
        }

        yield return new WaitForSeconds(0.2f);
        int waves = Mathf.Max(1, arcWaveCount);

        for (int wave = 0; wave < waves; wave++)
        {
            if (isStaggered || currentHealth <= 0)
            {
                if (animator != null)
                {
                    animator.SetBool("IsRangedAttacking", false);
                    animator.SetBool("IsIdle", true);
                }
                yield break;
            }
            float angleOffset = wave * arcWaveAngleOffset - ((waves - 1) * arcWaveAngleOffset * 0.5f);
            FireArcWave(angleOffset);
            if (wave < waves - 1) yield return new WaitForSeconds(arcWaveInterval);
        }

        if (animator != null)
        {
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsIdle", true);
        }

        yield return new WaitForSeconds(arcAttackDuration);
    }

    private void FireArcWave(float extraAngle)
    {
        if (projectilePrefab == null || firePoint == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("projetil boss");

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
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRangedAttacking", true);
        }

        yield return new WaitForSeconds(0.2f);
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        int burstCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(minBurstShotCount, maxBurstShotCount, 1f - healthRatio)));

        for (int i = 0; i < burstCount; i++)
        {
            if (isStaggered || currentHealth <= 0)
            {
                if (animator != null)
                {
                    animator.SetBool("IsRangedAttacking", false);
                    animator.SetBool("IsIdle", true);
                }
                yield break;
            }
            Transform nearestPlayer = GetNearestPlayerTransform();

            if (nearestPlayer != null && firePoint != null && projectilePrefab != null)
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySound("projetil boss");

                Vector2 dirToPlayer = ((Vector2)nearestPlayer.position - (Vector2)firePoint.position).normalized;
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null) rbProj.linearVelocity = dirToPlayer * burstProjectileSpeed;
            }
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }

        if (animator != null)
        {
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsIdle", true);
        }

        yield return new WaitForSeconds(burstAttackDuration);
    }

    private IEnumerator CircleRingAttack()
    {
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRangedAttacking", true);
        }

        yield return new WaitForSeconds(0.2f);
        FireCircleWave(0f);

        if (animator != null)
        {
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsIdle", true);
        }

        yield return new WaitForSeconds(circleAttackDuration);
    }

    private IEnumerator CircleWaveAttack()
    {
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRangedAttacking", true);
        }

        yield return new WaitForSeconds(0.2f);
        int waves = Mathf.Max(1, circleWaveCount);

        for (int wave = 0; wave < waves; wave++)
        {
            if (isStaggered || currentHealth <= 0)
            {
                if (animator != null)
                {
                    animator.SetBool("IsRangedAttacking", false);
                    animator.SetBool("IsIdle", true);
                }
                yield break;
            }
            FireCircleWave(wave * circleWaveRotationOffset);
            if (wave < waves - 1) yield return new WaitForSeconds(circleWaveInterval);
        }

        if (animator != null)
        {
            animator.SetBool("IsRangedAttacking", false);
            animator.SetBool("IsIdle", true);
        }

        yield return new WaitForSeconds(circleAttackDuration);
    }

    private void FireCircleWave(float rotationOffset)
    {
        if (projectilePrefab == null || firePoint == null) return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("projetil boss");

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
        float padding = 0.5f;

        float[] yHeights = { bounds.min.y + 3f, bounds.min.y + 2f, bounds.min.y + 1f };
        bool startFromLeft = true;
        float healthRatio = (float)currentHealth / Mathf.Max(1, maxHealth);
        float baseDashSpeed = Mathf.Lerp(minQueenBeeDashSpeed, maxQueenBeeDashSpeed, 1f - healthRatio);

        SetActiveCollider(dashCollider);
        if (dashTrailRenderer != null) dashTrailRenderer.enabled = true;

        SetSpriteMaterial(thinOutlineMaterial);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("boss meelee");

        if (animator != null)
        {
            animator.speed = 1f;
            animator.ResetTrigger("TriggerDash");
            animator.SetTrigger("TriggerDash");
        }

        for (int i = 0; i < yHeights.Length; i++)
        {
            if (isStaggered || currentHealth <= 0)
            {
                SetSpriteMaterial(normalOutlineMaterial);
                yield break;
            }

            float currentY = Mathf.Clamp(yHeights[i], bounds.min.y + padding, bounds.max.y - padding);
            float startX = startFromLeft ? bounds.min.x - padding : bounds.max.x + padding;
            float targetX = startFromLeft ? bounds.max.x + padding : bounds.min.x - padding;

            yield return MoveSmoothlyToPosition(new Vector2(startX, currentY), 0.25f);

            if (spriteRenderer != null) spriteRenderer.flipX = !startFromLeft;

            float currentDashSpeed = Mathf.Lerp(baseDashSpeed, maxQueenBeeDashSpeed, (float)i / (yHeights.Length - 1));

            yield return MoveDynamicDash(new Vector2(targetX, currentY), currentDashSpeed, i);

            startFromLeft = !startFromLeft;
        }

        if (dashTrailRenderer != null) dashTrailRenderer.enabled = false;
        SetActiveCollider(defaultCollider);

        SetSpriteMaterial(normalOutlineMaterial);

        if (bossEffects != null) bossEffects.TriggerImpact();

        SpawnImpactParticlesLine(dashImpactParticlesPrefab, dashImpactParticleCount, transform.position, dashParticleVerticalOffset, dashParticleLineSpacing);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("boss dano");

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsDashAttacking", false);
            animator.SetBool("IsStaggered", true);
        }

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

        if (staggerRoutineInstance != null)
            StopCoroutine(staggerRoutineInstance);
        staggerRoutineInstance = StartCoroutine(StaggerRoutine());
    }

    private IEnumerator MoveDynamicDash(Vector2 targetPosition, float speed, int dashIndex)
    {
        Vector2 startPos = transform.position;
        float distance = Vector2.Distance(startPos, targetPosition);
        float duration = distance / speed;
        float elapsed = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (isStaggered || currentHealth <= 0) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            Vector2 basePos = Vector2.Lerp(startPos, targetPosition, smoothT);

            float waveOffset = Mathf.Sin(t * Mathf.PI * 2f + dashIndex) * dashWaveAmplitude * (1f - t);
            transform.position = new Vector2(basePos.x, basePos.y + waveOffset);

            yield return null;
        }

        transform.position = targetPosition;
    }

    private IEnumerator FivePointJumpAttack()
    {
        int totalPoints = Mathf.Max(1, jumpPointCount);
        yield return new WaitForSeconds(0.35f);

        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsJumpAttacking", true);
        }

        SetActiveCollider(jumpCollider);

        SetSpriteMaterial(thinOutlineMaterial);

        for (int i = 0; i < totalPoints; i++)
        {
            if (currentHealth <= 0 || isStaggered)
            {
                SetSpriteMaterial(normalOutlineMaterial);
                yield break;
            }

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySound("boss meelee");

            bool isFinalDive = i == totalPoints - 1;

            PlayerMovement targetPlayer = GetNearestPlayer();
            float targetX = targetPlayer != null ? targetPlayer.transform.position.x : transform.position.x;

            if (flightAreaCollider != null)
                targetX = Mathf.Clamp(targetX, flightAreaCollider.bounds.min.x + 0.5f, flightAreaCollider.bounds.max.x - 0.5f);

            float landingY = GetGroundLandingY(targetX);
            if (spriteRenderer != null) spriteRenderer.flipX = targetX < transform.position.x;

            float apexY = landingY + Mathf.Max(1f, isFinalDive ? finalDivePreparationHeight : jumpHeight);
            if (flightAreaCollider != null) apexY = Mathf.Min(apexY, flightAreaCollider.bounds.max.y - 0.5f);

            float progressFactor = (float)i / Mathf.Max(1, totalPoints - 1);
            float ascentDuration = isFinalDive ? finalDivePreparationDuration : Mathf.Lerp(jumpDuration, jumpDuration * 0.45f, progressFactor);

            if (animator != null)
            {
                animator.speed = 1f;
                animator.ResetTrigger("TriggerJump");
                animator.SetTrigger("TriggerJump");
            }

            yield return MoveBossToPosition(new Vector2(targetX, apexY), ascentDuration);
            if (currentHealth <= 0)
            {
                SetSpriteMaterial(normalOutlineMaterial);
                yield break;
            }

            float diveDuration = isFinalDive ? finalDiveDuration : Mathf.Max(0.12f, Mathf.Lerp(jumpDuration * 0.65f, jumpDuration * 0.3f, progressFactor));
            yield return DiveBossToGround(new Vector2(targetX, landingY), diveDuration);
            if (currentHealth <= 0)
            {
                SetSpriteMaterial(normalOutlineMaterial);
                yield break;
            }

            if (bossEffects != null) bossEffects.TriggerImpact();

            SpawnImpactParticlesLine(jumpImpactParticlesPrefab, jumpImpactParticleCount, new Vector2(targetX, landingY), jumpParticleVerticalOffset, jumpParticleLineSpacing);

            if (isFinalDive)
            {
                if (animator != null)
                {
                    animator.SetBool("IsJumpAttacking", false);
                }

                SetActiveCollider(defaultCollider);

                SetSpriteMaterial(normalOutlineMaterial);

                yield return new WaitForSeconds(groundImpactShakeDuration);
                if (staggerRoutineInstance != null)
                    StopCoroutine(staggerRoutineInstance);
                staggerRoutineInstance = StartCoroutine(StaggerRoutine());
                yield break;
            }
            yield return new WaitForSeconds(Mathf.Lerp(jumpPause, jumpPause * 0.4f, progressFactor));
        }

        if (animator != null)
        {
            animator.SetBool("IsJumpAttacking", false);
            animator.SetBool("IsIdle", true);
        }

        SetActiveCollider(defaultCollider);

        SetSpriteMaterial(normalOutlineMaterial);
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
        SetActiveCollider(defaultCollider);

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsStaggered", true);
        }

        yield return new WaitForSeconds(staggerDuration);

        isStaggered = false;

        if (animator != null)
            animator.SetBool("IsStaggered", false);

        transform.rotation = Quaternion.identity;
        UpdateSpriteRotation();
        staggerRoutineInstance = null;
    }
}