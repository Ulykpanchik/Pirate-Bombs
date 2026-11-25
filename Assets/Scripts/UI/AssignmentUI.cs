using UnityEngine;
using UnityEngine.UI;

public sealed class AssignmentUI : MonoBehaviour
{
    public LevelSession session;
    public Text hud, progress, resultText, saveStatus;
    public GameObject menuPanel, confirmationPanel, resultPanel;
    public Button pauseButton, resumeButton, resetButton, confirmButton, cancelButton, retryButton, nextButton, resultMenuButton;
    public Button[] levelButtons;
    public Slider volume; // Music slider; field retained for existing tooling.
    public Slider sfxVolume;
    public Button homeButton;
    public Text menuTitle;
    public Button creditsButton, closeCreditsButton;
    public GameObject creditsPanel;

    void Awake()
    {
        pauseButton.onClick.AddListener(() => session.SetPaused(true));
        resumeButton.onClick.AddListener(() => { if(session.InMainMenu) session.StartGame(); else session.SetPaused(false); });
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
        volume.onValueChanged.AddListener(v => { LevelSession.Store.SetMusicVolume(v); Refresh(); });
        if(sfxVolume != null) sfxVolume.onValueChanged.AddListener(v => {LevelSession.Store.SetSfxVolume(v);Refresh();});
        if(homeButton != null) homeButton.onClick.AddListener(session.OpenMainMenu);
        if(creditsButton != null) creditsButton.onClick.AddListener(() => creditsPanel.SetActive(true));
        if(closeCreditsButton != null) closeCreditsButton.onClick.AddListener(() => creditsPanel.SetActive(false));
        if(creditsPanel != null) creditsPanel.SetActive(false);
        foreach(var button in GetComponentsInChildren<Button>(true)) button.onClick.AddListener(() => AudioManager.Play(SoundEvent.Click));
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
        volume.SetValueWithoutNotify(d.musicVolume);
        if(sfxVolume != null) sfxVolume.SetValueWithoutNotify(d.sfxVolume);
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

    public void ShowMainMenu(bool show) {
        if(menuTitle != null) menuTitle.text=show ? "PIRATE BOMBS" : "Pirate Bombs - Levels & Settings";
        resumeButton.GetComponentInChildren<Text>().text=show ? "Play / Continue" : "Resume";
        if(homeButton != null) homeButton.gameObject.SetActive(!show);
        if(show) {resultPanel.SetActive(false);menuPanel.SetActive(true);}
    }
    public void ShowMenu(bool show)
    {
        menuPanel.SetActive(show);
        if(creditsPanel != null) creditsPanel.SetActive(false);
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
