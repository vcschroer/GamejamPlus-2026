using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Movimentação & Patrulha")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 4f;
    private Transform currentTarget;
    private Rigidbody2D rb;

    [Header("Configurações de Vida do Boss")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    [Header("Configurações de Combate & Pausas")]
    [SerializeField] private float timeBetweenAttacks = 3f;
    [SerializeField] private float arcAttackDuration = 1.5f;
    [SerializeField] private float burstAttackDuration = 2.0f;
    private bool isActing = false;
    public bool IsActing => isActing;

    [Header("Ataque 1: Arco de Projéteis")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int projectileCount = 5;
    [SerializeField] private float arcAngle = 60f;

    [Header("Ataque 2: 4 Tiros Direcionados")]
    [SerializeField] private int burstShotCount = 4;
    [SerializeField] private float timeBetweenBurstShots = 0.4f;

    [Header("Referências Visuais")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        if (pointA != null && pointB != null) currentTarget = pointB;
        StartCoroutine(BossBehaviorLoop());
    }

    private void FixedUpdate()
    {
        if (!isActing) Patrol();
    }

    private void Patrol()
    {
        if (pointA == null || pointB == null || currentTarget == null) return;

        Vector2 newPos = Vector2.MoveTowards(rb.position,
            new Vector2(currentTarget.position.x, rb.position.y),
            moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (spriteRenderer != null)
        {
            if (currentTarget.position.x > transform.position.x) spriteRenderer.flipX = false;
            else if (currentTarget.position.x < transform.position.x) spriteRenderer.flipX = true;
        }

        if (Mathf.Abs(transform.position.x - currentTarget.position.x) < 0.2f)
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);
        Debug.Log("Boss tomou dano! Vida restante: " + currentHealth);
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
            isActing = true;
            int chosenAttack = Random.Range(0, 2);

            if (chosenAttack == 0) yield return StartCoroutine(ArcProjectileAttack());
            else yield return StartCoroutine(TargetedBurstAttack());

            isActing = false;
            yield return new WaitForSeconds(timeBetweenAttacks);
        }
    }

    private IEnumerator ArcProjectileAttack()
    {
        yield return new WaitForSeconds(0.3f);
        if (projectilePrefab != null && firePoint != null)
        {
            float startAngle = -arcAngle / 2f;
            float angleStep = projectileCount > 1 ? arcAngle / (projectileCount - 1) : 0f;

            for (int i = 0; i < projectileCount; i++)
            {
                float currentAngle = startAngle + angleStep * i;
                float facingDir = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
                Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);
                Vector2 direction = rotation * (facingDir > 0 ? Vector2.right : Vector2.left);

                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null) rbProj.linearVelocity = direction * 10f;
            }
        }
        yield return new WaitForSeconds(arcAttackDuration);
    }

    private IEnumerator TargetedBurstAttack()
    {
        yield return new WaitForSeconds(0.3f);
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        if (players.Length == 0) yield break;

        Transform targetPlayer = players[0].transform;
        for (int i = 0; i < burstShotCount; i++)
        {
            if (targetPlayer != null && firePoint != null && projectilePrefab != null)
            {
                Vector2 dirToPlayer = (targetPlayer.position - firePoint.position).normalized;
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null) rbProj.linearVelocity = dirToPlayer * 12f;
            }
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
        yield return new WaitForSeconds(burstAttackDuration);
    }
}
