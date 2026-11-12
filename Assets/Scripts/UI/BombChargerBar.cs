
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BombChargeBar : MonoBehaviour
{
    [Header("Charge Sprites")]
    public Sprite[] chargeFrames;

    [Header("Settings")]
    public bool hideWhenNotCharging = true;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (hideWhenNotCharging)
            spriteRenderer.enabled = false;
    }

    public void Show()
    {
        spriteRenderer.enabled = true;
        SetCharge(0f);
    }

    public void SetCharge(float normalizedCharge)
    {
        if (chargeFrames == null ||
            chargeFrames.Length == 0)
            return;

        normalizedCharge = Mathf.Clamp01(normalizedCharge);

        int frameIndex = Mathf.FloorToInt(
            normalizedCharge * (chargeFrames.Length - 1)
        );

        frameIndex = Mathf.Clamp(
            frameIndex,
            0,
            chargeFrames.Length - 1
        );

        spriteRenderer.sprite = chargeFrames[frameIndex];
    }

    public void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}
