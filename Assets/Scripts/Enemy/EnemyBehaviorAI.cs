
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBehaviorAI : MonoBehaviour
{
    public enum EnemyType
    {
        Extinguisher,
        Coward,
        BombEater
    }

    private enum State
    {
        Idle,
        Alert,
        Chase,
        Flee,
        Attack
    }

    [Header("Enemy Type")]
    public EnemyType enemyType;

    [Header("Detection")]
    public Transform player;
    public float detectionRadius = 6f;
    public float attackRange = 1f;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Jump")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.15f;
    public float jumpForce = 8f;
    public float jumpHeightThreshold = 0.8f;
    public float jumpHorizontalRange = 1.2f;
    public float jumpCooldown = 1.2f;

    [Header("Obstacle Detection")]
    public LayerMask obstacleLayers;
    public float obstacleCheckDistance = 0.4f;
    public float obstacleCheckHeight = 0.35f;

    [Header("Attack")]
    public int attackDamage = 1;
    public float attackHitTime = 0.2f;
    public float attackDuration = 0.5f;
    public float attackCooldown = 0.8f;

    [Header("Bomb Interaction")]
    public float restAfterBomb = 1f;
    public float fleeDistance = 8f;

    [Header("Alert")]
    public Transform alertPoint;
    public GameObject questionPrefab;
    public GameObject exclamationPrefab;
    public float questionDuration = 0.3f;
    public float alertDuration = 0.4f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth health;

    private State state = State.Idle;
    private Transform target;

    private GameObject questionInstance;
    private GameObject exclamationInstance;

    private float stateTimer;
    private float attackTimer;
    private float jumpTimer;
    private float restTimer;

    private bool attackApplied;
    private bool exclamationShown;
    private bool bombInteracted;

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
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
    }

    void Start()
    {
        CreateAlertEffects();
        ChangeState(State.Idle);
    }

    void Update()
    {
        if (IsBlocked())
        {
            HideAlerts();
            return;
        }

        stateTimer += Time.deltaTime;

        attackTimer = Mathf.Max(
            0f,
            attackTimer - Time.deltaTime
        );

        jumpTimer = Mathf.Max(
            0f,
            jumpTimer - Time.deltaTime
        );

        restTimer = Mathf.Max(
            0f,
            restTimer - Time.deltaTime
        );

        switch (state)
        {
            case State.Idle:
                if (restTimer <= 0f)
                    SearchForTarget();
                break;

            case State.Alert:
                UpdateAlert();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Flee:
                UpdateFlee();
                break;

            case State.Attack:
                UpdateAttack();
                break;
        }
    }

    void FixedUpdate()
    {
        if (IsBlocked())
            return;

        float horizontalSpeed = 0f;

        if (state == State.Chase && TargetIsValid())
        {
            float dx =
                target.position.x - transform.position.x;

            float dy =
                target.position.y - transform.position.y;

            float direction = Mathf.Sign(dx);

            if (Mathf.Abs(dx) > attackRange * 0.5f)
            {
                horizontalSpeed = direction * moveSpeed;
            }

            bool targetIsPlayer =
                target.GetComponent<PlayerHealth>() != null;

            // Only jump towards the player on higher platforms
            bool targetAbove =
                targetIsPlayer &&
                dy > jumpHeightThreshold &&
                Mathf.Abs(dx) <= jumpHorizontalRange &&
                Mathf.Abs(dx) > attackRange * 0.5f;

            // Jump if there is an actual obstacle ahead
            bool obstacleAhead =
                Mathf.Abs(dx) > attackRange &&
                HasObstacleAhead(direction);

            if (IsGrounded() &&
                jumpTimer <= 0f &&
                (targetAbove || obstacleAhead))
            {
                Jump();
            }
        }
        else if (state == State.Flee && TargetIsValid())
        {
            float dx =
                transform.position.x - target.position.x;

            float direction;

            if (Mathf.Abs(dx) > 0.05f)
            {
                direction = Mathf.Sign(dx);
            }
            else
            {
                direction = spriteRenderer != null &&
                    spriteRenderer.flipX ? 1f : -1f;
            }

            horizontalSpeed = direction * moveSpeed;

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction < 0f;

            // Jump over obstacles while escaping
            if (IsGrounded() &&
                jumpTimer <= 0f &&
                HasObstacleAhead(direction))
            {
                Jump();
            }
        }

        // Keep vertical physics intact
        Velocity = new Vector2(
            horizontalSpeed,
            Velocity.y
        );
    }

    void Jump()
    {
        if (!IsGrounded() || jumpTimer > 0f)
            return;

        Velocity = new Vector2(
            Velocity.x,
            jumpForce
        );

        jumpTimer = jumpCooldown;
        PlayAnimation("Jump");
    }

    bool IsBlocked()
    {
        return health != null &&
            (health.IsDead || health.IsTakingHit);
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

    bool HasObstacleAhead(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return false;

        if (bodyCollider == null)
            return false;

        Bounds bounds = bodyCollider.bounds;

        // Start in front of the body, not at ground level
        Vector2 origin = new Vector2(
            bounds.center.x +
                direction * (bounds.extents.x + 0.03f),
            bounds.center.y -
                bounds.extents.y * 0.25f
        );

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            Vector2.right * direction,
            obstacleCheckDistance,
            obstacleLayers
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            // Ignore our own colliders
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            Bomb hitBomb =
                hit.collider.GetComponentInParent<Bomb>();

            // Approach the bomb we want to extinguish/eat
            if (state == State.Chase &&
                hitBomb != null &&
                target != null &&
                hitBomb.transform == target)
            {
                continue;
            }

            Debug.DrawRay(
                origin,
                Vector2.right * direction *
                    obstacleCheckDistance,
                Color.red
            );

            return true;
        }

        Debug.DrawRay(
            origin,
            Vector2.right * direction *
                obstacleCheckDistance,
            Color.green
        );

        return false;
    }

    Bomb FindNearestBomb()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            detectionRadius
        );

        Bomb nearest = null;
        float closest = detectionRadius;

        foreach (Collider2D hit in hits)
        {
            Bomb bomb =
                hit.GetComponentInParent<Bomb>();

            if (bomb == null || !bomb.IsActive)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                bomb.transform.position
            );

            if (distance < closest)
            {
                closest = distance;
                nearest = bomb;
            }
        }

        return nearest;
    }

    Transform FindTarget()
    {
        // All three enemies react to active bombs
        Bomb bomb = FindNearestBomb();

        if (bomb != null)
            return bomb.transform;

        if (player != null)
        {
            PlayerHealth playerHealth =
                player.GetComponent<PlayerHealth>();

            if (playerHealth != null &&
                !playerHealth.IsDead &&
                Vector2.Distance(
                    transform.position,
                    player.position
                ) <= detectionRadius)
            {
                return player;
            }
        }

        return null;
    }

    void SearchForTarget()
    {
        target = FindTarget();

        if (target != null)
            ChangeState(State.Alert);
    }

    void UpdateAlert()
    {
        if (!TargetIsValid())
        {
            ChangeState(State.Idle);
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
            if (enemyType == EnemyType.Coward &&
                target.GetComponent<Bomb>() != null)
            {
                ChangeState(State.Flee);
            }
            else
            {
                ChangeState(State.Chase);
            }
        }
    }

    void UpdateChase()
    {
        if (!TargetIsValid())
        {
            ChangeState(State.Idle);
            return;
        }

        // The Coward prioritizes escaping from bombs
        if (enemyType == EnemyType.Coward &&
            target.GetComponent<Bomb>() == null)
        {
            Bomb danger = FindNearestBomb();

            if (danger != null)
            {
                target = danger.transform;
                ChangeState(State.Alert);
                return;
            }
        }

        FaceTarget();

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
            attackTimer <= 0f)
        {
            ChangeState(State.Attack);
        }
    }

    void UpdateFlee()
    {
        if (!TargetIsValid())
        {
            ChangeState(State.Idle);
            return;
        }

        Bomb danger = target.GetComponent<Bomb>();

        if (danger == null)
        {
            ChangeState(State.Idle);
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            danger.transform.position
        );

        // Get outside detection range before stopping
        float safeDistance = Mathf.Max(
            fleeDistance,
            detectionRadius + 1f
        );

        if (distance >= safeDistance)
        {
            ChangeState(State.Idle);
            restTimer = 0.5f;
            return;
        }

        if (!IsGrounded())
{
    PlayAnimation("Jump");
}
else
{
    PlayAnimation("ScareRun");
}
    }

    void UpdateAttack()
    {
        if (!attackApplied &&
            stateTimer >= attackHitTime)
        {
            attackApplied = true;
            PerformAttack();
        }

        if (stateTimer >= attackDuration)
        {
            attackTimer = attackCooldown;

            if (bombInteracted)
            {
                bombInteracted = false;

                ChangeState(State.Idle);
                restTimer = restAfterBomb;
            }
            else if (TargetIsValid())
            {
                ChangeState(State.Chase);
            }
            else
            {
                ChangeState(State.Idle);
            }
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

        Bomb bomb = target.GetComponent<Bomb>();

        if (bomb != null && bomb.IsActive)
        {
            if (enemyType == EnemyType.Extinguisher)
            {
                bomb.Extinguish();
                bombInteracted = true;
            }
            else if (enemyType == EnemyType.BombEater)
            {
                bomb.Consume();
                bombInteracted = true;
            }

            if (bombInteracted)
            {
                // Forget this bomb but finish the animation
                target = null;
            }

            return;
        }

        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        if (playerHealth != null &&
            !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(
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

        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        return playerHealth != null &&
            !playerHealth.IsDead;
    }

    void FaceTarget()
    {
        if (target == null || spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            target.position.x < transform.position.x;
    }

    void ChangeState(State newState)
    {
        state = newState;
        stateTimer = 0f;

        HideAlerts();

        switch (state)
        {
            case State.Idle:
            {
                target = null;
                PlayAnimation("Idle");
                break;
            }

            case State.Alert:
            {
                exclamationShown = false;
                ShowQuestion();
                PlayAnimation("Idle");
                break;
            }

            case State.Chase:
{
    PlayAnimation("Run");
    break;
}

case State.Flee:
{
    PlayAnimation("ScareRun");
    break;
}

            case State.Attack:
            {
                attackApplied = false;
                bombInteracted = false;

                Bomb targetBomb = target != null
                    ? target.GetComponent<Bomb>()
                    : null;

                if (targetBomb != null)
                {
                    if (enemyType == EnemyType.Extinguisher)
                    {
                        PlayAnimation("Extinguish");
                    }
                    else if (enemyType == EnemyType.BombEater)
                    {
                        PlayAnimation("Eat");
                    }
                    else
                    {
                        PlayAnimation("Attack");
                    }
                }
                else
                {
                    PlayAnimation("Attack");
                }

                break;
            }
        }
    }

    void PlayAnimation(string name)
    {
        if (animator == null ||
            currentAnimation == name)
            return;

        animator.Play(name, 0, 0f);
        currentAnimation = name;
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
        if (health != null && health.IsDead)
            return;

        currentAnimation = "";
        ChangeState(State.Idle);
    }

    void OnDisable()
    {
        HideAlerts();
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
