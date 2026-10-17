using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Componentes")]
    private Rigidbody2D rb;
    private PlayerInput playerInput;

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

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = baseGravity;
        originalGravity = baseGravity;
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null && playerInput.actions != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            dashAction = playerInput.actions["Dash"];
            attackAction = playerInput.actions["Attack"];
        }

        if (aimArrow != null) aimArrow.SetActive(false);
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
        if (isDashing) return;

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

        StartCoroutine(KnockbackRoutine(knockbackDir, currentKnockbackForce));
    }

    private IEnumerator KnockbackRoutine(Vector2 knockbackDir, float force)
    {
        isKnockbacked = true;

        rb.linearVelocity = knockbackDir * force;

        yield return new WaitForSeconds(knockbackDuration);

        isKnockbacked = false;
    }

    private void FixedUpdate()
    {
        if (isDashing || isAiming) return;

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
        if (isDashing || isAiming || isKnockbacked) return;
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