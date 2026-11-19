using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Assignment11Setup
{
    static readonly string CommandPath = "Library/Assignment11-command.txt";
    static Assignment11Setup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        File.Delete(CommandPath);
        try {
            if (command == "setup") Configure();
            else if (command == "tests") Assignment11Verification.Run();
            else if (command == "play") EditorApplication.isPlaying = true;
            else if (command == "stop") { SessionState.SetBool("Assignment11.Demo", false); EditorApplication.isPlaying = false; }
            else if (command == "android") BuildAndroid();
            else if (command == "inspect") Inspect();
            else if (command == "layout") FixLayout();
            else if (command == "demo") Assignment11Demo.Begin();
            else if (command == "encode") EncodeVideo();
            else if (command == "pause") LevelSession.Instance.SetPaused(true);
            else if (command == "snapshot") ScreenCapture.CaptureScreenshot("Library/Assignment11-game.png");
            File.WriteAllText("Library/Assignment11-command-result.txt", "OK: " + command);
        } catch (Exception e) {
            File.WriteAllText("Library/Assignment11-command-result.txt", e.ToString());
            Debug.LogException(e);
        }
    }

    [MenuItem("Pirate Bombs/Assignment 11/Encode Recorded Video")]
    public static void EncodeVideo()
    {
        string folder = "Library/Assignment11Recording";
        string[] dimensions = File.ReadAllText(folder + "/dimensions.txt").Split(',');
        int width = int.Parse(dimensions[0]), height = int.Parse(dimensions[1]), fps = int.Parse(dimensions[2]);
        var attributes = new UnityEditor.Media.VideoTrackAttributes {
            width = (uint)width, height = (uint)height,
            frameRate = new UnityEditor.Media.MediaRational(fps), includeAlpha = false
        };
        Directory.CreateDirectory("Deliverables");
        string output = "Deliverables/PirateBombs-Assignment11-demo.mp4";
        string[] frames = Directory.GetFiles(folder, "frame-*.jpg");
        Array.Sort(frames, StringComparer.Ordinal);
        using (var encoder = new UnityEditor.Media.MediaEncoder(output, attributes)) {
            var source = new Texture2D(width, height, TextureFormat.RGB24, false);
            var rgba = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try {
                foreach (string frame in frames) {
                    source.LoadImage(File.ReadAllBytes(frame));
                    rgba.SetPixels32(source.GetPixels32()); rgba.Apply();
                    if (!encoder.AddFrame(rgba)) throw new Exception("Video encoder rejected " + frame);
                }
            } finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(rgba); }
        }
        File.WriteAllText(folder + "/encoded.txt", output + " : " + frames.Length + " frames at " + fps + " fps");
    }

    static void FixLayout()
    {
        var ui = UnityEngine.Object.FindFirstObjectByType<AssignmentUI>();
        var r = ui.hud.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0, -40); r.sizeDelta = new Vector2(500, 70);
        ui.hud.alignment = TextAnchor.MiddleCenter;
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
    }

    static void Inspect()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine("Screen " + Screen.width + "x" + Screen.height + " safe " + Screen.safeArea);
        b.AppendLine("Save " + Application.persistentDataPath);
        var session = UnityEngine.Object.FindFirstObjectByType<LevelSession>();
        if (session != null) b.AppendLine("Paused " + session.Paused + " active " + session.gameObject.activeInHierarchy);
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.AppendLine("Canvas " + canvas.name + " enabled " + canvas.enabled + " active " + canvas.gameObject.activeInHierarchy + " mode " + canvas.renderMode + " scale " + canvas.scaleFactor + " rect " + canvas.pixelRect);
        if (session != null) foreach (var r in session.GetComponentsInChildren<RectTransform>(true)) {
            var corners = new Vector3[4]; r.GetWorldCorners(corners);
            b.AppendLine(r.name + " active=" + r.gameObject.activeInHierarchy + " scale=" + r.lossyScale + " rect=" + r.rect + " corner=" + corners[0]);
        }
        File.WriteAllText("Library/Assignment11-inspect.txt", b.ToString());
    }

    [MenuItem("Pirate Bombs/Assignment 11/Configure Scene")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity")
            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        if (UnityEngine.Object.FindFirstObjectByType<LevelSession>() != null) {
            Debug.Log("Assignment 11 already configured."); return;
        }
        var root = new GameObject("Assignment11");
        var session = root.AddComponent<LevelSession>();
        session.levels = new LevelData[3];
        for (int i = 0; i < 3; i++)
            session.levels[i] = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level" + (i + 1) + "Data.asset");
        var canvasObject = new GameObject("AssignmentCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var safe = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.zero);
        safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
        safe.gameObject.AddComponent<SafeAreaFitter>();
        var ui = canvasObject.AddComponent<AssignmentUI>(); ui.session = session; session.ui = ui;
        ui.hud = Label("LevelHUD", safe, "", new Vector2(0, -40), new Vector2(500, 70), 22);
        var hudRect = ui.hud.rectTransform; hudRect.anchorMin = hudRect.anchorMax = new Vector2(0.5f, 1);
        ui.hud.alignment = TextAnchor.MiddleCenter;
        ui.pauseButton = Button("Menu", safe, "Pause / Menu", new Vector2(-105, -35), new Vector2(190, 52));
        var pauseRect = ui.pauseButton.GetComponent<RectTransform>(); pauseRect.anchorMin = pauseRect.anchorMax = Vector2.one;

        ui.resultPanel = Panel("LevelResult", safe, new Vector2(600, 330));
        ui.resultText = Label("ResultText", ui.resultPanel.transform, "Level complete!", new Vector2(0, 108), new Vector2(560, 60), 30);
        ui.nextButton = Button("NextLevel", ui.resultPanel.transform, "Next level", new Vector2(0, 40));
        ui.retryButton = Button("Retry", ui.resultPanel.transform, "Replay level", new Vector2(0, -30));
        ui.resultMenuButton = Button("ResultMenu", ui.resultPanel.transform, "Levels / Settings", new Vector2(0, -100));

        ui.menuPanel = Panel("PauseMenu", safe, new Vector2(620, 590));
        Label("Title", ui.menuPanel.transform, "Pirate Bombs - Levels & Settings", new Vector2(0, 248), new Vector2(600, 55), 27);
        ui.progress = Label("Progress", ui.menuPanel.transform, "", new Vector2(0, 195), new Vector2(580, 45), 22);
        ui.levelButtons = new Button[3];
        for (int i = 0; i < 3; i++)
            ui.levelButtons[i] = Button("Level" + (i + 1), ui.menuPanel.transform, "Level " + (i + 1), new Vector2(0, 137 - i * 64), new Vector2(420, 53));
        Label("VolumeLabel", ui.menuPanel.transform, "Sound volume", new Vector2(0, -57), new Vector2(500, 36), 21);
        var sliderRect = Rect("SoundVolume", ui.menuPanel.transform, new Vector2(0, -96), new Vector2(420, 28));
        sliderRect.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.35f);
        ui.volume = sliderRect.gameObject.AddComponent<Slider>();
        var handle = Rect("Handle", sliderRect, Vector2.zero, new Vector2(28, 36));
        var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(1f, 0.8f, 0.35f);
        ui.volume.handleRect = handle; ui.volume.targetGraphic = handleImage; ui.volume.minValue = 0; ui.volume.maxValue = 1;
        ui.resetButton = Button("ResetProgress", ui.menuPanel.transform, "Reset Progress", new Vector2(-140, -161), new Vector2(250, 55));
        ui.resumeButton = Button("Resume", ui.menuPanel.transform, "Resume", new Vector2(140, -161), new Vector2(250, 55));
        ui.saveStatus = Label("SaveStatus", ui.menuPanel.transform, "", new Vector2(0, -218), new Vector2(580, 42), 18);
        Label("Controls", ui.menuPanel.transform, "Move: A/D or arrows   Jump: Space   Bomb: hold/release F", new Vector2(0, -260), new Vector2(590, 34), 16);

        ui.confirmationPanel = Panel("ResetConfirmation", safe, new Vector2(660, 620));
        Label("ConfirmTitle", ui.confirmationPanel.transform, "Reset all progress?", new Vector2(0, 80), new Vector2(600, 65), 32);
        Label("ConfirmExplanation", ui.confirmationPanel.transform, "Levels and sound volume return to defaults.", new Vector2(0, 12), new Vector2(620, 50), 22);
        ui.confirmButton = Button("YesReset", ui.confirmationPanel.transform, "Yes, Reset", new Vector2(-145, -80), new Vector2(250, 65));
        ui.cancelButton = Button("Cancel", ui.confirmationPanel.transform, "Cancel", new Vector2(145, -80), new Vector2(250, 65));
        ui.menuPanel.SetActive(false); ui.confirmationPanel.SetActive(false); ui.resultPanel.SetActive(false);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.piratebombs.assignment11");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/Assignment11-setup.txt", "Configured SampleScene: Assignment11 / AssignmentCanvas. Save: " + Application.persistentDataPath + "/progress.json");
        Debug.Log("Assignment 11 scene configured and saved.");
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var g = new GameObject(name, typeof(RectTransform));
        var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = position; r.sizeDelta = size; return r;
    }
    static GameObject Panel(string name, Transform parent, Vector2 size)
    {
        var r = Rect(name, parent, Vector2.zero, size);
        r.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.09f, 0.13f, 0.98f);
        return r.gameObject;
    }
    static Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var t = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text = text;
        t.fontSize = fontSize; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false; return t;
    }
    static Button Button(string name, Transform parent, string text, Vector2 position, Vector2? size = null)
    {
        var r = Rect(name, parent, position, size ?? new Vector2(400, 55));
        var img = r.gameObject.AddComponent<Image>(); img.color = new Color(0.2f, 0.35f, 0.45f);
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;
        var label = Label("Label", r, text, Vector2.zero, r.sizeDelta, 23);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.sizeDelta = Vector2.zero;
        return b;
    }

    [MenuItem("Pirate Bombs/Assignment 11/Build Android APK")]
    public static void BuildAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Android Build Support is not installed for Unity 6000.6.3f1. Install Android Build Support, SDK/NDK Tools and OpenJDK through Unity Hub.");
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" }, target = BuildTarget.Android,
            locationPathName = "Builds/Android/PirateBombs-Assignment11.apk", options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("APK build failed: " + report.summary.result);
    }
}
