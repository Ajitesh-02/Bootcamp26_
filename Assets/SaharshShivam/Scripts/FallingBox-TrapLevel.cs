using UnityEngine;

public class FallingBox : MonoBehaviour
{
    [Header("Fall Speed Settings")]
    [Tooltip("Higher gravity pulls the box down faster")]
    public float fallGravityScale = 8f; // Increased fall gravity (was 3f)

    [Tooltip("Instant initial downward push when triggered")]
    public float initialDownVelocity = 10f;

    private Rigidbody2D rb;
    private bool hasFallen = false;
    private bool hasLanded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Drop()
    {
        if (hasFallen) return;
        hasFallen = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallGravityScale;

            // Lock horizontal drift and rotation so it falls completely straight
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

            // Give it an instant downward burst so there is no delay at the start
            rb.linearVelocity = new Vector2(0f, -initialDownVelocity);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasFallen || hasLanded) return;

        if (collision.gameObject.CompareTag("Ground") || 
            collision.gameObject.name.Contains("Platform") || 
            collision.gameObject.name.Contains("Wall") ||
            collision.gameObject.name.Contains("StandTrap"))
        {
            hasLanded = true;

            // Completely freeze so the player can't move or nudge it
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }
}