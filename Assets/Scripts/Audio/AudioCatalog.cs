using System;
using UnityEngine;
public enum SoundEvent { Jump, Hit, EnemyWindup, Death, Click, Win, Lose, BombPlace, BombThrow, Explosion }
[Serializable] public sealed class SoundEntry {
    public SoundEvent id;
    public AudioClip[] clips;
    [Range(0,1)] public float volume = 0.7f;
    public bool varyPitch = true;
}
[CreateAssetMenu(menuName="Pirate Bombs/Audio Catalog")]
public sealed class AudioCatalog : ScriptableObject {
    public AudioClip menuMusic, gameplayMusic;
    public SoundEntry[] sounds;
    public SoundEntry Find(SoundEvent id) {
        if (sounds != null) foreach(var sound in sounds) if(sound.id == id) return sound;
        return null;
    }
}
