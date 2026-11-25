using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
public sealed class MusicManager : MonoBehaviour {
    public static MusicManager Instance { get; private set; }
    public AudioCatalog catalog;
    public AudioMixerGroup musicGroup;
    public float crossfadeSeconds=1f;
    AudioSource[] sources;
    AudioClip requested;
    Coroutine fade;
    public int TrackStarts {get;private set;}
    public AudioClip CurrentTrack => requested;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {Instance=null;}
    void Awake() {
        if(Instance != null && Instance != this) {Destroy(gameObject);return;}
        Instance=this; DontDestroyOnLoad(gameObject);
        sources=new AudioSource[2];
        for(int i=0;i<2;i++) {sources[i]=gameObject.AddComponent<AudioSource>();sources[i].playOnAwake=false;
            sources[i].loop=true;sources[i].volume=0;sources[i].spatialBlend=0;sources[i].outputAudioMixerGroup=musicGroup;}
        SceneManager.sceneLoaded+=OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene,LoadSceneMode mode) {SetMenu(!LevelSession.GameStarted);}
    public void SetMenu(bool menu) {if(catalog != null) PlayTrack(menu?catalog.menuMusic:catalog.gameplayMusic);}
    public void PlayTrack(AudioClip clip) {
        if(clip == null || musicGroup == null || requested == clip) return;
        requested=clip;if(fade != null) StopCoroutine(fade);
        int target=-1;
        for(int i=0;i<2;i++) if(sources[i].clip==clip && sources[i].isPlaying) target=i;
        if(target<0) { target=sources[0].volume<=sources[1].volume?0:1;
            sources[target].Stop();sources[target].clip=clip;sources[target].volume=0;sources[target].Play();TrackStarts++; }
        fade=StartCoroutine(Crossfade(target));
    }
    IEnumerator Crossfade(int target) {
        int other=1-target;float a=sources[target].volume,b=sources[other].volume,t=0;
        while(t<crossfadeSeconds) {t+=Time.unscaledDeltaTime;float f=Mathf.Clamp01(t/Mathf.Max(0.01f,crossfadeSeconds));
            sources[target].volume=Mathf.Lerp(a,0.65f,f);sources[other].volume=Mathf.Lerp(b,0,f);yield return null;}
        sources[target].volume=0.65f;sources[other].volume=0;sources[other].Stop();fade=null;
    }
    void OnDestroy() {if(Instance!=this)return;SceneManager.sceneLoaded-=OnSceneLoaded;Instance=null;}
}
