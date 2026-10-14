using UnityEngine;

public class Transporter : MonoBehaviour
{
    // Target teleport location
    [SerializeField] private Vector3 targetPosition = new Vector3(53.8300018f, -23.1399994f, 0f);

    // Tag used to identify the player
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            TeleportPlayer(other.gameObject);
        }
    }

    // Fallback in case your collider is not set to Trigger
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            TeleportPlayer(collision.gameObject);
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        // If the player uses a Rigidbody2D, update position directly and clear velocity
        if (player.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.position = targetPosition;
            rb.linearVelocity = Vector2.zero; // Resets momentum so the player doesn't carry old falling speed
        }
        else
        {
            player.transform.position = targetPosition;
        }
    }
}