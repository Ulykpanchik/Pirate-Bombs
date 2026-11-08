
using UnityEngine;

public class HeartUI : MonoBehaviour
{
    [Header("Player")]
    public PlayerHealth playerHealth;

    [Header("Hearts")]
    public HeartSpriteAnimator[] hearts;

    void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth not assigned!");
            return;
        }

        playerHealth.onHealthChanged.AddListener(
            UpdateHearts
        );

        UpdateHearts(playerHealth.CurrentHealth);
    }

    void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
                continue;

            if (i >= currentHealth)
            {
                hearts[i].PlayDisappear();
            }
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.onHealthChanged.RemoveListener(
                UpdateHearts
            );
        }
    }
}
