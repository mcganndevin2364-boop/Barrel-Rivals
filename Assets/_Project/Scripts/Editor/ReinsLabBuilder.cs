using System;
using System.IO;
using System.Linq;
using BarrelRivals.Core;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BarrelRivals.Editor
{
    public static class ReinsLabBuilder
    {
        public const string Root="Assets/_Project/Generated/ReinsLab";
        public const string ScenePath=Root+"/Arena_ReinsLab.unity";
        private static readonly Color Ink=new Color(.025f,.065f,.08f,.94f);
        [MenuItem("Barrel Rivals/Setup Reins Lab")]
        public static void Generate()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(PracticeBuilder.ScenePath);
            EditorSceneManager.SaveScene(scene,ScenePath);
            foreach(string name in new[]{"Skill practice","Skill HUD","Practice mode navigation"})
            {var old=GameObject.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old);}
            var horse=GameObject.Find("Horse proxy").transform;
            var barrels=Enumerable.Range(1,3).Select(i=>GameObject.Find("Barrel "+i).transform).ToArray();
            var camera=Camera.main;var controller=new GameObject("Reins practice").AddComponent<ReinsLabController>();
            // Classic's decorative inner rails have no contact rules. Use separate world meshes for the free-steering lab.
            PracticePresentationBuilder.Build(horse,barrels[0],camera,includeAlleyRails:false);
            var dirt=GameObject.Find("Arena dirt").GetComponent<Renderer>().sharedMaterial;
            var patches=new Renderer[6];var boundaries=new[]{-16f,12f,34f,64f};
            for(int row=0;row<3;row++)for(int side=0;side<2;side++)
            {
                var patch=GameObject.CreatePrimitive(PrimitiveType.Plane);patch.name="Footing patch "+(row*2+side);UnityEngine.Object.DestroyImmediate(patch.GetComponent<Collider>());
                patch.transform.position=new Vector3(side==0?-14:14,.008f,(boundaries[row]+boundaries[row+1])*.5f);patch.transform.localScale=new Vector3(2.8f,1,(boundaries[row+1]-boundaries[row])/10);
                var renderer=patch.GetComponent<Renderer>();renderer.sharedMaterial=dirt;renderer.shadowCastingMode=ShadowCastingMode.Off;patches[row*2+side]=renderer;
            }
            controller.gameObject.AddComponent<ReinsFootingVisual>().Configure(patches);
            for(int i=0;i<3;i++)CreateZones(barrels[i],i);
            var canvas=new GameObject("Reins HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
            var header=Panel(safe,"Header",new Vector2(0,1),Vector2.zero,new Vector2(1280,112),Ink);header.anchorMax=Vector2.one;header.sizeDelta=new Vector2(0,112);
            var title=Label(header,"Title","REINS RACING · PRACTICE",new Vector2(0,1),new Vector2(24,-15),new Vector2(720,42),28);
            var status=Label(header,"Timer","0.00s",new Vector2(1,1),new Vector2(-24,-18),new Vector2(520,40),22,TextAnchor.MiddleRight);
            var hint=Label(header,"Instruction","",new Vector2(0,1),new Vector2(26,-67),new Vector2(1228,38),18);
            var left=Panel(safe,"Left rein",Vector2.zero,new Vector2(24,50),new Vector2(280,258),new Color(.03f,.13f,.15f,.45f));
            var right=Panel(safe,"Right rein",new Vector2(1,0),new Vector2(-24,50),new Vector2(280,258),new Color(.03f,.13f,.15f,.45f));
            left.GetComponent<Image>().raycastTarget=true;right.GetComponent<Image>().raycastTarget=true;
            left.gameObject.AddComponent<ReinsInputSurface>().Configure(controller,ReinsPad.Left);right.gameObject.AddComponent<ReinsInputSurface>().Configure(controller,ReinsPad.Right);
            Label(left,"Left label","LEFT REIN\nDrag down to pull",new Vector2(.5f,1),new Vector2(0,-18),new Vector2(250,64),22,TextAnchor.MiddleCenter);
            Label(right,"Right label","RIGHT REIN\nDrag down to pull",new Vector2(.5f,1),new Vector2(0,-18),new Vector2(250,64),22,TextAnchor.MiddleCenter);
            var lf=Fill(left,"Left tension",new Vector2(.5f,0),new Vector2(0,22),new Vector2(226,12));var rf=Fill(right,"Right tension",new Vector2(.5f,0),new Vector2(0,22),new Vector2(226,12));
            var rhythm=Panel(safe,"Rhythm and wrap",new Vector2(.5f,0),new Vector2(0,50),new Vector2(330,130),new Color(.04f,.19f,.19f,.96f));rhythm.GetComponent<Image>().raycastTarget=true;rhythm.gameObject.AddComponent<ReinsInputSurface>().Configure(controller,ReinsPad.Rhythm);
            var action=Label(rhythm,"Action","TAP THE BEAT",new Vector2(.5f,.5f),new Vector2(0,12),new Vector2(312,78),20,TextAnchor.MiddleCenter);
            var beat=Fill(rhythm,"Beat",new Vector2(.5f,0),new Vector2(0,14),new Vector2(290,12));
            var feedback=Label(safe,"Feedback","",new Vector2(.5f,0),new Vector2(0,202),new Vector2(640,48),22,TextAnchor.MiddleCenter);
            feedback.color=new Color(1,.78f,.38f);
            var mapPanel=Panel(safe,"Course map",new Vector2(1,1),new Vector2(-26,-122),new Vector2(210,192),Ink);
            var mapObject=new GameObject("Route",typeof(RectTransform),typeof(ReinsMapGraphic));var mapRect=mapObject.GetComponent<RectTransform>();mapRect.SetParent(mapPanel,false);mapRect.anchorMin=Vector2.zero;mapRect.anchorMax=Vector2.one;mapRect.offsetMin=new Vector2(8,8);mapRect.offsetMax=new Vector2(-8,-8);var map=mapObject.GetComponent<ReinsMapGraphic>();map.raycastTarget=false;
            var start=Button(safe,"Begin","BEGIN RUN",new Vector2(.5f,.5f),new Vector2(0,15),new Vector2(290,74),25,out _);
            var resultPanel=Panel(safe,"Run result",new Vector2(.5f,.5f),new Vector2(0,30),new Vector2(680,326),Ink);var resultGroup=resultPanel.gameObject.AddComponent<CanvasGroup>();
            var result=Label(resultPanel,"Result","",new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(630,280),24,TextAnchor.MiddleCenter);
            var retry=Button(safe,"Retry","RETRY SAME",new Vector2(0,1),new Vector2(24,-124),new Vector2(180,44),17,out _);
            var surface=Button(safe,"Surface","DIRT: HARD PACK",new Vector2(0,1),new Vector2(214,-124),new Vector2(222,44),16,out var surfaceText);
            var ghost=Button(safe,"Own ghost","OWN BEST: —",new Vector2(0,1),new Vector2(446,-124),new Vector2(196,44),16,out var ghostText);
            var sound=Button(safe,"Sound","SOUND ON",new Vector2(0,0),new Vector2(24,6),new Vector2(150,34),15,out var soundText);
            var cameraMode=Button(safe,"Camera","BODYCAM",new Vector2(0,0),new Vector2(182,6),new Vector2(150,34),15,out var cameraText);
            var classic=Button(safe,"Classic practice","CLASSIC PRACTICE",new Vector2(1,0),new Vector2(-24,6),new Vector2(220,34),15,out _);
            Label(safe,"Practice status","FREE PRACTICE · LOCAL RECORDINGS",new Vector2(.5f,0),new Vector2(0,9),new Vector2(450,28),13,TextAnchor.MiddleCenter);
            var ghostMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Generated/Practice/PersonalBestGhost.mat");
            controller.Configure(horse,barrels,camera,safe,new[]{title,hint,status,feedback,action,result,surfaceText,soundText,cameraText,ghostText},new[]{beat,lf,rf},resultGroup,map,new[]{start,retry,surface,sound,cameraMode,classic,ghost},ghostMaterial);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(PracticeBuilder.ScenePath,true),new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(FoundationBuilder.ScenePath,true)};
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            AddClassicNavigation();EditorSceneManager.OpenScene(ScenePath);Validate();
            Debug.Log("BARREL_REINS: both practice modes generated, saved and reopened.");
        }
        private static void AddClassicNavigation()
        {
            var scene=EditorSceneManager.OpenScene(PracticeBuilder.ScenePath);
            var old=GameObject.Find("Practice mode navigation");if(old)UnityEngine.Object.DestroyImmediate(old);
            var canvas=GameObject.Find("Skill HUD").transform;
            var button=Button(canvas,"Practice mode navigation","TRY REINS RACING",new Vector2(0,1),new Vector2(600,-14),new Vector2(216,32),15,out _);
            var link=button.gameObject.AddComponent<PracticeModeLink>();UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,link.OpenReinsLab);
            foreach(var fill in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(fill.type==Image.Type.Filled || fill.name=="Drawing time left")
                {fill.sprite=MeterSprite();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;}
            EditorSceneManager.SaveScene(scene);
        }
        public static void Validate()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
            var controller=UnityEngine.Object.FindFirstObjectByType<ReinsLabController>();
            if(!controller || !controller.HasBindings)throw new InvalidOperationException("Reins scene bindings missing.");
            using(var bytes=new MemoryStream())
            {
                foreach(string name in new[]{"ReinsContracts.cs","ReinsCourseJudge.cs","ReinsRun.cs","ReinsReplay.cs"})
                {var prefix=System.Text.Encoding.UTF8.GetBytes(name+"\0");bytes.Write(prefix,0,prefix.Length);var data=File.ReadAllBytes("Packages/com.barrelrivals.core/Runtime/Reins/"+name);bytes.Write(data,0,data.Length);}
                using(var sha=System.Security.Cryptography.SHA256.Create())
                    if(BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-","").ToLowerInvariant()!=ReinsRuleFingerprint.Sha256)
                        throw new InvalidOperationException("Reins rule source changed: update its fingerprint and replay compatibility before building.");
            }
            if(UnityEngine.Object.FindObjectsByType<ReinsInputSurface>(FindObjectsSortMode.None).Length!=3)throw new InvalidOperationException("Expected left/right/rhythm pads.");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script: "+t.name);
            Debug.Log("BARREL_REINS: persisted scene validation PASS.");
        }
        public static void ExportIOS()
        {
            Validate();Version();PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;PlayerSettings.iOS.targetOSVersionString="15.0";PlayerSettings.iOS.appleEnableAutomaticSigning=true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS,new[]{GraphicsDeviceType.Metal});
            Build(BuildTarget.iOS,IPhonePracticeBuilder.ExportPath);
        }
        public static void BuildAndroid()
        {
            Validate();Version();EditorUserBuildSettings.buildAppBundle=false;PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            Build(BuildTarget.Android,"Builds/Android/BarrelRivals-ReinsLab.apk");
        }
        private static void Version(){PlayerSettings.bundleVersion="0.4.0";PlayerSettings.iOS.buildNumber="4";PlayerSettings.Android.bundleVersionCode=4;}
        private static void Build(BuildTarget target,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{PracticeBuilder.ScenePath,ScenePath},locationPathName=path,target=target,options=BuildOptions.Development});
            Debug.Log($"BARREL_REINS: {target} build {report.summary.result}; errors={report.summary.totalErrors}.");if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Reins mobile build failed.");
        }
        private static void CreateZones(Transform barrel,int index)
        {
            var colors=new[]{new Color(.35f,.83f,.58f),new Color(.98f,.72f,.23f),new Color(.92f,.38f,.28f),new Color(1,.86f,.6f)};
            var radii=new[]{5f,3.5f,2.2f,1.2f};
            for(int band=0;band<4;band++)
            {
                string path=Root+"/Zone"+band+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,path);}material.SetColor("_BaseColor",colors[band]);EditorUtility.SetDirty(material);
                var ring=new GameObject("Barrel "+(index+1)+" zone "+band).AddComponent<LineRenderer>();ring.sharedMaterial=material;ring.useWorldSpace=true;ring.loop=true;ring.widthMultiplier=.045f;ring.positionCount=96;ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
                var center=StandardCourse.Barrel(index);for(int p=0;p<96;p++){float angle=p*Mathf.PI*2/96;ring.SetPosition(p,new Vector3((float)center.X+Mathf.Cos(angle)*radii[band],.04f,(float)center.Z+Mathf.Sin(angle)*radii[band]));}
            }
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
        private static Text Label(Transform parent,string name,string content,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,TextAnchor alignment=TextAnchor.MiddleLeft)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.color=Color.white;text.text=content;text.alignment=alignment;text.raycastTarget=false;return text;}
        private static Button Button(Transform parent,string name,string content,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,out Text text)
        {var r=Panel(parent,name,anchor,pos,size,new Color(.09f,.23f,.25f,.96f));r.GetComponent<Image>().raycastTarget=true;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();text=Label(r,"Label",content,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(10,2),fontSize,TextAnchor.MiddleCenter);return button;}
        internal static Sprite MeterSprite()
        {
            const string path="Assets/_Project/Generated/Practice/TimingFillSprite.asset";
            var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(sprite)return sprite;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.name="Meter white";texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();AssetDatabase.CreateAsset(texture,path);
            sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);sprite.name="Meter white sprite";AssetDatabase.AddObjectToAsset(sprite,texture);return sprite;
        }
        private static Image Fill(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
        {var track=Panel(parent,name+" track",anchor,pos,size,new Color(.16f,.29f,.3f));var r=Panel(track,name,Vector2.zero,Vector2.zero,size,new Color(.35f,.9f,.77f));var image=r.GetComponent<Image>();image.sprite=MeterSprite();image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillAmount=0;return image;}
    }
}
