using System.Collections;
using UnityEngine;

/// <summary>
/// Side-view enemy: Idle -> Walk (follow) -> Attack animation when near the player.
/// No damage or health yet. Player is found by TAG.
/// Requires: Rigidbody2D, Collider2D, Animator.
/// Animator parameters: float "Speed", trigger "Attack".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyAi : MonoBehaviour, IDamageable
{
    [Header("Target (found by tag)")]
    [SerializeField] private string playerTag = "Player";

    [Header("Following")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float detectRange = 8f;          // starts following inside this
    [SerializeField] private float maxHeightDifference = 2f;  // ignores player on a very different height

    [Header("Attack Animation")]
    [SerializeField] private float attackRange = 0.6f;        // collider-edge gap at which it stops and attacks
    [SerializeField] private float attackCooldown = 1f;       // time between attack animations
    [Tooltip("Seconds the enemy stands still while attacking. 0 = auto-detect from a clip with 'attack' in its name.")]
    [SerializeField] private float attackDuration = 0f;

    [Header("Sprite")]
    [Tooltip("Tick if your sprite artwork faces LEFT by default.")]
    [SerializeField] private bool spriteFacesLeft = false;

    [Header("Health")]
    [SerializeField] private int maxHealth = 60;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float deathFadeTime = 0.6f;

    [Header("Damage To Player")]
    [SerializeField] private int attackDamage = 10;
    [Tooltip("When in the attack animation the hit lands (0 = start, 1 = end).")]
    [Range(0f, 1f)]
    [SerializeField] private float hitMoment = 0.5f;
    [Tooltip("Extra reach on top of Attack Range, so a player stepping back slightly still gets hit.")]
    [SerializeField] private float hitTolerance = 0.5f;

    private int currentHealth;
    private bool isDead;
    private bool damageDealt;
    private float hitTime;
    private SpriteRenderer sr;
    private Color baseColor = Color.white;
    private PlayerMovement playerHealth;

    public bool IsDead => isDead;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;

    private Transform player;
    private Collider2D playerCol;

    private bool facingRight;
    private bool isAttacking;
    private float attackEndTime;
    private float nextAttackTime;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearDamping = 0f;   // Unity 2022 or older: rb.drag = 0f
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        currentHealth = maxHealth;

        facingRight = (transform.localScale.x > 0f) != spriteFacesLeft;

        if (attackDuration <= 0f) attackDuration = FindAttackClipLength();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (!FindPlayer() || (playerHealth != null && playerHealth.IsDead))
        {
            Stop();
            return;
        }

        // Stand still until the attack animation is over
        if (isAttacking)
        {
            // Deal damage once, at the hit moment of the attack animation
            if (!damageDealt && Time.time >= hitTime)
            {
                damageDealt = true;
                TryHitPlayer();
            }

            if (Time.time >= attackEndTime) isAttacking = false;
            else { Stop(); return; }
        }

        float dx = player.position.x - transform.position.x;
        float dy = Mathf.Abs(player.position.y - transform.position.y);

        // horizontal gap between collider edges (0 = touching)
        float gapX = Mathf.Max(0f, Mathf.Abs(dx) - (col.bounds.extents.x + playerCol.bounds.extents.x));

        bool playerInReach = gapX <= detectRange && dy <= maxHeightDifference;

        if (!playerInReach)
        {
            Stop();                                   // IDLE
        }
        else if (gapX > attackRange)
        {
            Face(dx);                                 // WALK toward the player
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * moveSpeed, rb.linearVelocity.y);
            anim.SetFloat(SpeedHash, 1f);
        }
        else
        {
            Face(dx);                                 // NEAR: attack animation
            Stop();

            if (Time.time >= nextAttackTime)
                StartAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        damageDealt = false;
        attackEndTime = Time.time + attackDuration;
        hitTime = Time.time + attackDuration * hitMoment;
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, attackDuration);
        anim.SetTrigger(AttackHash);
    }

    // Optional: add as an Animation Event on the last frame of the attack clip to unlock instantly.
    public void EndAttack()
    {
        isAttacking = false;
    }

    private float FindAttackClipLength()
    {
        if (anim.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            {
                if (clip.name.ToLower().Contains("attack"))
                    return clip.length;
            }
        }
        return 0.6f;   // fallback if no clip with "attack" in its name is found
    }

    // Looks the player up by tag; keeps trying until found.
    private bool FindPlayer()
    {
        if (player != null && playerCol != null) return true;

        GameObject p = GameObject.FindWithTag(playerTag);
        if (p == null) return false;

        playerCol = p.GetComponentInChildren<Collider2D>();
        if (playerCol == null) return false;

        player = p.transform;
        playerHealth = p.GetComponentInParent<PlayerMovement>();
        if (playerHealth == null) playerHealth = p.GetComponentInChildren<PlayerMovement>();
        return true;
    }

    // ---------------- Combat ----------------
    private void TryHitPlayer()
    {
        if (playerHealth == null || player == null || playerCol == null) return;

        float dx = player.position.x - transform.position.x;
        float dy = Mathf.Abs(player.position.y - transform.position.y);
        float gapX = Mathf.Max(0f, Mathf.Abs(dx) - (col.bounds.extents.x + playerCol.bounds.extents.x));

        if (gapX <= attackRange + hitTolerance && dy <= maxHeightDifference)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (sr != null) StartCoroutine(HitFlash());

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(0.12f);
        if (!isDead) sr.color = baseColor;
    }

    private void Die()
    {
        isDead = true;
        isAttacking = false;
        Stop();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        col.enabled = false;
        if (anim != null) anim.speed = 0f;
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float t = 0f;
        while (t < deathFadeTime && sr != null)
        {
            t += Time.deltaTime;
            Color c = hitFlashColor;
            c.a = 1f - t / deathFadeTime;
            sr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void Stop()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetFloat(SpeedHash, 0f);
    }

    private void Face(float dx)
    {
        if (Mathf.Abs(dx) < 0.1f) return;

        bool shouldFaceRight = dx > 0f;
        if (shouldFaceRight == facingRight) return;

        facingRight = shouldFaceRight;
        Vector3 s = transform.localScale;
        float sign = facingRight ? 1f : -1f;
        if (spriteFacesLeft) sign = -sign;
        s.x = Mathf.Abs(s.x) * sign;
        transform.localScale = s;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}