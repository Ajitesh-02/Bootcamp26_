using System.Collections;
using UnityEngine;

/// <summary>
/// A door that blocks the player until they carry the required item (the Key by default).
/// Touch it with the key and it fades away. Adds its own BoxCollider2D if it has none.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LockedDoor : MonoBehaviour
{
    [Header("Lock")]
    public CollectibleType requiredItem = CollectibleType.Key;
    public string lockedMessage = "The door is locked. Defeat the Elite to get the key.";
    public string openedMessage = "The door opens!";

    [Header("Opening")]
    public float fadeDuration = 0.6f;

    private SpriteRenderer sr;
    private Collider2D col;
    private bool isOpen;
    private float nextMessageTime;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            if (sr.sprite != null)
            {
                box.size = sr.sprite.bounds.size;
                box.offset = sr.sprite.bounds.center;
            }
            col = box;
        }
        col.isTrigger = false; // solid until opened
    }

    private void OnCollisionEnter2D(Collision2D collision) { CheckPlayer(collision.collider); }
    private void OnCollisionStay2D(Collision2D collision) { CheckPlayer(collision.collider); }

    private void CheckPlayer(Collider2D other)
    {
        if (isOpen || !PlayerInventory.IsPlayer(other)) return;

        if (PlayerInventory.Has(requiredItem))
        {
            Open();
        }
        else if (Time.time >= nextMessageTime)
        {
            GameHUD.Instance.ShowMessage(lockedMessage);
            nextMessageTime = Time.time + 2.5f;
        }
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        col.enabled = false;
        if (!string.IsNullOrEmpty(openedMessage)) GameHUD.Instance.ShowMessage(openedMessage);
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        Color start = sr.color;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            sr.color = new Color(start.r, start.g, start.b, Mathf.Lerp(start.a, 0f, t / fadeDuration));
            yield return null;
        }
        sr.enabled = false;
    }
}
