using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpspeed = 5f;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    public bool isOnGround;

    [Header("Player Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;
    private bool isDead = false;

    [Header("UI References")]
    public HealthBar healthBar; // Reference to the HealthBar script

    [Header("Lightning Bolt Attack")]
    public GameObject lightningBoltPrefab;
    public Transform boltAttackPoint; // Attack point for lightning bolt (e.g., player's hand)

    [Header("Air Slash Skill")]
    public GameObject airSlashPrefab;
    public Transform airSlashAttackPoint; // Attack point for air slash (e.g., sword tip)
    public float airSlashCooldown = 3f;
    private float lastAirSlashTime = -999f;

    [Header("Melee Attack (H / U / K)")]
    public int attack0Damage = 15;          // H
    public int attack1Damage = 20;          // U
    public int attack2Damage = 30;          // K
    public float meleeReach = 0.8f;         // how far in front of the player the hit lands
    public float meleeRadius = 0.9f;        // size of the hit area
    public float meleeHitDelay = 0.15f;     // time into the swing when damage is applied
    public float meleeCooldown = 0.35f;
    private float nextMeleeTime;

    [Header("Hit Feedback")]
    public Color hitFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
    public float hitFlashInterval = 0.1f;
    [Tooltip("Sound when hit. Leave empty to use a small generated 'uhh' grunt.")]
    public AudioClip hurtSound;
    [Range(0f, 1f)] public float hurtVolume = 0.6f;
    public float shakeStrength = 0.18f;   // world units
    public float shakeDuration = 0.2f;

    [Header("Death")]
    public bool reloadSceneOnDeath = true;
    public float reloadDelay = 2.5f;

    private Collider2D bodyCollider;

    public bool IsDead => isDead;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();

        // No health bar assigned in the Inspector -> use the auto HUD (top-left corner)
        if (healthBar == null)
        {
            healthBar = GameHUD.Instance.HealthBar;
        }

        // Initialize health & update UI at game start
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        // Disable controls and movement if player is dead
        if (isDead) return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // Horizontal Movement & Sprite Flip
        if (horizontalInput > 0)
        {
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
            spriteRenderer.flipX = true;
        }

        // Jump Input
        if (Input.GetKeyDown(KeyCode.Space) && isOnGround)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpspeed);
            isOnGround = false;
        }

        // Basic Attack Inputs
        if (Input.GetKeyDown(KeyCode.H)) MeleeAttack("attack0", attack0Damage);
        if (Input.GetKeyDown(KeyCode.U)) MeleeAttack("attack1", attack1Damage);
        if (Input.GetKeyDown(KeyCode.K)) MeleeAttack("attack2", attack2Damage);

        // Lightning Bolt Attack Input (C Key)
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (anim != null)
            {
                anim.SetTrigger("attack4");
            }
            SpawnLightningBolt();
        }

        // Air Slash Attack Input (J Key with 3-second Cooldown)
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (Time.time >= lastAirSlashTime + airSlashCooldown)
            {
                if (anim != null)
                {
                    anim.SetTrigger("attack3");
                }

                SpawnAirSlash();
                lastAirSlashTime = Time.time;
            }
            else
            {
                float remainingCooldown = (lastAirSlashTime + airSlashCooldown) - Time.time;
                Debug.Log("Air Slash Cooldown: " + remainingCooldown.ToString("F1") + "s remaining");
            }
        }

        // Update Animator Parameters
        if (anim != null)
        {
            anim.SetBool("isRunning", horizontalInput != 0);
            anim.SetBool("isGrounded", isOnGround);
            anim.SetFloat("yVelocity", rb.linearVelocity.y);
        }
    }

    // Call this method when taking damage from enemies or hazards
    public void TakeDamage(int damage)
    {
        if (isInvincible || isDead) return;

        currentHealth -= damage;
        HitFeedback.PlayerHit(hurtSound, hurtVolume, shakeStrength, shakeDuration);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            if (anim != null)
            {
                anim.SetTrigger("hurt"); // Triggers hurt animation if set in Animator
            }
            StartCoroutine(InvincibilityRoutine());
        }

        // Update Health Bar UI
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    // Call this method to heal the player
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // Update Health Bar UI
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Player Has Died!");

        if (anim != null)
        {
            anim.SetTrigger("die"); // Triggers death animation if set in Animator
        }

        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.color = hitFlashColor;

        GameHUD.Instance.ShowMessage("You died!", reloadDelay);
        if (reloadSceneOnDeath)
        {
            StartCoroutine(ReloadAfterDeath());
        }
    }

    private IEnumerator ReloadAfterDeath()
    {
        yield return new WaitForSeconds(reloadDelay);
        SceneFader.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------- Melee ----------------
    private void MeleeAttack(string trigger, int damage)
    {
        if (Time.time < nextMeleeTime) return;
        nextMeleeTime = Time.time + meleeCooldown;

        if (anim != null) anim.SetTrigger(trigger);
        StartCoroutine(MeleeHit(damage));
    }

    private IEnumerator MeleeHit(int damage)
    {
        yield return new WaitForSeconds(meleeHitDelay);
        if (isDead) yield break;

        Vector2 center = MeleeCenter();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeRadius);
        HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        foreach (Collider2D hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // don't hit yourself
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead || !alreadyHit.Add(target)) continue;
            target.TakeDamage(damage);
        }
    }

    private Vector2 MeleeCenter()
    {
        Vector2 origin = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
        bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        return origin + (facingLeft ? Vector2.left : Vector2.right) * meleeReach;
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(MeleeCenter(), meleeRadius);
    }

    // Temporary invincibility period with visual flashing effect
    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        if (spriteRenderer != null)
        {
            // Flash red while invincible
            Color originalColor = spriteRenderer.color;
            float end = Time.time + invincibilityDuration;
            bool red = true;
            while (Time.time < end)
            {
                spriteRenderer.color = red ? hitFlashColor : originalColor;
                red = !red;
                yield return new WaitForSeconds(hitFlashInterval);
            }
            spriteRenderer.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }

        isInvincible = false;
    }

    // Spawns Lightning Bolt from boltAttackPoint
    public void SpawnLightningBolt()
    {
        if (lightningBoltPrefab == null)
        {
            Debug.LogWarning("Lightning Bolt Prefab is not assigned in the Inspector!");
            return;
        }

        Vector3 spawnPos = boltAttackPoint != null ? boltAttackPoint.position : transform.position;
        GameObject bolt = Instantiate(lightningBoltPrefab, spawnPos, Quaternion.identity);

        Vector2 travelDir = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        LightningBolt boltScript = bolt.GetComponent<LightningBolt>();
        if (boltScript != null)
        {
            boltScript.Initialize(travelDir);
        }
    }

    // Spawns Air Slash from airSlashAttackPoint
    public void SpawnAirSlash()
    {
        if (airSlashPrefab == null)
        {
            Debug.LogWarning("Air Slash Prefab is not assigned in the Inspector!");
            return;
        }

        Vector3 spawnPos = airSlashAttackPoint != null ? airSlashAttackPoint.position : transform.position;
        GameObject slash = Instantiate(airSlashPrefab, spawnPos, Quaternion.identity);

        Vector2 travelDir = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        AirSlash slashScript = slash.GetComponent<AirSlash>();
        if (slashScript != null)
        {
            slashScript.Initialize(travelDir);
        }
    }

    void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (collision2D.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
       
    }
}