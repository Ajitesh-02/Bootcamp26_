using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Items the player picked up in the current dungeon (Key, Crystal).
/// Cleared automatically every time a new scene is loaded.
/// </summary>
public static class PlayerInventory
{
    private static readonly HashSet<CollectibleType> items = new HashSet<CollectibleType>();

    public static bool Has(CollectibleType type)
    {
        return type == CollectibleType.None || items.Contains(type);
    }

    public static void Add(CollectibleType type, Sprite icon, Color tint)
    {
        if (type == CollectibleType.None) return;
        if (items.Add(type))
        {
            GameHUD.Instance.AddInventoryIcon(type, icon, tint);
        }
    }

    /// <summary>True if this collider belongs to the player.</summary>
    public static bool IsPlayer(Collider2D other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        items.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) items.Clear();
    }
}
