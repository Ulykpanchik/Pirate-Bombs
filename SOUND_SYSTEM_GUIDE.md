# Sound System Guide

## Start the game

Open Assets/Scenes/SampleScene.unity and press Play. A simple main menu appears over the existing game. Use Play / Continue or select an unlocked level. Pause / Menu opens the existing level/settings menu. Main Menu returns to menu music. Reset Progress still asks for confirmation.

## Files and responsibilities

| File | Responsibility |
|---|---|
| Assets/Scripts/Audio/AudioManager.cs | Persistent SFX pool, dB conversion, snapshot transitions, safe missing clips and delayed defeat cue |
| Assets/Scripts/Audio/MusicManager.cs | Two music sources, scene notification, one-second unscaled crossfade, same-track and duplicate protection |
| Assets/Scripts/Audio/AudioCatalog.cs | Named event entries, volume, pitch variation and two music references |
| Assets/Audio/PirateBombsAudio.asset | Configured clip catalog |
| Assets/Audio/PirateBombsAudio.mixer | Master, Music, SFX and MusicPause processing |
| Assets/Resources/AudioSystem.prefab | Persistent managers with all catalog/mixer references |
| Assets/Scripts/Data/ProgressStore.cs | Existing JSON persistence extended with schema migration and two volume settings |
| Assets/Scripts/Data/LevelSession.cs | Startup menu, level flow, audio bootstrap, pause and result notifications |
| Assets/Scripts/UI/AssignmentUI.cs | Sliders, menu buttons, click sounds and in-game audio credits |
| Assets/Scripts/Editor/Assignment12Setup.cs | Repeatable Editor configuration and Android build command |
| Assets/Scripts/Editor/Assignment12Verification.cs | Isolated migration, mixer and gameplay integration checks |

## Script map

~~~text
LevelSession.Awake → AudioManager.EnsureExists → Resources/AudioSystem
PlayerMovement.FixedUpdate → AudioManager.Play(Jump)
PlayerHealth.ApplyDamage → Play(Hit) or Play(Death)
PlayerBomb.PlaceBomb / ThrowBomb → Play(BombPlace / BombThrow)
Bomb.Explode → Play(Explosion), once after its guard
Enemy1_Kicker.ChangeState(Attack) → Play(EnemyWindup)
EnemyBehaviorAI.ChangeState(Attack) → Play(EnemyWindup)
    existing stateTimer reaches attackHitTime → PerformAttack → damage/bomb interaction
AssignmentUI button.onClick → Play(Click)
LevelSession completion → Play(Win)
LevelSession failure → AudioManager.PlayDefeat → Play(Lose) after 0.45 real seconds
AssignmentUI sliders → ProgressStore.SetMusicVolume / SetSfxVolume
    → AudioManager.ApplySettings → mixer.SetFloat → ProgressStore.Save
LevelSession.SetPaused → AudioManager.SetPaused → snapshot.TransitionTo
LevelSession.OpenMainMenu / StartGame / LoadLevel → MusicManager.SetMenu
SceneManager.sceneLoaded → MusicManager rechecks menu/gameplay; same clip is retained
~~~

## Mixer and pause

Double-click Assets/Audio/PirateBombsAudio.mixer in the Project window. Master is the output. SFX goes directly to Master. Music goes through MusicPause and then Master. Select a group to see its effects. The exposed parameters list contains MasterVolumeDb, MusicVolumeDb and SfxVolumeDb; spelling is case-sensitive.

MusicPause has an Attenuation stage and a Lowpass effect. The Normal snapshot sets 0 dB / 22000 Hz; Paused sets −10 dB / 1000 Hz. User volume is on Music, so a snapshot never overwrites the slider setting. The mixer uses Unscaled Time so transitions finish when Time.timeScale is zero. Gameplay effect sources pause/unpause; clicks and result cues can still play. Pause does not switch music tracks.

## How crossfade works

MusicManager keeps two looping AudioSources. It starts the new track at zero volume, lowers the old one and raises the new one over about one second, then stops the old source. It uses unscaled time. A repeated request for the current clip returns immediately. If a transition is interrupted, the next fade starts from the sources' current volumes instead of resetting both.

