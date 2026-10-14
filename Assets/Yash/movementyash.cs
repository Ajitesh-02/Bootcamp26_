using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MobChase : MonoBehaviour
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

    private Rigidbody2D rb;
    private Transform player;
    private bool isAttacking;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("MobChase: no object tagged 'Player' found.");
    }

    private void FixedUpdate()
    {
        if (player == null) return;

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
        animator.SetTrigger("Attack");

        if (useAnimationEvent)
        {
            // The Animation Event calls FireProjectile() at the release frame
            yield return new WaitForSeconds(attackDuration);
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
        if (projectilePrefab == null || firePoint == null) return;

        float dir = Mathf.Sign(transform.localScale.x);   // facing direction
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.GetComponent<MobProjectile>().Init(dir);
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