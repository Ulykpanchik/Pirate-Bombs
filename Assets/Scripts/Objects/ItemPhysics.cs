
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ItemPhysics : MonoBehaviour
{
    [Header("Explosion Physics")]
    public float explosionForce = 8f;
    public float upwardForce = 4f;
    public float rotationForce = 150f;

    [Header("Settings")]
    public bool startStatic = true;
    public bool canBeExplodedAgain = true;

    private Rigidbody2D rb;
    private bool hasExploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Stay still until an explosion
        if (startStatic)
            rb.bodyType = RigidbodyType2D.Static;
    }

    public void ApplyExplosion(Vector2 explosionPosition)
    {
        if (hasExploded && !canBeExplodedAgain)
            return;

        hasExploded = true;

        // Activate physics
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.WakeUp();

        Vector2 direction =
            (Vector2)transform.position - explosionPosition;

        // Send the item away from the explosion
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;

        direction.Normalize();

        Vector2 force =
            direction * explosionForce +
            Vector2.up * upwardForce;

        rb.AddForce(force, ForceMode2D.Impulse);

        // Random rotation
        float torque = Random.Range(
            -rotationForce,
            rotationForce
        );

        rb.AddTorque(torque, ForceMode2D.Impulse);
    }
}
