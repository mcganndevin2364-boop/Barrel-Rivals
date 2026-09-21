using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using BarrelRivals.Practice;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        static void ProbeCoat()
        {
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var enabled=lights.Select(l=>l.enabled).ToArray();
            var oldProbe=RenderSettings.ambientProbe;float reflection=RenderSettings.reflectionIntensity;
            var mesh=new Mesh();var quad=new GameObject("Coat direction probe",typeof(MeshFilter),typeof(MeshRenderer));
            var view=new GameObject("Coat probe view",typeof(Camera)).GetComponent<Camera>();
            var light=new GameObject("Coat probe light",typeof(Light)).GetComponent<Light>();
            var material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Bay coat.mat"));
            try
            {
                foreach(var l in lights)l.enabled=false;RenderSettings.ambientProbe=new SphericalHarmonicsL2();RenderSettings.reflectionIntensity=0;
                quad.transform.position=new Vector3(1000,1000,1000);
                mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
                mesh.triangles=new[]{0,1,2,0,2,3};mesh.normals=Enumerable.Repeat(Vector3.back,4).ToArray();mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
                mesh.colors=Enumerable.Repeat(new Color(.12f,.04f,.016f,0),4).ToArray();mesh.RecalculateBounds();
                quad.GetComponent<MeshFilter>().sharedMesh=mesh;quad.GetComponent<MeshRenderer>().sharedMaterial=material;
                material.SetFloat("_MicroNormal",0);material.SetFloat("_CoatVariation",0);
                light.type=LightType.Directional;light.color=Color.white;light.intensity=2;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(-15,-30,0);
                view.transform.position=quad.transform.position+Vector3.back*2;view.transform.rotation=Quaternion.identity;view.orthographic=true;view.orthographicSize=.5f;view.nearClipPlane=.1f;view.farClipPlane=3;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.black;
                Color32[] Render(string name,float sheen,Vector4 tangent)
                {
                    material.SetFloat("_CoatSheen",sheen);mesh.tangents=Enumerable.Repeat(tangent,4).ToArray();
                    string file=Path.Combine(Output,"probe-"+name+".png");Capture(view,file,128,128);
                    var image=new Texture2D(2,2);try{image.LoadImage(File.ReadAllBytes(file));return image.GetPixels32();}finally{Object.DestroyImmediate(image);}
                }
                float Difference(Color32[] a,Color32[] b)=>a.Select((c,i)=>(Mathf.Abs(c.r-b[i].r)+Mathf.Abs(c.g-b[i].g)+Mathf.Abs(c.b-b[i].b))/3f).Average();
                var onX=Render("flow-x",.65f,new Vector4(1,0,0,1));var onY=Render("flow-y",.65f,new Vector4(0,1,0,1));
                var offX=Render("off-x",0,new Vector4(1,0,0,1));var offY=Render("off-y",0,new Vector4(0,1,0,1));
                var result=new CoatProbe{directionDelta=Difference(onX,onY),disabledDelta=Difference(offX,offY),effectDelta=Difference(onX,offX)};
                File.WriteAllText(Path.Combine(Output,"coat-probe.json"),JsonUtility.ToJson(result,true)+"\n");
                Require(result.directionDelta>.5f && result.disabledDelta<.05f && result.effectDelta>.5f,"Coat highlight must follow actual tangent direction");
            }
            finally
            {
                for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=enabled[i];RenderSettings.ambientProbe=oldProbe;RenderSettings.reflectionIntensity=reflection;
                Object.DestroyImmediate(quad);Object.DestroyImmediate(mesh);Object.DestroyImmediate(material);Object.DestroyImmediate(view.gameObject);Object.DestroyImmediate(light.gameObject);
            }
        }
        [Serializable] class CoatProbe{public float directionDelta,disabledDelta,effectDelta;public string units="Mean RGB byte difference over128x128 rendered pixels";}

        public static void UpdateSavedPlayerHat()
        {
            Directory.CreateDirectory(Output);EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
            var rider=Object.FindFirstObjectByType<ReinsRiderBodyPresentation>();
            var skin=rider.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name.Contains("hat"));
            var candidate=AssetDatabase.LoadAssetAtPath<Mesh>(WesternHatBuilder.MeshPath).vertices;
            WesternHatBuilder.Apply(rider.GetComponentsInChildren<SkinnedMeshRenderer>(),rider.transform.parent);
            float error=candidate.Select((v,i)=>Vector3.Distance(v,skin.sharedMesh.vertices[i])).Max();
            Require(error<.0001f,"Candidate and production hat spaces must agree");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllText(Path.Combine(Output,"player-hat.json"),JsonUtility.ToJson(new PlayerHat{maximumCandidateDifference=error,triangles=skin.sharedMesh.triangles.Length/3},true)+"\n");
            // Save before these inspection-only poses and camera changes.
            var horse=GameObject.Find("Horse proxy").transform;var source=horse.GetComponent<ReinsHorsePresentation>();
            source.ResetFrame(new HorsePresentationFrame(0,horse.position,horse.rotation,0,0,0,0,false,false));
            horse.GetComponentInChildren<ReinsRiderTackPresentation>().RenderImmediate();rider.RenderImmediate();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            foreach(var surface in rider.GetComponentsInChildren<SkinnedMeshRenderer>())surface.shadowCastingMode=ShadowCastingMode.On;
            var camera=Camera.main;camera.transform.position=horse.position+new Vector3(3.8f,2.8f,4.5f);camera.transform.LookAt(horse.position+new Vector3(0,1.5f,0));camera.fieldOfView=39;
            Capture(camera,Path.Combine(Output,"player-arena.png"),1280,720);
            Debug.Log("PLAYER_HAT_UPDATED");
        }
        [Serializable] class PlayerHat{public float maximumCandidateDifference;public int triangles;public string scope="Saved racing-scene hat mesh/material only. No new native artifact or wardrobe slot.";}
    }
}
