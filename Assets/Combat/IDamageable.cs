/// <summary>
/// Anything the player's attacks (melee, Air Slash, Lightning Bolt) can hurt.
/// EnemyAi and Elite implement this.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
    bool IsDead { get; }
}
