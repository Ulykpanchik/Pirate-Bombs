
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bomb : MonoBehaviour
{
    [Header("Explosion")]
    public float explosionDelay = 3f;
    public float explosionRadius = 2f;
    public int damage = 1;
    public LayerMask damageLayers = ~0;

    [Header("Explosion Effect")]
    public GameObject explosionPrefab;
    public Vector2 explosionOffset = new Vector2(0f, 0.35f);

    [Header("Fuse Settings")]
    public float ignitionDelay = 0.5f;

    [Header("Extinguished Bomb")]
    public Sprite extinguishedSprite;
    public float extinguishDuration = 0.5f;

    private Rigidbody2D rb;
    private Collider2D bombCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float timer;
    private bool fuseStarted;
    private bool exploded;
    private bool extinguished;

    public bool IsActive => !exploded && !extinguished;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bombCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Start with normal bomb animation
        if (animator != null)
        {
            animator.Play("Bomb_Idle", 0, 0f);
        }
    }

    void Update()
    {
        if (!IsActive)
            return;

        timer += Time.deltaTime;

        // Start burning after delay
        if (!fuseStarted && timer >= ignitionDelay)
        {
            fuseStarted = true;

            if (animator != null)
            {
                animator.Play("Bomb_Prepare", 0, 0f);
            }
        }

        // Explode after timer
        if (timer >= explosionDelay)
        {
            Explode();
        }
    }

    public void IgnorePlayerCollision(Collider2D playerCollider)
    {
        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(
                bombCollider,
                playerCollider
            );
        }
    }

    public void Throw(Vector2 force)
    {
        if (!IsActive)
            return;

        rb.AddForce(force, ForceMode2D.Impulse);
    }

    public void Extinguish()
    {
        if (!IsActive)
            return;

        extinguished = true;
        StartCoroutine(ExtinguishRoutine());
    }

    private IEnumerator ExtinguishRoutine()
    {
        // Stop bomb movement
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;
#else
        rb.velocity = Vector2.zero;
#endif

        rb.simulated = false;

        // Stop burning animation
        if (animator != null)
            animator.enabled = false;

        // Show extinguished bomb sprite
        if (extinguishedSprite != null)
            spriteRenderer.sprite = extinguishedSprite;

        // Disable collisions
        if (bombCollider != null)
            bombCollider.enabled = false;

        yield return new WaitForSeconds(extinguishDuration);

        Destroy(gameObject);
    }

    private void Explode()
    {
        if (!IsActive)
            return;

        exploded = true;

        // Spawn explosion animation
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position + (Vector3)explosionOffset,
                Quaternion.identity
            );
        }

        // Explosion damage
        Collider2D[] targets = Physics2D.OverlapCircleAll(
            transform.position,
            explosionRadius,
            damageLayers
        );

        HashSet<PlayerHealth> damagedPlayers =
            new HashSet<PlayerHealth>();

        foreach (Collider2D target in targets)
        {
            PlayerHealth health =
                target.GetComponentInParent<PlayerHealth>();

            if (health != null &&
                damagedPlayers.Add(health))
            {
                health.TakeDamage(damage, transform.position);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}
