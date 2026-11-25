using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public sealed class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }
    public AudioCatalog catalog;
    public AudioMixer mixer;
    public AudioMixerGroup sfxGroup;
    public AudioMixerSnapshot normal, paused;
    public const int VoiceLimit = 12;
    AudioSource[] voices;
    bool[] gameplayVoice;
    bool gameplayPaused;
    int nextVoice;
    Coroutine defeat;
    public int PlayedCount { get; private set; }
    public SoundEvent LastEvent { get; private set; }
    public double LastWindupTime { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    public static void EnsureExists() {
        if (Instance != null) return;
        var prefab = Resources.Load<GameObject>("AudioSystem");
        if(prefab != null) Instantiate(prefab);
        else Debug.LogError("AudioSystem prefab is missing from Resources.");
    }
    void Awake() {
        if(Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance=this; DontDestroyOnLoad(gameObject);
        voices=new AudioSource[VoiceLimit]; gameplayVoice=new bool[VoiceLimit];
        for(int i=0;i<voices.Length;i++) {
            voices[i]=gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake=false; voices[i].spatialBlend=0;
            voices[i].outputAudioMixerGroup=sfxGroup;
        }
        if(mixer != null) mixer.updateMode=AudioMixerUpdateMode.UnscaledTime;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void Start() { ApplySettings(); SetPaused(false); }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if(defeat != null) { StopCoroutine(defeat); defeat=null; }
        for(int i=0;i<voices.Length;i++) if(gameplayVoice[i]) voices[i].Stop();
        SetPaused(false); ApplySettings();
    }
    public static float ToDecibels(float value) {
        if(float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0001f) return -80f;
        return Mathf.Max(-80f,20f*Mathf.Log10(Mathf.Clamp01(value)));
    }
    public void ApplySettings() {
        if(mixer == null || LevelSession.Store == null) return;
        AudioListener.volume=1f;
        mixer.SetFloat("MasterVolumeDb",0f);
        mixer.SetFloat("MusicVolumeDb",ToDecibels(LevelSession.Store.Data.musicVolume));
        mixer.SetFloat("SfxVolumeDb",ToDecibels(LevelSession.Store.Data.sfxVolume));
    }
    public static void Play(SoundEvent id) { if(Instance != null) Instance.PlaySound(id); }
    public void PlaySound(SoundEvent id) {
        bool gameplay = id != SoundEvent.Click && id != SoundEvent.Win && id != SoundEvent.Lose && id != SoundEvent.Death;
        if(gameplay && (gameplayPaused || Time.timeScale == 0)) return;
        var entry=catalog != null ? catalog.Find(id) : null;
        if(sfxGroup == null || entry == null || entry.clips == null || entry.clips.Length == 0) return;
        var clip=entry.clips[Random.Range(0,entry.clips.Length)]; if(clip == null) return;
        int index=-1;
        for(int i=0;i<voices.Length;i++) if(!voices[i].isPlaying && !(gameplayPaused && gameplayVoice[i])) {index=i;break;}
        if(index<0) { // Prefer replacing a gameplay voice over a UI/result cue.
            for(int i=0;i<voices.Length;i++) {int n=(nextVoice+i)%voices.Length;if(gameplayVoice[n]) {index=n;break;}}
            if(index<0) return;
        }
        nextVoice=(index+1)%voices.Length;
        var source=voices[index]; source.Stop(); source.clip=clip;
        source.pitch=entry.varyPitch ? Random.Range(0.92f,1.08f) : 1f;
        source.volume=entry.volume; gameplayVoice[index]=gameplay; source.Play();
        PlayedCount++; LastEvent=id;
        if(id == SoundEvent.EnemyWindup) LastWindupTime=AudioSettings.dspTime;
    }
    public void PlayDefeat() { if(defeat == null) defeat=StartCoroutine(DefeatCue()); }
    IEnumerator DefeatCue() { yield return new WaitForSecondsRealtime(0.45f); PlaySound(SoundEvent.Lose); defeat=null; }
    public void SetPaused(bool value) {
        if(gameplayPaused != value && voices != null) for(int i=0;i<voices.Length;i++) if(gameplayVoice[i]) {
            if(value) voices[i].Pause(); else voices[i].UnPause();
        }
        gameplayPaused=value;
        var snapshot=value ? paused : normal;
        if(snapshot != null) snapshot.TransitionTo(0.3f);
    }
    void OnDestroy() { if(Instance != this) return; SceneManager.sceneLoaded-=OnSceneLoaded; Instance=null; }
}
