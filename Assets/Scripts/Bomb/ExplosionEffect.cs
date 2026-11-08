
using System.Collections;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Explosion Animation")]
    public AnimationClip explosionClip;

    private IEnumerator Start()
    {
        if (explosionClip == null)
        {
            Debug.LogError(
                "ExplosionEffect: Explosion Clip is not assigned!"
            );

            Destroy(gameObject);
            yield break;
        }

        // Wait for the explosion animation to finish
        yield return new WaitForSeconds(
            explosionClip.length
        );

        // Remove the explosion effect
        Destroy(gameObject);
    }
}
