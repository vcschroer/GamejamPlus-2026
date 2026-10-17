using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 5f;

    private bool hasHit;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D other)
    {
        if (hasHit || other == null) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            player = other.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            player.TakeBossDamage(damage, transform.position);
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("Wall");

        if (other.CompareTag("Ground") || other.CompareTag("Wall") ||
            (groundLayer >= 0 && other.gameObject.layer == groundLayer) ||
            (wallLayer >= 0 && other.gameObject.layer == wallLayer))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
