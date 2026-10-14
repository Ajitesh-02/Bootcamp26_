using UnityEngine;

public enum CollectibleType
{
    None = 0,
    Key = 1,
    Crystal = 2
}

/// <summary>
/// A pickup dropped by an Elite. Created at runtime by Collectible.Spawn(),
/// so no prefab is needed. If no sprite is given it shows a coloured square.
/// </summary>
public class Collectible : MonoBehaviour
{
    public CollectibleType type;
    public float pickupDelay = 0.4f;   // can't be picked up instantly when it pops out
    public float bobHeight = 0.12f;
    public float bobSpeed = 3f;

    private Vector3 basePos;
    private float spawnTime;
    private bool collected;
    private Sprite iconSprite;
    private Color iconTint = Color.white;

    /// <summary>Default colour of the placeholder square for each item.</summary>
    public static Color DefaultColor(CollectibleType type)
    {
        switch (type)
        {
            case CollectibleType.Key: return new Color(1f, 0.82f, 0.1f);      // gold
            case CollectibleType.Crystal: return new Color(0.3f, 0.9f, 1f);   // cyan
            default: return Color.white;
        }
    }

    public static Collectible Spawn(CollectibleType type, Sprite sprite, float worldSize, Vector3 position, Material material = null)
    {
        GameObject go = new GameObject(type + " Pickup");
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        bool placeholder = sprite == null;
        sr.sprite = placeholder ? GameHUD.WhiteSprite : sprite;
        sr.color = placeholder ? DefaultColor(type) : Color.white;
        sr.sortingOrder = 20;
        if (material != null) sr.sharedMaterial = material;

        // Scale so the biggest side of the sprite is 'worldSize' units, whatever its pixels-per-unit is
        Vector2 size = sr.sprite.bounds.size;
        float largest = Mathf.Max(size.x, size.y);
        if (largest > 0.0001f) go.transform.localScale = Vector3.one * (worldSize / largest);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = largest * 0.6f;

        Collectible c = go.AddComponent<Collectible>();
        c.type = type;
        c.iconSprite = sr.sprite;
        c.iconTint = sr.color;
        return c;
    }

    private void Start()
    {
        basePos = transform.position;
        spawnTime = Time.time;
    }

    private void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
    }

    private void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    private void OnTriggerStay2D(Collider2D other) { TryCollect(other); }

    private void TryCollect(Collider2D other)
    {
        if (collected || Time.time < spawnTime + pickupDelay) return;
        if (!PlayerInventory.IsPlayer(other)) return;

        collected = true;
        PlayerInventory.Add(type, iconSprite, iconTint);

        switch (type)
        {
            case CollectibleType.Key:
                GameHUD.Instance.ShowMessage("You got the KEY! The door is now unlocked.");
                break;
            case CollectibleType.Crystal:
                GameHUD.Instance.ShowMessage("You got the CRYSTAL! Find the exit to escape.");
                break;
        }

        Destroy(gameObject);
    }
}
