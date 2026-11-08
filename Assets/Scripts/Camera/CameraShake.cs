
using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Settings")]
    public float duration = 0.2f;
    public float intensity = 0.08f;

    private Coroutine shakeRoutine;
    private Vector3 currentOffset;

    void Awake()
    {
        Instance = this;
    }

    public void Shake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            RemoveOffset();
        }

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            RemoveOffset();

            currentOffset = new Vector3(
                Random.Range(-1f, 1f) * intensity,
                Random.Range(-1f, 1f) * intensity,
                0f
            );

            transform.localPosition += currentOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        RemoveOffset();
        shakeRoutine = null;
    }

    void RemoveOffset()
    {
        transform.localPosition -= currentOffset;
        currentOffset = Vector3.zero;
    }

    void OnDisable()
    {
        RemoveOffset();
    }
}
