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
            StableShowroomBuilder.Build(camera,horse.transform);
            new GameObject("Stable event system",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var riderPreview=StableWardrobeArtBuilder.BuildPreview();
            StableThumbnailBuilder.Generate(horse.transform,riderPreview);
            StableUiBuilder.Build(horse.transform,camera,riderPreview);
            Object.FindFirstObjectByType<StableController>().ConfigureRiderCamera(new Vector3(.7f,1.5f,4.7f),new Vector3(-1.35f,1.05f,1.5f));
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(PracticeBuilder.ScenePath,true),new EditorBuildSettingsScene(ReinsLabBuilder.ScenePath,true),new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(FoundationBuilder.ScenePath,true)};
            EditorSceneManager.OpenScene(ScenePath);
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Stable missing script: "+t.name);
            Debug.Log("BARREL_STABLE: saved and reopened MyStable / Gear with shared horse and original western tack.");
        }
    }
}
