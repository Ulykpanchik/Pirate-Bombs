using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class LevelSession : MonoBehaviour
{
    public LevelData[] levels;
    public AssignmentUI ui;
    public static LevelSession Instance { get; private set; }
    public static LevelData ActiveData { get; private set; }
    public static ProgressStore Store { get; private set; }
    public bool Completed { get; private set; }
    public bool Failed { get; private set; }
    public bool Paused { get; private set; }
    public int RemainingEnemies => enemies == null ? 0 : enemies.Count(e => e != null && !e.IsDead);
    EnemyHealth[] enemies;
    PlayerHealth player;

#if UNITY_EDITOR
    // Editor tests use a separate file and never overwrite the player's progress.
    public static string TestSavePath;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; ActiveData = null; Store = null; }

    void Awake()
    {
        Instance = this;
        string savePath = Path.Combine(Application.persistentDataPath, "progress.json");
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("Assignment11.Tests", false)) savePath = Path.GetFullPath("Library/Assignment11Tests/play-progress.json");
        else if (UnityEditor.SessionState.GetBool("Assignment11.Demo", false)) savePath = Path.GetFullPath("Library/Assignment11Recording/demo-progress.json");
        else if (!string.IsNullOrEmpty(TestSavePath)) savePath = TestSavePath;
#endif
        if (Store == null || Store.FilePath != savePath) Store = new ProgressStore(savePath);
        ActiveData = levels[Store.Data.currentLevel - 1];
        AudioListener.volume = Store.Data.soundVolume;
        Time.timeScale = 1f;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        var all = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)
            .OrderBy(e => e.transform.position.x).ThenBy(e => e.name).ToArray();
        for (int i = ActiveData.enemyCount; i < all.Length; i++) all[i].gameObject.SetActive(false);
        enemies = all.Take(ActiveData.enemyCount).ToArray();
        if (enemies.Length == 0) Debug.LogError("Level has no enemies; completion disabled.");
        ui.Refresh();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!Paused);
        if (Completed || Failed || Paused || enemies == null) return;
        if (player == null || player.IsDead) { Failed = true; ui.ShowResult(false); return; }
        if (enemies.Length > 0 && RemainingEnemies == 0) {
            Completed = true;
            Store.Complete(ActiveData.levelNumber);
            ui.ShowResult(true);
        }
        ui.RefreshHUD();
    }

    public void SetPaused(bool pause)
    {
        Paused = pause;
        if (pause) {
            var bomb = FindFirstObjectByType<PlayerBomb>();
            if (bomb != null) bomb.CancelBombHold();
            MobileButton.LeftHeld = MobileButton.RightHeld = MobileButton.BombHeld = false;
            MobileButton.ConsumeJump();
            Store.Save("pause");
        }
        Time.timeScale = pause ? 0f : 1f;
        ui.ShowMenu(pause);
    }

    public void LoadLevel(int level)
    {
        if (level < 1 || level > Store.Data.highestUnlockedLevel || level > levels.Length) return;
        Store.Data.currentLevel = level;
        Store.Save("level selected");
        Time.timeScale = 1f;
        SceneManager.LoadScene(levels[level - 1].sceneName);
    }

    public void ConfirmReset()
    {
        Store.Reset();
        ui.Refresh();
        LoadLevel(1);
    }

    void OnApplicationPause(bool paused) { if (paused && Store != null) SetPaused(true); }
    void OnApplicationFocus(bool focus) { if (!focus && Store != null && ui != null) SetPaused(true); }
    void OnApplicationQuit() { if (Store != null) Store.Save("quit"); }
    void OnDestroy() { Time.timeScale = 1f; if (Instance == this) Instance = null; }
}
