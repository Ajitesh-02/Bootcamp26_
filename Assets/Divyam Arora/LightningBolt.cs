using UnityEngine;

public class LightningBolt : MonoBehaviour
{
    [Header("Movement & Lifetime")]
    public float speed = 12f;
    public float lifeTime = 5f; // Automatically destroyed after 5 seconds if no enemy hit

    [Header("Combat")]
    public int damage = 10; // Editable in Inspector

    [Header("Impact Settings")]
    public GameObject explosionPrefab;
    public float explosionLifetime = 0.5f;

    private Vector2 direction = Vector2.right;

    public void Initialize(Vector2 travelDirection)
    {
        direction = travelDirection.normalized;

        // Flip sprite scale if shooting to the left
        if (direction.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // Destroy bolt after lifetime expires
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Moves forward along the X axis
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private bool hasHit;

    private void OnTriggerEnter2D(Collider2D other) { HandleHit(other); }

    // The bolt's collider is not a trigger, so enemy hits arrive here
    private void OnCollisionEnter2D(Collision2D collision) { HandleHit(collision.collider); }

    private void HandleHit(Collider2D other)
    {
        if (hasHit) return;

        // Ignore environment or player collisions
        if (other.CompareTag("Ground") || other.CompareTag("Spikes") || other.CompareTag("Player"))
        {
            return;
        }

        // Damage enemies, then explode
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null && !target.IsDead)
        {
            hasHit = true;
            target.TakeDamage(damage);
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionPrefab != null)
        {
            GameObject blast = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(blast, explosionLifetime);
        }

        Destroy(gameObject); // Destroys the bolt on impact
    }
}