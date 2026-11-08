
using System.Collections.Generic;
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
    public float detectionRadius = 7f;
    public float attackRange = 1f;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Jump Settings")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.15f;
    public float jumpForce = 10f;
    public float jumpHeightThreshold = 0.6f;
    public float jumpHorizontalRange = 1.5f;
    public float jumpCooldown = 0.8f;
    [Header("Obstacle Detection")]
public LayerMask obstacleLayers;
public float obstacleCheckDistance = 0.8f;
public float obstacleCheckHeight = 0.35f;

    [Header("Attack")]
    public int attackDamage = 1;
    public float bombKickForce = 9f;
    public float attackHitTime = 0.2f;
    public float attackDuration = 0.5f;
    public float attackCooldown = 0.8f;

    [Header("After Bomb Kick")]
    public float restAfterBombKick = 1f;

    [Header("Alert Position")]
    public Transform alertPoint;

    [Header("Alert Animation Prefabs")]
    public GameObject questionPrefab;
    public GameObject exclamationPrefab;

    [Header("Alert Timing")]
    public float questionDuration = 0.3f;
    public float alertDuration = 0.4f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth enemyHealth;

    private EnemyState state = EnemyState.Idle;
    private Transform target;

    private GameObject questionInstance;
    private GameObject exclamationInstance;

    private HashSet<Bomb> kickedBombs = new HashSet<Bomb>();

    private float stateTimer;
    private float attackCooldownTimer;
    private float jumpCooldownTimer;
    private float restTimer;

    private bool attackApplied;
    private bool exclamationShown;

    private string currentAnimation = "";

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
        set
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        CreateAlertEffects();
        ChangeState(EnemyState.Idle);
    }

    void Update()
    {
        if (IsBlocked())
        {
            HideAlerts();
            return;
        }

        stateTimer += Time.deltaTime;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.deltaTime;

        if (restTimer > 0f)
            restTimer -= Time.deltaTime;

        switch (state)
        {
            case EnemyState.Idle:
                if (restTimer <= 0f)
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
    }

    void FixedUpdate()
    {
        if (IsBlocked())
            return;

        float horizontalSpeed = 0f;

        if (state == EnemyState.Chase && TargetIsValid())
        {
            float differenceX =
                target.position.x - transform.position.x;

            if (Mathf.Abs(differenceX) > attackRange * 0.5f)
            {
                horizontalSpeed =
                    Mathf.Sign(differenceX) * moveSpeed;
            }

            TryJump(differenceX);
        }

        Velocity = new Vector2(
            horizontalSpeed,
            Velocity.y
        );
    }

    bool IsBlocked()
    {
        return enemyHealth != null &&
            (enemyHealth.IsDead || enemyHealth.IsTakingHit);
    }

    bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    
void TryJump(float differenceX)
{
    if (!IsGrounded() || jumpCooldownTimer > 0f)
        return;

    float differenceY =
        target.position.y - transform.position.y;

    bool targetAbove =
        differenceY > jumpHeightThreshold &&
        Mathf.Abs(differenceX) <= jumpHorizontalRange;

    float direction = Mathf.Sign(differenceX);

    bool obstacleAhead =
        Mathf.Abs(differenceX) > attackRange &&
        HasObstacleAhead(direction);

    if (targetAbove || obstacleAhead)
    {
        Velocity = new Vector2(
            Velocity.x,
            jumpForce
        );

        jumpCooldownTimer = jumpCooldown;
        PlayAnimation("Jump");
    }
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
        float closest = detectionRadius;

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

                if (distance <= closest)
                {
                    nearest = player;
                    closest = distance;
                }
            }
        }

        Collider2D[] nearby =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRadius
            );

        foreach (Collider2D hit in nearby)
        {
            Bomb bomb = hit.GetComponentInParent<Bomb>();

            if (bomb == null || !bomb.IsActive)
                continue;

            // Never chase a previously kicked bomb
            if (kickedBombs.Contains(bomb))
    continue;

            float distance = Vector2.Distance(
                transform.position,
                bomb.transform.position
            );

            if (distance < closest)
            {
                nearest = bomb.transform;
                closest = distance;
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

        if (!exclamationShown &&
            stateTimer >= questionDuration)
        {
            exclamationShown = true;
            ShowExclamation();
        }

        if (stateTimer >=
            questionDuration + alertDuration)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    void UpdateChase()
    {
        if (!TargetIsValid())
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        FaceTarget();

        // Use jump animation while in the air
        if (!IsGrounded())
        {
            PlayAnimation("Jump");
        }
        else
        {
            PlayAnimation("Run");
        }

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

        // Attack may have changed the state
        if (state != EnemyState.Attack)
            return;

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

    // Remember the kicked bomb
    kickedBombs.Add(bomb);

    // Stop following the bomb
    target = null;
    restTimer = restAfterBombKick;

    // Do not change to Idle yet.
    // Let the Attack animation finish.
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
        {
            return bomb.IsActive &&
    !kickedBombs.Contains(bomb);
        }

        PlayerHealth health =
            target.GetComponent<PlayerHealth>();

        return health != null && !health.IsDead;
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

        HideAlerts();

        switch (state)
        {
            case EnemyState.Idle:
                target = null;
                PlayAnimation("Idle");
                break;

            case EnemyState.Alert:
                exclamationShown = false;
                ShowQuestion();
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
        if (animator == null ||
            currentAnimation == animationName)
            return;

        animator.Play(animationName, 0, 0f);
        currentAnimation = animationName;
    }

    void CreateAlertEffects()
    {
        if (alertPoint == null)
            return;

        if (questionPrefab != null)
        {
            questionInstance = Instantiate(
                questionPrefab,
                alertPoint
            );

            questionInstance.transform.localPosition =
                Vector3.zero;

            questionInstance.SetActive(false);
        }

        if (exclamationPrefab != null)
        {
            exclamationInstance = Instantiate(
                exclamationPrefab,
                alertPoint
            );

            exclamationInstance.transform.localPosition =
                Vector3.zero;

            exclamationInstance.SetActive(false);
        }
    }

    void ShowQuestion()
    {
        HideAlerts();

        if (questionInstance != null)
        {
            questionInstance.SetActive(true);
            RestartEffect(questionInstance);
        }
    }

    void ShowExclamation()
    {
        HideAlerts();

        if (exclamationInstance != null)
        {
            exclamationInstance.SetActive(true);
            RestartEffect(exclamationInstance);
        }
    }

    void RestartEffect(GameObject effect)
    {
        Animator effectAnimator =
            effect.GetComponent<Animator>();

        if (effectAnimator != null)
            effectAnimator.Play(0, 0, 0f);
    }

    void HideAlerts()
    {
        if (questionInstance != null)
            questionInstance.SetActive(false);

        if (exclamationInstance != null)
            exclamationInstance.SetActive(false);
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

    void OnDisable()
    {
        HideAlerts();
    }
    
bool HasObstacleAhead(float direction)
{
    if (Mathf.Abs(direction) < 0.01f)
        return false;

    Vector2 origin = groundCheck != null
        ? (Vector2)groundCheck.position +
          Vector2.up * obstacleCheckHeight
        : (Vector2)transform.position;

    RaycastHit2D hit = Physics2D.Raycast(
        origin,
        Vector2.right * direction,
        obstacleCheckDistance,
        obstacleLayers
    );

    if (hit.collider == null)
        return false;

    // Don't jump over the bomb we're trying to kick
    Bomb hitBomb = hit.collider.GetComponentInParent<Bomb>();

    if (hitBomb != null &&
        target != null &&
        hitBomb.transform == target)
    {
        return false;
    }

    return true;
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
}
