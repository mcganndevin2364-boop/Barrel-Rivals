using System;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static class StableBuilder
    {
        public const string ScenePath="Assets/_Project/Generated/ReinsLab/MyStable.unity";
        private static readonly Color Ink=new Color(.033f,.045f,.040f,.94f),Gold=new Color(.81f,.66f,.41f),Paper=new Color(.94f,.91f,.83f);
        [MenuItem("Barrel Rivals/Generate MyStable and Gear")]
        public static void Generate()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
            var raceHorse=GameObject.Find("Horse proxy");
            if(!raceHorse.GetComponent<StableAppearance>())throw new InvalidOperationException("Generate Reins before MyStable.");
            var copy=Object.Instantiate(raceHorse);copy.name="Copper";copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            // Keep only the shared rigged horse/tack and its material bindings. No race behaviours run in the showroom.
            foreach(var c in copy.GetComponents<Component>())if(!(c is Transform) && !(c is StableAppearance))Object.DestroyImmediate(c);
            for(int i=copy.transform.childCount-1;i>=0;i--)if(copy.transform.GetChild(i).name!="Reference horse")Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);
            foreach(var c in copy.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            StableTackBuilder.MakeStablePose(copy.transform);
            var prefab=PrefabUtility.SaveAsPrefabAsset(copy,StableTackBuilder.Root+"/Stable horse.prefab");
            Object.DestroyImmediate(copy);
            var sky=RenderSettings.skybox;
            var grade=Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).OrderByDescending(v=>v.priority).First().sharedProfile;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var horse=(GameObject)PrefabUtility.InstantiatePrefab(prefab);horse.name="Copper";horse.transform.localRotation=Quaternion.Euler(0,-22,0);
            var camera=new GameObject("Stable camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(4.8f,2.8f,6.1f);camera.transform.LookAt(new Vector3(0,1.14f,.25f));camera.fieldOfView=38;camera.nearClipPlane=.08f;camera.farClipPlane=240;camera.allowHDR=true;
            var urp=camera.GetUniversalAdditionalCameraData();urp.renderPostProcessing=true;urp.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            var volume=new GameObject("Stable color grade").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=grade;
            RenderSettings.skybox=sky;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.64f,.60f,.53f);RenderSettings.ambientEquatorColor=new Color(.54f,.43f,.32f);RenderSettings.ambientGroundColor=new Color(.30f,.24f,.17f);
            var sun=new GameObject("Stable evening sunlight").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.83f,.64f);sun.intensity=1.8f;sun.shadows=LightShadows.Soft;sun.shadowBias=.035f;sun.shadowNormalBias=.16f;sun.transform.rotation=Quaternion.Euler(34,-38,0);RenderSettings.sun=sun;
            var fill=new GameObject("Stable soft lantern fill").AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(1,.84f,.68f);fill.intensity=1.8f;fill.range=9;fill.shadows=LightShadows.None;fill.transform.position=new Vector3(1.4f,3,2.8f);
            Barn();
            new GameObject("Stable event system",typeof(EventSystem),typeof(InputSystemUIInputModule));
            BuildUI(horse.transform);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(PracticeBuilder.ScenePath,true),new EditorBuildSettingsScene(ReinsLabBuilder.ScenePath,true),new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(FoundationBuilder.ScenePath,true)};
            EditorSceneManager.OpenScene(ScenePath);
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Stable missing script: "+t.name);
            Debug.Log("BARREL_STABLE: saved and reopened MyStable / Gear with shared horse and original western tack.");
        }
        private static void Barn()
        {
            var root=new GameObject("Original timber stable").transform;
            var wood=AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Weathered wood.mat");
            var steel=AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Worn painted steel.mat");
            var soil=ReinsPremiumArenaBuilder.Pbr("Stable packed earth","ArenaSoil_Albedo_2K.png","ArenaSoil_NormalGL_2K.png","ArenaSoil_Roughness_1K.jpg",null,new Color(.63f,.51f,.38f),0);
            soil.SetFloat("_Smoothness",.05f);soil.SetFloat("_BumpScale",.25f);soil.mainTextureScale=new Vector2(30,30);EditorUtility.SetDirty(soil);
            var dark=StableTackBuilder.Solid("Stable stall rubber",new Color(.046f,.043f,.034f),.05f);
            var brass=StableTackBuilder.Solid("Stable brass",new Color(.46f,.31f,.12f),.42f,.65f);
            Part(root,"Packed stable floor",new Vector3(0,-.12f,0),new Vector3(180,.16f,180),soil);
            Part(root,"Rubber grooming mat",new Vector3(0,-.025f,.1f),new Vector3(3.3f,.025f,4.5f),dark);
            for(int i=-38;i<=38;i++) {
                float x=i*.245f;if(Mathf.Abs(x)<1.55f)continue;
                Part(root,"Barn back weatherboards",new Vector3(x,1.9f,-7.2f),new Vector3(.24f,3.8f,.12f),wood);
            }
            for(int side=-1;side<=1;side+=2) {
                for(int n=0;n<3;n++) {
                    float x=side*(3.2f+n*2.2f);Part(root,"Stall timber post",new Vector3(x,1.65f,-3.2f),new Vector3(.18f,3.3f,.22f),wood);
                    Part(root,"Brass post cap",new Vector3(x,3.32f,-3.2f),new Vector3(.23f,.07f,.25f),brass);
                }
                for(int n=0;n<17;n++)Part(root,"Rear stall planks",new Vector3(side*(3.35f+n*.235f),.76f,-3.3f),new Vector3(.22f,1.5f,.095f),wood);
                for(int n=0;n<25;n++)Part(root,"Upper stall bars",new Vector3(side*(3.35f+n*.157f),2.05f,-3.3f),new Vector3(.023f,1.0f,.023f),steel);
                for(int n=0;n<4;n++)Part(root,"Stall horizontal rail",new Vector3(side*5.25f,1.5f+n*.48f,-3.3f),new Vector3(4.35f,.07f,.07f),steel);
                Part(root,"Aisle side beam",new Vector3(side*3.15f,3.6f,0),new Vector3(.23f,.32f,13),wood);
                for(int n=0;n<4;n++)Part(root,"Aisle post",new Vector3(side*3.15f,1.7f,-5.0f+n*3.5f),new Vector3(.24f,3.4f,.24f),wood);
            }
            for(int n=0;n<7;n++) {
                Part(root,"Overhead timber truss",new Vector3(0,3.75f,-7+n*2.2f),new Vector3(15,.24f,.23f),wood);
                var beam=Part(root,"Diagonal truss left",new Vector3(-1.7f,4.15f,-7+n*2.2f),new Vector3(3.8f,.16f,.16f),wood);beam.rotation=Quaternion.Euler(0,0,14);
                beam=Part(root,"Diagonal truss right",new Vector3(1.7f,4.15f,-7+n*2.2f),new Vector3(3.8f,.16f,.16f),wood);beam.rotation=Quaternion.Euler(0,0,-14);
            }
            // An open central aisle allows dusk light and a view through the barn rather than a flat backdrop.
            Part(root,"Back aisle threshold",new Vector3(0,3.4f,-7),new Vector3(6.1f,.3f,.2f),wood);
            for(int n=0;n<6;n++)Part(root,"Distant paddock fence",new Vector3(-6+n*2.4f,1,-12),new Vector3(.15f,2,.15f),wood);
            for(int n=0;n<3;n++)Part(root,"Distant paddock rail",new Vector3(0,.45f+n*.5f,-12),new Vector3(15,.09f,.09f),wood);
            var warm=StableTackBuilder.Solid("Lantern warm glass",new Color(.95f,.62f,.26f),.2f);warm.EnableKeyword("_EMISSION");warm.SetColor("_EmissionColor",new Color(1,.58f,.2f)*2);EditorUtility.SetDirty(warm);
            for(int side=-1;side<=1;side+=2) {
                Part(root,"Lantern wall mount",new Vector3(side*3.0f,2.72f,-3.05f),new Vector3(.28f,.34f,.24f),dark);
                Part(root,"Lantern glass",new Vector3(side*3.0f,2.73f,-2.91f),new Vector3(.18f,.22f,.06f),warm);
            }
        }
        private static Transform Part(Transform root,string name,Vector3 p,Vector3 scale,Material m)
        {if(!m)throw new InvalidOperationException("Missing stable material for "+name);var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;go.isStatic=true;return go.transform;}
        private static void BuildUI(Transform horse)
        {
            var owner=new GameObject("MyStable controller").AddComponent<StableController>();
            var go=new GameObject("Stable HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var safe=Group(go.transform,"Safe area");
            var top=Panel(safe,"Stable header",new Vector2(0,1),Vector2.zero,new Vector2(1280,96),Ink);
            StretchBar(top);
            Label(top,"Brand","BARREL RIVALS",new Vector2(0,1),new Vector2(26,-12),new Vector2(340,42),28,true);
            Label(top,"Location","COPPER CREEK  /  YOUR STABLE",new Vector2(0,1),new Vector2(28,-54),new Vector2(360,24),12).color=Gold;
            var stable=Button(top,"MyStable tab","MY STABLE",new Vector2(.5f,1),new Vector2(-80,-22),new Vector2(162,51),18,out _);UnityEventTools.AddPersistentListener(stable.onClick,owner.ShowStable);
            var gear=Button(top,"Gear tab","GEAR",new Vector2(.5f,1),new Vector2(94,-22),new Vector2(150,51),18,out _);UnityEventTools.AddPersistentListener(gear.onClick,owner.ShowGear);
            var highlights=new[]{Panel(stable.transform,"Active stable",new Vector2(.5f,0),Vector2.zero,new Vector2(142,3),Gold).GetComponent<Image>(),Panel(gear.transform,"Active gear",new Vector2(.5f,0),Vector2.zero,new Vector2(130,3),Gold).GetComponent<Image>()};
            Label(top,"Collection label","STARTER COLLECTION",new Vector2(1,1),new Vector2(-26,-29),new Vector2(240,35),13, false,TextAnchor.MiddleRight).color=Gold;
            var stablePanel=Group(safe,"MyStable content");
            var card=Panel(stablePanel,"Horse card",new Vector2(0,1),new Vector2(26,-128),new Vector2(282,392),Ink);
            Label(card,"Horse eyebrow","YOUR HORSE  /  01",new Vector2(0,1),new Vector2(20,-20),new Vector2(242,24),12).color=Gold;
            Label(card,"Horse name","COPPER",new Vector2(0,1),new Vector2(20,-54),new Vector2(242,48),36,true);
            Label(card,"Horse detail","BAY  ·  STARTER HORSE",new Vector2(0,1),new Vector2(20,-112),new Vector2(242,28),13).color=Gold;
            Label(card,"Horse copy","Your first partner in the arena.\n\nOutfit Copper with your choice of western leather, a woven pad and braided reins.\n\nYour equipped look carries into every practice run.",new Vector2(0,1),new Vector2(20,-159),new Vector2(242,177),17);
            var tack=Button(card,"Customize Copper","CUSTOMIZE GEAR",new Vector2(.5f,0),new Vector2(0,18),new Vector2(242,43),15,out _);UnityEventTools.AddPersistentListener(tack.onClick,owner.ShowGear);
            var gearPanel=Group(safe,"Gear content");
            var details=Panel(gearPanel,"Selected gear",new Vector2(0,1),new Vector2(26,-128),new Vector2(282,376),Ink);
            Label(details,"Gear eyebrow","EQUIPMENT  /  INSPECT",new Vector2(0,1),new Vector2(20,-20),new Vector2(242,25),12).color=Gold;
            var title=Label(details,"Gear title","RANCH LEATHER",new Vector2(0,1),new Vector2(20,-56),new Vector2(242,76),27,true);
            var detail=Label(details,"Gear description","",new Vector2(0,1),new Vector2(20,-146),new Vector2(242,158),17);
            var equip=Button(details,"Equip selected","EQUIP GEAR",new Vector2(.5f,0),new Vector2(0,18),new Vector2(242,45),16,out var equipLabel);
            var collection=Panel(gearPanel,"Owned gear",new Vector2(1,1),new Vector2(-26,-128),new Vector2(308,406),Ink);
            var buttons=new Button[StableCatalog.Gear.Count];var labels=new Text[buttons.Length];
            for(int slot=0;slot<3;slot++) {
                Label(collection,"Slot "+slot,((StableSlot)slot).ToString().ToUpperInvariant(),new Vector2(0,1),new Vector2(16,-14-slot*129),new Vector2(276,25),12).color=Gold;
                for(int item=0;item<2;item++) {int index=slot*2+item;buttons[index]=Button(collection,"Select "+StableCatalog.Gear[index].Id,StableCatalog.Gear[index].Name,new Vector2(0,1),new Vector2(16+item*144,-45-slot*129),new Vector2(132,70),15,out labels[index]);}
            }
            var orbit=Panel(safe,"Drag horse to rotate",new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(580,432),Color.clear);orbit.GetComponent<Image>().raycastTarget=true;orbit.gameObject.AddComponent<StableOrbit>().Configure(owner);
            Label(safe,"Orbit instruction","DRAG TO ROTATE",new Vector2(.5f,0),new Vector2(0,132),new Vector2(280,25),12,false,TextAnchor.MiddleCenter).color=Gold;
            var reset=Button(safe,"Reset view","RESET VIEW",new Vector2(.5f,0),new Vector2(0,93),new Vector2(150,32),12,out _);UnityEventTools.AddPersistentListener(reset.onClick,owner.ResetView);
            var footer=Panel(safe,"Stable footer",new Vector2(0,0),Vector2.zero,new Vector2(1280,80),Ink);
            StretchBar(footer);
            var notice=Label(footer,"Save status","",new Vector2(0,.5f),new Vector2(28,0),new Vector2(755,53),15);
            var race=Button(footer,"Race","RIDE TO ARENA",new Vector2(1,.5f),new Vector2(-26,0),new Vector2(268,51),18,out _);UnityEventTools.AddPersistentListener(race.onClick,owner.Race);
            owner.Configure(horse,horse.GetComponent<StableAppearance>(),safe,stablePanel.gameObject,gearPanel.gameObject,new[]{title,detail,notice,equipLabel},equip,buttons,labels,highlights);
        }
        private static void StretchBar(RectTransform r)
        {r.anchorMin=new Vector2(0,r.anchorMin.y);r.anchorMax=new Vector2(1,r.anchorMax.y);r.pivot=new Vector2(.5f,r.pivot.y);r.anchoredPosition=new Vector2(0,r.anchoredPosition.y);r.sizeDelta=new Vector2(0,r.sizeDelta.y);}
        private static RectTransform Group(Transform parent,string name)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {var r=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;var image=r.GetComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
        private static Text Label(Transform parent,string name,string text,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,bool display=false,TextAnchor alignment=TextAnchor.UpperLeft)
        {var r=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;var label=r.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>(ReinsPremiumArenaBuilder.Root+"/Fonts/"+(display?"Cinzel-SemiBold.ttf":"Lato-Regular.ttf"));label.fontSize=fontSize;label.color=Paper;label.text=text;label.alignment=alignment;label.raycastTarget=false;return label;}
        private static Button Button(Transform parent,string name,string text,Vector2 anchor,Vector2 pos,Vector2 size,int fontSize,out Text label)
        {var r=Panel(parent,name,anchor,pos,size,new Color(.14f,.14f,.11f,.98f));r.GetComponent<Image>().raycastTarget=true;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();var colors=button.colors;colors.disabledColor=new Color(.57f,.57f,.52f,.8f);colors.pressedColor=new Color(.71f,.65f,.50f);button.colors=colors;label=Label(r,"Label",text,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,4),fontSize,false,TextAnchor.MiddleCenter);return button;}
    }
}
