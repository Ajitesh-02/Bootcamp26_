using UnityEngine;

/// <summary>
/// Elite melee enemy (side view): Idle -> Walk (follow) -> Attack animation when near the player.
/// No damage or health yet. Player is found by TAG.
/// Requires: Rigidbody2D, Collider2D, Animator.
/// Animator parameters: float "Speed", trigger "Attack".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Elite : MonoBehaviour
{
    [Header("Target (found by tag)")]
    [SerializeField] private string playerTag = "Player";

    [Header("Following")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectRange = 10f;         // starts following inside this
    [SerializeField] private float maxHeightDifference = 2f;  // ignores player on a very different height

    [Header("Attack Animation")]
    [SerializeField] private float attackRange = 0.8f;        // collider-edge gap at which it stops and attacks
    [SerializeField] private float attackCooldown = 1.5f;     // time between attack animations
    [Tooltip("Seconds the enemy stands still during Attack 1. 0 = auto-detect from a clip with 'attack' and '1' in its name.")]
    [SerializeField] private float attack1Duration = 0f;
    [Tooltip("Seconds the enemy stands still during Attack 2. 0 = auto-detect from a clip with 'attack' and '2' in its name.")]
    [SerializeField] private float attack2Duration = 0f;

    [Header("Sprite")]
    [Tooltip("Tick if your sprite artwork faces LEFT by default.")]
    [SerializeField] private bool spriteFacesLeft = false;
    [Tooltip("Tick if ONLY the walking animation faces the wrong way. Flips the sprite while walking.")]
    [SerializeField] private bool flipWhileWalking = true;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;

    private Transform player;
    private Collider2D playerCol;

    [Header("Debug (read only)")]
    [SerializeField] private bool isPlayerInRange;   // true while the player is inside detect range and height limit
    public bool IsPlayerInRange => isPlayerInRange;  // other scripts can read this

    private bool isMoving;   // true only when the script itself is walking the Elite

    private bool facingRight;
    private bool isAttacking;
    private float attackEndTime;
    private float nextAttackTime;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int PlayerInRangeHash = Animator.StringToHash("PlayerInRange");
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearDamping = 0f;

        facingRight = (transform.localScale.x > 0f) != spriteFacesLeft;

        if (attack1Duration <= 0f) attack1Duration = FindAttackClipLength(1);
        if (attack2Duration <= 0f) attack2Duration = FindAttackClipLength(2);
    }

    // Walk animation follows the REAL movement, so it can never walk while the Elite is stuck or stopped.
    private void Update()
    {
        // Walk only if the script is moving it AND it really moves (ignores pushes from the player/other bodies)
        bool reallyWalking = isMoving && Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim.SetFloat(SpeedHash, reallyWalking ? 1f : 0f);
        anim.SetBool(PlayerInRangeHash, isPlayerInRange);   // Animator bool for Idle -> Walk
    }

    private void FixedUpdate()
    {
        if (!FindPlayer())
        {
            isPlayerInRange = false;
            Stop();
            return;
        }

        // Stand still until the attack animation is over
        if (isAttacking)
        {
            if (Time.time >= attackEndTime) isAttacking = false;
            else { Stop(); return; }
        }

        float dx = playerCol.bounds.center.x - col.bounds.center.x;   // collider centers, not pivots
        float dy = Mathf.Abs(player.position.y - transform.position.y);

        // horizontal gap between collider edges (0 = touching). If player has no collider fall back to center distance.
        float sumExtentsX = 0f;
        if (playerCol != null) sumExtentsX = col.bounds.extents.x + playerCol.bounds.extents.x;
        float gapX = Mathf.Max(0f, Mathf.Abs(dx) - sumExtentsX);

        // Use center distance for detection if colliders are missing or unreliable
        bool playerInReach = Mathf.Abs(dx) <= detectRange && dy <= maxHeightDifference;
        isPlayerInRange = playerInReach;

        if (!playerInReach)
        {
            Stop();                                   // IDLE
        }
        else if (gapX > attackRange)
        {
            Face(dx);                                 // WALK toward the player
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * moveSpeed, rb.linearVelocity.y);
            isMoving = true;
            if (sr != null) sr.flipX = flipWhileWalking;   // walking sprite only
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
        // Randomly pick one of the two attacks
        bool useFirst = Random.value < 0.5f;
        float duration = useFirst ? attack1Duration : attack2Duration;

        isAttacking = true;
        attackEndTime = Time.time + duration;
        nextAttackTime = Time.time + Mathf.Max(attackCooldown, duration);
        anim.SetTrigger(useFirst ? Attack1Hash : Attack2Hash);
    }

    // Optional: add as an Animation Event on the last frame of the attack clip to unlock instantly.
    public void EndAttack()
    {
        isAttacking = false;
    }

    // index = 1 or 2. Prefers a clip named like "attack1"/"attack 2"; otherwise uses the 1st/2nd clip containing "attack".
    private float FindAttackClipLength(int index)
    {
        if (anim.runtimeAnimatorController != null)
        {
            int found = 0;
            AnimationClip nth = null;

            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            {
                string n = clip.name.ToLower();
                if (!n.Contains("attack")) continue;

                if (n.Contains(index.ToString())) return clip.length;

                found++;
                if (found == index) nth = clip;
            }

            if (nth != null) return nth.length;
        }
        return 0.6f;   // fallback if no matching clip is found
    }

    // Looks the player up by tag; keeps trying until found.
    private bool FindPlayer()
    {
        if (player != null && playerCol != null) return true;

        GameObject p = GameObject.FindWithTag(playerTag);
        if (p == null) return false;

        // Prefer a collider on the player GameObject itself; fall back to children.
        playerCol = p.GetComponent<Collider2D>();
        if (playerCol == null) playerCol = p.GetComponentInChildren<Collider2D>();
        if (playerCol == null)
        {
            // Still cache the transform so we can use center-based detection, but signal that collider is missing.
            player = p.transform;
            return true;
        }

        player = p.transform;
        return true;
    }

    private void Stop()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetFloat(SpeedHash, 0f);
        isMoving = false;
        if (sr != null) sr.flipX = false;                  // idle and attack stay normal
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