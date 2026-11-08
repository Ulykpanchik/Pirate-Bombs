
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 2;

    [Header("Knockback")]
    public float knockbackForce = 4f;
    public float knockbackUpForce = 7f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.15f;

    [Header("Timing")]
    public float hitDuration = 0.35f;
    public float landingTimeout = 3f;
    public float destroyAfterDeath = 2f;

    [Header("Events")]
    public UnityEvent onEnemyDied;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsTakingHit { get; private set; }

    private Rigidbody2D rb;
    private Animator animator;
    private Enemy1_Kicker kicker;
    private EnemyBehaviorAI otherAI;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        kicker = GetComponent<Enemy1_Kicker>();
        CurrentHealth = maxHealth;
        otherAI = GetComponent<EnemyBehaviorAI>();
    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (damage <= 0 || IsDead || IsTakingHit)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        float direction = transform.position.x >= sourcePosition.x
            ? 1f : -1f;

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

    private void Knockback(float direction)
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

    private bool CheckGround()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private IEnumerator WaitForLanding()
    {
        bool leftGround = false;
        float elapsed = 0f;

        while (elapsed < landingTimeout)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            if (!CheckGround())
                leftGround = true;

            if (leftGround && CheckGround())
                yield break;
        }

        Debug.LogWarning(
            "EnemyHealth: Ground landing was not detected."
        );
    }

    private void PlayAnimation(string stateName)
    {
        if (animator != null)
            animator.Play(stateName, 0, 0f);
    }

    private IEnumerator HitRoutine(float direction)
    {
        IsTakingHit = true;

        PlayAnimation("Hit");

        yield return new WaitForFixedUpdate();
        Knockback(direction);

        yield return WaitForLanding();
        yield return new WaitForSeconds(hitDuration);

        IsTakingHit = false;

        // EnemyKicker will resume its normal animation
        IsTakingHit = false;

if (kicker != null && kicker.enabled)
{
    kicker.ResumeAfterHit();
}
else if (otherAI != null && otherAI.enabled)
{
    otherAI.ResumeAfterHit();
}
else
{
    PlayAnimation("Idle");
}
    }

    private IEnumerator DeathRoutine(float direction)
    {
        IsTakingHit = true;

        if (kicker != null)
            kicker.enabled = false;

        PlayAnimation("DeadHit");

        yield return new WaitForFixedUpdate();
        Knockback(direction);

        yield return WaitForLanding();

        PlayAnimation("DeadGround");

        onEnemyDied?.Invoke();

        yield return new WaitForSeconds(destroyAfterDeath);
        Destroy(gameObject);
    }
}
