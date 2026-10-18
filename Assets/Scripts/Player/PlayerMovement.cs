using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    public enum PlayerID { Player1, Player2 }

    [Header("Configurações do Jogador (ID & Cor)")]
    [SerializeField] private PlayerID playerID = PlayerID.Player1;
    [SerializeField] private Color player1Color = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color player2Color = new Color(1f, 0.2f, 0.3f);
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Customização Visual por Jogador")]
    [SerializeField] private Sprite player1Sprite;
    [SerializeField] private RuntimeAnimatorController player1AnimatorController;

    [SerializeField] private Sprite player2Sprite;
    [SerializeField] private RuntimeAnimatorController player2AnimatorController;

    public PlayerID ID => playerID;
    public Color PlayerColor =>
        playerID == PlayerID.Player1 ? player1Color : player2Color;

    [Header("Sistema de Vidas & Espírito")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [SerializeField] private UnityEngine.UI.Image[] lifeIcons;
    [SerializeField] private float ghostDuration = 12f;
    [SerializeField] private float ghostFloatSpeed = 0.8f;
    [SerializeField] private float ghostWobbleFrequency = 3f;
    [SerializeField] private float ghostWobbleAmount = 0.4f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isDead = false;
    public bool IsDead => isDead;

    [Header("Componentes")]
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Collider2D playerCollider;

    [Header("Efeitos Visuais & Mira")]
    [SerializeField] private PlayerEffects playerEffects;
    [SerializeField] public ParticleSystem Dust;
    [SerializeField] private GameObject aimArrow;
    [SerializeField] private float aimOrbitRadius = 1.2f;

    [Header("Movimentação Básica")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 60f;

    private float horizontalInput;
    private bool facingRight = true;
    private Vector3 initialScale;
    private Vector3 wallCheckInitialLocalPosition;

    [Header("Pulo, Pulo Duplo & Gravidade")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float baseGravity = 4f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    private int extraJumps;
    [SerializeField] private int maxExtraJumps = 1;

    [Header("Mecânicas de Parede")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlidingSpeed = 3f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(14f, 16f);

    private bool isTouchingWall;
    private bool isWallSliding;

    [Header("Material de Física")]
    [SerializeField] private bool removePlayerFriction = true;

    [Header("Dash")]
    [SerializeField] private float dashPower = 24f;
    [SerializeField] private float dashTime = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;

    private bool canDash = true;
    private bool isDashing;

    [Header("Checagem de Chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private bool isGrounded;

    [Header("Mecânica da Bomba & Mira")]
    private Bomb heldBomb = null;
    public bool IsHoldingBomb => heldBomb != null;

    private bool isAiming = false;
    private float aimTimer = 0f;

    [SerializeField] private float maxAimTime = 3f;

    private float originalGravity;
    private Vector2 currentAimDir;

    [Header("Configurações de Planagem ao Mirar")]
    [SerializeField] private float aimGravityScale = 0.2f;
    [SerializeField] private float maxAimFallSpeed = 0.6f;

    [Header("Configurações de Arremesso")]
    [SerializeField] private float minThrowSpeed = 18f;
    [SerializeField] private float maxThrowSpeed = 36f;

    [Header("Knockback do Arremesso")]
    [SerializeField] private float throwKnockbackForce = 12f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private bool scaleKnockbackWithCharge = true;

    private bool isKnockbacked = false;

    [Header("Knockback entre Jogadores")]
    [SerializeField] private float playerKnockbackForce = 10f;
    [SerializeField] private float playerKnockbackDuration = 0.15f;

    [Header("Dano do Boss: Knockback e Invulnerabilidade")]
    [SerializeField] private float bossKnockbackForce = 26f;
    [SerializeField] private float bossKnockbackDuration = 0.3f;
    [SerializeField] private float bossKnockbackUpward = 0.75f;
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float invulnerabilityBlinkInterval = 0.08f;

    [Header("Piscar Conforme a Vida")]
    [SerializeField] private Color lowHealthBlinkColor = Color.red;
    [Min(0.01f)][SerializeField] private float mediumHealthBlinkInterval = 0.65f;
    [Min(0.01f)][SerializeField] private float lowHealthBlinkInterval = 0.15f;
    [Min(0f)][SerializeField] private float healthBlinkTransitionSpeed = 12f;

    private float healthBlinkTimer;
    private bool healthBlinkOn;
    private Color originalSpriteColor = Color.white;

    private bool isInvulnerable = false;
    private Coroutine invulnerabilityCoroutine;
    private Coroutine knockbackCoroutine;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();

        initialScale = transform.localScale;
        facingRight = true;

        rb.gravityScale = baseGravity;
        originalGravity = baseGravity;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (wallCheck != null)
            wallCheckInitialLocalPosition = wallCheck.localPosition;

        ApplyFacingDirection();

        if (removePlayerFriction && playerCollider != null)
        {
            PhysicsMaterial2D noFriction =
                new PhysicsMaterial2D("Player No Friction");

            noFriction.friction = 0f;
            noFriction.bounciness = 0f;

            playerCollider.sharedMaterial = noFriction;
        }

        AutoAssignPlayerID();

        currentHealth = maxHealth;
        UpdateLifeUI();

        if (playerInput != null && playerInput.actions != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            dashAction = playerInput.actions["Dash"];
            attackAction = playerInput.actions["Attack"];
        }

        if (aimArrow != null)
            aimArrow.SetActive(false);
    }

    private void AutoAssignPlayerID()
    {
        PlayerMovement[] existingPlayers =
            FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        bool hasPlayer1 = false;

        foreach (PlayerMovement p in existingPlayers)
        {
            if (p != this && p.ID == PlayerID.Player1)
            {
                hasPlayer1 = true;
                break;
            }
        }

        playerID = hasPlayer1 ? PlayerID.Player2 : PlayerID.Player1;
        ApplyPlayerVisuals();
    }

    public void SetPlayerID(PlayerID newID)
    {
        playerID = newID;
        ApplyPlayerVisuals();
    }

    public void ApplyPlayerVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerID == PlayerID.Player1)
        {
            if (player1Sprite != null && animator == null)
                spriteRenderer.sprite = player1Sprite;

            if (player1AnimatorController != null && animator != null)
                animator.runtimeAnimatorController = player1AnimatorController;
        }
        else
        {
            if (player2Sprite != null && animator == null)
                spriteRenderer.sprite = player2Sprite;

            if (player2AnimatorController != null && animator != null)
                animator.runtimeAnimatorController = player2AnimatorController;
        }

        UpdatePlayerColor();
    }

    public void UpdatePlayerColor()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalSpriteColor = PlayerColor;
            originalSpriteColor.a = 1f;
            ApplyHealthBlinkVisual(true);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || isInvulnerable)
            return;

        ApplyDamage(damageAmount);

        if (!isDead)
            StartInvulnerability(invulnerabilityDuration);
    }

    public void TakeBossDamage(
        int damageAmount,
        Vector2 bossOrProjectilePosition)
    {
        if (isDead || isInvulnerable)
            return;

        ApplyDamage(damageAmount);

        if (isDead)
            return;

        Vector2 direction =
            (Vector2)transform.position - bossOrProjectilePosition;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction.Normalize();
        direction.y = Mathf.Max(direction.y, bossKnockbackUpward);
        direction.Normalize();

        StartKnockback(
            direction,
            bossKnockbackForce,
            bossKnockbackDuration
        );

        StartInvulnerability(invulnerabilityDuration);
    }

    private void ApplyDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("Player dano");

        if (PlayerUI.Instance != null)
            PlayerUI.Instance.UpdateHealth(playerID, currentHealth);

        healthBlinkTimer = 0f;
        healthBlinkOn = false;
        UpdateLifeUI();

        if (currentHealth <= 0)
            Die();
    }

    private void StartInvulnerability(float duration)
    {
        if (invulnerabilityCoroutine != null)
            StopCoroutine(invulnerabilityCoroutine);

        invulnerabilityCoroutine =
            StartCoroutine(InvulnerabilityRoutine(duration));
    }

    private IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;

        yield return new WaitForSeconds(duration);

        isInvulnerable = false;
        invulnerabilityCoroutine = null;
        ApplyHealthBlinkVisual(true);
    }

    private void UpdateHealthBlink()
    {
        if (spriteRenderer == null || isDead)
            return;

        if (currentHealth >= maxHealth || currentHealth <= 0)
        {
            healthBlinkOn = false;
            healthBlinkTimer = 0f;
            ApplyHealthBlinkVisual(true);
            return;
        }

        float interval = currentHealth == 1
            ? lowHealthBlinkInterval
            : mediumHealthBlinkInterval;

        healthBlinkTimer += Time.deltaTime;
        if (healthBlinkTimer >= interval)
        {
            healthBlinkTimer = 0f;
            healthBlinkOn = !healthBlinkOn;
        }

        ApplyHealthBlinkVisual(false);
    }

    private void ApplyHealthBlinkVisual(bool instant)
    {
        if (spriteRenderer == null || isDead)
            return;

        Color targetColor = (currentHealth < maxHealth && currentHealth > 0 && healthBlinkOn)
            ? lowHealthBlinkColor
            : PlayerColor;
        targetColor.a = 1f;

        if (instant || healthBlinkTransitionSpeed <= 0f)
            spriteRenderer.color = targetColor;
        else
            spriteRenderer.color = Color.Lerp(
                spriteRenderer.color,
                targetColor,
                Time.deltaTime * healthBlinkTransitionSpeed);
    }

    private void UpdateLifeUI()
    {
        if (lifeIcons == null)
            return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null)
                lifeIcons[i].gameObject.SetActive(i < currentHealth);
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        if (PlayerDeathTracker.Instance != null)
        {
            PlayerDeathTracker.Instance.RegisterPlayerDeath(this);
        }
        isWallSliding = false;
        isKnockbacked = false;

        if (animator != null)
            animator.SetBool("IsWallSliding", false);

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }

        isInvulnerable = false;

        if (heldBomb != null)
        {
            Vector2 throwDir = facingRight ? Vector2.right : Vector2.left;
            PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (PlayerMovement p in allPlayers)
            {
                if (p != this && !p.IsDead)
                {
                    Vector2 dirToOther = (p.transform.position - transform.position);
                    if (dirToOther.sqrMagnitude > 0.001f)
                    {
                        throwDir = dirToOther.normalized;
                    }
                    break;
                }
            }

            heldBomb.DropByDeadPlayerToward(throwDir, 12f);
            heldBomb = null;
        }

        if (aimArrow != null)
            aimArrow.SetActive(false);

        PlayerMovement[] existingPlayers =
            FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement p in existingPlayers)
        {
            Collider2D otherCollider = p.GetComponent<Collider2D>();

            if (p != this && otherCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(
                    playerCollider,
                    otherCollider,
                    true
                );
            }
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        StartCoroutine(DeathAndGhostRoutine());
    }

    private IEnumerator DeathAndGhostRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 50;
        }

        float squashDuration = 0.25f;
        float elapsedSquash = 0f;

        Vector3 originalDeathScale = transform.localScale;

        Vector3 squashedScale = new Vector3(
            originalDeathScale.x * 1.4f,
            originalDeathScale.y * 0.5f,
            originalDeathScale.z
        );

        Vector3 stretchedScale = new Vector3(
            originalDeathScale.x * 0.7f,
            originalDeathScale.y * 1.3f,
            originalDeathScale.z
        );

        while (elapsedSquash < squashDuration)
        {
            elapsedSquash += Time.deltaTime;
            float t = elapsedSquash / squashDuration;

            if (t < 0.5f)
            {
                transform.localScale = Vector3.Lerp(
                    originalDeathScale,
                    squashedScale,
                    t * 2f
                );
            }
            else
            {
                transform.localScale = Vector3.Lerp(
                    squashedScale,
                    stretchedScale,
                    (t - 0.5f) * 2f
                );
            }

            yield return null;
        }

        transform.localScale = originalDeathScale;

        float elapsedGhost = 0f;
        Color startColor = spriteRenderer != null
            ? spriteRenderer.color
            : Color.white;

        Vector3 startPosition = transform.position;

        BossController boss = FindFirstObjectByType<BossController>();
        if (boss != null)
        {
            Collider2D bossCol = boss.GetComponent<Collider2D>();
            if (bossCol != null && bossCol.bounds.Contains(startPosition))
            {
                float pushDistance = bossCol.bounds.extents.x + 1.5f;
                float dir = startPosition.x < boss.transform.position.x ? -1f : 1f;
                startPosition.x = boss.transform.position.x + (dir * pushDistance);
                transform.position = startPosition;
            }
        }

        while (isDead)
        {
            elapsedGhost += Time.deltaTime;
            float progress = elapsedGhost / ghostDuration;

            float horizontalWobble =
                Mathf.Sin(elapsedGhost * ghostWobbleFrequency)
                * ghostWobbleAmount;

            float verticalOffset =
                elapsedGhost * ghostFloatSpeed
                + Mathf.Cos(elapsedGhost * ghostWobbleFrequency * 0.5f)
                * 0.1f;

            Vector3 currentGhostPos = new Vector3(
                startPosition.x + horizontalWobble,
                startPosition.y + verticalOffset,
                startPosition.z
            );

            if (boss != null)
            {
                Collider2D bossCol = boss.GetComponent<Collider2D>();
                if (bossCol != null && bossCol.bounds.Contains(currentGhostPos))
                {
                    float pushDistance = bossCol.bounds.extents.x + 1.5f;
                    float dir = currentGhostPos.x < boss.transform.position.x ? -1f : 1f;
                    startPosition.x = boss.transform.position.x + (dir * pushDistance);
                    currentGhostPos.x = startPosition.x + horizontalWobble;
                }
            }

            transform.position = currentGhostPos;

            float currentAlpha = Mathf.Lerp(0.8f, 0f, progress);

            if (spriteRenderer != null)
            {
                Color c = startColor;
                c.a = currentAlpha;
                spriteRenderer.color = c;
            }

            if (progress >= 1f || currentAlpha <= 0f)
            {
                if (PlayerDeathTracker.Instance != null)
                {
                    PlayerDeathTracker.Instance.RegisterGhostTimeExpired(this);
                }
                else
                {
                    Debug.LogError(
                        "PlayerDeathTracker não está presente na cena!");
                }

                yield break;
            }

            yield return null;
        }
    }

    public void Revive()
    {
        if (!isDead)
            return;

        isDead = false;

        if (PlayerDeathTracker.Instance != null)
        {
            PlayerDeathTracker.Instance.RegisterPlayerRevive(this);
        }
        isWallSliding = false;
        isKnockbacked = false;

        if (animator != null)
            animator.SetBool("IsWallSliding", false);

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 0;
        }

        currentHealth = maxHealth;
        healthBlinkTimer = 0f;
        healthBlinkOn = false;
        UpdateLifeUI();

        if (PlayerUI.Instance != null)
            PlayerUI.Instance.UpdateHealth(playerID, currentHealth);

        PlayerMovement[] allPlayers =
            FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement p in allPlayers)
        {
            Collider2D otherCollider = p.GetComponent<Collider2D>();

            if (p != this && otherCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(
                    playerCollider,
                    otherCollider,
                    false
                );
            }
        }

        rb.gravityScale = baseGravity;
        rb.linearVelocity = Vector2.zero;

        transform.localScale = initialScale;
        ApplyFacingDirection();

        StartInvulnerability(3.5f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead)
            return;

        PlayerMovement otherPlayer =
            collision.collider.GetComponent<PlayerMovement>();

        if (otherPlayer == null)
        {
            otherPlayer =
                collision.collider.GetComponentInParent<PlayerMovement>();
        }

        if (otherPlayer != null && otherPlayer != this)
        {
            if (!otherPlayer.IsDead)
                ApplyContactKnockback(otherPlayer.transform);

            return;
        }

        BossController boss =
            collision.collider.GetComponent<BossController>();

        if (boss == null)
            boss = collision.collider.GetComponentInParent<BossController>();

        if (boss != null)
            TakeBossDamage(1, boss.transform.position);
    }

    private void ApplyContactKnockback(Transform otherTransform)
    {
        Vector2 pushDir =
            (Vector2)transform.position - (Vector2)otherTransform.position;

        if (pushDir.sqrMagnitude < 0.001f)
            pushDir = Vector2.right;

        pushDir.Normalize();
        pushDir.y = 0.4f;
        pushDir.Normalize();

        StartKnockback(
            pushDir,
            playerKnockbackForce,
            playerKnockbackDuration
        );
    }

    private void StartKnockback(
        Vector2 direction,
        float force,
        float duration)
    {
        if (isDead)
            return;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(
            KnockbackRoutine(direction, force, duration)
        );
    }

    private void OnEnable()
    {
        if (jumpAction != null)
            jumpAction.performed += OnJumpPerformed;

        if (dashAction != null)
            dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        if (jumpAction != null)
            jumpAction.performed -= OnJumpPerformed;

        if (dashAction != null)
            dashAction.performed -= OnDashPerformed;
    }

    private void Update()
    {
        UpdateHealthBlink();

        if (isDead || isDashing)
            return;

        HandleBombAiming();

        if (isAiming)
            return;

        Vector2 moveInput = moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : Vector2.zero;

        horizontalInput = Mathf.Round(moveInput.x);

        if (!isKnockbacked)
        {
            if (horizontalInput > 0 && !facingRight)
                SetFacing(true);
            else if (horizontalInput < 0 && facingRight)
                SetFacing(false);
        }

        CheckSurroundings();
        HandleWallSlide();
    }

    private void HandleBombAiming()
    {
        if (!IsHoldingBomb || attackAction == null)
            return;

        if (attackAction.IsPressed())
        {
            if (!isAiming)
                StartAiming();

            UpdateAiming();
        }
        else if (isAiming)
        {
            ReleaseAim();
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        aimTimer = 0f;
        originalGravity = rb.gravityScale;

        currentAimDir = new Vector2(facingRight ? 1f : -1f, 0f);

        if (aimArrow != null)
        {
            aimArrow.SetActive(true);
            aimArrow.transform.position =
                transform.position + (Vector3)(currentAimDir * aimOrbitRadius);
        }
    }

    private void UpdateAiming()
    {
        aimTimer += Time.deltaTime;

        if (!isGrounded)
        {
            rb.gravityScale = aimGravityScale;

            float yVel = rb.linearVelocity.y > 0f
                ? 0f
                : rb.linearVelocity.y;

            float clampedY = Mathf.Max(yVel, -maxAimFallSpeed);
            rb.linearVelocity = new Vector2(0f, clampedY);
        }

        bool isKeyboardScheme =
            playerInput != null &&
            playerInput.currentControlScheme != null &&
            (
                playerInput.currentControlScheme.ToLower().Contains("keyboard") ||
                playerInput.currentControlScheme.ToLower().Contains("mouse")
            );

        if (isKeyboardScheme && Mouse.current != null && Camera.main != null)
        {
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPos.x,
                    mouseScreenPos.y,
                    -Camera.main.transform.position.z
                )
            );

            Vector2 dirToMouse =
                (Vector2)mouseWorldPos - (Vector2)transform.position;

            if (dirToMouse.sqrMagnitude > 0.01f)
                currentAimDir = dirToMouse.normalized;
        }
        else
        {
            Vector2 moveInput = moveAction != null
                ? moveAction.ReadValue<Vector2>()
                : Vector2.zero;

            if (moveInput.sqrMagnitude > 0.01f)
                currentAimDir = moveInput.normalized;
        }

        Vector3 targetPosition =
            transform.position + (Vector3)(currentAimDir * aimOrbitRadius);

        if (aimArrow != null)
        {
            aimArrow.transform.position = Vector3.Lerp(
                aimArrow.transform.position,
                targetPosition,
                Time.deltaTime * 20f
            );

            float angle =
                Mathf.Atan2(currentAimDir.y, currentAimDir.x) * Mathf.Rad2Deg;

            aimArrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (aimTimer >= maxAimTime)
            ReleaseAim();
    }

    private void ReleaseAim()
    {
        if (!isAiming)
            return;

        isAiming = false;
        rb.gravityScale = originalGravity;

        if (aimArrow != null)
            aimArrow.SetActive(false);

        if (heldBomb != null)
        {
            float chargeRatio = Mathf.Clamp01(aimTimer / maxAimTime);

            float finalThrowSpeed = Mathf.Lerp(
                minThrowSpeed,
                maxThrowSpeed,
                chargeRatio
            );

            Collider2D playerCol = GetComponent<Collider2D>();

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySound("Arremesar bomba");

            heldBomb.Throw(currentAimDir, playerCol, finalThrowSpeed);
            heldBomb = null;

            ApplyThrowKnockback(currentAimDir, chargeRatio);
        }
    }

    private void ApplyThrowKnockback(
        Vector2 throwDirection,
        float chargeRatio)
    {
        if (throwKnockbackForce <= 0f)
            return;

        Vector2 knockbackDir = -throwDirection.normalized;

        float currentKnockbackForce = scaleKnockbackWithCharge
            ? throwKnockbackForce * Mathf.Lerp(0.5f, 1f, chargeRatio)
            : throwKnockbackForce;

        if (playerEffects != null)
            playerEffects.TriggerThrow();

        StartKnockback(
            knockbackDir,
            currentKnockbackForce,
            knockbackDuration
        );
    }

    private IEnumerator KnockbackRoutine(
        Vector2 knockbackDir,
        float force,
        float duration)
    {
        isKnockbacked = true;
        isWallSliding = false;

        if (animator != null)
            animator.SetBool("IsWallSliding", false);

        rb.linearVelocity = knockbackDir.normalized * force;

        yield return new WaitForSeconds(duration);

        isKnockbacked = false;
        knockbackCoroutine = null;
    }

    private void FixedUpdate()
    {
        if (isDead || isDashing || isAiming)
            return;

        if (isKnockbacked)
        {
            ApplyCustomGravity();
            return;
        }

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0f, -wallSlidingSpeed);
        }
        else
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float speedDif = targetSpeed - rb.linearVelocity.x;

            float accelRate = Mathf.Abs(targetSpeed) > 0.01f
                ? acceleration
                : deceleration;

            float movement = speedDif * accelRate;
            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }

        ApplyCustomGravity();

        if (animator != null)
        {
            float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0f && !isWallSliding)
        {
            rb.linearVelocity += Vector2.up
                * Physics2D.gravity.y
                * (fallMultiplier - 1f)
                * Time.fixedDeltaTime;
        }
        else if (
            rb.linearVelocity.y > 0f &&
            jumpAction != null &&
            !jumpAction.IsPressed() &&
            !isWallSliding)
        {
            rb.linearVelocity += Vector2.up
                * Physics2D.gravity.y
                * (lowJumpMultiplier - 1f)
                * Time.fixedDeltaTime;
        }
    }

    private void CheckSurroundings()
    {
        bool wasGrounded = isGrounded;

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }
        else
        {
            isGrounded = false;
        }

        if (!wasGrounded && isGrounded)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySound("Pouso");

            if (playerEffects != null)
                playerEffects.TriggerLand();

            PlayDust();
        }

        if (isGrounded)
            extraJumps = maxExtraJumps;

        if (wallCheck != null)
        {
            Vector2 direction = facingRight
                ? Vector2.right
                : Vector2.left;

            isTouchingWall = Physics2D.Raycast(
                wallCheck.position,
                direction,
                wallCheckDistance,
                wallLayer
            );
        }
        else
        {
            isTouchingWall = false;
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isDead || isDashing || isAiming || isKnockbacked)
            return;

        Jump();
    }

    private void Jump()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("Pulo");

        if (isWallSliding || (isTouchingWall && !isGrounded))
        {
            int wallDir = facingRight ? -1 : 1;

            rb.linearVelocity = new Vector2(
                wallJumpForce.x * wallDir,
                wallJumpForce.y
            );

            isWallSliding = false;

            if (animator != null)
                animator.SetBool("IsWallSliding", false);

            SetFacing(!facingRight);

            if (playerEffects != null)
                playerEffects.TriggerJump();

            PlayDust();
            return;
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            if (playerEffects != null)
                playerEffects.TriggerJump();

            PlayDust();
        }
        else if (extraJumps > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            extraJumps--;

            if (playerEffects != null)
                playerEffects.TriggerJump();

            PlayDust();
        }
    }

    private void HandleWallSlide()
    {
        bool shouldWallSlide =
            isTouchingWall &&
            !isGrounded &&
            !isKnockbacked &&
            !isDashing &&
            !isAiming &&
            rb.linearVelocity.y <= 0f;

        if (shouldWallSlide)
        {
            isWallSliding = true;
            extraJumps = maxExtraJumps;
        }
        else
        {
            isWallSliding = false;
        }

        if (animator != null)
        {
            animator.SetBool("IsWallSliding", isWallSliding);
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (isDead)
            return;

        if (canDash && !isDashing && !isAiming && !isKnockbacked)
            StartCoroutine(PerformDash());
    }

    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;
        isWallSliding = false;

        if (animator != null)
            animator.SetBool("IsWallSliding", false);

        float originalGrav = rb.gravityScale;
        rb.gravityScale = 0f;

        PlayDust();

        Vector2 moveInput = moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : Vector2.zero;

        Vector2 dashDir;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            dashDir = moveInput.normalized;
        }
        else
        {
            dashDir = new Vector2(facingRight ? 1f : -1f, 0f);
        }

        if (dashDir.x > 0f && !facingRight)
            SetFacing(true);
        else if (dashDir.x < 0f && facingRight)
            SetFacing(false);

        rb.linearVelocity = dashDir * dashPower;

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = originalGrav;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
    }

    public void HoldBomb(Bomb bomb)
    {
        heldBomb = bomb;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound("Pegar bomba");
    }

    public void DropBomb()
    {
        heldBomb = null;

        if (isAiming)
        {
            isAiming = false;
            rb.gravityScale = originalGravity;

            if (aimArrow != null)
                aimArrow.SetActive(false);
        }
    }

    private void PlayDust()
    {
        if (Dust != null)
            Dust.Play();
    }

    private void SetFacing(bool faceRight)
    {
        if (facingRight == faceRight)
        {
            ApplyFacingDirection();
            return;
        }

        facingRight = faceRight;
        ApplyFacingDirection();
    }

    private void ApplyFacingDirection()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;

        if (wallCheck != null)
        {
            Vector3 pos = wallCheckInitialLocalPosition;

            pos.x = Mathf.Abs(wallCheckInitialLocalPosition.x) *
                    (facingRight ? 1f : -1f);

            wallCheck.localPosition = pos;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;

            Vector3 direction = facingRight
                ? Vector3.right
                : Vector3.left;

            Gizmos.DrawRay(
                wallCheck.position,
                direction * wallCheckDistance
            );
        }
    }
}