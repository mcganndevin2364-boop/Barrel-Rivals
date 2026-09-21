using System;
using System.Collections;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class GloveTailoringRenderTests
    {
        private string profileDirectory;
        [SetUp] public void SetUp()
        {
            profileDirectory=Path.Combine(Path.GetTempPath(),"GloveTailoring-"+Guid.NewGuid().ToString("N"));
            StableSession.UseForTests(new StableProfileStore(profileDirectory));
        }
        [TearDown] public void TearDown()
        {
            StableSession.UseForTests(null);
            if(Directory.Exists(profileDirectory))Directory.Delete(profileDirectory,true);
        }
        [UnityTest] public IEnumerator SavedLeatherDyesChangeRenderedLeatherButNotTheThread()
        {
            yield return SceneManager.LoadSceneAsync(StableController.SceneName);yield return null;
            // A batch-mode player loop does not guarantee a screen render. Initialize
            // URP with the saved scene before asking a material for its active passes.
            Camera.main.Render();
            var controller=Object.FindFirstObjectByType<StableController>();controller.ShowRiderGear();
            var glove=GameObject.Find("Inspect glove shell").GetComponent<Renderer>();
            var appearance=glove.GetComponentInParent<StableAppearance>();var original=appearance.Applied.Copy();
            var stage=new GameObject("Temporary sewn leather render probe");stage.transform.position=new Vector3(400,40,400);
            var mesh=new Mesh();
            mesh.vertices=new[]{new Vector3(-.9f,-.4f,0),new Vector3(-.1f,-.4f,0),new Vector3(-.9f,.4f,0),new Vector3(-.1f,.4f,0),
                new Vector3(.1f,-.4f,0),new Vector3(.9f,-.4f,0),new Vector3(.1f,.4f,0),new Vector3(.9f,.4f,0)};
            mesh.normals=Enumerable.Repeat(Vector3.back,8).ToArray();mesh.tangents=Enumerable.Repeat(new Vector4(1,0,0,-1),8).ToArray();
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one,Vector2.zero,Vector2.right,Vector2.up,Vector2.one};
            mesh.colors=Enumerable.Repeat(new Color(1,1,1,0),4).Concat(Enumerable.Repeat(Color.white,4)).ToArray();
            mesh.triangles=new[]{0,2,1,1,2,3,4,6,5,5,6,7};mesh.RecalculateBounds();
            var patch=new GameObject("Leather and thread",typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(stage.transform,false);patch.layer=31;
            patch.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=patch.GetComponent<MeshRenderer>();
            var camera=new GameObject("Probe camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.transform.localPosition=new Vector3(0,0,-2);camera.cullingMask=1<<31;
            camera.orthographic=true;camera.orthographicSize=.6f;camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.allowHDR=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var light=new GameObject("Probe light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.intensity=1.3f;light.cullingMask=1<<31;
            var target=new RenderTexture(256,128,24);var image=new Texture2D(256,128,TextureFormat.RGB24,false);var active=RenderTexture.active;
            string output=Environment.GetEnvironmentVariable("BARREL_GLOVE_TAILORING_OUTPUT");if(!string.IsNullOrEmpty(output))Directory.CreateDirectory(output);
            Color? thread=null,leather=null;float maxDye=0,maxThread=0;
            try
            {
                camera.targetTexture=target;
                foreach(var item in StableCatalog.Gear.Where(g=>g.Slot==StableSlot.Gloves))
                {
                    var profile=original.Copy();profile.TryEquip(StableSlot.Gloves,item.Id);appearance.Apply(profile);renderer.sharedMaterial=glove.sharedMaterial;
                    // Let Unity initialize the active render pipeline and upload the newly
                    // enabled inspection material before an explicit offscreen capture.
                    yield return null;
                    Assert.AreEqual(4,renderer.sharedMaterial.passCount,item.Id);
                    foreach(var pass in new[]{"SurfaceForward","ShadowCaster","DepthOnly","DepthNormals"})Assert.GreaterOrEqual(renderer.sharedMaterial.FindPass(pass),0,item.Id+"/"+pass);
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,128),0,0);image.Apply();
                    Color Average(int x){Color sum=Color.clear;for(int y=53;y<75;y++)for(int i=x;i<x+22;i++)sum+=image.GetPixel(i,y);return sum/(22*22);}
                    var l=Average(64);var t=Average(164);
                    Assert.Greater(t.maxColorComponent,.05f,"The forward shader must render visible thread, not a black or missing pass.");
                    Assert.IsFalse(t.r>.9f && t.b>.9f && t.g<.2f,"Reject error-magenta shader fallback.");
                    if(thread.HasValue) {maxDye=Mathf.Max(maxDye,Vector3.Distance(new Vector3(l.r,l.g,l.b),new Vector3(leather.Value.r,leather.Value.g,leather.Value.b)));
                        maxThread=Mathf.Max(maxThread,Vector3.Distance(new Vector3(t.r,t.g,t.b),new Vector3(thread.Value.r,thread.Value.g,thread.Value.b)));}
                    else{thread=t;leather=l;}
                    if(!string.IsNullOrEmpty(output))File.WriteAllBytes(Path.Combine(output,item.Id+"-material.png"),image.EncodeToPNG());
                }
                Assert.Greater(maxDye,.06f,"Saved dyes must change the actual rendered leather.");Assert.Less(maxThread,1f/255,"Thread must remain fixed across all six dyes.");
                if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,"material-proof.json"),JsonUtility.ToJson(new Proof{maximumLeatherDelta=maxDye,maximumThreadDelta=maxThread},true)+"\n");
            }
            finally{appearance.Apply(original);camera.targetTexture=null;RenderTexture.active=active;Object.Destroy(image);Object.Destroy(target);Object.Destroy(mesh);Object.Destroy(stage);}
            yield return null;
            if(!string.IsNullOrEmpty(output))
            {
                var canvas=GameObject.Find("Stable HUD").GetComponent<Canvas>();
                var rendered=OverlayEvidenceCapture.Render(Camera.main,canvas,1280,720,()=>controller.RefreshLayout(1280,720,new Rect(0,0,1280,720)));
                try{File.WriteAllBytes(Path.Combine(output,"rider-gear.png"),rendered.EncodeToPNG());}
                finally{Object.Destroy(rendered);controller.RefreshLayout();}
            }
            LogAssert.NoUnexpectedReceived();
        }
        [Serializable] class Proof {public int dyes=6;public float maximumLeatherDelta,maximumThreadDelta;public string scope="Linear Unity camera render probe using saved materials. Not phone qualification.";}
    }
}
