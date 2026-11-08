
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterDust : MonoBehaviour
{
    [Header("Dust Prefabs")]
    public GameObject runDustPrefab;
    public GameObject jumpDustPrefab;
    public GameObject landingDustPrefab;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.15f;

    [Header("Settings")]
    public float runDustInterval = 0.15f;
    public float minRunSpeed = 0.5f;
[Header("Dust Position")]
public Vector2 runDustOffset = new Vector2(0f, 0.2f);
    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private bool wasGrounded;
    private float runDustTimer;
    private SpriteRenderer playerSprite;

    private Vector2 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        playerSprite = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        wasGrounded = IsGrounded();
    }

    void Update()
    {
        if (groundCheck == null)
            return;

        bool grounded = IsGrounded();

        bool blocked = playerHealth != null &&
            (playerHealth.IsDead || playerHealth.IsTakingHit);

        // Running dust
        if (grounded &&
            !blocked &&
            Mathf.Abs(Velocity.x) > minRunSpeed)
        {
            runDustTimer += Time.deltaTime;

            if (runDustTimer >= runDustInterval)
            {
                SpawnDust(runDustPrefab);
                runDustTimer = 0f;
            }
        }
        else
        {
            runDustTimer = 0f;
        }

        // Jump dust
        if (wasGrounded && !grounded && Velocity.y > 0.1f && !blocked)
        {
            SpawnDust(jumpDustPrefab);
        }

        // Landing dust
        if (!wasGrounded && grounded)
        {
            SpawnDust(landingDustPrefab);
        }

        wasGrounded = grounded;
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    


void SpawnDust(GameObject prefab)
{
    if (prefab == null || groundCheck == null)
        return;

    Vector3 position = groundCheck.position;
    if (prefab == runDustPrefab)
{
    float direction = playerSprite.flipX ? 1f : -1f;

    position += new Vector3(
        direction * 0.2f,
        runDustOffset.y,
        0f
    );
}
    // Raise RunDust slightly
    if (prefab == runDustPrefab)
    {
        position += (Vector3)runDustOffset;
    }

    // Spawn dust
    GameObject dust = Instantiate(
        prefab,
        position,
        Quaternion.identity
    );

    // Flip dust based on player direction
    SpriteRenderer dustSprite =
        dust.GetComponent<SpriteRenderer>();

    if (dustSprite != null && playerSprite != null)
    {
        dustSprite.flipX = playerSprite.flipX;
    }

    // Destroy only after animation finishes
    StartCoroutine(DestroyAfterAnimation(dust));
}


System.Collections.IEnumerator DestroyAfterAnimation(GameObject dust)
{
    Animator dustAnimator = dust.GetComponent<Animator>();

    if (dustAnimator == null)
    {
        Destroy(dust, 0.5f);
        yield break;
    }

    // Wait for Animator to start its animation
    yield return null;

    while (dust != null)
    {
        AnimatorStateInfo state =
            dustAnimator.GetCurrentAnimatorStateInfo(0);

        if (!dustAnimator.IsInTransition(0) &&
            state.normalizedTime >= 1f)
        {
            Destroy(dust);
            yield break;
        }

        yield return null;
    }
}


}
