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

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore environment or player collisions
        if (other.CompareTag("Ground") || other.CompareTag("Spikes") || other.CompareTag("Player"))
        {
            return;
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