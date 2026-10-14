using System.Collections;
using UnityEngine;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

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
        if (Input.GetKeyDown(KeyCode.H) && anim != null) anim.SetTrigger("attack0");
        if (Input.GetKeyDown(KeyCode.U) && anim != null) anim.SetTrigger("attack1");
        if (Input.GetKeyDown(KeyCode.K) && anim != null) anim.SetTrigger("attack2");

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
    }

    // Temporary invincibility period with visual flashing effect
    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            yield return new WaitForSeconds(invincibilityDuration);
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
        if (collision2D.gameObject.CompareTag("Spikes"))
        {
            TakeDamage(25);
            transform.position = Vector3.zero;
        }
    }
}