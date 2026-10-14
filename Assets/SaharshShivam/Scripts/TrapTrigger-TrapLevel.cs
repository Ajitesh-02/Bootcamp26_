using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TrapTrigger : MonoBehaviour
{
    [Header("Delay Settings")]
    [Tooltip("Delay in seconds before triggering actions. Set to 0 for instant.")]
    public float triggerDelay = 0f;

    [Header("Trap Actions")]
    public UnityEvent onTriggered;

    private bool triggered = false;

    private void CheckAndTrigger(GameObject obj)
    {
        if (triggered) return;

        if (obj.CompareTag("Player") || obj.name == "Player" || obj.GetComponentInParent<PlayerMovement>() != null)
        {
            triggered = true;
            StartCoroutine(ExecuteTrigger());
        }
    }

    private IEnumerator ExecuteTrigger()
    {
        if (triggerDelay > 0f)
        {
            yield return new WaitForSeconds(triggerDelay);
        }

        onTriggered?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckAndTrigger(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckAndTrigger(collision.gameObject);
    }
}