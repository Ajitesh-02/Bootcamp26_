using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MobProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool hasHit;

    public void Init(float direction)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = new Vector2(direction * speed, 0f);

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * direction;
        transform.localScale = s;

        Destroy(gameObject, lifetime);
    }

    // Fires when the projectile collider has Is Trigger ON
    private void OnTriggerEnter2D(Collider2D other) { HandleHit(other); }

    // Fires when the projectile collider has Is Trigger OFF
    private void OnCollisionEnter2D(Collision2D collision) { HandleHit(collision.collider); }

    private void HandleHit(Collider2D other)
    {
        if (hasHit) return;

        bool isPlayer = other.CompareTag("Player") || other.transform.root.CompareTag("Player");
        bool isGround = (groundLayer.value & (1 << other.gameObject.layer)) != 0;

        // Diagnostic: shows exactly what the projectile touched
        Debug.Log($"Projectile touched '{other.name}' | layer: {LayerMask.LayerToName(other.gameObject.layer)} | player: {isPlayer} | ground: {isGround}");

        if (isPlayer || isGround)
        {
            if (isPlayer) Debug.Log("Player hit");   // replace with your damage call
            Vanish();
        }
    }

    private void Vanish()
    {
        hasHit = true;
        rb.linearVelocity = Vector2.zero;

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        Destroy(gameObject);
    }
}