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
        private static readonly Color Ink=new Color(.055f,.060f,.057f,.84f);
        private static readonly Color Gold=new Color(.83f,.69f,.43f,1);
        private static readonly Color Paper=new Color(.96f,.93f,.86f,1);
        private const string FontRoot="Assets/_Project/Art/Reins/Premium/Fonts/";
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
            PracticePresentationBuilder.Build(horse,barrels[0],camera,includeAlleyRails:false, includeCrowd:false);
            ReinsReferenceArtBuilder.Apply(horse,camera);
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
            ReinsPremiumArenaBuilder.Apply(camera,horse,barrels,patches);
            ReinsRiderTackBuilder.Build(horse);
            var canvas=new GameObject("Reins HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
            // Dark, outlined cards keep the foreground visible while full-size invisible pads retain thumb reach.
            var header=Panel(safe,"Course status",new Vector2(0,1),new Vector2(20,-14),new Vector2(430,78),Ink);
            var brand=Panel(header,"Barrel Rivals mark",new Vector2(0,1),new Vector2(14,-9),new Vector2(34,31),new Color(.15f,.125f,.080f,.64f));
            var initials=Label(brand,"BR mark","BR",new Vector2(.5f,.5f),Vector2.zero,new Vector2(31,26),16,TextAnchor.MiddleCenter);
            initials.font=HudFont("Cinzel-SemiBold.ttf");initials.color=Gold;
            var title=Label(header,"Title","REINS RACING · PRACTICE",new Vector2(0,1),new Vector2(59,-5),new Vector2(357,35),26);
            title.font=HudFont("Cinzel-SemiBold.ttf");title.color=Paper;
            title.resizeTextForBestFit=true;title.resizeTextMinSize=18;title.resizeTextMaxSize=26;
            var hint=Label(header,"Instruction","",new Vector2(0,1),new Vector2(16,-41),new Vector2(400,31),14);
            hint.resizeTextForBestFit=true;hint.resizeTextMinSize=12;hint.resizeTextMaxSize=14;hint.lineSpacing=1.02f;
            var stats=Panel(safe,"Race stats",new Vector2(1,1),new Vector2(-20,-14),new Vector2(300,78),Ink);
            var status=Label(stats,"Timer","0.00s  ·  +0s   |   0 km/h",new Vector2(.5f,1),new Vector2(0,-8),new Vector2(274,35),22,TextAnchor.MiddleCenter);
            status.font=HudFont("Lato-Bold.ttf");status.resizeTextForBestFit=true;status.resizeTextMinSize=17;status.resizeTextMaxSize=22;
            Label(stats,"Stat captions","TIME          PENALTY          SPEED",new Vector2(.5f,0),new Vector2(0,13),new Vector2(273,18),11,TextAnchor.MiddleCenter).color=new Color(.78f,.77f,.71f,1);
            var left=ReinPad(safe,"Left rein","Left label",controller,ReinsPad.Left,new Vector2(0,0),new Vector2(20,54),out var lf);
            var right=ReinPad(safe,"Right rein","Right label",controller,ReinsPad.Right,new Vector2(1,0),new Vector2(-20,54),out var rf);
            var rhythm=Panel(safe,"Rhythm and wrap",new Vector2(.5f,0),new Vector2(0,47),new Vector2(358,150),Color.clear,false);
            rhythm.GetComponent<Image>().raycastTarget=true;rhythm.gameObject.AddComponent<ReinsInputSurface>().Configure(controller,ReinsPad.Rhythm);
            var rhythmCard=Panel(rhythm,"Rhythm card",new Vector2(.5f,0),new Vector2(0,10),new Vector2(312,86),Ink);
            var action=Label(rhythmCard,"Action","TAP THE BEAT",new Vector2(.5f,1),new Vector2(0,-8),new Vector2(286,47),16,TextAnchor.MiddleCenter);
            action.font=HudFont("Lato-Bold.ttf");action.lineSpacing=1.1f;
            var beat=Fill(rhythmCard,"Beat",new Vector2(.5f,0),new Vector2(0,13),new Vector2(280,7));
            var feedback=Label(safe,"Feedback","",new Vector2(.5f,0),new Vector2(0,160),new Vector2(620,40),17,TextAnchor.MiddleCenter);
            feedback.color=Gold;var feedbackShadow=feedback.gameObject.AddComponent<Shadow>();feedbackShadow.effectColor=new Color(0,0,0,.8f);feedbackShadow.effectDistance=new Vector2(0,-1.5f);
            var mapPanel=Panel(safe,"Course map",new Vector2(1,1),new Vector2(-20,-104),new Vector2(132,124),new Color(.045f,.053f,.050f,.70f));
            mapPanel.GetComponent<ReinsHudPanel>().ShowGrid=true;
            var mapObject=new GameObject("Route",typeof(RectTransform),typeof(ReinsMapGraphic));var mapRect=mapObject.GetComponent<RectTransform>();mapRect.SetParent(mapPanel,false);mapRect.anchorMin=Vector2.zero;mapRect.anchorMax=Vector2.one;mapRect.offsetMin=new Vector2(8,8);mapRect.offsetMax=new Vector2(-8,-8);var map=mapObject.GetComponent<ReinsMapGraphic>();map.raycastTarget=false;
            var start=Button(safe,"Begin","BEGIN RUN",new Vector2(.5f,.5f),new Vector2(0,15),new Vector2(250,58),22,out var startText);
            startText.font=HudFont("Cinzel-SemiBold.ttf");
            var resultPanel=Panel(safe,"Run result",new Vector2(.5f,.5f),new Vector2(0,30),new Vector2(680,326),new Color(.035f,.041f,.038f,.97f));var resultGroup=resultPanel.gameObject.AddComponent<CanvasGroup>();
            var result=Label(resultPanel,"Result","",new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(630,280),24,TextAnchor.MiddleCenter);
            var retry=Button(safe,"Retry","RETRY SAME",new Vector2(0,1),new Vector2(20,-102),new Vector2(126,38),13,out _);
            var surface=Button(safe,"Surface","DIRT: HARD PACK",new Vector2(0,1),new Vector2(154,-102),new Vector2(156,38),13,out var surfaceText);
            var ghost=Button(safe,"Own ghost","OWN BEST: —",new Vector2(0,1),new Vector2(318,-102),new Vector2(132,38),13,out var ghostText);
            var sound=Button(safe,"Sound","SOUND ON",new Vector2(0,0),new Vector2(20,10),new Vector2(144,36),14,out var soundText);
            var cameraMode=Button(safe,"Camera","BODYCAM",new Vector2(0,0),new Vector2(174,10),new Vector2(144,36),14,out var cameraText);
            // Preserve the v1 navigation for this graphics checkpoint; startup changes ship with v2.
            var classic=Button(safe,"Classic practice","CLASSIC PRACTICE",new Vector2(1,0),new Vector2(-20,10),new Vector2(220,36),14,out _);
            Label(safe,"Practice status","FREE PRACTICE · LOCAL RECORDINGS",new Vector2(.5f,0),new Vector2(0,14),new Vector2(430,26),12,TextAnchor.MiddleCenter).color=new Color(.88f,.85f,.76f,.85f);
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
                var ring=new GameObject("Barrel "+(index+1)+" zone "+band).AddComponent<LineRenderer>();ring.sharedMaterial=material;ring.enabled=false;ring.useWorldSpace=true;ring.loop=true;ring.widthMultiplier=.045f;ring.positionCount=96;ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
                var center=StandardCourse.Barrel(index);for(int p=0;p<96;p++){float angle=p*Mathf.PI*2/96;ring.SetPosition(p,new Vector3((float)center.X+Mathf.Cos(angle)*radii[band],.04f,(float)center.Z+Mathf.Sin(angle)*radii[band]));}
            }
        }
        private static RectTransform ReinPad(Transform parent,string name,string labelName,ReinsLabController controller,ReinsPad pad,Vector2 anchor,Vector2 position,out Image tension)
        {
            var hit=Panel(parent,name,anchor,position,new Vector2(280,258),Color.clear,false);
            hit.GetComponent<Image>().raycastTarget=true;hit.gameObject.AddComponent<ReinsInputSurface>().Configure(controller,pad);
            var card=Panel(hit,"Rein card",new Vector2(anchor.x,0),new Vector2(0,8),new Vector2(184,118),new Color(.050f,.055f,.048f,.68f));
            var label=Label(card,labelName,pad==ReinsPad.Left?"LEFT REIN\nDrag down to pull":"RIGHT REIN\nDrag down to pull",new Vector2(.5f,1),new Vector2(0,-9),new Vector2(164,42),16,TextAnchor.MiddleCenter);
            label.lineSpacing=1.08f;
            var glyphObject=new GameObject("Pull gesture",typeof(RectTransform),typeof(ReinsPullGlyph));var glyph=glyphObject.GetComponent<ReinsPullGlyph>();
            var rect=glyphObject.GetComponent<RectTransform>();rect.SetParent(card,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,-15);rect.sizeDelta=new Vector2(72,42);glyph.color=Gold;glyph.raycastTarget=false;
            tension=Fill(card,pad==ReinsPad.Left?"Left tension":"Right tension",new Vector2(.5f,0),new Vector2(0,11),new Vector2(156,5));
            tension.color=new Color(.12f,.83f,.83f,1);
            return hit;
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color,bool framed=true)
        {
            var go=new GameObject(name,typeof(RectTransform),framed?typeof(ReinsHudPanel):typeof(Image));var r=go.GetComponent<RectTransform>();
            r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
            if(framed)go.GetComponent<ReinsHudPanel>().Configure(new Color(Gold.r,Gold.g,Gold.b,.66f),7,1);
            return r;
        }
        private static Text Label(Transform parent,string name,string content,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;
            var text=go.GetComponent<Text>();text.font=HudFont("Lato-Regular.ttf");text.fontSize=fontSize;text.color=Paper;text.text=content;text.alignment=alignment;text.raycastTarget=false;
            return text;
        }
        private static Font HudFont(string file)
            =>AssetDatabase.LoadAssetAtPath<Font>(FontRoot+file)??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static Button Button(Transform parent,string name,string content,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,out Text text)
        {
            var r=Panel(parent,name,anchor,pos,size,new Color(.06f,.065f,.057f,.88f));r.GetComponent<Image>().raycastTarget=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.1f,1.04f);colors.pressedColor=new Color(.8f,.76f,.62f);colors.disabledColor=new Color(.64f,.64f,.61f,.65f);colors.fadeDuration=.08f;button.colors=colors;
            text=Label(r,"Label",content,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,2),fontSize,TextAnchor.MiddleCenter);
            return button;
        }
        internal static Sprite MeterSprite()
        {
            const string path="Assets/_Project/Generated/Practice/TimingFillSprite.asset";
            var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(sprite)return sprite;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.name="Meter white";texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();AssetDatabase.CreateAsset(texture,path);
            sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);sprite.name="Meter white sprite";AssetDatabase.AddObjectToAsset(sprite,texture);return sprite;
        }
        private static Image Fill(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
        {var track=Panel(parent,name+" track",anchor,pos,size,new Color(.12f,.25f,.25f,.85f),false);var r=Panel(track,name,Vector2.zero,Vector2.zero,size,new Color(.35f,.9f,.77f),false);var image=r.GetComponent<Image>();image.sprite=MeterSprite();image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillAmount=0;return image;}
    }
}
