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

    [Header("Potion (Q)")]
    public int potionHealAmount = 40;

    [Header("Lightning Bolt")]
    public float lightningCooldown = 0.6f;
    private float nextLightningTime;
    private float nextLockedMessageTime;

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
    [Tooltip("On death, go back to the hub (Guide) after the delay.")]
    public bool reloadSceneOnDeath = true;
    public float reloadDelay = 2.5f;

    private Collider2D bodyCollider;

    [Header("Chain Climbing (B to grab, Up/Down or W/S to climb, Space at the top to jump off)")]
    public float climbSpeed = 1.5f;
    public float climbJumpCooldown = 0.5f;   // can't jump again right after leaving a chain

    private enum ClimbState { Normal, Climbing, AtTop }
    private ClimbState climbState = ClimbState.Normal;
    private ChainClimbZone currentZone;
    private ChainClimbZone blockedZone;
    private readonly Dictionary<ChainClimbZone, int> zoneTouches = new Dictionary<ChainClimbZone, int>();
    private ChainClimbZone nearbyZone;
    private Collider2D nearbyZoneCol;
    private float originalGravity = 1f;
    private float climbStartY;
    private float climbX;
    private float jumpLockUntil;

    public bool IsClimbing => climbState != ClimbState.Normal;
    public bool IsDead => isDead;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        if (rb != null) originalGravity = rb.gravityScale;

        // No health bar assigned in the Inspector -> use the auto HUD (top-left corner)
        if (healthBar == null)
        {
            healthBar = GameHUD.Instance.HealthBar;
        }

        // Health carries over between levels (PlayerProgress)
        maxHealth = PlayerProgress.MaxHealth;
        currentHealth = PlayerProgress.Health > 0 ? PlayerProgress.Health : maxHealth;
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        // Disable controls and movement if player is dead (or a level intro panel is open)
        if (isDead || LevelIntro.IsOpen) return;

        // Health potion (bought from the Guide)
        if (Input.GetKeyDown(KeyCode.Q)) DrinkPotion();

        // Chain climbing takes over movement while on a chain
        if (climbState == ClimbState.Climbing) { Climb(); return; }
        if (climbState == ClimbState.AtTop) { AtTop(); return; }

        // Grab a chain (B) when standing next to one
        if (Input.GetKeyDown(KeyCode.B) && nearbyZone != null && nearbyZone != blockedZone)
        {
            StartClimbing(nearbyZone, nearbyZoneCol);
            if (climbState != ClimbState.Normal) return;
        }

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
        if (Input.GetKeyDown(KeyCode.Space) && isOnGround && Time.time >= jumpLockUntil)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpspeed);
            isOnGround = false;
            groundContacts.Clear();
            jumpLockUntil = Time.time + 0.15f;   // no instant re-jump while still touching the floor
        }

        // Basic Attack Inputs
        if (Input.GetKeyDown(KeyCode.H)) MeleeAttack("attack0", attack0Damage);
        if (Input.GetKeyDown(KeyCode.U)) MeleeAttack("attack1", attack1Damage);
        if (Input.GetKeyDown(KeyCode.K)) MeleeAttack("attack2", attack2Damage);

        // Lightning Bolt Attack Input (C Key) - must be bought from the Guide first
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!PlayerProgress.Has(Skill.LightningBolt))
            {
                LockedSkillMessage("Lightning Bolt");
            }
            else if (Time.time >= nextLightningTime)
            {
                nextLightningTime = Time.time + lightningCooldown;
                if (anim != null)
                {
                    anim.SetTrigger("attack4");
                }
                SpawnLightningBolt();
            }
        }

        // Air Slash Attack Input (J Key with 3-second Cooldown) - must be bought from the Guide first
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (!PlayerProgress.Has(Skill.AirSlash))
            {
                LockedSkillMessage("Air Slash");
            }
            else if (Time.time >= lastAirSlashTime + airSlashCooldown)
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

        PlayerProgress.SetHealth(currentHealth); // remember for the next level

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

        PlayerProgress.SetHealth(currentHealth); // remember for the next level

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

        GameHUD.Instance.ShowMessage("You died! Returning to the Guide...", reloadDelay);
        if (reloadSceneOnDeath)
        {
            StartCoroutine(ReloadAfterDeath());
        }
    }

    private IEnumerator ReloadAfterDeath()
    {
        yield return new WaitForSeconds(reloadDelay);
        // Back to the hub next to the Guide: full HP, coins and bought attacks kept
        PlayerProgress.OnDeath();
        SceneFader.LoadScene(PlayerProgress.HubScene);
    }

    private void DrinkPotion()
    {
        if (PlayerProgress.Potions <= 0)
        {
            GameHUD.Instance.ShowMessage("No potions left. Buy them from the Guide.");
            return;
        }
        if (currentHealth >= maxHealth)
        {
            GameHUD.Instance.ShowMessage("You are already at full health.");
            return;
        }
        PlayerProgress.UsePotion();
        Heal(potionHealAmount);
        GameHUD.Instance.ShowMessage("+" + potionHealAmount + " HP", 1.2f);
        if (spriteRenderer != null) StartCoroutine(PotionGlow());
    }

    private IEnumerator PotionGlow()
    {
        Color baseCol = spriteRenderer.color;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(new Color(0.5f, 1f, 0.5f, baseCol.a), baseCol, t / 0.5f);
            yield return null;
        }
        spriteRenderer.color = baseCol;
    }

    private void LockedSkillMessage(string skillName)
    {
        if (Time.time < nextLockedMessageTime) return;
        nextLockedMessageTime = Time.time + 1.5f;
        GameHUD.Instance.ShowMessage(skillName + " is locked. Buy it from the Guide with coins.");
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

    // ---------------- Chain climbing (same behaviour as the Trap level's DemoMovement) ----------------
    private void Climb()
    {
        SetClimbAnim();

        float moveY = 0f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            moveY = climbSpeed;
        else if ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) && transform.position.y > climbStartY)
            moveY = -climbSpeed;

        Vector3 p = transform.position;
        p.x = climbX;
        p.y += moveY * Time.deltaTime;

        float topY = currentZone.anchorTop.position.y;
        if (p.y >= topY)
        {
            p.y = topY;
            transform.position = p;
            climbState = ClimbState.AtTop;
            return;
        }
        transform.position = p;
    }

    private void AtTop()
    {
        SetClimbAnim();

        Vector3 p = transform.position;
        p.x = climbX;
        p.y = currentZone.anchorTop.position.y;
        transform.position = p;

        if (Input.GetKeyDown(KeyCode.Space))
            JumpOffTop();
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            climbState = ClimbState.Climbing;
    }

    private void JumpOffTop()
    {
        climbState = ClimbState.Normal;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = originalGravity;

        float dir = currentZone.grabFromLeft ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * currentZone.topJumpX, currentZone.topJumpY);
        if (spriteRenderer != null) spriteRenderer.flipX = dir < 0f;
        groundContacts.Clear();
        isOnGround = false;
        jumpLockUntil = Time.time + climbJumpCooldown;

        if (GetZoneTouches(currentZone) == 0) blockedZone = null;
    }

    private void StartClimbing(ChainClimbZone zone, Collider2D chainCol)
    {
        if (zone == null || zone.anchorTop == null || chainCol == null) return;

        float chainX = chainCol.bounds.center.x;
        bool onCorrectSide = zone.grabFromLeft ? transform.position.x < chainX : transform.position.x > chainX;
        if (!onCorrectSide) return;

        currentZone = zone;
        blockedZone = zone;
        climbState = ClimbState.Climbing;
        groundContacts.Clear();
        isOnGround = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        climbStartY = transform.position.y;
        climbX = zone.anchorTop.position.x + (zone.grabFromLeft ? -zone.sideOffset : zone.sideOffset);
        if (spriteRenderer != null) spriteRenderer.flipX = !zone.grabFromLeft;   // face the chain
    }

    private void SetClimbAnim()
    {
        if (anim == null) return;
        anim.SetBool("isRunning", false);
        anim.SetBool("isGrounded", false);
        anim.SetFloat("yVelocity", 0f);
    }

    private int GetZoneTouches(ChainClimbZone zone)
    {
        return zoneTouches.TryGetValue(zone, out int n) ? n : 0;
    }

    private void ZoneEnter(Collider2D col)
    {
        ChainClimbZone zone = col.GetComponentInParent<ChainClimbZone>();
        if (zone == null) return;
        zoneTouches[zone] = GetZoneTouches(zone) + 1;
        nearbyZone = zone;
        nearbyZoneCol = col;
    }

    private void ZoneExit(Collider2D col)
    {
        ChainClimbZone zone = col.GetComponentInParent<ChainClimbZone>();
        if (zone == null) return;
        zoneTouches[zone] = Mathf.Max(0, GetZoneTouches(zone) - 1);
        if (zoneTouches[zone] == 0)
        {
            if (nearbyZone == zone)
            {
                nearbyZone = null;
                nearbyZoneCol = null;
            }
            if (zone == blockedZone && climbState == ClimbState.Normal)
                blockedZone = null;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { ZoneEnter(other); }
    void OnTriggerExit2D(Collider2D other) { ZoneExit(other); }
    // ---------------- Ground check ----------------
    // Grounded = touching anything tagged "Ground" OR standing on top of any solid surface
    // (many platforms, e.g. in the Trap level, are not tagged "Ground").
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();

    private void UpdateGround(Collision2D c)
    {
        if (c.collider.isTrigger) return;
        bool standingOn = c.gameObject.CompareTag("Ground");
        for (int i = 0; i < c.contactCount && !standingOn; i++)
        {
            if (c.GetContact(i).normal.y > 0.5f) standingOn = true;   // surface is below us
        }
        if (standingOn) groundContacts.Add(c.collider);
        else groundContacts.Remove(c.collider);
        isOnGround = groundContacts.Count > 0;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (climbState == ClimbState.Normal) UpdateGround(c);
    }

    void OnCollisionExit2D(Collision2D c)
    {
        ZoneExit(c.collider);
        groundContacts.Remove(c.collider);
        if (groundContacts.Count == 0) isOnGround = false;
    }

    void OnCollisionEnter2D(Collision2D collision2D)
    {
        ZoneEnter(collision2D.collider);

        if (collision2D.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
        if (climbState == ClimbState.Normal) UpdateGround(collision2D);
       
    }
}