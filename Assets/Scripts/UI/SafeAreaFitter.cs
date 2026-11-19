
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateSafeArea();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            UpdateSafeArea();
        }
    }

    void UpdateSafeArea()
    {
        Rect safeArea = Screen.safeArea;

        lastSafeArea = safeArea;
        // Device Simulator can leave a safe area from a different resolution.
        // Never let that stale rectangle move the HUD and touch controls off-screen.
        if (Screen.width <= 0 || Screen.height <= 0) return;
        if (safeArea.width <= 0 || safeArea.height <= 0 ||
            safeArea.xMin < 0 || safeArea.yMin < 0 ||
            safeArea.xMax > Screen.width || safeArea.yMax > Screen.height)
            safeArea = new Rect(0, 0, Screen.width, Screen.height);
        lastScreenSize = new Vector2Int(
            Screen.width, Screen.height
        );

        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
