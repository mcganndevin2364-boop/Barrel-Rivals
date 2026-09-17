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
            var owner=new GameObject("Skill practice").AddComponent<PracticeController>();

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
            Label(retryPanel,"Retry label","RETRY",new Vector2(.5f,.5f),Vector2.zero,new Vector2(120,46),19,Color.white,TextAnchor.MiddleCenter);

            var action=Panel(safe,"Practice action",new Vector2(0,0),new Vector2(0,0),new Vector2(26,42),new Vector2(650,126),new Color(.04f,.15f,.17f,.96f));
            var actionGroup=action.gameObject.AddComponent<CanvasGroup>();
            action.gameObject.AddComponent<PracticeInputSurface>().Configure(owner,false);
            Text actionText=Label(action,"Action label","HOLD TO BEGIN",new Vector2(.5f,.5f),new Vector2(0,12),new Vector2(610,60),32,Color.white,TextAnchor.MiddleCenter);
            var track=Panel(action,"Timing track",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,17),new Vector2(600,10),new Color(.15f,.29f,.30f));
            track.GetComponent<Image>().raycastTarget=false;
            var fill=Panel(track,"Timing fill",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,new Color(1,.73f,.32f));
            Stretch(fill); var progress=fill.GetComponent<Image>(); progress.raycastTarget=false;
            progress.type=Image.Type.Filled; progress.fillMethod=Image.FillMethod.Horizontal;
            var mark=Panel(track,"Center mark",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,22),Color.white);
            mark.GetComponent<Image>().raycastTarget=false;

            var draw=Panel(safe,"Drawing panel",new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-26,-50),new Vector2(390,434),new Color(.025f,.095f,.12f,.97f));
            var drawingGroup=draw.gameObject.AddComponent<CanvasGroup>();
            Text shape=Label(draw,"Shape label","DRAW FROM MEMORY",new Vector2(.5f,1),new Vector2(0,-12),new Vector2(370,30),17,new Color(1,.77f,.39f),TextAnchor.MiddleCenter);
            var drawTrack=Panel(draw,"Drawing clock",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,383),new Vector2(356,6),new Color(.15f,.29f,.30f));
            drawTrack.GetComponent<Image>().raycastTarget=false;
            var drawFill=Panel(drawTrack,"Drawing time left",Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.3f,1,.8f));
            Stretch(drawFill); var drawProgress=drawFill.GetComponent<Image>(); drawProgress.raycastTarget=false;
            var pad=Panel(draw,"Drawing pad",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,18),new Vector2(356,356),new Color(.045f,.14f,.16f));
            pad.gameObject.AddComponent<PracticeInputSurface>().Configure(owner,true);
            var plot=new GameObject("Pattern plot",typeof(RectTransform),typeof(PatternGraphic)).GetComponent<RectTransform>();
            plot.SetParent(pad,false); Stretch(plot); var pattern=plot.GetComponent<PatternGraphic>(); pattern.raycastTarget=false;
            var result=Panel(safe,"Practice result",new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(26,-36),new Vector2(660,340),new Color(.025f,.095f,.12f,.97f));
            var resultGroup=result.gameObject.AddComponent<CanvasGroup>();
            Text resultText=Label(result,"Result text","",new Vector2(.5f,.5f),Vector2.zero,new Vector2(612,310),24,Color.white,TextAnchor.MiddleLeft);
            Label(safe,"Footer","ONE-BARREL PRACTICE  /  NO RANKED REWARDS",new Vector2(0,0),new Vector2(28,9),new Vector2(700,25),14,new Color(.86f,.9f,.88f));
            owner.Configure(horse,barrel,camera,safe,phase,hint,timer,actionText,shape,resultText,actionGroup,drawingGroup,resultGroup,progress,drawProgress,pattern,retry);
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
            PlayerSettings.bundleVersion="0.2.0"; PlayerSettings.Android.bundleVersionCode=2;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36; PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath},locationPathName="Builds/Android/BarrelRivals-Practice.apk",target=BuildTarget.Android,options=BuildOptions.Development });
            Debug.Log($"BARREL_M1: Android build {report.summary.result}; errors={report.summary.totalErrors}.");
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Practice Android build failed.");
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
