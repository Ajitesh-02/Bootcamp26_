using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed;
    private readonly Vector3 respawnPosition = new Vector3(5.5999999f, -2.4000001f, 0f);

    public void Initialize(Vector2 direction, float speed)
    {
        moveDirection = direction;
        moveSpeed = speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, 4f);
    }

    void Update()
    {
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    // Handles trigger colliders (Is Trigger checked)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckHit(collision.gameObject);
    }

    // Handles regular solid colliders (Is Trigger unchecked)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckHit(collision.gameObject);
    }

    private void CheckHit(GameObject hitObj)
    {
        // Search in the hit object OR its parents for PlayerMovement or tag
        PlayerMovement playerMovement = hitObj.GetComponentInParent<PlayerMovement>();
        bool isPlayer = hitObj.CompareTag("Player") || hitObj.name == "Player" || playerMovement != null;

        if (isPlayer)
        {
            Debug.Log("<color=green>Arrow hit the player! Teleporting...</color>");

            Transform targetTransform = playerMovement != null ? playerMovement.transform : hitObj.transform;
            Rigidbody2D rb = targetTransform.GetComponent<Rigidbody2D>();

            // Teleport player
            targetTransform.position = respawnPosition;

            // Reset physics linearVelocity
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            Destroy(gameObject);
            return;
        }

        // Destroy arrow on walls / ground
        if (hitObj.name.Contains("Wall") || hitObj.name.Contains("Platform") || hitObj.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}