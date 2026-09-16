using System;
using System.IO;
using System.Linq;
using BarrelRivals.Core;
using BarrelRivals.Foundation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BarrelRivals.Editor
{
    /// <summary>Owns generated M0 assets only. Keep hand-authored production assets elsewhere.</summary>
    public static class FoundationBuilder
    {
        public const string Root = "Assets/_Project/Generated/Foundation";
        public const string ScenePath = Root + "/Arena_Foundation.unity";
        public const string PipelinePath = Root + "/MobilePipeline.asset";

        public static void Generate()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            ConfigurePipeline();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f,0.60f,0.66f);
            RenderSettings.fog = false;
            Material dirt = Material("Dirt",new Color(0.49f,0.31f,0.16f));
            Material wood = Material("Fence",new Color(0.24f,0.12f,0.055f));
            Material cream = Material("Cream",new Color(0.95f,0.87f,0.65f));
            Material red = Material("BarrelRed",new Color(0.72f,0.055f,0.035f));
            Material dark = Material("Dark",new Color(0.06f,0.07f,0.08f));
            Material chestnut = Material("HorseChestnut",new Color(0.29f,0.11f,0.055f));
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.4f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42,-28,0); RenderSettings.sun = sun;
            Primitive("Arena dirt",PrimitiveType.Cube,new Vector3(0,-0.2f,24),new Vector3(60,0.4f,84),dirt);
            for (int i = 0; i < 3; i++)
            {
                var point = StandardCourse.Barrel(i);
                var barrel = new GameObject("Barrel " + (i+1));
                barrel.transform.position = new Vector3((float)point.X,0,(float)point.Z);
                Primitive("Drum",PrimitiveType.Cylinder,new Vector3(0,0.44f,0),new Vector3(0.60f,0.44f,0.60f),red,barrel.transform);
                Primitive("Top band",PrimitiveType.Cylinder,new Vector3(0,0.67f,0),new Vector3(0.61f,0.05f,0.61f),cream,barrel.transform);
                Primitive("Bottom band",PrimitiveType.Cylinder,new Vector3(0,0.21f,0),new Vector3(0.61f,0.05f,0.61f),cream,barrel.transform);
                PrefabUtility.SaveAsPrefabAssetAndConnect(barrel,Root+"/Barrel_"+(i+1)+".prefab",InteractionMode.AutomatedAction);
            }
            Fence(new Vector3(-28,0,-16),new Vector3(-28,0,64),wood);
            Fence(new Vector3(28,0,-16),new Vector3(28,0,64),wood);
            Fence(new Vector3(-28,0,64),new Vector3(28,0,64),wood);
            Fence(new Vector3(-28,0,-16),new Vector3(-6,0,-16),wood);
            Fence(new Vector3(6,0,-16),new Vector3(28,0,-16),wood);
            Primitive("Score line",PrimitiveType.Cube,new Vector3(0,0.012f,0),new Vector3(12,0.02f,0.15f),cream);
            Primitive("Gate left",PrimitiveType.Cube,new Vector3(-6,1.5f,0),new Vector3(0.25f,3,0.25f),wood);
            Primitive("Gate right",PrimitiveType.Cube,new Vector3(6,1.5f,0),new Vector3(0.25f,3,0.25f),wood);

            var horse = new GameObject("Horse proxy"); horse.transform.position = new Vector3(0,0,-10);
            Primitive("Body",PrimitiveType.Sphere,new Vector3(0,1.1f,0),new Vector3(0.8f,0.85f,1.8f),chestnut,horse.transform);
            Primitive("Neck",PrimitiveType.Capsule,new Vector3(0,1.5f,0.6f),new Vector3(0.40f,0.6f,0.4f),chestnut,horse.transform).transform.localRotation = Quaternion.Euler(-28,0,0);
            Primitive("Head",PrimitiveType.Sphere,new Vector3(0,1.9f,0.97f),new Vector3(0.40f,0.45f,0.78f),chestnut,horse.transform);
            Primitive("Muzzle",PrimitiveType.Sphere,new Vector3(0,1.8f,1.24f),new Vector3(0.36f,0.29f,0.30f),dark,horse.transform);
            for (int x = -1; x <= 1; x += 2)
            {
                Primitive("Ear",PrimitiveType.Capsule,new Vector3(x*0.14f,2.2f,0.80f),new Vector3(0.09f,0.17f,0.10f),chestnut,horse.transform);
                for (int z = -1; z <= 1; z += 2)
                    Primitive("Leg",PrimitiveType.Capsule,new Vector3(x*0.27f,0.48f,z*0.55f),new Vector3(0.14f,0.48f,0.15f),chestnut,horse.transform);
            }
            Primitive("Saddle",PrimitiveType.Cube,new Vector3(0,1.53f,-0.12f),new Vector3(0.78f,0.12f,0.62f),dark,horse.transform);
            PrefabUtility.SaveAsPrefabAssetAndConnect(horse,Root+"/HorseProxy.prefab",InteractionMode.AutomatedAction);

            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(36,31,-29); camera.transform.LookAt(new Vector3(0,0,20));
            camera.fieldOfView = 55; camera.nearClipPlane = 0.05f; camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.47f,0.65f,0.76f);
            camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var arrival = new GameObject("Preview destination").transform;
            var first = StandardCourse.Barrel(0); arrival.position = new Vector3((float)first.X,0,(float)first.Z-4);

            var canvas = new GameObject("Practice HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 0.5f;
            var safe = new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvas.transform,false); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            Label(safe,"Title","BARREL RIVALS",new Vector2(0,1),new Vector2(28,-24),new Vector2(600,52),32,TextAnchor.MiddleLeft);
            Text status = Label(safe,"Status","Explore the practice arena",new Vector2(0,1),new Vector2(30,-80),new Vector2(700,36),22,TextAnchor.MiddleLeft);
            Label(safe,"Mode","COURSE PREVIEW",new Vector2(1,1),new Vector2(-28,-24),new Vector2(320,42),20,TextAnchor.MiddleRight);
            Button preview = Button(safe,"Preview ride",new Vector2(-142,36));
            Button reset = Button(safe,"Reset view",new Vector2(142,36));
            new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var controller = new GameObject("Foundation preview").AddComponent<FoundationPreview>();
            controller.Configure(horse.transform,camera,arrival,status,preview,reset,safe);

            PlayerSettings.companyName = "Barrel Rivals"; PlayerSettings.productName = "Barrel Rivals";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.barrelrivals.foundation");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.barrelrivals.foundation");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Could not save foundation scene.");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            EditorSceneManager.OpenScene(ScenePath); Validate();
            Debug.Log("BARREL_M0: foundation scene generated, reopened and validated.");
        }

        private static void ConfigurePipeline()
        {
            string dataPath = Root+"/MobileRenderer.asset";
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(dataPath);
            if (!data) { data = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(data,dataPath); }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (!pipeline) { pipeline = UniversalRenderPipelineAsset.Create(data); AssetDatabase.CreateAsset(pipeline,PipelinePath); }
            pipeline.msaaSampleCount = 2; pipeline.supportsHDR = false; pipeline.shadowDistance = 80; pipeline.renderScale = 1;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int initial = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i,false); QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(initial,false); EditorUtility.SetDirty(pipeline);
        }
        private static Material Material(string name,Color color)
        {
            string path = Root+"/"+name+".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader = shader; material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",0.12f);
            EditorUtility.SetDirty(material); return material;
        }
        private static GameObject Primitive(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private static void Fence(Vector3 start,Vector3 end,Material material)
        {
            float length = Vector3.Distance(start,end); int count = Mathf.CeilToInt(length/4);
            for (int i = 0; i <= count; i++)
                Primitive("Fence post",PrimitiveType.Cube,Vector3.Lerp(start,end,i/(float)count)+Vector3.up*0.8f,new Vector3(0.18f,1.6f,0.18f),material);
            for (int i = 0; i < 2; i++)
                Primitive("Fence rail",PrimitiveType.Cube,(start+end)/2+Vector3.up*(0.65f+i*0.6f),new Vector3(0.12f,0.12f,length),material).transform.rotation = Quaternion.LookRotation(end-start);
        }
        private static Text Label(Transform parent,string name,string content,Vector2 anchor,Vector2 offset,Vector2 size,int fontSize,TextAnchor alignment)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.color = new Color(1,0.96f,0.86f); text.text = content; text.alignment = alignment;
            text.raycastTarget = false;
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor;
            rect.anchoredPosition = offset; rect.sizeDelta = size; return text;
        }
        private static Button Button(Transform parent,string name,Vector2 position)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false);
            go.GetComponent<Image>().color = new Color(0.12f,0.16f,0.18f,0.96f);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0);
            rect.pivot = new Vector2(0.5f,0); rect.sizeDelta = new Vector2(260,62); rect.anchoredPosition = position;
            Label(go.transform,"Label",name,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(250,56),23,TextAnchor.MiddleCenter);
            return go.GetComponent<Button>();
        }
        public static void Validate()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            var preview = UnityEngine.Object.FindFirstObjectByType<FoundationPreview>();
            if (!preview || !preview.HasBindings) throw new InvalidOperationException("Preview bindings are missing.");
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                        throw new InvalidOperationException("Missing script on "+t.name);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
                        throw new InvalidOperationException("Missing or unsaved material on "+renderer.name);
            if (!GraphicsSettings.defaultRenderPipeline || !QualitySettings.renderPipeline)
                throw new InvalidOperationException("An active render pipeline is missing.");
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath))
                throw new InvalidOperationException("Foundation scene is absent from the build.");
            Debug.Log("BARREL_M0: scene references and build registration PASS.");
        }
        public static void BuildAndroid()
        {
            Validate(); Directory.CreateDirectory("Builds/Android"); EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.useCustomKeystore = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Android/BarrelRivals-Foundation.apk",
                target = BuildTarget.Android, options = BuildOptions.Development
            });
            Debug.Log($"BARREL_M0: Android build {report.summary.result}; errors={report.summary.totalErrors}.");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Android build failed.");
        }
        public static void RenderOverview()
        {
            Validate(); var camera = Camera.main;
            var target = new RenderTexture(1280,720,24); var previous = RenderTexture.active;
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                Directory.CreateDirectory("Evidence"); File.WriteAllBytes("Evidence/Arena-Overview.png",image.EncodeToPNG());
                Debug.Log("BARREL_M0: saved Evidence/Arena-Overview.png.");
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
