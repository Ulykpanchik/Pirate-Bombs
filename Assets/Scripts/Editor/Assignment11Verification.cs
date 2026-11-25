using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class Assignment11Verification
{
    const string Folder = "Library/Assignment11Tests";
    const string Report = Folder + "/verification.txt";
    static IEnumerator routine;
    static double deadline;
    static Assignment11Verification()
    {
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, stack, type) => {
            if (SessionState.GetBool("Assignment11.Tests", false) && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
                File.AppendAllText(Folder + "/runtime-errors.txt", message + Environment.NewLine + stack + Environment.NewLine);
        };
    }
    static void Check(bool condition, string name)
    {
        File.AppendAllText(Report, (condition ? "PASS " : "FAIL ") + name + Environment.NewLine);
        if (!condition) throw new Exception("Verification failed: " + name);
    }
    [MenuItem("Pirate Bombs/Assignment 11/Run Verification")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/runtime-errors.txt", "");
        File.WriteAllText(Report, "Assignment 11 Unity verification - " + DateTime.Now + Environment.NewLine);
        int missing = 0;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
        Check(missing == 0, "Scene contains no missing scripts");
        string file = Path.GetFullPath(Folder + "/unit-progress.json");
        if (File.Exists(file)) File.Delete(file);
        var store = new ProgressStore(file);
        Check(File.Exists(file) && store.Data.highestUnlockedLevel == 1, "Missing file creates default JSON");
        store.Complete(1); store.SetVolume(0.37f);
        store = new ProgressStore(file);
        Check(store.Data.completedLevels[0] && store.Data.highestUnlockedLevel == 2 &&
            Mathf.Approximately(store.Data.soundVolume, 0.37f), "New store restores progress and sound from disk");
        string valid = JsonUtility.ToJson(ProgressStore.Defaults());
        string[] invalid = { "", "{bad json", "null", "{}", "[]",
            valid.Replace("\"version\":2", "\"version\":99"),
            valid.Replace("\"highestUnlockedLevel\":1", "\"highestUnlockedLevel\":9"),
            valid.Replace("\"soundVolume\":1.0", "\"soundVolume\":-1.0"),
            valid.Replace("\"completedLevels\":[false,false,false]", "\"completedLevels\":[true]"),
            valid.Replace("\"currentLevel\":1,", "") };
        for (int i = 0; i < invalid.Length; i++) {
            Check(invalid[i] != valid, "Invalid fixture differs from valid JSON " + i);
            File.WriteAllText(file, invalid[i]); store = new ProgressStore(file);
            Check(ProgressStore.IsValid(store.Data) && store.Data.highestUnlockedLevel == 1 &&
                !store.Data.completedLevels.Any(v => v), "Invalid save recovers safely " + i);
        }
        Check(Directory.GetFiles(Folder, "unit-progress.json.invalid-*").Length > 0, "Corrupt data preserved for diagnosis");
        store.Complete(1); store.Reset(); store = new ProgressStore(file);
        Check(store.Data.highestUnlockedLevel == 1 && store.Data.soundVolume == 1f, "Reset is persisted");
        string blocked = Path.GetFullPath(Folder + "/not-a-directory");
        File.WriteAllText(blocked, "test");
        var unwritable = new ProgressStore(Path.Combine(blocked, "progress.json"));
        Check(unwritable.Data != null && unwritable.LastError != null, "Disk failure keeps valid in-memory defaults without crashing");

        string playFile = Path.GetFullPath(Folder + "/play-progress.json");
        if (File.Exists(playFile)) File.Delete(playFile);
        SessionState.SetInt("Assignment11.TestStage", 1);
        SessionState.SetBool("Assignment11.Tests", true);
        EditorApplication.isPlaying = true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("Assignment11.Tests", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) {
            routine = SessionState.GetInt("Assignment11.TestStage", 1) == 1 ? PlayTests() : RestartTests();
            deadline = EditorApplication.timeSinceStartup + 90;
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt("Assignment11.TestStage", 0) == 2)
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (routine == null) return;
        try {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Play tests timed out.");
            if (!routine.MoveNext()) routine = null;
        } catch (Exception e) {
            File.AppendAllText(Report, "FAIL " + e + Environment.NewLine);
            routine = null; SessionState.SetBool("Assignment11.Tests", false);
            EditorApplication.isPlaying = false; Debug.LogException(e);
        }
    }
    static IEnumerator Wait(double seconds)
    {
        double until = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < until) yield return null;
    }
    static IEnumerator PlayTests()
    {
        var wait = Wait(1); while (wait.MoveNext()) yield return null;
        var session = LevelSession.Instance;
        Check(session != null && session.ui != null, "Scene has manager and connected UI");
        Check(LevelSession.Store.FilePath.EndsWith("play-progress.json"), "Play tests use isolated save file");
        session.ui.pauseButton.onClick.Invoke();
        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        session.ui.menuPanel.GetComponent<RectTransform>().GetWorldCorners(corners);
        Check(corners[0].x >= 0 && corners[0].y >= 0 && corners[2].x <= Screen.width && corners[2].y <= Screen.height,
            "Menu lies inside visible screen bounds");
        Check(Time.timeScale == 0 && session.ui.menuPanel.activeSelf, "Pause button freezes gameplay and shows settings");
        Check(LevelSession.Store.LastSaveReason == "pause", "Pause writes JSON");
        string before = File.ReadAllText(LevelSession.Store.FilePath);
        session.ui.volume.value = 0.42f;
        Check(File.ReadAllText(LevelSession.Store.FilePath) != before && Mathf.Approximately(LevelSession.Store.Data.musicVolume, 0.42f) && AudioManager.Instance.mixer.GetFloat("MusicVolumeDb", out float musicDb) && Mathf.Abs(musicDb - AudioManager.ToDecibels(0.42f)) < 0.01f,
            "Volume slider changes audio and JSON immediately");
        session.ui.resetButton.onClick.Invoke();
        Check(session.ui.confirmationPanel.activeSelf, "Reset opens in-game confirmation");
        before = File.ReadAllText(LevelSession.Store.FilePath);
        session.ui.cancelButton.onClick.Invoke();
        Check(!session.ui.confirmationPanel.activeSelf && before == File.ReadAllText(LevelSession.Store.FilePath),
            "Cancel preserves JSON and progress");
        Check(!session.ui.levelButtons[1].interactable && !session.ui.levelButtons[2].interactable, "Locked levels cannot be selected");
        session.LoadLevel(3);
        Check(LevelSession.Store.Data.currentLevel == 1, "Manager rejects locked level requests");
        int writes = LevelSession.Store.SuccessfulWrites;
        session.SendMessage("OnApplicationFocus", false);
        Check(LevelSession.Store.SuccessfulWrites > writes, "Focus loss saves");
        writes = LevelSession.Store.SuccessfulWrites;
        session.SendMessage("OnApplicationPause", true);
        Check(LevelSession.Store.SuccessfulWrites > writes, "Application pause saves");
        for (int level = 1; level <= 3; level++) {
            session = LevelSession.Instance;
            session.SetPaused(true);
            var d = LevelSession.ActiveData;
            Check(d == session.levels[level - 1] && d.levelNumber == level, "Level " + level + " reads its own ScriptableObject");
            Check(session.RemainingEnemies == d.enemyCount, "Level " + level + " enemy count comes from data");
            var player = Object.FindFirstObjectByType<PlayerHealth>();
            var movement = player.GetComponent<PlayerMovement>();
            var animator = player.GetComponent<Animator>();
            Check(animator.HasState(0, Animator.StringToHash("Player_Ground")) && animator.HasState(0, Animator.StringToHash("Player_Idle")),
                "Player animation state names match the existing controller in level " + level);
            Check(player.CurrentHealth == d.playerHealth.maxHealth && movement.moveSpeed == d.playerMovement.moveSpeed,
                "Level " + level + " player balance applied before health initialization");
            var pb = player.GetComponent<PlayerBomb>();
            Check(pb.bombPrefab != null, "Original bomb prefab reference preserved");
            var bomb = Object.Instantiate(pb.bombPrefab, new Vector3(-1000, -1000), Quaternion.identity);
            Check(bomb.damage == d.bomb.damage && bomb.explosionRadius == d.bomb.explosionRadius, "Spawned bomb reads level data");
            Object.Destroy(bomb.gameObject);
            float originalRadius = d.bomb.explosionRadius;
            try {
                d.bomb.explosionRadius = originalRadius + 0.75f;
                bomb = Object.Instantiate(pb.bombPrefab, new Vector3(-1000, -1000), Quaternion.identity);
                Check(bomb.explosionRadius == originalRadius + 0.75f, "Changing LevelData affects newly spawned gameplay object");
                Object.Destroy(bomb.gameObject);
            } finally { d.bomb.explosionRadius = originalRadius; }
            // Exercise the real explosion damage path, not a completion shortcut.
            var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            player.transform.position = new Vector3(-1000, 0, 0);
            int originalDamage = d.bomb.damage;
            try {
                d.bomb.damage = d.enemyHealth.maxHealth;
                foreach (var enemy in enemies) {
                    if (enemy.IsDead) continue;
                    bomb = Object.Instantiate(pb.bombPrefab, enemy.transform.position, Quaternion.identity);
                    Physics2D.SyncTransforms();
                    bomb.SendMessage("Explode");
                    Check(enemy.IsDead, "Bomb explosion defeats " + enemy.name + " in level " + level);
                }
            } finally { d.bomb.damage = originalDamage; }
            session.SetPaused(false);
            wait = Wait(0.4); while (wait.MoveNext()) yield return null;
            Check(session.Completed && session.ui.resultPanel.activeSelf, "Defeating enemies completes level " + level);
            var disk = new ProgressStore(LevelSession.Store.FilePath);
            Check(disk.Data.completedLevels[level - 1] && disk.Data.highestUnlockedLevel == Mathf.Min(level + 1, 3),
                "Level " + level + " completion saves and unlocks the next level");
            if (level < 3) {
                session.ui.nextButton.onClick.Invoke();
                wait = Wait(0.7); while (wait.MoveNext()) yield return null;
            }
        }
        LevelSession.Store.SetVolume(0.42f);
        SessionState.SetInt("Assignment11.TestStage", 2);
        routine = null;
        EditorApplication.isPlaying = false;
    }
    static IEnumerator RestartTests()
    {
        var wait = Wait(1); while (wait.MoveNext()) yield return null;
        var session = LevelSession.Instance;
        Check(LevelSession.Store.Data.currentLevel == 3 && LevelSession.Store.Data.completedLevels.All(v => v) &&
            Mathf.Approximately(LevelSession.Store.Data.soundVolume, 0.42f), "Progress and settings survive exiting and re-entering Play mode (domain reload)");
        session.SetPaused(true);
        session.ui.resetButton.onClick.Invoke();
        string before = File.ReadAllText(LevelSession.Store.FilePath);
        session.ui.cancelButton.onClick.Invoke();
        Check(File.ReadAllText(LevelSession.Store.FilePath) == before && LevelSession.Store.Data.highestUnlockedLevel == 3,
            "Cancel preserves completed three-level progress");
        session.ui.resetButton.onClick.Invoke();
        session.ui.confirmButton.onClick.Invoke();
        wait = Wait(0.7); while (wait.MoveNext()) yield return null;
        session = LevelSession.Instance;
        Check(LevelSession.ActiveData.levelNumber == 1 && LevelSession.Store.Data.highestUnlockedLevel == 1 &&
            !LevelSession.Store.Data.completedLevels.Any(v => v), "Confirm resets progression and reloads level 1");
        Check(LevelSession.Store.Data.soundVolume == 1 && !session.ui.levelButtons[1].interactable,
            "Reset updates volume and locked-level UI immediately");
        var disk = new ProgressStore(LevelSession.Store.FilePath);
        Check(disk.Data.highestUnlockedLevel == 1 && !disk.Data.completedLevels.Any(v => v), "Confirmed reset is saved to JSON");
        Check(new FileInfo(Folder + "/runtime-errors.txt").Length == 0, "No runtime errors or exceptions during Play verification");
        File.AppendAllText(Report, "ALL AUTOMATED CHECKS PASSED" + Environment.NewLine);
        SessionState.SetBool("Assignment11.Tests", false);
        SessionState.SetInt("Assignment11.TestStage", 0);
        routine = null; EditorApplication.isPlaying = false;
    }
}
