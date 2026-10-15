using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MobChase : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float stopDistance = 5f;      // mob stops here and shoots

    [Header("Ranged Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;          // child object at the mob's "hand"/muzzle
    [SerializeField] private float attackCooldown = 2f;    // pause between attacks
    [SerializeField] private float fireDelay = 0.4f;       // time into the animation when the shot spawns
    [SerializeField] private float attackDuration = 0.8f;  // total length of the attack animation
    [SerializeField] private bool useAnimationEvent = true; // true = fire from an Animation Event (exact sync)

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Health")]
    [SerializeField] private int maxHealth = 40;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float deathFadeTime = 0.6f;

    [Header("Drop On Death")]
    [Tooltip("Tick on the ONE enemy that should drop the Crystal needed to exit.")]
    [SerializeField] private bool dropsCrystal = false;
    [Tooltip("Your crystal PNG. Leave empty for a cyan square.")]
    [SerializeField] private Sprite crystalSprite;
    [SerializeField] private float dropSize = 0.8f;

    private Rigidbody2D rb;
    private Transform player;
    private bool isAttacking;
    private float nextAttackTime;
    private bool firedThisAttack;

    private int currentHealth;
    private bool isDead;
    private SpriteRenderer sr;
    private Collider2D col;
    private Color baseColor = Color.white;
    private PlayerMovement playerHealth;

    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        col = GetComponent<Collider2D>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponentInParent<PlayerMovement>();
            if (playerHealth == null) playerHealth = p.GetComponentInChildren<PlayerMovement>();
        }
        else Debug.LogWarning("MobChase: no object tagged 'Player' found.");
    }

    private void FixedUpdate()
    {
        if (isDead || player == null) return;

        // Player is dead: stand still
        if (playerHealth != null && playerHealth.IsDead) { StopMoving(); return; }

        // Stand still while the attack animation plays
        if (isAttacking) { StopMoving(); return; }

        float dx = player.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (dist > detectionRange)
        {
            StopMoving();
            animator.SetBool("IsRunning", false);
            return;
        }

        Face(Mathf.Sign(dx));

        if (dist > stopDistance)
        {
            // Run toward the player
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * speed, rb.linearVelocity.y);
            animator.SetBool("IsRunning", true);
        }
        else
        {
            // Reached stop distance: stop and shoot
            StopMoving();
            animator.SetBool("IsRunning", false);

            if (Time.time >= nextAttackTime)
                StartCoroutine(RangedAttack());
        }
    }

    private IEnumerator RangedAttack()
    {
        isAttacking = true;
        firedThisAttack = false;
        animator.SetTrigger("Attack");

        if (useAnimationEvent)
        {
            // The Animation Event calls FireProjectile() at the release frame
            yield return new WaitForSeconds(attackDuration);

            // Safety net: if the animation event never fired, shoot anyway
            if (!firedThisAttack) FireProjectile();
        }
        else
        {
            yield return new WaitForSeconds(fireDelay);
            FireProjectile();
            yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - fireDelay));
        }

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    // Called by the Animation Event (must be public)
    public void FireProjectile()
    {
        if (isDead || projectilePrefab == null || firePoint == null) return;
        if (firedThisAttack) return;   // one shot per attack
        firedThisAttack = true;

        float dir = Mathf.Sign(transform.localScale.x);   // facing direction
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.GetComponent<MobProjectile>().Init(dir);
    }

    // ---------------- Health ----------------
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
        StopAllCoroutines();
        isAttacking = false;

        Vector3 dropPos = col != null ? col.bounds.center : transform.position;
        dropPos.z = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col != null) col.enabled = false;
        if (animator != null) animator.speed = 0f;

        if (dropsCrystal)
        {
            Collectible.Spawn(CollectibleType.Crystal, crystalSprite, dropSize, dropPos, sr != null ? sr.sharedMaterial : null);
        }

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

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void Face(float dir)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;
    }
}