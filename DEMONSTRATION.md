# Assignment 11 demonstration checklist

Open C:/Users/ulikp/Pirate Bombs in Unity 6000.6.3f1 and open Assets/Scenes/SampleScene.unity.

1. Show Assets/Data/Level1Data.asset, Level2Data.asset and Level3Data.asset in the Inspector. Explain: the same existing combat map has three rounds; enemy count, health and speed increase.
2. Press Play. Move with A/D or arrow keys, jump with Space, tap F to place a bomb or hold/release F to throw. The on-screen mobile buttons still use the original controls.
3. Press Pause / Menu (or Escape). Show locked levels and the sound slider. Change volume.
4. Open C:/Users/ulikp/AppData/LocalLow/DefaultCompany/Pirate Bombs/progress.json in a text editor. Show soundVolume and the level fields. Pause or switch away from the game to trigger another save.
5. Defeat all enemies with bombs. Show the completion panel, then the JSON completedLevels/highestUnlockedLevel fields. Choose Next level and repeat. Level 3 completes the campaign. A defeat gives Replay level.
6. Stop and restart Play. Show that completed/unlocked levels and volume remain. A standalone application relaunch was not tested because an APK cannot be built on this installation.
7. In Pause / Menu choose Reset Progress, then Cancel. Show that nothing changes. Choose Reset Progress again, then Yes, Reset. Show level 1, locked levels 2/3, restored volume and reset JSON.
8. Stop Play before testing broken saves. Back up progress.json. Rename it and press Play: defaults are created. Stop, replace the file contents with {broken json, then press Play: the game recovers. Restore your backed-up progress after this demonstration.
9. To show a data edit, stop Play, note the original Bomb > Explosion Radius in Level1Data, change it, and restart level 1. The blast reach changes. Restore the original value afterward.
10. Show DECISIONS.md and the automated verification report. Run Pirate Bombs > Assignment 11 > Run Verification for repeatable checks using separate test saves.

## Suggested narration
"This is the existing Pirate Bombs game. These three ScriptableObjects hold the balance settings. The manager applies the selected data before gameplay starts. Progress and volume are saved as readable JSON in persistentDataPath. Completing a level unlocks the next one and saves. Changing volume and pausing also save. Missing or invalid JSON recovers to defaults. Reset requires confirmation, and Cancel preserves progress."

## Recording a manual version
Use your preferred screen recorder and capture Unity plus the text editor. Record steps 1-7 and 9-10; include step 8 if your teacher wants the recovery test on camera. Keep the original JSON backed up. Avoid displaying unrelated windows. Aim for 3-5 minutes, trimming combat time if needed.

The supplied automated capture labels accelerated completion and uses a demo save; it is not a manual playthrough. No Android device or APK launch has been verified.

## APK blocker
Unity 6000.6.3f1 lacks Android Build Support. In Unity Hub, add Android Build Support including Android SDK & NDK Tools and OpenJDK for this Editor. Then run Pirate Bombs > Assignment 11 > Build Android APK. Expected path: C:/Users/ulikp/Pirate Bombs/Builds/Android/PirateBombs-Assignment11.apk.
