using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Assignment12Verification {
    const string Folder="Library/Assignment12Tests";
    const string Report=Folder+"/verification.txt";
    static IEnumerator routine;
    static double deadline;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Assignment12Verification(){
        EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;
        Application.logMessageReceived+=(message,stack,type)=>{
            if(SessionState.GetBool("Assignment12.Tests",false)&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert))
                File.AppendAllText(Folder+"/runtime-errors.txt",message+"\n"+stack+"\n");};
    }
    static void Check(bool ok,string label){File.AppendAllText(Report,(ok?"PASS ":"FAIL ")+label+"\n");if(!ok)throw new Exception(label);}
    [MenuItem("Pirate Bombs/Assignment 12/Run Verification")]
    public static void Run(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first.");
        Directory.CreateDirectory(Folder);File.WriteAllText(Report,"Assignment 12 verification - "+DateTime.UtcNow.ToString("u")+"\n");
        File.WriteAllText(Folder+"/runtime-errors.txt","");
        string file=Path.GetFullPath(Folder+"/migration.json");
        string old="{\"version\":1,\"format\":\"PirateBombs.Progress\",\"highestUnlockedLevel\":3,\"currentLevel\":2,\"completedLevels\":[true,true,false],\"soundVolume\":0.37}";
        File.WriteAllText(file,old);var store=new ProgressStore(file);
        Check(store.Data.version==2&&store.Data.currentLevel==2&&store.Data.highestUnlockedLevel==3&&store.Data.completedLevels[1],"Version 1 migration preserves progression");
        Check(store.Data.musicVolume==0.37f&&store.Data.sfxVolume==0.37f,"Old volume migrates to both channels");
        Check(File.ReadAllText(file+".bak")==old,"Migration retains original version-1 backup");
        store.SetMusicVolume(0.2f);store.SetSfxVolume(0.7f);store=new ProgressStore(file);
        Check(store.Data.musicVolume==0.2f&&store.Data.sfxVolume==0.7f&&store.Data.highestUnlockedLevel==3,"Independent settings round-trip preserves progress");
        store.SetMusicVolume(float.NaN);store.SetSfxVolume(float.PositiveInfinity);
        Check(store.Data.musicVolume==0.2f&&store.Data.sfxVolume==0.7f,"Invalid volume inputs rejected");
        Check(AudioManager.ToDecibels(0)==-80&&Mathf.Abs(AudioManager.ToDecibels(0.5f)+6.0206f)<0.001f&&AudioManager.ToDecibels(1)==0,"Safe logarithmic dB conversion");
        var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/PirateBombsAudio.mixer");
        Check(mixer!=null&&mixer.FindSnapshot("Normal")!=null&&mixer.FindSnapshot("Paused")!=null,"Mixer and both snapshots exist");
        Check(new[]{"Master","Music","SFX","MusicPause"}.All(n=>mixer.FindMatchingGroups(n).Any(g=>g.name==n)),"All mixer groups exist");
        var bus=mixer.FindMatchingGroups("MusicPause").First(g=>g.name=="MusicPause");
        Check((float)Assignment12Setup.Call(bus,"GetValueForVolume",mixer,mixer.FindSnapshot("Paused"))==-10f,"Paused music attenuation is -10 dB");
        var effects=(Array)bus.GetType().GetProperty("effects").GetValue(bus);
        var lowpass=effects.Cast<object>().First(e=>(string)e.GetType().GetProperty("effectName").GetValue(e)=="Lowpass");
        Check((float)Assignment12Setup.Call(lowpass,"GetValueForParameter",mixer,mixer.FindSnapshot("Paused"),"Cutoff freq")==1000f,"Paused low-pass cutoff is 1000 Hz");
        Check((float)Assignment12Setup.Call(lowpass,"GetValueForParameter",mixer,mixer.FindSnapshot("Normal"),"Cutoff freq")==22000f,"Normal cutoff is restored to 22000 Hz");
        var catalog=AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/Audio/PirateBombsAudio.asset");
        Check(catalog.menuMusic!=null&&catalog.gameplayMusic!=null&&catalog.menuMusic!=catalog.gameplayMusic,"Two distinct music clips assigned");
        Check(catalog.sounds.Length==10&&catalog.sounds.All(s=>s.clips.Length>0&&s.clips.All(c=>c!=null)),"All ten sound events have real clips");
        var warning=catalog.Find(SoundEvent.EnemyWindup).clips[0];warning.LoadAudioData();
        float[] pcm=new float[Mathf.Min(warning.samples,warning.frequency/10)*warning.channels];warning.GetData(pcm,0);
        Check(pcm.Any(v=>Mathf.Abs(v)>0.005f),"Enemy warning has signal within its first 100 ms");
        foreach(var d in AssetDatabase.FindAssets("t:LevelData").Select(g=>AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g))))
            Check(d.enemy1_Kicker.attackHitTime==0.2f&&d.enemyBehaviorAI.attackHitTime==0.2f&&d.enemy1_Kicker.attackDamage==1&&d.enemyBehaviorAI.attackDamage==1,"Original attack timing/damage retained in "+d.name);
        File.WriteAllText(Path.GetFullPath(Folder+"/play-progress.json"),JsonUtility.ToJson(ProgressStore.Defaults()));
        SessionState.SetInt("Assignment12.Stage",1);SessionState.SetBool("Assignment12.Tests",true);EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state){
        if(!SessionState.GetBool("Assignment12.Tests",false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){routine=SessionState.GetInt("Assignment12.Stage",1)==1?PlayTests():RestartTests();deadline=EditorApplication.timeSinceStartup+180;}
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetInt("Assignment12.Stage",0)==2)EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
    }
    static void Tick(){if(routine==null)return;try{if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Tests timed out");if(!routine.MoveNext())routine=null;}
        catch(Exception e){File.AppendAllText(Report,"FAIL "+e+"\n");routine=null;SessionState.SetBool("Assignment12.Tests",false);EditorApplication.isPlaying=false;Debug.LogException(e);}}
    static IEnumerator Wait(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    static void Field(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
    static void Invoke(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
    static IEnumerator PlayTests(){
        var w=Wait(1.5f);while(w.MoveNext())yield return null;
        var session=LevelSession.Instance;var audio=AudioManager.Instance;var music=MusicManager.Instance;
        Check(session.InMainMenu&&session.Paused&&Time.timeScale==0,"Startup main menu freezes gameplay");
        Check(LevelSession.Store.FilePath.Replace('\\','/').EndsWith("Assignment12Tests/play-progress.json"),"Tests use an isolated save");
        Check(music.CurrentTrack==audio.catalog.menuMusic,"Main menu selects menu track");
        Canvas.ForceUpdateCanvases();var corners=new Vector3[4];session.ui.menuPanel.GetComponent<RectTransform>().GetWorldCorners(corners);
        Check(corners[0].x>=-1&&corners[0].y>=-1&&corners[2].x<=Screen.width+1&&corners[2].y<=Screen.height+1,"Menu fits current screen");
        int clicks=audio.PlayedCount;session.ui.resumeButton.onClick.Invoke();
        Check(audio.PlayedCount==clicks+1&&audio.LastEvent==SoundEvent.Click,"Play button emits one click");
        Check(!session.InMainMenu&&Time.timeScale==1,"Play resumes gameplay");
        w=Wait(0.2f);while(w.MoveNext())yield return null;
        var musicSources=music.GetComponents<AudioSource>().Where(s=>s.outputAudioMixerGroup==music.musicGroup).ToArray();
        Check(musicSources.Length==2&&musicSources.All(s=>s.isPlaying&&s.volume>0),"Both tracks overlap during crossfade");
        w=Wait(1f);while(w.MoveNext())yield return null;
        Check(musicSources.Count(s=>s.isPlaying)==1&&music.CurrentTrack==audio.catalog.gameplayMusic,"Crossfade completes near one second");
        session.SetPaused(true);session.ui.volume.value=0.25f;session.ui.sfxVolume.value=0.7f;
        audio.mixer.GetFloat("MusicVolumeDb",out float m);audio.mixer.GetFloat("SfxVolumeDb",out float s);
        Check(Mathf.Abs(m-AudioManager.ToDecibels(0.25f))<0.01f&&Mathf.Abs(s-AudioManager.ToDecibels(0.7f))<0.01f,"Sliders independently change mixer dB");
        session.ui.volume.value=0;audio.mixer.GetFloat("MusicVolumeDb",out m);Check(m==-80,"Zero slider safely mutes");session.ui.volume.value=0.25f;
        int before=audio.PlayedCount;AudioManager.Play(SoundEvent.Jump);Check(before==audio.PlayedCount,"Gameplay effects suppressed while paused");
        for(int i=0;i<5;i++){session.SetPaused(false);session.SetPaused(true);}
        Check(Time.timeScale==0&&LevelSession.Store.Data.musicVolume==0.25f,"Repeated pause preserves settings");
        session.SetPaused(false);
        // Test the actual event-producing methods with controlled isolated objects.
        var player=Object.FindAnyObjectByType<PlayerHealth>();var movement=player.GetComponent<PlayerMovement>();var pb=player.GetComponent<PlayerBomb>();
        foreach(var enemy in Object.FindObjectsByType<EnemyHealth>()) {foreach(var ai in enemy.GetComponents<MonoBehaviour>().Where(c=>c is Enemy1_Kicker||c is EnemyBehaviorAI))ai.enabled=false;}
        player.GetComponent<Rigidbody2D>().simulated=false;
        before=audio.PlayedCount;Field(movement,"jumpRequested",true);Invoke(movement,"FixedUpdate");
        Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.Jump,"Real jump impulse emits one sound");
        before=audio.PlayedCount;pb.StartBombHold();pb.ReleaseBomb();Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.BombPlace,"Real placement emits one sound");
        pb.StartBombHold();Field(pb,"holdStartTime",Time.time-1);before=audio.PlayedCount;pb.ReleaseBomb();Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.BombThrow,"Real charged throw emits one sound");
        foreach(var bomb in Object.FindObjectsByType<Bomb>())Object.Destroy(bomb.gameObject);
        var isolated=Object.Instantiate(pb.bombPrefab,new Vector3(-1000,-1000),Quaternion.identity);before=audio.PlayedCount;
        isolated.SendMessage("Explode");isolated.SendMessage("Explode");Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.Explosion,"Explosion guard prevents duplicate sound");
        before=audio.PlayedCount;player.TakeDamage(1);player.TakeDamage(1);Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.Hit,"Accepted damage plays once; repeated hit rejected");
        session.LoadLevel(3); // Locked request must still be rejected.
        Check(LevelSession.Store.Data.currentLevel==1,"Locked level guard preserved");
        int starts=music.TrackStarts;session.LoadLevel(1);w=Wait(1f);while(w.MoveNext())yield return null;
        Check(music==MusicManager.Instance&&music.TrackStarts==starts,"Reload retains manager and music playback");
        Check(Object.FindObjectsByType<AudioManager>().Length==1&&Object.FindObjectsByType<MusicManager>().Length==1,"Exactly one manager of each type");
        Check(Object.FindObjectsByType<AudioSource>().All(a=>a.outputAudioMixerGroup!=null),"Every live AudioSource is mixer-routed");
        session=LevelSession.Instance;session.SetPaused(true);
        Object.Instantiate(Resources.Load<GameObject>("AudioSystem"));w=Wait(0.1f);while(w.MoveNext())yield return null;
        Check(Object.FindObjectsByType<AudioManager>().Length==1&&Object.FindObjectsByType<MusicManager>().Length==1,"Duplicate audio prefab is safely rejected");
        session.SetPaused(false);
        player=Object.FindAnyObjectByType<PlayerHealth>();player.GetComponent<Rigidbody2D>().simulated=false;
        foreach(var health in Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include)){
            health.gameObject.SetActive(true);health.GetComponent<Rigidbody2D>().simulated=false;
            MonoBehaviour ai=health.GetComponent<Enemy1_Kicker>();if(ai==null)ai=health.GetComponent<EnemyBehaviorAI>();
            ai.enabled=false;player.transform.position=health.transform.position+Vector3.right*0.5f;
            Field(player,"<IsTakingHit>k__BackingField",false);Field(player,"<IsDead>k__BackingField",false);Field(player,"<CurrentHealth>k__BackingField",3);
            Field(ai,"target",player.transform);var enumType=ai.GetType().GetNestedTypes(BindingFlags.NonPublic).First(t=>t.IsEnum);
            before=audio.PlayedCount;Invoke(ai,"ChangeState",Enum.Parse(enumType,"Attack"));
            Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.EnemyWindup,"Warning at attack entry: "+health.name);
            Field(ai,"stateTimer",0.19f);Invoke(ai,"UpdateAttack");Check(player.CurrentHealth==3,"No damage before wind-up finishes: "+health.name);
            Field(ai,"stateTimer",0.2f);Invoke(ai,"UpdateAttack");Check(player.CurrentHealth==2,"Original damage at 0.2 seconds: "+health.name);
            player.StopAllCoroutines();
        }
        Field(player,"<IsTakingHit>k__BackingField",false);before=audio.PlayedCount;player.TakeDamage(99);
        Check(audio.PlayedCount==before+1&&audio.LastEvent==SoundEvent.Death,"Lethal damage immediately emits death sound");
        w=Wait(0.7f);while(w.MoveNext())yield return null;
        Check(LevelSession.Instance.Failed&&audio.LastEvent==SoundEvent.Lose,"Defeat result emits delayed lose cue with time frozen");
        session.LoadLevel(1);w=Wait(0.7f);while(w.MoveNext())yield return null;session=LevelSession.Instance;session.SetPaused(true);
        foreach(var enemy in Object.FindObjectsByType<EnemyHealth>())enemy.TakeDamage(99,enemy.transform.position);
        session.SetPaused(false);w=Wait(0.2f);while(w.MoveNext())yield return null;
        Check(session.Completed&&audio.LastEvent==SoundEvent.Win,"Real enemy defeat triggers victory sound");
        Check(LevelSession.Store.Data.completedLevels[0],"Victory still saves progression");
        foreach(var source in audio.GetComponents<AudioSource>().Where(a=>a.outputAudioMixerGroup==audio.sfxGroup))Check(source.pitch>=0.92f&&source.pitch<=1.08f,"SFX pitch remains within variation bounds");
        Check(audio.GetComponents<AudioSource>().Length==AudioManager.VoiceLimit+2,"Source count is bounded to 12 SFX plus 2 music");
        LevelSession.Store.SetMusicVolume(0.25f);LevelSession.Store.SetSfxVolume(0.7f);
        SessionState.SetInt("Assignment12.Stage",2);routine=null;EditorApplication.isPlaying=false;
    }
    static IEnumerator RestartTests(){
        var w=Wait(1.3f);while(w.MoveNext())yield return null;
        Check(LevelSession.Store.Data.musicVolume==0.25f&&LevelSession.Store.Data.sfxVolume==0.7f&&LevelSession.Store.Data.completedLevels[0],"Settings and progression survive Play-mode domain reload");
        Check(LevelSession.Instance.InMainMenu,"Fresh application session returns to main menu");
        Check(new FileInfo(Folder+"/runtime-errors.txt").Length==0,"No runtime errors during isolated sound tests");
        File.AppendAllText(Report,"ALL ASSIGNMENT 12 AUTOMATED CHECKS PASSED\nListening and real-device checks are separate.\n");
        SessionState.SetBool("Assignment12.Tests",false);SessionState.SetInt("Assignment12.Stage",0);routine=null;EditorApplication.isPlaying=false;
    }
}
