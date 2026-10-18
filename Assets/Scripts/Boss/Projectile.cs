
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Configurações do Projétil")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 5f;

    [Header("Efeito de Explosão")]
    [SerializeField] private GameObject explosionParticlesPrefab;
    [SerializeField] private float explosionDestroyDelay = 2f;

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
        if (hasHit || other == null)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player == null)
            player = other.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            player.TakeBossDamage(damage, transform.position);
            ExplodeAndDestroy();
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("Wall");

        if (other.CompareTag("Ground") ||
            other.CompareTag("Wall") ||
            (groundLayer >= 0 && other.gameObject.layer == groundLayer) ||
            (wallLayer >= 0 && other.gameObject.layer == wallLayer))
        {
            ExplodeAndDestroy();
        }
    }

    private void ExplodeAndDestroy()
    {
        if (hasHit)
            return;

        hasHit = true;

        if (explosionParticlesPrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionParticlesPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(explosion, explosionDestroyDelay);
        }

        Destroy(gameObject);
    }
}
