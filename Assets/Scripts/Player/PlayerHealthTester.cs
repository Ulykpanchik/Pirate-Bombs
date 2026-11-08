
using UnityEngine;

public class PlayerHealthTester : MonoBehaviour
{
    private PlayerHealth health;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            health.TakeDamage(1);
        }
    }
}
