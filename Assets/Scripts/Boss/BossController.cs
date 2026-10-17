using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
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

    [Header("Referências Visuais")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BossEffects bossEffects;

    private Coroutine behaviorCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        if (bossEffects == null)
            bossEffects = GetComponent<BossEffects>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = maxHealth;

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
        hoverTimer += Time.fixedDeltaTime;

        if (isStaggered)
        {
            float wobble =
                Mathf.Sin(Time.time * staggerWobbleSpeed) *
                staggerWobbleAngle;

            transform.rotation = Quaternion.Euler(0f, 0f, wobble);
            return;
        }

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

    private Vector2 GetCurrentHoverOffset(
        float freqMultiplier = 1f,
        float amountMultiplier = 1f)
    {
        float freq = hoverFrequency * freqMultiplier;

        float offsetX =
            Mathf.Sin(hoverTimer * freq * 0.8f) *
            hoverAmountX * amountMultiplier;

        float offsetY =
            Mathf.Cos(hoverTimer * freq) *
            hoverAmountY * amountMultiplier;

        return new Vector2(offsetX, offsetY);
    }

    private void FlyInArea()
    {
        if (flightAreaCollider == null)
            return;

        Bounds bounds = flightAreaCollider.bounds;

        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float rageFactor = 1f - healthRatio;

        float currentSpeed =
            Mathf.Lerp(minFlySpeed, maxFlySpeed, rageFactor);

        float currentHoverFreq =
            Mathf.Lerp(1f, 1.8f, rageFactor);

        Vector2 hoverOffset =
            GetCurrentHoverOffset(currentHoverFreq, 1f);

        Vector2 targetWithHover = currentBaseTarget + hoverOffset;

        targetWithHover.x = Mathf.Clamp(
            targetWithHover.x, bounds.min.x, bounds.max.x);

        targetWithHover.y = Mathf.Clamp(
            targetWithHover.y, bounds.min.y, bounds.max.y);

        Vector2 smoothPos = Vector2.Lerp(
            rb.position,
            targetWithHover,
            currentSpeed * Time.fixedDeltaTime * 2.5f);

        rb.MovePosition(smoothPos);

        Transform nearestPlayer = GetNearestPlayerTransform();

        if (spriteRenderer != null && nearestPlayer != null)
        {
            spriteRenderer.flipX =
                nearestPlayer.position.x < transform.position.x;
        }

        moveCooldownTimer -= Time.fixedDeltaTime;

        if (Vector2.Distance(rb.position, currentBaseTarget)
                < waypointReachDistance &&
            moveCooldownTimer <= 0f)
        {
            ChooseNextFlightTarget();

            moveCooldownTimer = Mathf.Lerp(
                maxMoveCooldown,
                minMoveCooldown,
                rageFactor);
        }
    }

    private void ApplyHoverOnly()
    {
        Vector2 hoverOffset = GetCurrentHoverOffset(1f, 0.5f);

        Vector2 smoothPos = Vector2.Lerp(
            rb.position,
            rb.position + hoverOffset,
            Time.fixedDeltaTime * 2f);

        rb.MovePosition(smoothPos);
    }

    private void ChooseNextFlightTarget()
    {
        if (flightAreaCollider == null)
            return;

        Bounds bounds = flightAreaCollider.bounds;
        List<PlayerMovement> alivePlayers = GetAlivePlayersList();

        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float adjustedChaseChance = Mathf.Lerp(
            chasePlayerChance * 1.5f,
            chasePlayerChance,
            healthRatio);

        bool shouldChasePlayer =
            alivePlayers.Count > 0 &&
            Random.value < adjustedChaseChance;

        if (shouldChasePlayer)
        {
            PlayerMovement targetPlayer = GetNearestPlayer();

            if (targetPlayer == null)
                targetPlayer =
                    alivePlayers[Random.Range(0, alivePlayers.Count)];

            Vector2 playerPos = targetPlayer.transform.position;

            Vector2 desiredPos = playerPos +
                new Vector2(
                    Random.Range(-2f, 2f),
                    Random.Range(1.5f, 3.5f));

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
        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

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

        if (alivePlayers.Count == 0)
            return null;

        PlayerMovement nearest = alivePlayers[0];

        float minDst = Vector2.Distance(
            transform.position,
            nearest.transform.position);

        for (int i = 1; i < alivePlayers.Count; i++)
        {
            float dst = Vector2.Distance(
                transform.position,
                alivePlayers[i].transform.position);

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
            Debug.Log(
                "O Boss está protegido! Aguarde o Stagger para causar dano.");
            return;
        }

        currentHealth = Mathf.Clamp(
            currentHealth - damageAmount,
            0,
            maxHealth);

        Debug.Log(
            "DANO CONFIRMADO NO STAGGER! Vida restante do Boss: " +
            currentHealth);

        if (bossEffects != null)
            bossEffects.TriggerDamage();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log("Boss derrotado!");

        if (behaviorCoroutine != null)
            StopCoroutine(behaviorCoroutine);

        StopAllCoroutines();
        Destroy(gameObject);
    }

    private IEnumerator BossBehaviorLoop()
    {
        yield return new WaitForSeconds(2f);

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

                int staggerAttack = Random.Range(0, 2);

                if (bossEffects != null)
                    bossEffects.TriggerAttack();

                if (staggerAttack == 0)
                    yield return StartCoroutine(QueenBeeDashAttack());
                else
                    yield return StartCoroutine(FivePointJumpAttack());
            }
            else
            {
                int chosenAttack = Random.Range(0, 5);

                if (bossEffects != null)
                    bossEffects.TriggerAttack();

                switch (chosenAttack)
                {
                    case 0:
                        yield return StartCoroutine(ArcProjectileAttack());
                        break;
                    case 1:
                        yield return StartCoroutine(TargetedBurstAttack());
                        break;
                    case 2:
                        yield return StartCoroutine(CircleRingAttack());
                        break;
                    case 3:
                        yield return StartCoroutine(ArcWaveAttack());
                        break;
                    case 4:
                        yield return StartCoroutine(CircleWaveAttack());
                        break;
                }

                normalAttackCounter++;
            }

            isActing = false;

            if (currentHealth <= 0)
                yield break;

            if (isStaggered)
            {
                yield return new WaitUntil(
                    () => !isStaggered || currentHealth <= 0);

                if (currentHealth <= 0)
                    yield break;
            }

            ChooseNextFlightTarget();

            float healthRatio =
                (float)currentHealth / Mathf.Max(1, maxHealth);

            float rageFactor = 1f - healthRatio;

            float currentAttackCooldown = Mathf.Lerp(
                maxTimeBetweenAttacks,
                minTimeBetweenAttacks,
                rageFactor);

            yield return new WaitForSeconds(currentAttackCooldown);
        }
    }

    private IEnumerator StaggerChargeRoutine()
    {
        if (staggerChargeDuration <= 0f)
            yield break;

        Debug.Log("Boss está carregando o ataque especial/stagger!");

        if (bossEffects != null)
            bossEffects.TriggerAttack();

        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < staggerChargeDuration)
        {
            if (currentHealth <= 0)
                yield break;

            elapsed += Time.deltaTime;

            Vector2 shakeOffset = Random.insideUnitCircle * staggerChargeShakeIntensity;
            transform.position = (Vector2)originalPos + shakeOffset;

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
            if (isStaggered || currentHealth <= 0)
                yield break;

            float angleOffset =
                wave * arcWaveAngleOffset -
                ((waves - 1) * arcWaveAngleOffset * 0.5f);

            FireArcWave(angleOffset);

            if (wave < waves - 1)
                yield return new WaitForSeconds(arcWaveInterval);
        }

        yield return new WaitForSeconds(arcAttackDuration);
    }

    private void FireArcWave(float extraAngle)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float rageFactor = 1f - healthRatio;

        int count = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Lerp(
                minArcProjectileCount,
                maxArcProjectileCount,
                rageFactor)));

        Transform nearestPlayer = GetNearestPlayerTransform();

        Vector2 baseDir = nearestPlayer != null
            ? ((Vector2)(nearestPlayer.position - firePoint.position)).normalized
            : (spriteRenderer != null && spriteRenderer.flipX
                ? Vector2.left : Vector2.right);

        if (baseDir.sqrMagnitude < 0.001f)
            baseDir = Vector2.right;

        float baseAngle =
            Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        float startAngle = -arcAngle / 2f;
        float angleStep = count > 1 ? arcAngle / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float finalAngle =
                baseAngle + startAngle + angleStep * i + extraAngle;

            Vector2 direction =
                Quaternion.Euler(0f, 0f, finalAngle) * Vector2.right;

            GameObject proj = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.identity);

            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();

            if (rbProj != null)
                rbProj.linearVelocity = direction * arcProjectileSpeed;
        }
    }

    private IEnumerator TargetedBurstAttack()
    {
        yield return new WaitForSeconds(0.2f);

        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float rageFactor = 1f - healthRatio;

        int burstCount = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Lerp(
                minBurstShotCount,
                maxBurstShotCount,
                rageFactor)));

        for (int i = 0; i < burstCount; i++)
        {
            if (isStaggered || currentHealth <= 0)
                yield break;

            Transform nearestPlayer = GetNearestPlayerTransform();

            if (nearestPlayer != null &&
                firePoint != null &&
                projectilePrefab != null)
            {
                Vector2 dirToPlayer =
                    ((Vector2)nearestPlayer.position -
                     (Vector2)firePoint.position).normalized;

                GameObject proj = Instantiate(
                    projectilePrefab,
                    firePoint.position,
                    Quaternion.identity);

                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();

                if (rbProj != null)
                    rbProj.linearVelocity =
                        dirToPlayer * burstProjectileSpeed;
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
            if (isStaggered || currentHealth <= 0)
                yield break;

            FireCircleWave(wave * circleWaveRotationOffset);

            if (wave < waves - 1)
                yield return new WaitForSeconds(circleWaveInterval);
        }

        yield return new WaitForSeconds(circleAttackDuration);
    }

    private void FireCircleWave(float rotationOffset)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float rageFactor = 1f - healthRatio;

        int count = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Lerp(
                minCircleProjectileCount,
                maxCircleProjectileCount,
                rageFactor)));

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep + rotationOffset;

            Vector2 direction =
                Quaternion.Euler(0f, 0f, angle) * Vector2.right;

            GameObject proj = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.identity);

            Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();

            if (rbProj != null)
                rbProj.linearVelocity =
                    direction * circleProjectileSpeed;
        }
    }

    private IEnumerator QueenBeeDashAttack()
    {
        if (flightAreaCollider == null)
            yield break;

        Bounds bounds = flightAreaCollider.bounds;
        float padding = 1f;

        float[] yHeights =
        {
            bounds.min.y + 3f,
            bounds.min.y + 2f,
            bounds.min.y + 1f
        };

        bool startFromLeft = true;

        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float rageFactor = 1f - healthRatio;

        float baseDashSpeed = Mathf.Lerp(
            minQueenBeeDashSpeed,
            maxQueenBeeDashSpeed,
            rageFactor);

        for (int i = 0; i < yHeights.Length; i++)
        {
            if (isStaggered || currentHealth <= 0)
                yield break;

            float currentY = Mathf.Clamp(
                yHeights[i],
                bounds.min.y + padding,
                bounds.max.y - padding);

            float startX = startFromLeft
                ? bounds.min.x + padding
                : bounds.max.x - padding;

            float targetX = startFromLeft
                ? bounds.max.x - padding
                : bounds.min.x + padding;

            Vector2 startPos = new Vector2(startX, currentY);
            Vector2 targetPos = new Vector2(targetX, currentY);

            yield return MoveSmoothlyToPosition(startPos, 0.4f);

            if (spriteRenderer != null)
                spriteRenderer.flipX = !startFromLeft;

            if (bossEffects != null)
                bossEffects.TriggerDash();

            yield return new WaitForSeconds(0.12f);

            float currentDashSpeed = Mathf.Lerp(baseDashSpeed, maxQueenBeeDashSpeed, (float)i / (yHeights.Length - 1));

            yield return MoveSmoothlyToPositionWithSpeed(targetPos, currentDashSpeed);

            startFromLeft = !startFromLeft;
        }

        if (bossEffects != null)
            bossEffects.TriggerImpact();

        Vector2 knockbackDir =
            startFromLeft ? Vector2.right : Vector2.left;

        Vector3 initialPos = transform.position;

        Vector3 targetKnockbackPos =
            initialPos + (Vector3)(knockbackDir * staggerKnockbackForce);

        targetKnockbackPos.x = Mathf.Clamp(
            targetKnockbackPos.x,
            bounds.min.x + padding,
            bounds.max.x - padding);

        targetKnockbackPos.y = Mathf.Clamp(
            targetKnockbackPos.y,
            bounds.min.y + padding,
            bounds.max.y - padding);

        float elapsed = 0f;

        while (elapsed < staggerKnockbackDuration)
        {
            if (currentHealth <= 0)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(0.01f, staggerKnockbackDuration));

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            Vector3 pos = Vector3.Lerp(initialPos, targetKnockbackPos, smoothT);
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
            if (currentHealth <= 0 || isStaggered)
                yield break;

            bool isFinalDive = i == totalPoints - 1;

            PlayerMovement targetPlayer = GetNearestPlayer();

            float targetX = targetPlayer != null
                ? targetPlayer.transform.position.x
                : transform.position.x;

            if (flightAreaCollider != null)
            {
                Bounds bounds = flightAreaCollider.bounds;

                targetX = Mathf.Clamp(
                    targetX,
                    bounds.min.x + 0.5f,
                    bounds.max.x - 0.5f);
            }

            float landingY = GetGroundLandingY(targetX);

            Vector2 landingPosition = new Vector2(targetX, landingY);

            if (spriteRenderer != null)
                spriteRenderer.flipX = targetX < transform.position.x;

            if (bossEffects != null)
                bossEffects.TriggerDash();

            float extraHeight = isFinalDive
                ? finalDivePreparationHeight
                : jumpHeight;

            float apexY = landingY + Mathf.Max(1f, extraHeight);

            if (flightAreaCollider != null)
            {
                apexY = Mathf.Min(apexY, flightAreaCollider.bounds.max.y - 0.5f);
            }

            Vector2 apexPosition = new Vector2(targetX, apexY);

            float progressFactor = (float)i / Mathf.Max(1, totalPoints - 1);

            float ascentDuration = isFinalDive
                ? finalDivePreparationDuration
                : Mathf.Lerp(jumpDuration, jumpDuration * 0.45f, progressFactor);

            yield return MoveBossToPosition(
                apexPosition,
                ascentDuration);

            if (currentHealth <= 0)
                yield break;

            float diveDuration = isFinalDive
                ? finalDiveDuration
                : Mathf.Max(0.12f, Mathf.Lerp(jumpDuration * 0.65f, jumpDuration * 0.3f, progressFactor));

            yield return DiveBossToGround(
                landingPosition,
                diveDuration);

            if (currentHealth <= 0)
                yield break;

            transform.position = landingPosition;

            if (bossEffects != null)
                bossEffects.TriggerImpact();

            if (isFinalDive)
            {
                Debug.Log("Boss mergulhou até o chão!");

                yield return new WaitForSeconds(
                    groundImpactShakeDuration);

                yield return StartCoroutine(StaggerRoutine());

                yield break;
            }

            float currentPause = Mathf.Lerp(jumpPause, jumpPause * 0.4f, progressFactor);
            yield return new WaitForSeconds(currentPause);
        }
    }

    private float GetGroundLandingY(float targetX)
    {
        if (groundPoint != null)
            return groundPoint.position.y + groundImpactHeight;

        float rayStartY = transform.position.y + groundRaycastDistance * 0.5f;

        Vector2 rayOrigin = new Vector2(targetX, rayStartY);

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            rayOrigin,
            Vector2.down,
            groundRaycastDistance,
            groundLayers);

        float closestGroundY = float.NegativeInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
                continue;

            if (hit.collider.GetComponentInParent<PlayerMovement>() != null)
                continue;

            if (!string.IsNullOrEmpty(groundTag) && !hit.collider.CompareTag(groundTag))
                continue;

            if (hit.point.y > closestGroundY)
                closestGroundY = hit.point.y;
        }

        if (!float.IsNegativeInfinity(closestGroundY))
            return closestGroundY + groundImpactHeight;

        if (flightAreaCollider != null)
            return flightAreaCollider.bounds.min.y + groundImpactHeight;

        Debug.LogWarning(
            "BossController: chão não encontrado com a Tag/Layer configurada. " +
            "Verifique Ground Layers e Ground Tag no Inspector.");

        return transform.position.y;
    }

    private IEnumerator MoveBossToPosition(
        Vector2 targetPosition,
        float duration)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (currentHealth <= 0)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector2.Lerp(
                startPosition,
                targetPosition,
                smoothT);

            yield return null;
        }

        transform.position = targetPosition;
    }

    private IEnumerator MoveSmoothlyToPosition(Vector2 targetPos, float duration)
    {
        Vector2 startPos = transform.position;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (currentHealth <= 0)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector2.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        transform.position = targetPos;
    }

    private IEnumerator MoveSmoothlyToPositionWithSpeed(Vector2 targetPosition, float speed)
    {
        while (Vector2.Distance(transform.position, targetPosition) > 0.05f)
        {
            if (isStaggered || currentHealth <= 0)
                yield break;

            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime);

            yield return null;
        }

        transform.position = targetPosition;
    }

    private IEnumerator DiveBossToGround(
        Vector2 landingPosition,
        float duration)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;

        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            if (currentHealth <= 0)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float fallT = t * t * (3f - 2f * t);

            float currentX = landingPosition.x;
            float currentY = Mathf.Lerp(
                startPosition.y,
                landingPosition.y,
                fallT);

            transform.position = new Vector2(currentX, currentY);

            yield return null;
        }

        transform.position = landingPosition;
    }

    private IEnumerator StaggerRoutine()
    {
        isStaggered = true;

        Debug.Log(
            "BOSS FICOU TONTO! (STAGGER ATIVO - ATAQUE A BOMBA AGORA)");

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

            Gizmos.DrawWireCube(
                flightAreaCollider.bounds.center,
                flightAreaCollider.bounds.size);
        }

        if (groundPoint != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                groundPoint.position + Vector3.left * 2f,
                groundPoint.position + Vector3.right * 2f);
        }
    }
}