# Assignment 11 - Data and Saves

## Level configuration
LevelData ScriptableObjects keep balance in editable assets. The existing map, artwork, player controls, bomb prefab and enemy behaviours are reused. The original Level_1 scene is an empty alternative, so all three rounds load SampleScene.

Assets/Data/Level1Data.asset, Level2Data.asset and Level3Data.asset are assigned to the LevelSession component on the Assignment11 GameObject. LevelSession selects the saved current level before other components run Awake. Player movement, health, throws, enemy health/AI, bombs and item explosion physics read the selected asset. Runtime fields are hidden in the Inspector to avoid two competing sources of balance.

Level 1 has two enemies with one health; level 2 has three enemies with two health; level 3 has four enemies with three health. Enemy speed also increases. Existing positions are reused, ordered from left to right. Defeating all active enemies completes the round. Player death shows a retry screen and does not unlock a level. There was no scoring system, so no artificial score system was added.

Gameplay tuning numbers, including attack reach tolerance, flee safety/rest and bomb kick forces, live in the assets. Direction signs, normalized fractions, raycast tolerances, animation/VFX offsets, UI layout and save-schema constants stay in code because they are not balance settings.

## JSON progress
ProgressStore uses Unity JsonUtility because the data is small and readable, with no extra package. The file is Application.persistentDataPath/progress.json. In this Windows Editor it resolves to:
C:/Users/ulikp/AppData/LocalLow/DefaultCompany/Pirate Bombs/progress.json

The JSON includes a format marker, schema version, highest unlocked level, current level, three completion flags and master sound volume. PlayerPrefs is not used.

A missing file creates defaults and writes them immediately. Empty, malformed, incomplete, incompatible and invalid data recover to defaults. Invalid files are preserved with an .invalid- timestamp suffix when storage allows. Range, format, version, array length and progression consistency are validated. Failed reads/writes are caught; gameplay can continue with in-memory progress. Writes use a temporary file followed by a same-volume replacement, with a .bak copy of the previous save.

## Automatic saving
Save occurs on completion, selecting/retrying a level, changing the sound slider, opening the pause menu, application pause, losing focus and application quit. Losing focus also pauses gameplay; resuming is explicit. Sound volume controls AudioListener.volume. Settings work even when there is no sound currently playing.

## Reset
Pause / Menu contains Reset Progress. It opens the in-game question "Reset all progress?" with "Yes, Reset" and "Cancel". Cancel only dismisses the panel. Confirm restores defaults, saves immediately and reloads level 1, updating the HUD, locked levels and volume. The menus are real, saved Unity UI GameObjects with serialized references; listeners are bound in Awake.

## Compatibility and verification
Implemented in Unity 6000.6.3f1 using existing packages and both existing keyboard and mobile controls. SampleScene is the enabled build scene. Existing Android ARM64/IL2CPP settings and input system are preserved; package id is com.piratebombs.assignment11 and minimum Android API is 23.

SafeAreaFitter now rejects an invalid stale Device Simulator safe area. Without this check, changing from Simulator to Game view could move both existing touch controls and new menus beyond the screen.

Use Pirate Bombs > Assignment 11 > Run Verification while out of Play mode. The tests use Library/Assignment11Tests, never the real player save. They cover JSON recovery, failed storage, level data application, actual explosion damage, three completions/unlocks, settings, pause/focus, reset confirm/cancel and persistence across Play mode domain reloads. See verification.txt for the actual results. Test-assisted completion changes bomb damage only temporarily and restores the asset immediately.

## Android and video status
The APK build command was executed, but Unity reported that Android Build Support is missing. SDK/NDK Tools and OpenJDK must be installed with that module through Unity Hub for version 6000.6.3f1. No APK was produced or claimed as tested. After installation, run Pirate Bombs > Assignment 11 > Build Android APK. The intended output is Builds/Android/PirateBombs-Assignment11.apk.

The Editor-only demonstration recorder captures the running game and uses an isolated demo save. Captions identify the accelerated bomb-damage completion tests. It is excluded from builds. The recording is supporting evidence, not a claim of a full manual Android playthrough.

Two pre-existing player animation calls were corrected to the actual controller state names Player_Ground and Player_Idle. No animation assets were redesigned.
