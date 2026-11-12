
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Landing Animation")]
    public float landingDuration = 0.15f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth playerHealth;

    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpRequested;
    private bool isLanding;
    private float landingTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        CheckGround();
        wasGrounded = isGrounded;
    }

    void Update()
    {
        wasGrounded = isGrounded;
        CheckGround();

        // Block controls while damaged or dead
        if (playerHealth != null &&
            (playerHealth.IsTakingHit || playerHealth.IsDead))
        {
            horizontalInput = 0f;
            jumpRequested = false;
            isLanding = false;
            return;
        }

        // Keyboard + mobile movement
        float keyboardInput = Input.GetAxisRaw("Horizontal");

        float mobileInput = 0f;

        if (MobileButton.LeftHeld)
            mobileInput -= 1f;

        if (MobileButton.RightHeld)
            mobileInput += 1f;

        horizontalInput = Mathf.Clamp(
            keyboardInput + mobileInput, -1f, 1f
        );

        // Keyboard + mobile jump
        if ((Input.GetButtonDown("Jump") ||
             MobileButton.ConsumeJump()) &&
            isGrounded && !isLanding)
        {
            jumpRequested = true;
        }

        // Landing
        if (!wasGrounded && isGrounded &&
            !jumpRequested)
        {
            isLanding = true;
            landingTimer = landingDuration;
            animator.Play("Ground", 0, 0f);
        }

        if (isLanding)
        {
            landingTimer -= Time.deltaTime;

            if (landingTimer <= 0f)
                isLanding = false;
        }

        // Flip character
        if (horizontalInput > 0.01f)
            spriteRenderer.flipX = false;
        else if (horizontalInput < -0.01f)
            spriteRenderer.flipX = true;

        // Animator parameters
        animator.SetFloat(
            "Speed",
            Mathf.Abs(horizontalInput)
        );

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsLanding", isLanding);
    }

    void FixedUpdate()
    {
        if (playerHealth != null &&
            (playerHealth.IsTakingHit || playerHealth.IsDead))
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y
        );
#else
        rb.velocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.velocity.y
        );
#endif

        if (jumpRequested)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );
#else
            rb.velocity = new Vector2(
                rb.velocity.x,
                jumpForce
            );
#endif

            jumpRequested = false;
            isGrounded = false;
        }
    }

    void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
