using UnityEngine;

public enum CollectibleType
{
    None = 0,
    Key = 1,
    Crystal = 2,
    Coin = 3
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
    public int coinValue = 0;          // only used by coins
    public float magnetRadius = 1.6f;  // coins fly to the player inside this range

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
            case CollectibleType.Coin: return Color.white;                    // coin sprite is already gold
            default: return Color.white;
        }
    }

    public static Collectible Spawn(CollectibleType type, Sprite sprite, float worldSize, Vector3 position, Material material = null)
    {
        GameObject go = new GameObject(type + " Pickup");
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        bool placeholder = sprite == null;
        sr.sprite = placeholder ? (type == CollectibleType.Coin ? GameHUD.CoinSprite : GameHUD.WhiteSprite) : sprite;
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

    /// <summary>Drops 'total' coins as several small coins (each worth 'perCoin') around a point.</summary>
    public static void SpawnCoins(int total, int perCoin, Vector3 position, Material material = null)
    {
        if (total <= 0) return;
        perCoin = Mathf.Max(1, perCoin);
        int count = Mathf.CeilToInt((float)total / perCoin);
        int left = total;
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? 0f : Mathf.Lerp(-0.6f, 0.6f, (float)i / (count - 1));
            Collectible c = Spawn(CollectibleType.Coin, null, 0.4f, position + new Vector3(x, 0.1f * (i % 2), 0f), material);
            c.coinValue = Mathf.Min(perCoin, left);
            c.bobSpeed = 4f + i * 0.4f;
            left -= c.coinValue;
        }
    }

    private Transform magnetTarget;

    private void Start()
    {
        basePos = transform.position;
        spawnTime = Time.time;
    }

    private void Update()
    {
        // Coins drift towards a nearby player
        if (type == CollectibleType.Coin && Time.time >= spawnTime + pickupDelay)
        {
            if (magnetTarget == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) magnetTarget = p.transform;
            }
            if (magnetTarget != null)
            {
                Collider2D pc = magnetTarget.GetComponent<Collider2D>();
                Vector3 target = pc != null ? pc.bounds.center : magnetTarget.position;
                if (Vector2.Distance(basePos, target) < magnetRadius)
                {
                    basePos = Vector3.MoveTowards(basePos, target, 8f * Time.deltaTime);
                }
            }
        }

        transform.position = basePos + Vector3.up * Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
    }

    private void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    private void OnTriggerStay2D(Collider2D other) { TryCollect(other); }

    private void TryCollect(Collider2D other)
    {
        if (collected || Time.time < spawnTime + pickupDelay) return;
        if (!PlayerInventory.IsPlayer(other)) return;

        collected = true;

        if (type == CollectibleType.Coin)
        {
            PlayerProgress.AddCoins(coinValue);
            HitFeedback.CoinDing();
            Destroy(gameObject);
            return;
        }

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
