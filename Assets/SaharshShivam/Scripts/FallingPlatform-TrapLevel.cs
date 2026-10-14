using UnityEngine;
using System.Collections;

public class FallingPlatformSaharsh : MonoBehaviour
{
    [Header("Settings")]
    public string playerTag = "Player";   // Tag on your Player object
    public float rotationAngle = 40f;     // Degrees to rotate
    public float duration = 0.8f;         // Seconds (keep under 1)
    public bool negativeDirection = true; // true = -Z (clockwise), false = +Z

    private bool triggered = false;

    void Start()
    {
        // Colliders on the child "Platform" don't send collision events to this parent,
        // so attach a small relay to every child collider.
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            PlatformTouchRelay relay = col.gameObject.AddComponent<PlatformTouchRelay>();
            relay.owner = this;
        }
    }

    public void OnPlatformTouched(Collision2D collision)
    {
        if (triggered) return;

        bool isPlayer = collision.gameObject.CompareTag(playerTag)
                        || collision.transform.root.name == "Player";
        if (!isPlayer) return;

        triggered = true;
        StartCoroutine(RotateRoutine());
    }

    IEnumerator RotateRoutine()
    {
        Quaternion startRot = transform.rotation;
        float angle = negativeDirection ? -rotationAngle : rotationAngle;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, angle);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t; // speeds up like it's giving way; use "t = t;" for linear
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        transform.rotation = endRot; // stops exactly at 40 degrees
    }
}

// Helper: forwards collisions from the child Platform to the parent script
public class PlatformTouchRelay : MonoBehaviour
{
    [HideInInspector] public FallingPlatformSaharsh owner;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (owner != null) owner.OnPlatformTouched(collision);
    }
}