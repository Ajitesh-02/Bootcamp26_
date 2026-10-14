using UnityEngine;

public class FallingBallSaharsh : MonoBehaviour
{
    public Vector3 dropPosition = new Vector3(43.71f, -7.62f, 0f);

    [Header("Fall Speed")]
    public float gravityScale = 5f;       // higher = accelerates faster
    public float initialDownSpeed = 15f;  // instant speed at the moment of drop
    public float maxFallSpeed = 40f;      // speed cap (set very high to remove)

    private Rigidbody2D rb;
    private bool hasFallen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    public void Drop()
    {
        if (hasFallen) return;
        hasFallen = true;

        transform.position = dropPosition;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.None;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(0f, -initialDownSpeed);
        Debug.Log("Ball dropped");
    }

    void FixedUpdate()
    {
        if (!hasFallen) return;

        // Clamp downward speed so it doesn't grow forever
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }
}