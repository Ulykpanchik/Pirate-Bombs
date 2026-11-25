# Pirate Bombs — Assignment 12 Sound Pass

Unity 6000.6.3f1, 2D mobile platformer. Open Assets/Scenes/SampleScene.unity and press Play. The startup menu offers Play / Continue, unlocked levels, independent Music/SFX sliders, reset confirmation and Audio Credits. The original three rounds reuse this map and their LevelData assets.

## Audio Assets

All files below are actual imported assets, not placeholder slots. Effects are renamed copies with unchanged audio content. Music is unchanged. Unity compresses imported audio for the build. Files live in Assets/Audio/SFX and Assets/Audio/Music.

| Audio File | Purpose | Source URL | Author | License |
|---|---|---|---|---|
| Jump.ogg (original: phaseJump1.ogg) | Jump | https://kenney.nl/assets/digital-audio | Kenney | CC0 1.0; attribution optional |
| Hit.ogg (original: impactPunch_medium_000.ogg) | Hit | https://kenney.nl/assets/impact-sounds | Kenney | CC0 1.0; attribution optional |
| EnemyWindup.ogg (original: impactBell_heavy_000.ogg) | EnemyWindup | https://kenney.nl/assets/impact-sounds | Kenney | CC0 1.0; attribution optional |
| Death.ogg (original: lowDown.ogg) | Death | https://kenney.nl/assets/digital-audio | Kenney | CC0 1.0; attribution optional |
| Click.ogg (original: impactWood_light_000.ogg) | Click | https://kenney.nl/assets/impact-sounds | Kenney | CC0 1.0; attribution optional |
| Win.ogg (original: jingles_NES00.ogg) | Win | https://kenney.nl/assets/music-jingles | Kenney | CC0 1.0; attribution optional |
| Lose.ogg (original: jingles_NES01.ogg) | Lose | https://kenney.nl/assets/music-jingles | Kenney | CC0 1.0; attribution optional |
| BombPlace.ogg (original: impactWood_medium_000.ogg) | BombPlace | https://kenney.nl/assets/impact-sounds | Kenney | CC0 1.0; attribution optional |
| BombThrow.ogg (original: phaserUp1.ogg) | BombThrow | https://kenney.nl/assets/digital-audio | Kenney | CC0 1.0; attribution optional |
| Explosion.ogg (original: impactPlate_heavy_000.ogg) | Explosion | https://kenney.nl/assets/impact-sounds | Kenney | CC0 1.0; attribution optional |
| mutiny_2_2.mp3 | Main menu music, “Pirate Music” | https://opengameart.org/content/pirate-music | migfus20 | CC BY 4.0; attribution required |
| pirate_indenture_loop.wav | Gameplay music, “Pirate Indenture” loop | https://opengameart.org/content/pirate-indenture | Eldritch Grim | CC0 1.0; attribution not required |

Music attribution: **“Pirate Music” (mutiny_2_2.mp3) by migfus20**, from https://opengameart.org/content/pirate-music, licensed under **Creative Commons Attribution 4.0 International**, https://creativecommons.org/licenses/by/4.0/. Audio content is unmodified. This credit is also available in the game's Audio Credits panel, so it travels with the APK. “Pirate Indenture” by Eldritch Grim is CC0: https://creativecommons.org/publicdomain/zero/1.0/.

Original Kenney license notices are preserved in Assets/Audio/Licenses. These licenses concern the listed audio; they do not relicense the rest of the game. Source licenses were checked on 2026-11-23. The GPL-only “Pirate Theme” search result was not imported.

## Audio structure

Assets/Audio/PirateBombsAudio.mixer contains:

~~~text
Master [MasterVolumeDb]
├── MusicPause [snapshot attenuation + Lowpass]
│   └── Music [MusicVolumeDb] ← two music sources
└── SFX [SfxVolumeDb] ← up to twelve effect sources
~~~

The extra MusicPause stage keeps snapshot controls independent of the exposed user-volume parameters. Normal uses 0 dB attenuation and 22000 Hz cutoff. Paused uses −10 dB and 1000 Hz. Transitions take 0.3 seconds using unscaled time. UI sounds remain available while gameplay voices are paused. No AudioSource bypasses the mixer.

AudioManager and MusicManager share the persistent Resources/AudioSystem prefab. LevelSession creates it only when needed; both managers reject duplicate instances. Music uses a one-second crossfade and does not restart an already requested clip across scene reloads. Returning to Main Menu selects menu music; ordinary pause retains gameplay music.

Sound calls occur at accepted gameplay events. Enemy warnings start at Attack-state entry in both enemy controllers, before the existing 0.2-second damage point. Both controllers explicitly skip paused updates so a warning cannot be consumed during pause. Attack timing, damage and balance assets are unchanged. Death is signalled immediately on lethal damage; the lose cue follows 0.45 seconds later using real time. This avoids relying on a death-animation callback that the existing result screen can freeze.

Jump, hit, enemy warning, bomb placement, throw and explosion use pitch variation from 0.92 to 1.08. UI and result sounds keep a stable pitch. There is no pickup mechanic; bomb events provide the additional event coverage without adding gameplay mechanics.

## Saved settings

The existing ProgressStore still owns persistentDataPath/progress.json. Schema version 2 adds musicVolume and sfxVolume. Valid version-1 saves migrate both channels from soundVolume, retaining current level, unlocks and completion flags. The replacement keeps the original file as .bak. soundVolume remains a legacy compatibility field, not a second master control. AudioListener.volume stays at 1.

Sliders immediately apply 20 * log10(volume); zero and tiny values use −80 dB. Settings use the existing atomic-write, recovery and lifecycle-save behavior. Reset still requires confirmation and restores progression plus both audio volumes.

## Verification and building

Use Pirate Bombs > Assignment 12 > Run Verification in Edit mode. It uses Library/Assignment12Tests, not the real player save. Use Assignment 11 > Run Verification for existing gameplay/progression regression checks. Read the generated verification reports for actual results; code presence alone is not proof of passing tests.

Build with Pirate Bombs > Assignment 12 > Build Android APK. Output: Builds/Android/PirateBombs-Assignment12.apk. Consult the final report for the actual build outcome and device limitations.

See SOUND_SYSTEM_GUIDE.md for setup, replacement instructions, a script map and presentation notes. No final implementation commit or push is performed automatically.
