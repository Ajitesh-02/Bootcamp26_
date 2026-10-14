using UnityEngine;

public class BasicMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpspeed = 7f;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isOnGround;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Default all states to false at the start of every frame
        bool isRunning = false;
        bool isRunningRight = false;
        bool isRunningLeft = false;
        bool isRunningBack = false;

        // 2. Check individual key inputs independently
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
            isRunningRight = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
            isRunningLeft = true;
        }

        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(new Vector3(0, speed * Time.deltaTime, 0));
            isRunning = true; // Maps to your 'isRunning' parameter
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(new Vector3(0, -speed * Time.deltaTime, 0));
            isRunningBack = true;
        }

        // Jump Logic
        if (Input.GetKey(KeyCode.Space) && isOnGround)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpspeed);
            isOnGround = false;
        }

        // 3. Send the exact values to the Unity Animator
        anim.SetBool("isRunning", isRunning);
        anim.SetBool("isRunningRight", isRunningRight);
        anim.SetBool("isRunningLeft", isRunningLeft);
        anim.SetBool("isRunningBack", isRunningBack);
    }

    void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (collision2D.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
    }
}