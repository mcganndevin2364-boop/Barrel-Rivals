using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsFiberRenderTests
    {
        [UnityTest] public IEnumerator SavedHorseMaterialsMatchPrivateCopiesInActualLitScene()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var skin=GameObject.Find("Horse proxy").GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Horse strand hair");
            var original=skin.sharedMaterials;var copies=original.Select(m=>new Material(m)).ToArray();
            var camera=Camera.main;var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;float oldTime=Time.timeScale;
            var target=new RenderTexture(640,360,24);var picture=new Texture2D(640,360,TextureFormat.RGB24,false);
            try
            {
                Time.timeScale=0;camera.targetTexture=target;yield return null;
                Color32[] Read()
                {camera.Render();RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,640,360),0,0);picture.Apply();return picture.GetPixels32();}
                var saved=Read();skin.sharedMaterials=copies;var cloned=Read();
                double delta=0;int invalidRed=0;
                for(int i=0;i<saved.Length;i++)
                {
                    delta+=Math.Abs(saved[i].r-cloned[i].r)+Math.Abs(saved[i].g-cloned[i].g)+Math.Abs(saved[i].b-cloned[i].b);
                    // The authored mane is dark brown: an initial saturated-red shader
                    // artifact must not become a successful gameplay capture again.
                    if(saved[i].r>230 && saved[i].g<20 && saved[i].b<20)invalidRed++;
                }
                Assert.That(delta/(saved.Length*3),Is.LessThan(1),"Saved material assets must render like identical private copies.");
                Assert.That(invalidRed,Is.LessThan(50),"Unphysical flat-red material output in the actual horse view.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                skin.sharedMaterials=original;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;Time.timeScale=oldTime;
                foreach(var m in copies)Object.Destroy(m);Object.Destroy(target);Object.Destroy(picture);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator BothSavedHairLayersRenderDirectionalHighlightsAndPreserveTwoSidedCoverage()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var originals=GameObject.Find("Horse proxy").GetComponentsInChildren<SkinnedMeshRenderer>()
                .Single(r=>r.name=="Horse strand hair").sharedMaterials;
            var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            var enabled=lights.Select(l=>l.enabled).ToArray();var priorSun=RenderSettings.sun;
            var priorAmbientMode=RenderSettings.ambientMode;var priorAmbient=RenderSettings.ambientLight;
            bool priorFog=RenderSettings.fog;var priorActive=RenderTexture.active;
            var owner=new GameObject("Isolated fiber material render proof");
            var cameraObject=new GameObject("Fiber test camera");cameraObject.transform.SetParent(owner.transform);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=new Vector3(0,0,-2);camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;
            camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.allowHDR=false;camera.allowMSAA=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var lightObject=new GameObject("Fiber test sun");lightObject.transform.SetParent(owner.transform);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.color=Color.white;
            light.shadows=LightShadows.None;light.cullingMask=1<<29;light.transform.rotation=Quaternion.Euler(0,45,0);
            var quad=new GameObject("Atlas proof surface",typeof(MeshFilter),typeof(MeshRenderer));quad.layer=29;quad.transform.SetParent(owner.transform);
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.7f,-.8f,0),new Vector3(.7f,-.8f,0),new Vector3(.7f,.8f,0),new Vector3(-.7f,.8f,0)};
            mesh.normals=Enumerable.Repeat(Vector3.back,4).ToArray();mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            mesh.uv2=Enumerable.Repeat(new Vector2(0,.7f),4).ToArray();mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();
            quad.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=quad.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var picture=new Texture2D(256,256,TextureFormat.RGBA32,false);var copies=new List<Material>();var records=new List<LayerProof>();
            string output=Environment.GetEnvironmentVariable("BARREL_FIBER_STUDY_DIRECTORY");
            try
            {
                if(!string.IsNullOrEmpty(output))
                {Directory.CreateDirectory(output);Assert.IsEmpty(Directory.GetFiles(output,"*.png"),"Use a fresh shader-study folder.");}
                foreach(var item in lights)item.enabled=false;
                RenderSettings.sun=light;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.fog=false;
                yield return null;
                for(int layer=0;layer<originals.Length;layer++)
                {
                    Assert.AreEqual("Barrel Rivals/Horse Fiber",originals[layer].shader.name);
                    var material=new Material(originals[layer]);copies.Add(material);renderer.sharedMaterial=material;
                    // Remove diffuse/ambient, isolate one lobe: pixel changes must come
                    // from the supplied fiber direction, not moving lights or geometry.
                    material.SetColor("_BaseColor",Color.black);material.SetColor("_FiberTint",Color.white);
                    material.SetFloat("_PrimaryStrength",.4f);material.SetFloat("_SecondaryStrength",0);
                    material.SetFloat("_PrimaryShift",0);material.SetFloat("_SecondaryShift",0);material.SetFloat("_Scatter",0);
                    Color32[] Capture(Vector4 tangent,string label,bool reverse=false)
                    {
                        mesh.tangents=Enumerable.Repeat(tangent,4).ToArray();mesh.triangles=reverse?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};
                        camera.Render();RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,256,256),0,0);picture.Apply();
                        if(!string.IsNullOrEmpty(output))File.WriteAllBytes(Path.Combine(output,layer+"-"+label+".png"),picture.EncodeToPNG());
                        return picture.GetPixels32();
                    }
                    var along=Capture(new Vector4(1,0,0,-1),"vertical-fibers");
                    var across=Capture(new Vector4(0,1,0,-1),"horizontal-fibers");
                    var back=Capture(new Vector4(1,0,0,-1),"reverse-winding",true);
                    int visible=0,backVisible=0,changed=0;double alongEnergy=0,acrossEnergy=0;
                    for(int i=0;i<along.Length;i++)
                    {
                        if(along[i].a>127)visible++;if(back[i].a>127)backVisible++;
                        Assert.AreEqual(along[i].a,across[i].a,"Fiber orientation cannot change coverage.");
                        alongEnergy+=along[i].r+along[i].g+along[i].b;acrossEnergy+=across[i].r+across[i].g+across[i].b;
                        if(Math.Abs(along[i].r-across[i].r)+Math.Abs(along[i].g-across[i].g)+Math.Abs(along[i].b-across[i].b)>30)changed++;
                    }
                    Assert.That(visible,Is.InRange(along.Length/20,along.Length/2),"Atlas must draw strands AND clipped gaps.");
                    Assert.That(Math.Abs(backVisible-visible),Is.LessThan(12),"Both sides must retain the same coverage.");
                    Assert.That(changed,Is.GreaterThan(500),"Actual highlights must respond to fiber direction.");
                    Assert.That(alongEnergy,Is.GreaterThan(acrossEnergy*1.5),"Longitudinal fiber lighting cannot behave like isotropic plastic.");
                    var passes=Enumerable.Range(0,material.passCount).Select(material.GetPassName).ToArray();
                    foreach(string pass in new[]{"FiberForward","ShadowCaster","DepthOnly","DepthNormals"})
                        Assert.IsTrue(passes.Any(p=>string.Equals(p,pass,StringComparison.OrdinalIgnoreCase)),pass+": "+string.Join(",",passes));
                    int foundationPixels=0,ghostFoundationPixels=0;
                    if(layer==0)
                    {
                        mesh.uv3=Enumerable.Repeat(Vector2.right,4).ToArray();
                        mesh.uv2=Enumerable.Repeat(Vector2.zero,4).ToArray();
                        var root=Capture(new Vector4(1,0,0,-1),"continuous-foundation");
                        foundationPixels=root.Count(p=>p.a>0);
                        Assert.Greater(foundationPixels,visible+1500,"The dense crest must close the old atlas gutters.");
                        mesh.uv2=Enumerable.Repeat(Vector2.up,4).ToArray();
                        var tip=Capture(new Vector4(1,0,0,-1),"foundation-loose-tip");
                        Assert.That(Math.Abs(tip.Count(p=>p.a>127)-visible),Is.LessThan(12),"The lower edge must retain the atlas cutout.");
                        var ghost=new Material(Resources.Load<Shader>("ReinsGhostHair"));copies.Add(ghost);
                        ghost.SetTexture("_BaseMap",material.GetTexture("_BaseMap"));ghost.SetFloat("_Cutoff",material.GetFloat("_Cutoff"));
                        ghost.SetFloat("_FoundationCoverage",1);ghost.SetColor("_BaseColor",new Color(1,1,1,.28f));
                        renderer.sharedMaterial=ghost;mesh.uv2=Enumerable.Repeat(Vector2.zero,4).ToArray();
                        var ghostRoot=Capture(new Vector4(1,0,0,-1),"ghost-foundation");ghostFoundationPixels=ghostRoot.Count(p=>p.a>0);
                        Assert.That(Math.Abs(ghostFoundationPixels-foundationPixels),Is.LessThan(12),"Ghost coverage must match the solid crest before applying its lower opacity.");
                        for(int i=0;i<root.Length;i++)Assert.AreEqual(root[i].a>0,ghostRoot[i].a>0,"The ghost must not reopen filled root gaps.");
                        mesh.uv3=Enumerable.Repeat(Vector2.zero,4).ToArray();mesh.uv2=Enumerable.Repeat(new Vector2(0,.7f),4).ToArray();
                        renderer.sharedMaterial=material;
                    }
                    records.Add(new LayerProof{layer=layer,coveredPixels=visible,backCoveredPixels=backVisible,changedPixels=changed,
                        alongEnergy=alongEnergy,acrossEnergy=acrossEnergy,activePasses=passes,foundationPixels=foundationPixels,ghostFoundationPixels=ghostFoundationPixels});
                }
                if(!string.IsNullOrEmpty(output))File.WriteAllText(Path.Combine(output,"render-proof.json"),JsonUtility.ToJson(new Proof{layers=records.ToArray()},true)+"\n");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=enabled[i];
                RenderSettings.sun=priorSun;RenderSettings.ambientMode=priorAmbientMode;RenderSettings.ambientLight=priorAmbient;RenderSettings.fog=priorFog;
                RenderTexture.active=priorActive;camera.targetTexture=null;
                foreach(var material in copies)Object.Destroy(material);
                Object.Destroy(owner);Object.Destroy(mesh);Object.Destroy(target);Object.Destroy(picture);
            }
            yield return null;
        }
        [Serializable] private sealed class Proof
        {
            public string context="Isolated actual saved materials; controlled tangent rotation, clipped gaps and back-face coverage. Not photographic or phone-performance acceptance.";
            public LayerProof[] layers;
        }
        [Serializable] private sealed class LayerProof
        {public int layer,coveredPixels,backCoveredPixels,changedPixels,foundationPixels,ghostFoundationPixels;public double alongEnergy,acrossEnergy;public string[] activePasses;}
    }
}
