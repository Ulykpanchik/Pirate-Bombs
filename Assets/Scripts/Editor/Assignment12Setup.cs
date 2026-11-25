using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class Assignment12Setup {
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static Assignment12Setup(){EditorApplication.update+=Poll;}
    static void Poll(){
        const string file="Library/Assignment12-command.txt";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(file))return;
        string command=File.ReadAllText(file).Trim();File.Delete(file);
        try {
            if(command=="setup") Configure();
            else if(command=="tests") Assignment12Verification.Run();
            else if(command=="android") BuildAndroid();
            else if(command=="stop") {SessionState.SetBool("Assignment12.Tests",false);EditorApplication.isPlaying=false;}
            File.WriteAllText("Library/Assignment12-command-result.txt","OK "+command);
        }catch(Exception e){File.WriteAllText("Library/Assignment12-command-result.txt",e.ToString());Debug.LogException(e);}
    }
    public static object Call(object obj,string name,params object[] args){
        var type=obj as Type ?? obj.GetType();
        var method=type.GetMethods(Flags).First(m=>m.Name==name && m.GetParameters().Length==args.Length);
        return method.Invoke(obj is Type?null:obj,args);
    }
    static object Get(object obj,string name)=>obj.GetType().GetProperty(name,Flags).GetValue(obj);
    static void Set(object obj,string name,object value)=>obj.GetType().GetProperty(name,Flags).SetValue(obj,value);
    static Type AudioType(string name)=>typeof(Editor).Assembly.GetType("UnityEditor.Audio."+name,true);
    [MenuItem("Pirate Bombs/Assignment 12/Configure Audio and Menu")]
    public static void Configure(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before configuration.");
        Directory.CreateDirectory("Assets/Audio");Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
        const string mixerPath="Assets/Audio/PirateBombsAudio.mixer";
        var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
        if(mixer==null){
            mixer=(AudioMixer)Call(AudioType("AudioMixerController"),"CreateMixerControllerAtPath",mixerPath);
            mixer.name="PirateBombsAudio";
            var master=Get(mixer,"masterGroup");
            var music=Call(mixer,"CreateNewGroup","Music",false);
            var sfx=Call(mixer,"CreateNewGroup","SFX",false);
            var ambience=Call(mixer,"CreateNewGroup","MusicPause",false);
            Call(mixer,"AddChildToParent",ambience,master);Call(mixer,"AddChildToParent",music,ambience);Call(mixer,"AddChildToParent",sfx,master);
            var normal=((Array)Get(mixer,"snapshots")).GetValue(0);((Object)normal).name="Normal";
            Set(mixer,"TargetSnapshot",normal);
            Call(mixer,"CloneNewSnapshotFromTarget",false);
            var paused=Get(mixer,"TargetSnapshot");((Object)paused).name="Paused";
            var effect=Activator.CreateInstance(AudioType("AudioMixerEffectController"),new object[]{"Lowpass"});
            Call(effect,"PreallocateGUIDs");AssetDatabase.AddObjectToAsset((Object)effect,mixer);Call(ambience,"InsertEffect",effect,1);
            Call(ambience,"SetValueForVolume",mixer,normal,0f);Call(ambience,"SetValueForVolume",mixer,paused,-10f);
            Call(effect,"SetValueForParameter",mixer,normal,"Cutoff freq",22000f);
            Call(effect,"SetValueForParameter",mixer,paused,"Cutoff freq",1000f);
            var property=mixer.GetType().GetProperty("exposedParameters",Flags);
            var elementType=property.PropertyType.GetElementType();var exposed=Array.CreateInstance(elementType,3);
            object[] groups={master,music,sfx};string[] names={"MasterVolumeDb","MusicVolumeDb","SfxVolumeDb"};
            for(int i=0;i<3;i++){var item=Activator.CreateInstance(elementType);elementType.GetField("name",Flags).SetValue(item,names[i]);
                elementType.GetField("guid",Flags).SetValue(item,Call(groups[i],"GetGUIDForVolume"));exposed.SetValue(item,i);
                Call(groups[i],"SetValueForVolume",mixer,normal,0f);Call(groups[i],"SetValueForVolume",mixer,paused,0f);}
            property.SetValue(mixer,exposed);Set(mixer,"startSnapshot",normal);Set(mixer,"TargetSnapshot",normal);
            mixer.updateMode=AudioMixerUpdateMode.UnscaledTime;
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(mixerPath)) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
        var catalog=AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/Audio/PirateBombsAudio.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<AudioCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Audio/PirateBombsAudio.asset");}
        catalog.menuMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/mutiny_2_2.mp3");
        catalog.gameplayMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/pirate_indenture_loop.wav");
        catalog.sounds=Enum.GetValues(typeof(SoundEvent)).Cast<SoundEvent>().Select(id=>new SoundEntry{
            id=id,clips=new[]{AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/"+id+".ogg")},
            volume=id==SoundEvent.Explosion?0.65f:0.8f,varyPitch=id!=SoundEvent.Win&&id!=SoundEvent.Lose&&id!=SoundEvent.Death&&id!=SoundEvent.Click}).ToArray();
        EditorUtility.SetDirty(catalog);
        foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio"})){
            var p=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(p);
            var settings=importer.defaultSampleSettings;settings.loadType=p.Contains("/Music/")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=0.65f;
            importer.defaultSampleSettings=settings;importer.forceToMono=!p.Contains("/Music/");importer.SaveAndReimport();
        }
        var root=new GameObject("AudioSystem");
        var audio=root.AddComponent<AudioManager>();audio.catalog=catalog;audio.mixer=mixer;
        audio.sfxGroup=mixer.FindMatchingGroups("SFX").First();audio.normal=mixer.FindSnapshot("Normal");audio.paused=mixer.FindSnapshot("Paused");
        var musicManager=root.AddComponent<MusicManager>();musicManager.catalog=catalog;musicManager.musicGroup=mixer.FindMatchingGroups("Music").First(g=>g.name=="Music");
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/AudioSystem.prefab");Object.DestroyImmediate(root);
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var ui=Object.FindFirstObjectByType<AssignmentUI>();
        var parent=ui.menuPanel.transform;
        ui.menuTitle=parent.Find("Title").GetComponent<Text>();
        parent.GetComponent<RectTransform>().sizeDelta=new Vector2(650,650);
        Move(parent,"Title",0,287);Move(parent,"Progress",0,235);
        for(int i=0;i<3;i++) Move(parent,"Level"+(i+1),0,180-i*60);
        parent.Find("VolumeLabel").GetComponent<Text>().text="Music Volume";
        Move(parent,"VolumeLabel",0,15);Move(parent,"SoundVolume",0,-20);
        if(ui.sfxVolume==null){var label=Object.Instantiate(parent.Find("VolumeLabel").gameObject,parent);label.name="SfxVolumeLabel";label.GetComponent<Text>().text="SFX Volume";
            var slider=Object.Instantiate(ui.volume.gameObject,parent);slider.name="SfxVolume";ui.sfxVolume=slider.GetComponent<Slider>();}
        Move(parent,"SfxVolumeLabel",0,-60);Move(parent,"SfxVolume",0,-95);
        Move(parent,"ResetProgress",-140,-150);Move(parent,"Resume",140,-150);
        if(ui.homeButton==null){var home=Object.Instantiate(ui.resumeButton.gameObject,parent);home.name="MainMenu";ui.homeButton=home.GetComponent<Button>();ui.homeButton.GetComponentInChildren<Text>().text="Main Menu";}
        Move(parent,"MainMenu",0,-213);Move(parent,"SaveStatus",0,-264);Move(parent,"Controls",0,-302);
        parent.Find("Controls").GetComponent<Text>().text="Move, Jump and Bomb: use the on-screen controls";
        ui.confirmationPanel.transform.Find("ConfirmExplanation").GetComponent<Text>().text="Levels, Music and SFX return to defaults.";
        if(ui.creditsButton==null) {
            var button=Object.Instantiate(ui.homeButton.gameObject,parent);button.name="AudioCredits";ui.creditsButton=button.GetComponent<Button>();
            ui.creditsButton.GetComponentInChildren<Text>().text="Audio Credits";
            button.GetComponent<RectTransform>().sizeDelta=new Vector2(300,30);
            var panel=new GameObject("AudioCreditsPanel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(parent.parent,false);panel.GetComponent<RectTransform>().sizeDelta=new Vector2(650,650);
            panel.GetComponent<Image>().color=new Color(0.055f,0.09f,0.13f,0.99f);ui.creditsPanel=panel;
            var text=Object.Instantiate(ui.menuTitle.gameObject,panel.transform).GetComponent<Text>();text.name="CreditsText";
            text.rectTransform.anchoredPosition=new Vector2(0,35);text.rectTransform.sizeDelta=new Vector2(600,520);text.fontSize=21;
            text.text="AUDIO CREDITS\n\nPirate Music (mutiny_2_2.mp3)\nby migfus20 - CC BY 4.0\nUnmodified audio\nopengameart.org/content/pirate-music\ncreativecommons.org/licenses/by/4.0/\n\nPirate Indenture (loop)\nby Eldritch Grim - CC0\nopengameart.org/content/pirate-indenture\n\nSound effects by Kenney - CC0\nkenney.nl/assets/digital-audio\nkenney.nl/assets/impact-sounds\nkenney.nl/assets/music-jingles";
            var close=Object.Instantiate(ui.homeButton.gameObject,panel.transform);close.name="CloseCredits";ui.closeCreditsButton=close.GetComponent<Button>();
            ui.closeCreditsButton.GetComponentInChildren<Text>().text="Back";close.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-270);
            panel.SetActive(false);
        }
        Move(parent,"AudioCredits",0,-303);parent.Find("Controls").gameObject.SetActive(false);
        foreach(var panel in new[]{ui.menuPanel,ui.creditsPanel,ui.confirmationPanel}) if(panel.GetComponent<MenuPanelFitter>()==null) panel.AddComponent<MenuPanelFitter>();
        EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Library/Assignment12-setup.txt","Mixer, snapshots, filter, catalog, prefab and scene configured.");
    }
    static void Move(Transform parent,string name,float x,float y){parent.Find(name).GetComponent<RectTransform>().anchoredPosition=new Vector2(x,y);}
    [MenuItem("Pirate Bombs/Assignment 12/Build Android APK")]
    public static void BuildAndroid(){
        Directory.CreateDirectory("Builds/Android");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SampleScene.unity"},target=BuildTarget.Android,
            locationPathName="Builds/Android/PirateBombs-Assignment12.apk",options=BuildOptions.None});
        File.WriteAllText("Library/Assignment12-build.txt",result.summary.result+" errors="+result.summary.totalErrors+" size="+result.summary.totalSize);
    }
}
