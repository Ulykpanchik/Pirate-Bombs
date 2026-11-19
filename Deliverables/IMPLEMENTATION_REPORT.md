# Pirate Bombs - Assignment 11 handoff

## Open and play
The implementation is integrated directly into:
C:/Users/ulikp/Pirate Bombs

Unity version: 6000.6.3f1.
Open Assets/Scenes/SampleScene.unity and press Play. No manual component wiring is required. The existing map is reused for three increasingly difficult rounds.

## Implemented
- Three assigned LevelData assets drive player movement/health/throws, enemy health/AI, bomb damage/fuse/radius and object explosion forces at runtime.
- Enemy count and health increase across three rounds; completing a round saves progress and unlocks the next one.
- JSON progress and master volume persist in Application.persistentDataPath.
- Missing, empty, malformed, incomplete, incompatible and invalid saves recover to safe defaults. Disk failures do not crash gameplay.
- Automatic saving on completion, level selection, settings changes, pause, focus loss and quit.
- Saved Unity UI: HUD, pause/settings menu, locked level buttons, volume slider, completion/retry screen, and Reset all progress? with Yes, Reset / Cancel.
- Existing mobile and keyboard controls retained.
- Safe-area handling fixed for stale Device Simulator rectangles; player animation calls corrected to actual controller state names.
- DECISIONS.md, a teacher demonstration checklist, repeatable Unity verification tools and an MP4 capture prepared.

## Existing files modified (relative to the source backup)
- Assets/Scenes/SampleScene.unity
- Assets/Scripts/Bomb/Bomb.cs
- Assets/Scripts/Enemy/Enemy1_Kicker.cs
- Assets/Scripts/Enemy/EnemyBehaviorAI.cs
- Assets/Scripts/Enemy/EnemyHealth.cs
- Assets/Scripts/Objects/ItemPhysics.cs
- Assets/Scripts/Player/PlayerBomb.cs
- Assets/Scripts/Player/PlayerHealth.cs
- Assets/Scripts/Player/PlayerMovement.cs
- Assets/Scripts/UI/SafeAreaFitter.cs
- ProjectSettings/ProjectSettings.asset

ProjectSettings/ProjectSettings.asset contains the Android application identifier and minimum SDK configuration. EditorBuildSettings already included SampleScene; that correct scene list was retained. No packages were added, no other levels were overwritten, and the existing uncommitted project changes were backed up before editing.

## New files and assets
- Assets/Data/Level1Data.asset
- Assets/Data/Level2Data.asset
- Assets/Data/Level3Data.asset
- Assets/Editor/Assignment11Setup.cs
- Assets/Editor/Assignment11Verification.cs
- Assets/Scripts/Data/LevelData.cs
- Assets/Scripts/Data/LevelSession.cs
- Assets/Scripts/Data/ProgressStore.cs
- Assets/Scripts/Debug/Assignment11Demo.cs
- Assets/Scripts/UI/AssignmentUI.cs
- Corresponding Unity .meta files and folder metadata
- DECISIONS.md
- DEMONSTRATION.md
- Deliverables/PirateBombs-Assignment11-demo.mp4

## Configured scene and GameObjects
SampleScene now contains Assignment11 with LevelSession and three asset references.
Assignment11/AssignmentCanvas contains AssignmentUI, CanvasScaler and GraphicRaycaster.
Its SafeArea contains LevelHUD, Menu, LevelResult, PauseMenu and ResetConfirmation.
PauseMenu includes Level1/Level2/Level3, SoundVolume, ResetProgress, Resume, progress and save labels.
ResetConfirmation contains the exact question plus YesReset and Cancel buttons.
LevelResult contains NextLevel, Retry and ResultMenu.
The original GameCanvas, hearts, EventSystem, player, enemies, camera, prefabs, scenery and touch controls are reused.

## Save file
C:/Users/ulikp/AppData/LocalLow/DefaultCompany/Pirate Bombs/progress.json

Schema: version, format, highestUnlockedLevel, currentLevel, completedLevels[3], soundVolume.
The file was inspected after actual pointer input: volume changed to 0.40596675872802734, Cancel preserved it, and confirmed reset restored volume 1 and default level flags.
The final real save is at defaults, ready for a fresh demonstration.
Test saves are isolated under Library/Assignment11Tests. The video uses Library/Assignment11Recording/demo-progress.json.

## Verification
80 automated assertions passed in the final Unity run, with no runtime errors or exceptions. See verification.txt. Tests cover missing scripts, save recovery, invalid data, failed storage, actual explosion damage, all three configurations, automatic saves, menu bounds, locked levels, both reset outcomes and persistence across exiting/re-entering Play mode with a domain reload.
Runtime errors/exceptions are logged and fail the final check.
Actual pointer input also verified Pause, the volume slider, Reset, Cancel and Yes, Reset.
No Android device test or standalone application relaunch was performed.

## Video
PirateBombs-Assignment11-demo.mp4 is an actual capture of the running Unity Game view: 956 x 464, 8 fps, 421 frames, 52.625 seconds, without audio narration.
It demonstrates controls, settings, reset confirmation and three-level progression. Captions explicitly identify automated/accelerated completion. The demo temporarily boosts spawned bomb damage; production assets are unchanged.
See DEMONSTRATION.md for a manual recording script and instructions to show JSON recovery and Inspector edits.

## APK - not produced
The build command was invoked. Unity reported:
"Android Build Support is not installed for Unity 6000.6.3f1. Install Android Build Support, SDK/NDK Tools and OpenJDK through Unity Hub."

Install those modules for this exact Editor version. Then use:
Pirate Bombs > Assignment 11 > Build Android APK

Intended output:
C:/Users/ulikp/Pirate Bombs/Builds/Android/PirateBombs-Assignment11.apk

Existing Android ARM64/IL2CPP configuration is preserved. The APK and Android runtime remain unverified until the missing tools are installed and a build succeeds.

## Backup and repeatable tools
PirateBombs-before-Assignment11.zip contains the pre-change Assets, Packages and ProjectSettings, including your pre-existing local edits.
The extracted backup is also retained next to the ZIP.

Editor menu: Pirate Bombs > Assignment 11.
Run Verification uses isolated saves and leaves the real player save untouched.
Record Demonstration then Encode Recorded Video reproduces the supplied capture; keep the Game tab visible during recording.
