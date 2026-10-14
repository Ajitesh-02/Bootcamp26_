using System.Collections.Generic;
using UnityEngine;

public class DemoMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 7f;
    public float jumpForce = 11f;     // Snappy, instant upward impulse
    public float climbSpeed = 1.5f;
    public float jumpCooldown = 0.5f; // Time (seconds) before you can jump again

    [Header("Jump Snappiness")]
    public float fallMultiplier = 2.5f; // Pulls the player down faster after peak

    private Rigidbody2D rb;
    private enum State { Normal, Climbing, AtTop }
    private State state = State.Normal;

    private ChainClimbZone currentZone;
    private ChainClimbZone blockedZone;
    private Dictionary<ChainClimbZone, int> touches = new Dictionary<ChainClimbZone, int>();

    private ChainClimbZone nearbyZone;
    private Collider2D nearbyCol;

    private float originalGravity;
    private float climbStartY;
    private float climbX;
    private float jumpLockUntil = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (state == State.Climbing) { Climb(); return; }
        if (state == State.AtTop) { AtTop(); return; }

        // Horizontal movement
        if (Input.GetKey(KeyCode.D))
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        else if (Input.GetKey(KeyCode.A))
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));

        // ONE-CLICK INSTANT JUMP with cooldown (GetKeyDown fires once on tap)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= jumpLockUntil)
        {
            // Reset vertical velocity first so previous downward momentum doesn't eat the jump height
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpLockUntil = Time.time + jumpCooldown; // block jumping for 0.5s
        }

        // Fast-fall: Snappier landing so player doesn't float in the air
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = originalGravity * fallMultiplier;
        }
        else
        {
            rb.gravityScale = originalGravity;
        }

        // Chain manual grab (now on B)
        if (Input.GetKeyDown(KeyCode.B) && nearbyZone != null && nearbyZone != blockedZone)
        {
            StartClimbing(nearbyZone, nearbyCol);
        }
    }

    void Climb()
    {
        float moveY = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
            moveY = climbSpeed;
        else if (Input.GetKey(KeyCode.DownArrow) && transform.position.y > climbStartY)
            moveY = -climbSpeed;

        Vector3 p = transform.position;
        p.x = climbX;
        p.y += moveY * Time.deltaTime;

        float topY = currentZone.anchorTop.position.y;
        if (p.y >= topY)
        {
            p.y = topY;
            transform.position = p;
            state = State.AtTop;
            return;
        }
        transform.position = p;
    }

    void AtTop()
    {
        Vector3 p = transform.position;
        p.x = climbX;
        p.y = currentZone.anchorTop.position.y;
        transform.position = p;

        if (Input.GetKeyDown(KeyCode.Space))
            JumpOffTop();
        else if (Input.GetKey(KeyCode.DownArrow))
            state = State.Climbing;
    }

    void JumpOffTop()
    {
        state = State.Normal;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = originalGravity;

        float dir = currentZone.grabFromLeft ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * currentZone.topJumpX, currentZone.topJumpY);
        jumpLockUntil = Time.time + jumpCooldown;

        if (GetTouches(currentZone) == 0) blockedZone = null;
    }

    void StartClimbing(ChainClimbZone zone, Collider2D chainCol)
    {
        float chainX = chainCol.bounds.center.x;
        bool onCorrectSide = zone.grabFromLeft ? transform.position.x < chainX : transform.position.x > chainX;
        if (!onCorrectSide) return;

        currentZone = zone;
        blockedZone = zone;
        state = State.Climbing;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        climbStartY = transform.position.y;
        climbX = zone.anchorTop.position.x + (zone.grabFromLeft ? -zone.sideOffset : zone.sideOffset);
    }

    int GetTouches(ChainClimbZone zone)
    {
        int n;
        return touches.TryGetValue(zone, out n) ? n : 0;
    }

    void Enter(Collider2D col)
    {
        ChainClimbZone zone = col.GetComponentInParent<ChainClimbZone>();
        if (zone == null) return;

        touches[zone] = GetTouches(zone) + 1;
        nearbyZone = zone;
        nearbyCol = col;
    }

    void Exit(Collider2D col)
    {
        ChainClimbZone zone = col.GetComponentInParent<ChainClimbZone>();
        if (zone == null) return;

        touches[zone] = Mathf.Max(0, GetTouches(zone) - 1);
        if (touches[zone] == 0)
        {
            if (nearbyZone == zone)
            {
                nearbyZone = null;
                nearbyCol = null;
            }
            if (zone == blockedZone && state == State.Normal)
                blockedZone = null;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { Enter(other); }
    void OnTriggerExit2D(Collider2D other) { Exit(other); }
    void OnCollisionEnter2D(Collision2D c) { Enter(c.collider); }
    void OnCollisionExit2D(Collision2D c) { Exit(c.collider); }
}