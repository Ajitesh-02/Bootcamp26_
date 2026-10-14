using UnityEngine;
using System.Collections;

public class BeamFall : MonoBehaviour
{
    [Header("References")]
    public Transform ball;                   // Drag Ball here (auto-finds "Ball" if empty)

    [Header("Trigger")]
    public float triggerY = -15f;            // World Y of the ball that starts the fall

    [Header("Fall")]
    public float rotationAngle = 90f;        // Total rotation in degrees
    public float fallDuration = 2f;          // Seconds until the beam first hits the platform
    public bool counterClockwise = true;     // true = falls LEFT, false = falls RIGHT

    [Header("Rebound")]
    public int bounceCount = 2;              // Number of rebounds
    public float firstBounceAngle = 8f;      // How many degrees it bounces back up the first time
    public float firstBounceDuration = 0.3f; // Time of the first rebound (up and down)
    [Range(0.1f, 0.9f)]
    public float bounceDecay = 0.4f;         // Each rebound is this fraction of the previous one

    private bool triggered = false;
    private Quaternion startRot;
    private float sign;

    void Start()
    {
        if (ball == null)
        {
            GameObject b = GameObject.Find("Ball");
            if (b != null) ball = b.transform;
        }
        if (ball == null)
            Debug.LogError("BeamFall: Ball not assigned and no object named 'Ball' found!");

        startRot = transform.rotation;
        sign = counterClockwise ? 1f : -1f;
    }

    void Update()
    {
        if (triggered || ball == null) return;

        if (ball.position.y <= triggerY)
        {
            triggered = true;
            StartCoroutine(FallRoutine());
        }
    }

    void SetAngle(float angle)
    {
        transform.rotation = startRot * Quaternion.Euler(0f, 0f, sign * angle);
    }

    IEnumerator FallRoutine()
    {
        // Phase 1: accelerating fall (constant acceleration => angle proportional to t^2)
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            SetAngle(rotationAngle * t * t);
            yield return null;
        }
        SetAngle(rotationAngle);

        // Phase 2: rebounds, each smaller and shorter than the last
        float bounceAngle = firstBounceAngle;
        float bounceTime = firstBounceDuration;

        for (int i = 0; i < bounceCount; i++)
        {
            elapsed = 0f;
            while (elapsed < bounceTime)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / bounceTime);
                // Parabola: 0 -> peak -> 0 (up and back down under "gravity")
                float offset = bounceAngle * 4f * u * (1f - u);
                SetAngle(rotationAngle - offset);
                yield return null;
            }
            SetAngle(rotationAngle);

            bounceAngle *= bounceDecay;
            bounceTime *= Mathf.Sqrt(bounceDecay);   // smaller bounces take less time
        }

        SetAngle(rotationAngle);   // final resting position, exactly 90°
    }
}