## Settings and migration

Music and SFX each use values from 0 to 1. The manager turns these into decibels with 20 * log10(value). Zero uses −80 dB to avoid infinity. Only the corresponding mixer parameter changes. The existing ProgressStore writes the same progress.json file, now schema version 2. Old valid saves keep progression and copy their original volume into both channels. No PlayerPrefs or separate settings file is introduced.

## Replace a clip in the Inspector

1. Copy a legally usable audio file into Assets/Audio/SFX or Assets/Audio/Music.
2. Select Assets/Audio/PirateBombsAudio.asset in the Project window.
3. For music, drag the file into Menu Music or Gameplay Music.
4. For an effect, expand Sounds, find its Id, expand Clips, and drag the file into Element 0. Increase the array size to add alternatives.
5. Adjust that entry's Volume if necessary. Vary Pitch enables the 0.92–1.08 range.
6. Select the audio file. Use Decompress On Load and mono for short effects; Streaming for music. Apply importer changes.
7. Update the README Audio Assets table and the in-game credits when attribution is required.
8. Test at low, medium and high volume on the target phone. Check warning onset, musical balance and clipping by listening.

Missing or empty clips are safely skipped. They are not a substitute for finishing the assignment; the configured catalog contains real clips for all ten events.

## Inspector reference checklist

The provided scene and assets are configured by the Editor tool. If recovering configuration, exit Play mode and choose Pirate Bombs > Assignment 12 > Configure Audio and Menu. This resets audio catalog assignments and menu layout to this assignment's defaults; do not use it after personal customization without reviewing the changes.

Select Assets/Resources/AudioSystem.prefab:

- AudioManager: Catalog = PirateBombsAudio catalog; Mixer = PirateBombsAudio mixer; Sfx Group = SFX; Normal = Normal; Paused = Paused.
- MusicManager: same Catalog; Music Group = Music; Crossfade Seconds = 1.
- AudioSources are created at runtime and routed by these components. Do not add a second listener or place duplicate managers in each level.

In SampleScene select Assignment11 > AssignmentCanvas. AssignmentUI keeps its existing Session and button references. Volume is the Music slider (the field name is retained for older editor tools); Sfx Volume is the second slider. Home Button points to MainMenu, Menu Title to Title, and the credits fields point to the Audio Credits controls.

## Test without overwriting real progress

The Assignment 12 verification menu uses Library/Assignment12Tests/play-progress.json and separate migration fixtures. It exercises actual player/bomb/enemy methods in a controlled test scene, including both attack-controller types and all four enemy objects. Attack timers are inspected at 0.19 and 0.2 seconds; this verifies ordering, not subjective loudness or phone speaker latency. It restarts Play mode to check persistence across domain reloads. Assignment 11's tests continue covering progression, bomb damage and reset confirmation.

Manual checks: play all three rounds using touch controls; pause repeatedly while holding Bomb; change both sliders; return to Main Menu; replay a level; close and reopen the installed app. Listen to each warning before damage and check music/sound balance on phone speakers and headphones. Build success alone does not prove these checks.

## Short university defense (B1 English)

“I added one audio system to my existing game. AudioManager plays short sounds when a real game event happens. It uses a small number of audio sources, so it is suitable for a phone.

MusicManager has two sources. One plays menu music and one can play game music. When the music changes, the old track becomes quiet and the new track becomes louder for one second. The same music does not restart when a level reloads.

The AudioMixer has Master, Music and SFX groups. Music also passes through a pause group. The two sliders change Music and SFX separately. The game converts their values to decibels and saves them in the existing JSON file. Old saved progress is kept.

An enemy plays a warning when its attack starts. Damage happens after the original 0.2-second preparation time. I did not change enemy damage or speed.

When the game pauses, a snapshot makes music quieter and removes high frequencies. The sound returns smoothly when I continue. Some repeated effects have small pitch changes.

Player, bomb and enemy scripts call AudioManager. LevelSession controls pause, results and music state. AssignmentUI sends slider values to ProgressStore. The catalog stores the audio clips and the mixer controls the final sound.”
