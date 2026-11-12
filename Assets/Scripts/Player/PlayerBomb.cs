
using UnityEngine;

public class PlayerBomb : MonoBehaviour
{
    [Header("Bomb")]
    public Bomb bombPrefab;
    public Transform bombSpawnPoint;

    [Header("Throw Settings")]
    public float holdThreshold = 0.2f;
    public float maxChargeTime = 2f;
    public float minThrowForce = 4f;
    public float maxThrowForce = 14f;
    public float throwUpForce = 5f;

    [Header("Placement")]
    public float placementDistance = 0.7f;

    [Header("Charge Bar")]
    public BombChargeBar chargeBar;

    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private PlayerHealth playerHealth;

    private bool isHolding;
    private float holdStartTime;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            StartBombHold();

        if (Input.GetKeyUp(KeyCode.F))
            ReleaseBomb();

        // Update charge bar while holding
        if (isHolding)
        {
            if (!CanUseBomb())
            {
                CancelBombHold();
                return;
            }

            float holdTime = Time.time - holdStartTime;

            if (chargeBar != null)
            {
                float charge = Mathf.InverseLerp(
                    holdThreshold,
                    maxChargeTime,
                    holdTime
                );

                chargeBar.SetCharge(charge);
            }
        }
    }

    public void StartBombHold()
    {
        if (isHolding || !CanUseBomb())
            return;

        isHolding = true;
        holdStartTime = Time.time;

        // Show charge bar
        if (chargeBar != null)
        {
            chargeBar.Show();
        }
    }

    public void ReleaseBomb()
    {
        if (!isHolding)
            return;

        isHolding = false;

        // Hide charge bar
        if (chargeBar != null)
        {
            chargeBar.Hide();
        }

        if (!CanUseBomb() || bombPrefab == null)
            return;

        float holdTime = Time.time - holdStartTime;

        float direction = spriteRenderer != null &&
            spriteRenderer.flipX ? -1f : 1f;

        if (holdTime < holdThreshold)
        {
            PlaceBomb(direction);
        }
        else
        {
            ThrowBomb(direction, holdTime);
        }
    }

    public void CancelBombHold()
    {
        isHolding = false;

        if (chargeBar != null)
            chargeBar.Hide();
    }

    bool CanUseBomb()
    {
        return playerHealth == null ||
            (!playerHealth.IsDead &&
             !playerHealth.IsTakingHit);
    }

    void PlaceBomb(float direction)
    {
        Vector3 position =
            transform.position +
            Vector3.right * direction * placementDistance;

        Bomb bomb = Instantiate(
            bombPrefab,
            position,
            Quaternion.identity
        );

        bomb.IgnorePlayerCollision(playerCollider);
    }

    void ThrowBomb(float direction, float holdTime)
    {
        Vector3 position = bombSpawnPoint != null
            ? bombSpawnPoint.position
            : transform.position + Vector3.up;

        Bomb bomb = Instantiate(
            bombPrefab,
            position,
            Quaternion.identity
        );

        bomb.IgnorePlayerCollision(playerCollider);

        // Calculate charge from hold duration
        float charge = Mathf.InverseLerp(
            holdThreshold,
            maxChargeTime,
            holdTime
        );

        float horizontalForce = Mathf.Lerp(
            minThrowForce,
            maxThrowForce,
            charge
        );

        Vector2 force = new Vector2(
            horizontalForce * direction,
            throwUpForce
        );

        bomb.Throw(force);
    }

    void OnDisable()
    {
        CancelBombHold();
    }
}
