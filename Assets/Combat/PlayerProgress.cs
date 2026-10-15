using System;
using System.Collections.Generic;
using UnityEngine;

public enum Skill
{
    LightningBolt = 0,   // C
    AirSlash = 1         // J
}

/// <summary>
/// Everything that carries over between scenes for this play session:
/// health, coins, bought attacks, and whether the Guide's intro has been heard.
/// (Static = survives scene loads. Resets when you press Play again.)
/// </summary>
public static class PlayerProgress
{
    public const string HubScene = "NandiniStartScene";
    public const int MaxHealth = 100;

    public static int Health { get; private set; } = MaxHealth;
    public static int Coins { get; private set; }
    public static int Potions { get; private set; }
    public const int MaxPotions = 3;
    public static bool GuideIntroDone { get; set; }
    public static bool JustDied { get; set; }

    private static readonly HashSet<Skill> unlocked = new HashSet<Skill>();

    /// <summary>Fired whenever health or coins change (the HUD listens to this).</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        Health = MaxHealth;
        Coins = 0;
        Potions = 0;
        GuideIntroDone = false;
        JustDied = false;
        unlocked.Clear();
        Changed = null;
    }

    // ---------- Health ----------
    public static void SetHealth(int value)
    {
        Health = Mathf.Clamp(value, 0, MaxHealth);
        Changed?.Invoke();
    }

    public static void Heal(int amount) { SetHealth(Health + amount); }

    // ---------- Coins ----------
    public static void AddCoins(int amount)
    {
        Coins = Mathf.Max(0, Coins + amount);
        Changed?.Invoke();
    }

    public static bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        Changed?.Invoke();
        return true;
    }

    // ---------- Potions (bought from the Guide, drunk with Q) ----------
    public static bool AddPotion()
    {
        if (Potions >= MaxPotions) return false;
        Potions++;
        Changed?.Invoke();
        return true;
    }

    public static bool UsePotion()
    {
        if (Potions <= 0) return false;
        Potions--;
        Changed?.Invoke();
        return true;
    }

    // ---------- Skills ----------
    public static bool Has(Skill skill) { return unlocked.Contains(skill); }

    public static void Unlock(Skill skill)
    {
        unlocked.Add(skill);
        Changed?.Invoke();
    }

    // ---------- Death ----------
    /// <summary>Back to the Guide with full HP. Coins and bought attacks are kept.</summary>
    public static void OnDeath()
    {
        Health = MaxHealth;
        JustDied = true;
        Changed?.Invoke();
    }
}
