using UnityEngine;

public class AirSlash : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float lifeTime = 5f;

    [Header("Impact Effects (Optional)")]
    public GameObject impactVFXPrefab; // Drag explosion/burst particle here if you have one

    private Vector2 direction = Vector2.right;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Automatically destroy after 5 seconds if no collision occurs
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 travelDirection)
    {
        direction = travelDirection.normalized;

        // Flip the sprite if travelling left
        if (direction.x < 0)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                // Alternative: rotate 180 degrees along Y-axis if using scale/transform flipping
                transform.Rotate(0f, 180f, 0f);
            }
        }
    }

    private void Update()
    {
        // Move the air slash forward every frame
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore collision with the Player
        if (collision.CompareTag("Player")) return;

        // Burst / Destroy sequence when colliding with any object/collider
        BurstAndDestroy();
    }

    private void BurstAndDestroy()
    {
        // Spawn impact visual effects if assigned
        if (impactVFXPrefab != null)
        {
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the air slash projectile
        Destroy(gameObject);
    }
}