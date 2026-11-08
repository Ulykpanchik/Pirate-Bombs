
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HeartSpriteAnimator : MonoBehaviour
{
    [Header("Animation Frames")]
    public Sprite[] idleFrames;
    public Sprite[] disappearFrames;

    [Header("Animation Settings")]
    public float framesPerSecond = 20f;

    private Image heartImage;
    private Coroutine animationRoutine;
    private bool isDisappearing;

    void Awake()
    {
        heartImage = GetComponent<Image>();
    }

    void Start()
    {
        PlayIdle();
    }

    public void PlayIdle()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        isDisappearing = false;
        heartImage.enabled = true;

        animationRoutine = StartCoroutine(
            AnimateIdle()
        );
    }

    IEnumerator AnimateIdle()
    {
        if (idleFrames.Length == 0)
            yield break;

        int frame = 0;
        float delay = 1f / Mathf.Max(1f, framesPerSecond);

        while (!isDisappearing)
        {
            heartImage.sprite = idleFrames[frame];

            frame = (frame + 1) % idleFrames.Length;

            yield return new WaitForSecondsRealtime(delay);
        }
    }

    public void PlayDisappear()
    {
        if (isDisappearing)
            return;

        isDisappearing = true;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(
            AnimateDisappear()
        );
    }

    IEnumerator AnimateDisappear()
    {
        float delay = 1f / Mathf.Max(1f, framesPerSecond);

        foreach (Sprite frame in disappearFrames)
        {
            heartImage.sprite = frame;

            yield return new WaitForSecondsRealtime(delay);
        }

        heartImage.enabled = false;
        animationRoutine = null;
    }
}
