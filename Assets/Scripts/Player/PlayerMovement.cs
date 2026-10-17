using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public PlayerID ID => playerID;
    public Color PlayerColor => playerID == PlayerID.Player1 ? player1Color : player2Color;

    [Header("Sistema de Vidas & Espírito")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;
    [SerializeField] private UnityEngine.UI.Image[] lifeIcons;
    [SerializeField] private float ghostDuration = 15f;
    [SerializeField] private float ghostFloatSpeed = 0.8f;
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

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        rb.gravityScale = baseGravity;
        originalGravity = baseGravity;
        playerInput = GetComponent<PlayerInput>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

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

        if (aimArrow != null) aimArrow.SetActive(false);
    }

    private void AutoAssignPlayerID()
    {
        PlayerMovement[] existingPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        bool hasPlayer1 = false;

        foreach (PlayerMovement p in existingPlayers)
        {
            if (p != this && p.ID == PlayerID.Player1)
            {
                hasPlayer1 = true;
                break;
            }
        }

        if (hasPlayer1)
        {
            playerID = PlayerID.Player2;
        }
        else
        {
            playerID = PlayerID.Player1;
        }

        UpdatePlayerColor();
    }

    public void SetPlayerID(PlayerID newID)
    {
        playerID = newID;
        UpdatePlayerColor();
    }

    public void UpdatePlayerColor()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Color c = PlayerColor;
            c.a = spriteRenderer.color.a;
            spriteRenderer.color = c;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.UpdateHealth(playerID, currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateLifeUI()
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null)
            {
                lifeIcons[i].gameObject.SetActive(i < currentHealth);
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (heldBomb != null)
        {
            heldBomb.DropByDeadPlayer();
            heldBomb = null;
        }

        if (aimArrow != null) aimArrow.SetActive(false);

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement p in allPlayers)
        {
            if (p != this && p.GetComponent<Collider2D>() != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, p.GetComponent<Collider2D>(), true);
            }
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        StartCoroutine(GhostFloatRoutine());
    }

    private IEnumerator GhostFloatRoutine()
    {
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        float alpha = startColor.a;

        while (isDead)
        {
            elapsed += Time.deltaTime;
            transform.Translate(Vector3.up * ghostFloatSpeed * Time.deltaTime);

            if (spriteRenderer != null)
            {
                alpha -= Time.deltaTime / ghostDuration;
                alpha = Mathf.Clamp(alpha, 0.05f, 1f);
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }

            yield return null;
        }
    }

    public void Revive()
    {
        if (!isDead) return;
        isDead = false;

        currentHealth = maxHealth;
        UpdateLifeUI();

        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.UpdateHealth(playerID, currentHealth);
        }

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement p in allPlayers)
        {
            if (p != this && p.GetComponent<Collider2D>() != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, p.GetComponent<Collider2D>(), false);
            }
        }

        rb.gravityScale = baseGravity;

        if (spriteRenderer != null)
        {
            Color c = PlayerColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement otherPlayer = collision.gameObject.GetComponent<PlayerMovement>();
            if (otherPlayer != null && !otherPlayer.IsDead)
            {
                Vector2 pushDir = (transform.position - collision.transform.position).normalized;
                if (pushDir == Vector2.zero) pushDir = Vector2.right;
                pushDir.y = 0.2f; // Dá um leve impulso para cima também para soltar do chão

                StartCoroutine(KnockbackRoutine(pushDir, playerKnockbackForce, playerKnockbackDuration));
            }
        }
    }

    private void OnEnable()
    {
        if (jumpAction != null) jumpAction.performed += OnJumpPerformed;
        if (dashAction != null) dashAction.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        if (jumpAction != null) jumpAction.performed -= OnJumpPerformed;
        if (dashAction != null) dashAction.performed -= OnDashPerformed;
    }

    private void Update()
    {
        if (isDead || isDashing) return;

        HandleBombAiming();

        if (isAiming) return;

        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        horizontalInput = Mathf.Round(moveInput.x);

        if (!isKnockbacked)
        {
            if (horizontalInput > 0 && !facingRight) Flip();
            else if (horizontalInput < 0 && facingRight) Flip();
        }

        CheckSurroundings();
        HandleWallSlide();
    }

    private void HandleBombAiming()
    {
        if (!IsHoldingBomb || attackAction == null) return;

        if (attackAction.IsPressed())
        {
            if (!isAiming)
            {
                StartAiming();
            }

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
            aimArrow.transform.position = transform.position + (Vector3)(currentAimDir * aimOrbitRadius);
        }
    }

    private void UpdateAiming()
    {
        aimTimer += Time.deltaTime;

        if (!isGrounded)
        {
            rb.gravityScale = aimGravityScale;

            float yVel = rb.linearVelocity.y > 0 ? 0f : rb.linearVelocity.y;
            float clampedY = Mathf.Max(yVel, -maxAimFallSpeed);
            rb.linearVelocity = new Vector2(0f, clampedY);
        }

        bool isKeyboardScheme = playerInput != null && playerInput.currentControlScheme != null &&
                                (playerInput.currentControlScheme.ToLower().Contains("keyboard") ||
                                 playerInput.currentControlScheme.ToLower().Contains("mouse"));

        if (isKeyboardScheme && Mouse.current != null && Camera.main != null)
        {
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -Camera.main.transform.position.z));
            Vector2 dirToMouse = (Vector2)mouseWorldPos - (Vector2)transform.position;

            if (dirToMouse.sqrMagnitude > 0.01f)
            {
                currentAimDir = dirToMouse.normalized;
            }
        }
        else
        {
            Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                currentAimDir = moveInput.normalized;
            }
        }

        Vector3 targetPosition = transform.position + (Vector3)(currentAimDir * aimOrbitRadius);

        if (aimArrow != null)
        {
            aimArrow.transform.position = Vector3.Lerp(aimArrow.transform.position, targetPosition, Time.deltaTime * 20f);

            float angle = Mathf.Atan2(currentAimDir.y, currentAimDir.x) * Mathf.Rad2Deg;
            aimArrow.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (aimTimer >= maxAimTime)
        {
            ReleaseAim();
        }
    }

    private void ReleaseAim()
    {
        if (!isAiming) return;

        isAiming = false;
        rb.gravityScale = originalGravity;
        if (aimArrow != null) aimArrow.SetActive(false);

        if (heldBomb != null)
        {
            float chargeRatio = Mathf.Clamp01(aimTimer / maxAimTime);
            float finalThrowSpeed = Mathf.Lerp(minThrowSpeed, maxThrowSpeed, chargeRatio);

            Collider2D playerCol = GetComponent<Collider2D>();

            heldBomb.Throw(currentAimDir, playerCol, finalThrowSpeed);
            heldBomb = null;

            ApplyThrowKnockback(currentAimDir, chargeRatio);
        }
    }

    private void ApplyThrowKnockback(Vector2 throwDirection, float chargeRatio)
    {
        if (throwKnockbackForce <= 0f) return;

        Vector2 knockbackDir = -throwDirection.normalized;

        float currentKnockbackForce = scaleKnockbackWithCharge
            ? throwKnockbackForce * Mathf.Lerp(0.5f, 1f, chargeRatio)
            : throwKnockbackForce;

        if (playerEffects != null)
        {
            playerEffects.TriggerThrow();
        }

        StartCoroutine(KnockbackRoutine(knockbackDir, currentKnockbackForce, knockbackDuration));
    }

    private IEnumerator KnockbackRoutine(Vector2 knockbackDir, float force, float duration)
    {
        isKnockbacked = true;

        rb.linearVelocity = knockbackDir * force;

        yield return new WaitForSeconds(duration);

        isKnockbacked = false;
    }

    private void FixedUpdate()
    {
        if (isDead || isDashing || isAiming) return;

        if (isKnockbacked)
        {
            ApplyCustomGravity();
            return;
        }

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0f, Mathf.Max(rb.linearVelocity.y, -wallSlidingSpeed));
        }
        else
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float speedDif = targetSpeed - rb.linearVelocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
            float movement = speedDif * accelRate;

            rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }

        ApplyCustomGravity();
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0 && !isWallSliding)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && jumpAction != null && !jumpAction.IsPressed() && !isWallSliding)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void CheckSurroundings()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (!wasGrounded && isGrounded)
        {
            if (playerEffects != null) playerEffects.TriggerLand();
            PlayDust();
        }

        if (isGrounded)
        {
            extraJumps = maxExtraJumps;
        }

        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        isTouchingWall = Physics2D.Raycast(wallCheck.position, direction, wallCheckDistance, wallLayer);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isDead || isDashing || isAiming || isKnockbacked) return;
        Jump();
    }

    private void Jump()
    {
        if (isWallSliding || (isTouchingWall && !isGrounded))
        {
            int wallDir = facingRight ? -1 : 1;
            rb.linearVelocity = new Vector2(wallJumpForce.x * wallDir, wallJumpForce.y);
            Flip();

            if (playerEffects != null) playerEffects.TriggerJump();
            PlayDust();
            return;
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (playerEffects != null) playerEffects.TriggerJump();
            PlayDust();
        }
        else if (extraJumps > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            extraJumps--;
            if (playerEffects != null) playerEffects.TriggerJump();
            PlayDust();
        }
    }

    private void HandleWallSlide()
    {
        if (isTouchingWall && !isGrounded && horizontalInput != 0 && !isKnockbacked)
        {
            if (!isWallSliding && playerEffects != null)
            {
                playerEffects.TriggerWallSlide();
            }

            isWallSliding = true;
            extraJumps = maxExtraJumps;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (isDead) return;

        if (canDash && !isDashing && !isAiming && !isKnockbacked)
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;

        float originalGrav = rb.gravityScale;
        rb.gravityScale = 0f;

        PlayDust();

        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector2 dashDir;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            dashDir = moveInput.normalized;
        }
        else
        {
            dashDir = new Vector2(facingRight ? 1f : -1f, 0f);
        }

        if (dashDir.x > 0 && !facingRight) Flip();
        else if (dashDir.x < 0 && facingRight) Flip();

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
    }

    public void DropBomb()
    {
        heldBomb = null;
        if (isAiming) ReleaseAim();
    }

    private void PlayDust()
    {
        if (Dust != null)
        {
            Dust.Play();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector3 direction = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawRay(wallCheck.position, direction * wallCheckDistance);
        }
    }
}