using UnityEngine;
using UnityEngine.UI;

public sealed class AssignmentUI : MonoBehaviour
{
    public LevelSession session;
    public Text hud, progress, resultText, saveStatus;
    public GameObject menuPanel, confirmationPanel, resultPanel;
    public Button pauseButton, resumeButton, resetButton, confirmButton, cancelButton, retryButton, nextButton, resultMenuButton;
    public Button[] levelButtons;
    public Slider volume;

    void Awake()
    {
        pauseButton.onClick.AddListener(() => session.SetPaused(true));
        resumeButton.onClick.AddListener(() => session.SetPaused(false));
        resetButton.onClick.AddListener(() => confirmationPanel.SetActive(true));
        cancelButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
        confirmButton.onClick.AddListener(session.ConfirmReset);
        retryButton.onClick.AddListener(() => session.LoadLevel(LevelSession.ActiveData.levelNumber));
        nextButton.onClick.AddListener(() => session.LoadLevel(LevelSession.ActiveData.levelNumber + 1));
        resultMenuButton.onClick.AddListener(() => session.SetPaused(true));
        for (int i = 0; i < levelButtons.Length; i++) {
            int level = i + 1;
            levelButtons[i].onClick.AddListener(() => session.LoadLevel(level));
        }
        volume.onValueChanged.AddListener(v => { LevelSession.Store.SetVolume(v); Refresh(); });
        menuPanel.SetActive(false);
        confirmationPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    public void RefreshHUD()
    {
        var d = LevelSession.ActiveData;
        hud.text = "Level " + d.levelNumber + " - " + d.displayName + "\nEnemies left: " + session.RemainingEnemies;
    }

    public void Refresh()
    {
        if (LevelSession.Store == null) return;
        var d = LevelSession.Store.Data;
        volume.SetValueWithoutNotify(d.soundVolume);
        progress.text = "Unlocked: " + d.highestUnlockedLevel + "/3    Completed: " +
            System.Array.FindAll(d.completedLevels, value => value).Length + "/3";
        for (int i = 0; i < levelButtons.Length; i++) {
            levelButtons[i].interactable = i < d.highestUnlockedLevel;
            levelButtons[i].GetComponentInChildren<Text>().text = "Level " + (i + 1) +
                (d.completedLevels[i] ? " - Done" : i >= d.highestUnlockedLevel ? " - Locked" : "");
        }
        saveStatus.text = LevelSession.Store.LastError == null ? "Progress saved automatically" : "Could not save. Check available storage.";
        RefreshHUD();
    }

    public void ShowMenu(bool show)
    {
        menuPanel.SetActive(show);
        confirmationPanel.SetActive(false);
        if (!show && (session.Completed || session.Failed)) Time.timeScale = 0f;
        Refresh();
    }

    public void ShowResult(bool won)
    {
        resultPanel.SetActive(true);
        resultText.text = won ? (LevelSession.ActiveData.levelNumber == 3 ? "All three levels complete!" : "Level complete!") : "Defeated - try again";
        nextButton.gameObject.SetActive(won && LevelSession.ActiveData.levelNumber < 3);
        Time.timeScale = 0f;
        Refresh();
    }
}
