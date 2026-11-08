
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy1_Kicker : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Alert,
        Chase,
        Attack
    }

    [Header("Target Detection")]
    public Transform player;
    public float detectionRadius = 5f;
    public float attackRange = 1f;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Attack")]
    public int attackDamage = 1;
    public float bombKickForce = 9f;
    public float attackHitTime = 0.2f;
    public float attackDuration = 0.5f;
    public float attackCooldown = 0.8f;

    [Header("Alert Animations")]
    public GameObject questionEffect;
    public GameObject alertEffect;
    public float questionDuration = 0.3f;
    public float alertDuration = 0.4f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private EnemyState state = EnemyState.Idle;
    private Transform target;
private EnemyHealth enemyHealth;
    private float stateTimer;
    private float attackCooldownTimer;
    private bool attackApplied;

    private string currentAnimation = "";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        ChangeState(EnemyState.Idle);
    }

    void Update()
    {
        stateTimer += Time.deltaTime;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        switch (state)
        {
            case EnemyState.Idle:
                SearchForTarget();
                break;

            case EnemyState.Alert:
                UpdateAlert();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.Attack:
                UpdateAttack();
                break;
        }
        if (enemyHealth != null &&
    (enemyHealth.IsDead || enemyHealth.IsTakingHit))
{
    return;
}
    }

    void FixedUpdate()
    {
        float speed = 0f;

        if (state == EnemyState.Chase && TargetIsValid())
        {
            float distance =
                target.position.x - transform.position.x;

            if (Mathf.Abs(distance) > attackRange)
            {
                speed = Mathf.Sign(distance) * moveSpeed;
            }
        }
        if (enemyHealth != null &&
    (enemyHealth.IsDead || enemyHealth.IsTakingHit))
{
    return;
}

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(
            speed,
            rb.linearVelocity.y
        );
#else
        rb.velocity = new Vector2(
            speed,
            rb.velocity.y
        );
#endif
    }

    void SearchForTarget()
    {
        target = FindNearestTarget();

        if (target != null)
            ChangeState(EnemyState.Alert);
    }

    Transform FindNearestTarget()
    {
        Transform nearest = null;
        float closestDistance = detectionRadius;

        // Detect player
        if (player != null)
        {
            PlayerHealth health =
                player.GetComponent<PlayerHealth>();

            if (health != null && !health.IsDead)
            {
                float distance = Vector2.Distance(
                    transform.position,
                    player.position
                );

                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    nearest = player;
                }
            }
        }

        // Detect bombs
        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRadius
            );

        foreach (Collider2D hit in colliders)
        {
            Bomb bomb = hit.GetComponentInParent<Bomb>();

            if (bomb == null || !bomb.IsActive)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                bomb.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = bomb.transform;
            }
        }

        return nearest;
    }

    void UpdateAlert()
    {
        if (!TargetIsValid())
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        FaceTarget();

        if (stateTimer >= questionDuration)
        {
            if (questionEffect != null)
                questionEffect.SetActive(false);

            if (alertEffect != null)
                alertEffect.SetActive(true);
        }

        if (stateTimer >= questionDuration + alertDuration)
            ChangeState(EnemyState.Chase);
    }

    void UpdateChase()
    {
        if (!TargetIsValid())
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        FaceTarget();

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackRange &&
            attackCooldownTimer <= 0f)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    void UpdateAttack()
    {
        if (!attackApplied && stateTimer >= attackHitTime)
        {
            attackApplied = true;
            PerformAttack();
        }

        if (stateTimer >= attackDuration)
        {
            attackCooldownTimer = attackCooldown;

            if (TargetIsValid())
                ChangeState(EnemyState.Chase);
            else
                ChangeState(EnemyState.Idle);
        }
    }

    void PerformAttack()
    {
        if (!TargetIsValid())
            return;

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance > attackRange + 0.3f)
            return;

        float direction =
            spriteRenderer.flipX ? -1f : 1f;

        Bomb bomb = target.GetComponent<Bomb>();

        if (bomb != null && bomb.IsActive)
        {
            bomb.Throw(new Vector2(
                direction * bombKickForce,
                2f
            ));

            return;
        }

        PlayerHealth health =
            target.GetComponent<PlayerHealth>();

        if (health != null && !health.IsDead)
        {
            health.TakeDamage(
                attackDamage,
                transform.position
            );
        }
    }

    bool TargetIsValid()
    {
        if (target == null)
            return false;

        Bomb bomb = target.GetComponent<Bomb>();

        if (bomb != null)
            return bomb.IsActive;

        PlayerHealth health =
            target.GetComponent<PlayerHealth>();

        if (health != null)
            return !health.IsDead;

        return false;
    }

    void FaceTarget()
    {
        if (target == null || spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            target.position.x < transform.position.x;
    }

    void ChangeState(EnemyState newState)
    {
        state = newState;
        stateTimer = 0f;

        if (questionEffect != null)
            questionEffect.SetActive(false);

        if (alertEffect != null)
            alertEffect.SetActive(false);

        switch (state)
        {
            case EnemyState.Idle:
                target = null;
                PlayAnimation("Idle");
                break;

            case EnemyState.Alert:
                if (questionEffect != null)
                    questionEffect.SetActive(true);

                PlayAnimation("Idle");
                break;

            case EnemyState.Chase:
                PlayAnimation("Run");
                break;

            case EnemyState.Attack:
                attackApplied = false;
                PlayAnimation("Attack");
                break;
        }
    }

    void PlayAnimation(string animationName)
    {
        if (animator == null)
            return;

        if (currentAnimation == animationName)
            return;

        animator.Play(animationName, 0, 0f);
        currentAnimation = animationName;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
    public void ResumeAfterHit()
{
    if (enemyHealth != null && enemyHealth.IsDead)
        return;

    currentAnimation = "";

    if (TargetIsValid())
        ChangeState(EnemyState.Chase);
    else
        ChangeState(EnemyState.Idle);
}
}
