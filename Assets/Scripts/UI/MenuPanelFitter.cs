using UnityEngine;

// Keep the expanded settings panel inside the existing safe-area canvas.
[RequireComponent(typeof(RectTransform))]
public sealed class MenuPanelFitter : MonoBehaviour {
    RectTransform panel, parent;
    Vector2 lastAvailable, lastSize;
    void Awake() { panel=(RectTransform)transform; parent=transform.parent as RectTransform; }
    void OnEnable() { lastAvailable=Vector2.zero; LateUpdate(); }
    void LateUpdate() {
        if(parent==null)return;
        Vector2 available=parent.rect.size, size=panel.rect.size;
        if(available==lastAvailable&&size==lastSize)return;
        lastAvailable=available;lastSize=size;
        if(size.x<=0||size.y<=0)return;
        float scale=Mathf.Clamp(Mathf.Min((available.x-24)/size.x,(available.y-24)/size.y),0.1f,1f);
        panel.localScale=new Vector3(scale,scale,1);
    }
}
