#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Editor-only captured walkthrough. Never included in a player build.</summary>
public sealed class Assignment11Demo : MonoBehaviour
{
    const string Folder = "Library/Assignment11Recording";
    Text caption;
    bool recording;
    int frame;
    float nextFrame;

    [InitializeOnLoadMethod]
    static void Register()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Assignment11.Demo", false))
                new GameObject("Assignment11 demonstration capture").AddComponent<Assignment11Demo>();
        };
    }

    [MenuItem("Pirate Bombs/Assignment 11/Record Demonstration")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before recording.");
        Directory.CreateDirectory(Folder);
        // Only this isolated demo file is reset; the player's real save is untouched.
        string save = Folder + "/demo-progress.json";
        if (File.Exists(save)) File.Delete(save);
        foreach (string f in Directory.GetFiles(Folder, "frame-*.jpg")) File.Delete(f);
        SessionState.SetBool("Assignment11.Demo", true);
        EditorApplication.isPlaying = true;
    }

    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        var canvasGO = new GameObject("Demo captions", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var label = new GameObject("Caption", typeof(RectTransform), typeof(Image));
        label.transform.SetParent(canvasGO.transform, false);
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(0.5f, 0);
        rect.sizeDelta = new Vector2(0, 64); rect.anchoredPosition = Vector2.zero;
        label.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f); label.GetComponent<Image>().raycastTarget = false;
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text)); textGO.transform.SetParent(rect, false);
        caption = textGO.GetComponent<Text>(); caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caption.fontSize = 22; caption.color = Color.white; caption.alignment = TextAnchor.MiddleCenter; caption.raycastTarget = false;
        var tr = caption.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(12, 0); tr.offsetMax = new Vector2(-12, 0);
        yield return null;
        File.WriteAllText(Folder + "/dimensions.txt", Screen.width + "," + Screen.height + ",8");
        recording = true; StartCoroutine(Capture());
        caption.text = "Assignment 11 - live Unity capture (automated walkthrough; isolated save)";
        yield return new WaitForSecondsRealtime(3);
        caption.text = "Original movement, jump and charged bomb controls";
        MobileButton.RightHeld = true;
        yield return new WaitForSecondsRealtime(0.6f);
        MobileButton.RightHeld = false;
        var jump = Array.Find(Object.FindObjectsByType<MobileButton>(FindObjectsSortMode.None), b => b.buttonType == MobileButton.ButtonType.Jump);
        if (jump != null) jump.OnPointerDown(null);
        yield return new WaitForSecondsRealtime(1);
        var pb = Object.FindFirstObjectByType<PlayerBomb>();
        pb.StartBombHold();
        yield return new WaitForSecondsRealtime(0.8f);
        pb.ReleaseBomb();
        yield return new WaitForSecondsRealtime(3.5f);
        LevelSession.Instance.SetPaused(true);
        caption.text = "Pause saves automatically. Levels 2 and 3 start locked.";
        yield return new WaitForSecondsRealtime(3);
        LevelSession.Instance.ui.volume.value = 0.35f;
        caption.text = "Changing music volume writes progress.json immediately.";
        yield return new WaitForSecondsRealtime(3);
        LevelSession.Instance.ui.resetButton.onClick.Invoke();
        caption.text = "Reset asks for confirmation. Cancel keeps progress and settings.";
        yield return new WaitForSecondsRealtime(3);
        LevelSession.Instance.ui.cancelButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(1);
        // Reload the current level so the opening bomb cannot influence the test.
        LevelSession.Instance.LoadLevel(1);
        yield return new WaitForSecondsRealtime(1);
        for (int level = 1; level <= 3; level++) {
            var session = LevelSession.Instance;
            session.SetPaused(true);
            var data = LevelSession.ActiveData;
            caption.text = "Level " + level + " reads Level" + level + "Data: " + data.enemyCount + " enemies, " + data.enemyHealth.maxHealth + " health.";
            yield return new WaitForSecondsRealtime(3);
            session.SetPaused(false);
            caption.text = "Automated completion test: bomb damage temporarily raised, then restored.";
            var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            pb = Object.FindFirstObjectByType<PlayerBomb>();
            // Use real bomb explosions; only the demo's spawned bombs use boosted damage.
            foreach (var enemy in enemies) {
                if (enemy == null || enemy.IsDead) continue;
                var bomb = Object.Instantiate(pb.bombPrefab, enemy.transform.position, Quaternion.identity);
                bomb.damage = data.enemyHealth.maxHealth;
                bomb.damageLayers &= ~(1 << pb.gameObject.layer);
                Physics2D.SyncTransforms(); bomb.SendMessage("Explode");
            }
            yield return new WaitForSecondsRealtime(0.6f);
            caption.text = "Level complete -> JSON saved, next level unlocked automatically.";
            yield return new WaitForSecondsRealtime(3);
            if (level < 3) {
                session.ui.nextButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.7f);
            }
        }
        LevelSession.Instance.SetPaused(true);
        caption.text = "All three completions are stored in JSON.";
        yield return new WaitForSecondsRealtime(3);
        LevelSession.Instance.ui.resetButton.onClick.Invoke();
        caption.text = "Yes, Reset clears progression and restores default settings.";
        yield return new WaitForSecondsRealtime(3);
        LevelSession.Instance.ui.confirmButton.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.8f);
        LevelSession.Instance.SetPaused(true);
        caption.text = "Reset saved: Level 1 unlocked, Levels 2/3 locked, volume restored.";
        yield return new WaitForSecondsRealtime(3);
        caption.text = "Save recovery and restart persistence: see the Unity verification report.";
        yield return new WaitForSecondsRealtime(3);
        recording = false;
        File.WriteAllText(Folder + "/complete.txt", frame + " captured frames. Real player save was not changed.");
        SessionState.SetBool("Assignment11.Demo", false);
        EditorApplication.isPlaying = false;
    }

    IEnumerator Capture()
    {
        while (recording) {
            yield return new WaitForEndOfFrame();
            if (Time.realtimeSinceStartup < nextFrame) continue;
            nextFrame = Time.realtimeSinceStartup + 1f / 8f;
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Folder + "/frame-" + frame.ToString("D5") + ".jpg", texture.EncodeToJPG(85));
            Object.Destroy(texture);
            frame++;
        }
    }
}

#endif
