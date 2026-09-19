using System;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BarrelRivals.Editor
{
    public static class PracticeBuilder
    {
        public const string ScenePath="Assets/_Project/Generated/Practice/Arena_Practice.unity";
        [MenuItem("Barrel Rivals/Setup Skill Practice")]
        public static void Generate()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/_Project/Generated/Practice"); AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(FoundationBuilder.ScenePath);
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Could not save practice copy.");
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Practice HUD"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Foundation preview"));
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Preview destination"));
            var horse=GameObject.Find("Horse proxy").transform;
            var barrel=GameObject.Find("Barrel 1").transform;
            Camera camera=Camera.main;
            camera.transform.position=new Vector3(3,2.9f,-14.5f);
            camera.transform.LookAt(new Vector3(0,1.5f,-9)); camera.fieldOfView=65;
            if(PrefabUtility.IsPartOfPrefabInstance(horse.gameObject))
                PrefabUtility.UnpackPrefabInstance(horse.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            for(int i=1;i<=3;i++)
            {
                var barrelObject=GameObject.Find("Barrel "+i);
                if(barrelObject && PrefabUtility.IsPartOfPrefabInstance(barrelObject))
                    PrefabUtility.UnpackPrefabInstance(barrelObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            }
            PracticePresentationBuilder.Build(horse,barrel,camera);
            var owner=new GameObject("Skill practice").AddComponent<PracticeController>();
            var feedback=owner.gameObject.AddComponent<PracticeFeedback>();
            var ghost=owner.gameObject.AddComponent<PracticeGhost>();
            ghost.Configure(horse,GhostMaterial());
            var dust=CreateDust(owner.transform,horse);

            var canvas=new GameObject("Skill HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvas.transform,false); Stretch(safe);
            var header=Panel(safe,"Header",new Vector2(0,1),new Vector2(0,1),new Vector2(0,0),new Vector2(1280,144),new Color(.025f,.065f,.08f,.94f));
            header.anchorMax=new Vector2(1,1); header.sizeDelta=new Vector2(0,144);
            Label(header,"Brand","BARREL RIVALS",new Vector2(0,1),new Vector2(26,-13),new Vector2(500,30),19,new Color(1,.75f,.38f));
            Text phase=Label(header,"Phase","FIRST BARREL · PRACTICE",new Vector2(0,1),new Vector2(26,-47),new Vector2(700,43),30,Color.white);
            Text hint=Label(header,"Hint","Hold through the beeps. Release on GO.",new Vector2(0,1),new Vector2(28,-96),new Vector2(1090,35),19,new Color(.77f,.85f,.85f));
            Text timer=Label(header,"Timer","0.00s · +0s",new Vector2(1,1),new Vector2(-165,-22),new Vector2(240,43),26,Color.white,TextAnchor.MiddleRight);
            var retryPanel=Panel(header,"Retry",new Vector2(1,1),new Vector2(1,1),new Vector2(-22,-20),new Vector2(124,52),new Color(.14f,.24f,.26f));
            var retry=retryPanel.gameObject.AddComponent<Button>(); retry.targetGraphic=retryPanel.GetComponent<Image>();
            Label(retryPanel,"Retry label","RETRY SAME",new Vector2(.5f,.5f),Vector2.zero,new Vector2(120,46),16,Color.white,TextAnchor.MiddleCenter);

            Text best=Label(header,"Personal best","",new Vector2(1,1),new Vector2(-26,-69),new Vector2(430,26),15,new Color(.64f,.88f,.81f),TextAnchor.MiddleRight);

            var skillPanel=Panel(safe,"Skill confirmation",Vector2.zero,Vector2.zero,new Vector2(26,220),new Vector2(470,68),new Color(.025f,.065f,.08f,.86f));
            skillPanel.GetComponent<Image>().raycastTarget=false;
            var skillGroup=skillPanel.gameObject.AddComponent<CanvasGroup>();
            Text skill=Label(skillPanel,"Skill text","",new Vector2(.5f,.5f),Vector2.zero,new Vector2(432,62),28,Color.white,TextAnchor.MiddleLeft);
            var action=Panel(safe,"Practice action",new Vector2(0,0),new Vector2(0,0),new Vector2(26,42),new Vector2(650,126),new Color(.04f,.15f,.17f,.96f));
            var actionGroup=action.gameObject.AddComponent<CanvasGroup>();
            action.gameObject.AddComponent<PracticeInputSurface>().Configure(owner,false);
            Text actionText=Label(action,"Action label","HOLD TO BEGIN",new Vector2(.5f,.5f),new Vector2(0,12),new Vector2(610,60),32,Color.white,TextAnchor.MiddleCenter);
            var track=Panel(action,"Timing track",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,17),new Vector2(600,10),new Color(.15f,.29f,.30f));
            track.GetComponent<Image>().raycastTarget=false;
            var fill=Panel(track,"Timing fill",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,new Color(1,.73f,.32f));
            Stretch(fill); var progress=fill.GetComponent<Image>(); progress.raycastTarget=false;
            progress.sprite=ReinsLabBuilder.MeterSprite(); progress.type=Image.Type.Filled; progress.fillMethod=Image.FillMethod.Horizontal;
            var mark=Panel(track,"Center mark",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,22),Color.white);
            mark.GetComponent<Image>().raycastTarget=false;

            var draw=Panel(safe,"Drawing panel",new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-26,-50),new Vector2(390,434),new Color(.025f,.095f,.12f,.97f));
            var drawingGroup=draw.gameObject.AddComponent<CanvasGroup>();
            Text shape=Label(draw,"Shape label","DRAW FROM MEMORY",new Vector2(.5f,1),new Vector2(0,-12),new Vector2(370,30),17,new Color(1,.77f,.39f),TextAnchor.MiddleCenter);
            var drawTrack=Panel(draw,"Drawing clock",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,383),new Vector2(356,6),new Color(.15f,.29f,.30f));
            drawTrack.GetComponent<Image>().raycastTarget=false;
            var drawFill=Panel(drawTrack,"Drawing time left",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.3f,1,.8f));
            Stretch(drawFill); var drawProgress=drawFill.GetComponent<Image>(); drawProgress.raycastTarget=false;
            drawProgress.sprite=ReinsLabBuilder.MeterSprite(); drawProgress.type=Image.Type.Filled; drawProgress.fillMethod=Image.FillMethod.Horizontal;
            var pad=Panel(draw,"Drawing pad",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,18),new Vector2(356,356),new Color(.045f,.14f,.16f));
            pad.gameObject.AddComponent<PracticeInputSurface>().Configure(owner,true);
            var plot=new GameObject("Pattern plot",typeof(RectTransform),typeof(PatternGraphic)).GetComponent<RectTransform>();
            plot.SetParent(pad,false); Stretch(plot); var pattern=plot.GetComponent<PatternGraphic>(); pattern.raycastTarget=false;
            var result=Panel(safe,"Practice result",new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(26,-26),new Vector2(660,360),new Color(.025f,.095f,.12f,.97f));
            var resultGroup=result.gameObject.AddComponent<CanvasGroup>();
            Text resultText=Label(result,"Result text","",new Vector2(0,1),new Vector2(24,-14),new Vector2(612,268),22,Color.white,TextAnchor.UpperLeft);
            var retryResult=MakeButton(result,"Retry same challenge","RETRY SAME CHALLENGE",new Vector2(0,0),new Vector2(24,18),new Vector2(294,54),18,new Color(.13f,.32f,.30f),out _);
            // All retry buttons share the controller's explicit same-challenge method.
            UnityEditor.Events.UnityEventTools.AddPersistentListener(retryResult.onClick,owner.Retry);
            var next=MakeButton(result,"New challenge","NEW CHALLENGE",new Vector2(0,0),new Vector2(342,18),new Vector2(294,54),18,new Color(.27f,.22f,.15f),out _);
            var sound=MakeButton(safe,"Sound preference","SOUND ON",new Vector2(1,0),new Vector2(-276,8),new Vector2(118,36),13,new Color(.025f,.065f,.08f,.8f),out Text soundText);
            var haptics=MakeButton(safe,"Haptics preference","HAPTICS ON",new Vector2(1,0),new Vector2(-150,8),new Vector2(138,36),13,new Color(.025f,.065f,.08f,.8f),out Text hapticsText);
            var ghostToggle=MakeButton(safe,"Ghost preference","GHOST ON",new Vector2(1,0),new Vector2(-24,8),new Vector2(118,36),13,new Color(.025f,.065f,.08f,.8f),out Text ghostText);
            Label(safe,"Footer","ONE-BARREL PRACTICE  /  NO RANKED REWARDS",new Vector2(0,0),new Vector2(28,9),new Vector2(620,25),13,new Color(.86f,.9f,.88f));
            owner.Configure(horse,barrel,camera,safe,phase,hint,timer,actionText,shape,resultText,actionGroup,drawingGroup,resultGroup,progress,drawProgress,pattern,retry,
                next,sound,haptics,ghostToggle,soundText,hapticsText,ghostText,best,skill,skillGroup,feedback,ghost,dust);
            EditorBuildSettings.scenes=new[] { new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(FoundationBuilder.ScenePath,true) };
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene); EditorSceneManager.OpenScene(ScenePath); Validate();
            Debug.Log("BARREL_M1: practice scene generated and reopened.");
        }
        public static void Validate()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath) EditorSceneManager.OpenScene(ScenePath);
            var controller=UnityEngine.Object.FindFirstObjectByType<PracticeController>();
            if(!controller || !controller.HasBindings) throw new InvalidOperationException("Missing practice scene references.");
            var graphic=UnityEngine.Object.FindFirstObjectByType<PatternGraphic>();
            if(!graphic || !graphic.GetComponent<CanvasRenderer>()) throw new InvalidOperationException("Pattern drawing renderer is missing.");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Missing script: "+t.name);
            if(UnityEngine.Object.FindObjectsByType<PracticeInputSurface>(FindObjectsSortMode.None).Length!=2)
                throw new InvalidOperationException("Practice input surfaces are missing.");
            if(!EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==ScenePath)) throw new InvalidOperationException("Practice scene not in build settings.");
            Debug.Log("BARREL_M1: persisted scene validation PASS.");
        }
        public static void BuildAndroid()
        {
            Validate(); Directory.CreateDirectory("Builds/Android"); EditorUserBuildSettings.buildAppBundle=false;
            PlayerSettings.bundleVersion="0.3.0"; PlayerSettings.Android.bundleVersionCode=3;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36; PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/Android/BarrelRivals-Practice.apk",target=BuildTarget.Android,options=BuildOptions.Development });
            Debug.Log($"BARREL_M1: Android build {report.summary.result}; errors={report.summary.totalErrors}.");
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Practice Android build failed.");
        }
        private static PracticeDust CreateDust(Transform parent,Transform horse)
        {
            const string root="Assets/_Project/Generated/Practice/";
            const int size=32;
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"DustMask.asset");
            if(!mask) { mask=new Texture2D(size,size,TextureFormat.RGBA32,true); AssetDatabase.CreateAsset(mask,root+"DustMask.asset"); }
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float r=(new Vector2(x+.5f,y+.5f)/size-Vector2.one*.5f).sqrMagnitude*4;
                pixels[y*size+x]=new Color(1,1,1,Mathf.Exp(-r*5)*Mathf.Clamp01(1-r));
            }
            mask.SetPixels(pixels); mask.wrapMode=TextureWrapMode.Clamp; mask.Apply(); EditorUtility.SetDirty(mask);
            var material=AssetDatabase.LoadAssetAtPath<Material>(root+"Dust.mat");
            if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(material,root+"Dust.mat"); }
            material.SetTexture("_BaseMap",mask); material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Surface",1); material.SetFloat("_ZWrite",0);
            material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType","Transparent"); material.renderQueue=3000; EditorUtility.SetDirty(material);
            var system=new GameObject("Hoof dirt").AddComponent<ParticleSystem>(); system.transform.SetParent(parent,false);
            system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main; main.playOnAwake=false; main.loop=true; main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.maxParticles=48; main.startSpeed=0; main.startLifetime=.85f;
            var emission=system.emission; emission.enabled=false;
            var shape=system.shape; shape.enabled=false;
            var fade=system.colorOverLifetime; fade.enabled=true;
            var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(0,1)}); fade.color=gradient;
            var sizeOverLife=system.sizeOverLifetime; sizeOverLife.enabled=true; sizeOverLife.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.45f,1,1.9f));
            var renderer=system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
            var dust=parent.gameObject.AddComponent<PracticeDust>(); dust.Configure(horse,system); return dust;
        }
        private static Material GhostMaterial()
        {
            const string path="Assets/_Project/Generated/Practice/PersonalBestGhost.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",new Color(.35f,.88f,.77f,.28f));
            material.SetFloat("_Surface",1); material.SetFloat("_ZWrite",0);
            material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType","Transparent"); material.renderQueue=3000;
            EditorUtility.SetDirty(material); return material;
        }
        private static Button MakeButton(Transform parent,string name,string title,Vector2 anchor,Vector2 offset,Vector2 size,int fontSize,Color tint,out Text label)
        {
            var panel=Panel(parent,name,anchor,anchor,offset,size,tint);
            var button=panel.gameObject.AddComponent<Button>(); button.targetGraphic=panel.GetComponent<Image>();
            label=Label(panel,name+" label",title,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(8,4),fontSize,Color.white,TextAnchor.MiddleCenter);
            return button;
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 offset,Vector2 size,Color tint)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); var rect=go.GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=anchor; rect.pivot=pivot; rect.anchoredPosition=offset; rect.sizeDelta=size;
            go.GetComponent<Image>().color=tint; return rect;
        }
        private static Text Label(Transform parent,string name,string value,Vector2 anchor,Vector2 offset,Vector2 size,int fontSize,Color tint,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor; rect.pivot=anchor; rect.anchoredPosition=offset; rect.sizeDelta=size;
            var text=go.GetComponent<Text>(); text.text=value; text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=fontSize; text.color=tint; text.alignment=align; text.raycastTarget=false; return text;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
    }
}
