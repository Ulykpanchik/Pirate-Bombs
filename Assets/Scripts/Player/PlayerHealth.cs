
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 3;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackUpForce = 9f;

    [Header("Animation")]
    public float hitDuration = 0.3f;

    [Header("Events")]
    public UnityEvent<int> onHealthChanged;
    public UnityEvent onPlayerDied;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsTakingHit { get; private set; }

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerMovement movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();

        CurrentHealth = maxHealth;
    }

    void Start()
    {
        onHealthChanged?.Invoke(CurrentHealth);
    }

    public void TakeDamage(int damage)
    {
        // Default: knock away from facing direction
        float direction = spriteRenderer.flipX ? 1f : -1f;
        ApplyDamage(damage, direction);
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        // Knock away from the attacker
        float direction =
            transform.position.x >= attackerPosition.x ? 1f : -1f;

        ApplyDamage(damage, direction);
    }

    private void ApplyDamage(int damage, float direction)
    {
        if (IsDead || IsTakingHit || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        onHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth == 0)
        {
            IsDead = true;
            StartCoroutine(DeathRoutine(direction));
        }
        else
        {
            StartCoroutine(HitRoutine(direction));
        }
    }

    private void ApplyKnockback(float direction)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(
            direction * knockbackForce,
            knockbackUpForce
        );
#else
        rb.velocity = new Vector2(
            direction * knockbackForce,
            knockbackUpForce
        );
#endif
    }

    private bool IsGrounded()
    {
        if (movement == null || movement.groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            movement.groundCheck.position,
            movement.groundCheckRadius,
            movement.groundLayer
        );
    }

    private IEnumerator WaitForLanding()
    {
        bool hasLeftGround = false;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (!IsGrounded())
                hasLeftGround = true;

            if (hasLeftGround && IsGrounded())
                break;
        }
    }

    private IEnumerator HitRoutine(float direction)
{
    IsTakingHit = true;

    animator.Play("Hit", 0, 0f);

    yield return new WaitForFixedUpdate();
    ApplyKnockback(direction);

    yield return WaitForLanding();

    yield return new WaitForSeconds(hitDuration);

    animator.Play("Idle", 0, 0f);
    IsTakingHit = false;
}

    private IEnumerator DeathRoutine(float direction)
{
    IsTakingHit = true;

    animator.Play("DeadHit", 0, 0f);

    yield return new WaitForFixedUpdate();
    ApplyKnockback(direction);

    yield return WaitForLanding();

#if UNITY_6000_0_OR_NEWER
    rb.linearVelocity = Vector2.zero;
#else
    rb.velocity = Vector2.zero;
#endif

    animator.Play("DeadGround", 0, 0f);

    if (movement != null)
        movement.enabled = false;

    onPlayerDied?.Invoke();
}
}
