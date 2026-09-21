using System.Collections;
using System;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsRiderBodyTests
    {
        [UnityTest] public IEnumerator RiderDeformsAndKeepsSeatWristsAndFeetConnectedAcrossGaits()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;var source=horse.GetComponent<ReinsHorsePresentation>();
            var rider=horse.GetComponentInChildren<ReinsRiderBodyPresentation>();Assert.IsNotNull(rider);
            var skins=rider.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.AreEqual(6,skins.Length);
            foreach(var skin in skins){Assert.AreEqual(SkinQuality.Bone4,skin.quality);Assert.That(skin.bones.Length,Is.GreaterThan(0));}
            var animator=horse.GetComponentInChildren<Animator>();var tack=horse.GetComponentInChildren<ReinsRiderTackPresentation>();
            var shirt=skins.Single(s=>s.name.Contains("casualsuit"));var baked=new Mesh();Vector3[] first=null;float largestMotion=0;
            var boots=skins.Single(s=>s.name.Contains("ankle_boots"));
            var hardware=horse.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="Saddle hardware");
            float maximumSoleClearance=0,maximumReachError=0;
            var position=horse.position;var rotation=horse.rotation;
            Transform Named(string name)=>horse.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
            try
            {
                foreach(float speed in new[]{0f,1.5f,8f,14f})foreach(float tension in new[]{0f,.5f,1f})
                {
                    source.ResetFrame(new HorsePresentationFrame(0,position,rotation,speed,.65f,tension,1-tension,false,speed>10));
                    animator.Update(0);
                    foreach(float phase in new[]{0f,.25f,.50f,.75f})
                    {
                        animator.Play(0,0,phase);animator.Update(0);tack.RenderImmediate();rider.RenderImmediate();
                        Assert.That(rider.MaximumReachError,Is.LessThan(.015f),"Unreachable rider grip/stirrup at speed "+speed+", phase "+phase);
                        maximumReachError=Mathf.Max(maximumReachError,rider.MaximumReachError);
                        Assert.That(Vector3.Distance(Named("pelvis").position,Named("Rider pelvis anchor").position),Is.LessThan(.001f));
                        shirt.BakeMesh(baked,true);var points=baked.vertices;
                        // FBX renderer coordinates carry a conversion scale. Measure
                        // real deformation in rider-local metres, not its raw buffer units.
                        var matrix=rider.transform.worldToLocalMatrix*shirt.transform.localToWorldMatrix;
                        for(int i=0;i<points.Length;i++)points[i]=matrix.MultiplyPoint3x4(points[i]);
                        Assert.That(points.Length,Is.GreaterThan(2000));
                        if(first==null)first=points;
                        else for(int i=0;i<points.Length;i++)largestMotion=Mathf.Max(largestMotion,Vector3.Distance(points[i],first[i]));
                        foreach(var p in points)Assert.IsTrue(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z));
                        boots.BakeMesh(baked,true);
                        foreach(string side in new[]{"Left","Right"})
                        {
                            var foot=Named(side+" rider stirrup target");
                            var sole=baked.vertices.Select(p=>foot.InverseTransformPoint(boots.transform.TransformPoint(p)))
                                .Where(p=>Mathf.Abs(p.x)<.055f && p.z>.045f && p.z<.16f).ToArray();
                            var tread=hardware.sharedMesh.vertices.Select(p=>foot.InverseTransformPoint(hardware.transform.TransformPoint(p)))
                                .Where(p=>Mathf.Abs(p.x)<.060f && p.z>.09f && p.z<.12f && p.y>-.11f && p.y<-.077f).ToArray();
                            Assert.That(sole.Length,Is.GreaterThan(0));Assert.That(tread.Length,Is.GreaterThan(0));
                            float clearance=sole.Min(p=>p.y)-tread.Max(p=>p.y);
                            Assert.That(clearance,Is.InRange(-.005f,.012f),side+" boot must rest on its actual stirrup tread.");
                            maximumSoleClearance=Mathf.Max(maximumSoleClearance,Mathf.Abs(clearance));
                        }
                    }
                }
                Assert.That(largestMotion,Is.GreaterThan(.015f),"Animated bones must actually deform the imported clothing.");
                string output=Environment.GetEnvironmentVariable("BARREL_RIDER_PROOF_DIRECTORY");
                if(!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    var rows=horse.GetComponentsInChildren<Renderer>(true).Select(renderer=>
                    {
                        Mesh mesh=renderer is SkinnedMeshRenderer skinned?skinned.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
                        return new RenderRow{name=renderer.name,active=renderer.enabled&&renderer.gameObject.activeInHierarchy,
                            triangles=mesh?Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(int)mesh.GetIndexCount(i)/3):0,
                            slots=renderer.sharedMaterials.Length,shadowMode=renderer.shadowCastingMode.ToString()};
                    }).Where(row=>row.triangles>0).ToArray();
                    boots.BakeMesh(baked,true);
                    var leftFoot=Named("Left rider stirrup target");
                    var bootPoints=baked.vertices.Select(p=>leftFoot.InverseTransformPoint(boots.transform.TransformPoint(p))).Where(p=>Mathf.Abs(p.x)<.055f && p.z>.045f && p.z<.16f).ToArray();
                    Assert.That(bootPoints.Length,Is.GreaterThan(0));
                    File.WriteAllText(Path.Combine(output,"rider-proof.json"),JsonUtility.ToJson(new Proof{clothingDeformationMetres=largestMotion,
                        minimumBootSoleBelowAnkle=bootPoints.Min(p=>p.y),maximumSoleClearanceMetres=maximumSoleClearance,
                        maximumReachErrorMetres=maximumReachError,poseSamples=48,player=rows},true)+"\n");
                }
                Assert.That(Vector3.Distance(position,horse.position),Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(rotation,horse.rotation),Is.LessThan(.0001f));
                Assert.AreEqual(0,controller.Run.Tick);LogAssert.NoUnexpectedReceived();
            }
            finally{Object.Destroy(baked);}
        }

        [UnityTest] public IEnumerator SavedWesternHatFollowsTheHeadAndRetainsBodycamShadows()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;
            var rider=horse.GetComponentInChildren<ReinsRiderBodyPresentation>();
            rider.enabled=false;horse.GetComponentInChildren<Animator>().enabled=false;
            var hat=rider.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name.Contains("hat"));
            Assert.AreEqual("Original cattleman western hat",hat.sharedMesh.name);
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly,hat.shadowCastingMode);
            Assert.AreEqual(1,hat.sharedMaterials.Length);
            Assert.AreEqual("Barrel Rivals/Horse Surface",hat.sharedMaterial.shader.name);
            // Batch Play Mode can advance transforms without a graphics frame.
            // Activate the current SRP before querying its selected SubShader tags/passes.
            var camera=Camera.main;var previousTarget=camera.targetTexture;var target=RenderTexture.GetTemporary(64,64,24);
            try{camera.targetTexture=target;camera.Render();}
            finally{camera.targetTexture=previousTarget;RenderTexture.ReleaseTemporary(target);}
            Assert.AreEqual("Opaque",hat.sharedMaterial.GetTag("RenderType",false));
            var weights=hat.sharedMesh.boneWeights;var points=hat.sharedMesh.vertices;
            int headIndex=Array.FindIndex(hat.bones,b=>b.name=="head");Assert.That(headIndex,Is.GreaterThanOrEqualTo(0));
            Assert.That(weights.All(w=>w.boneIndex0==headIndex && w.weight0==1),Is.True);
            var head=hat.bones[headIndex];var rotation=head.localRotation;var baked=new Mesh();
            try
            {
                foreach(float angle in new[]{-15f,0f,20f})
                {
                    head.localRotation=rotation*Quaternion.Euler(0,angle,0);hat.BakeMesh(baked,true);
                    var matrix=head.localToWorldMatrix*hat.sharedMesh.bindposes[headIndex];var actual=baked.vertices;
                    for(int i=0;i<points.Length;i++)
                        Assert.That(Vector3.Distance(matrix.MultiplyPoint3x4(points[i]),hat.transform.TransformPoint(actual[i])),Is.LessThan(.0001f),"Saved hat must stay rigidly attached to the animated head.");
                }
            }
            finally{head.localRotation=rotation;Object.Destroy(baked);}
            LogAssert.NoUnexpectedReceived();
        }

        [Serializable] private sealed class RenderRow
        {public string name,shadowMode;public bool active;public int triangles,slots;}
        [Serializable] private sealed class Proof
        {public float clothingDeformationMetres,minimumBootSoleBelowAnkle,maximumSoleClearanceMetres,maximumReachErrorMetres;
            public int poseSamples;public RenderRow[] player;}

        [UnityTest] public IEnumerator GhostOwnsItsRiderPoseAndPonytailMaskWithoutHidingThePlayerHeadShadow()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var horse=GameObject.Find("Horse proxy").transform;var rider=horse.GetComponentInChildren<ReinsRiderBodyPresentation>();
            var ponytail=rider.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name.Contains("ponytail01"));
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly,ponytail.shadowCastingMode);
            var original=ponytail.sharedMaterial;var mask=original.GetTexture("_BaseMap");
            var tint=new Material(Shader.Find("Universal Render Pipeline/Lit"));tint.SetColor("_BaseColor",new Color(.1f,.7f,.8f,.28f));
            Transform ghost=null;
            try
            {
                ghost=ReinsLabController.CreatePresentationGhost(horse,tint);
                var ghostRider=ghost.GetComponentInChildren<ReinsRiderBodyPresentation>(true);Assert.IsNotNull(ghostRider);
                var ghostHair=ghostRider.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s=>s.name.Contains("ponytail01"));
                Assert.AreEqual(ShadowCastingMode.Off,ghostHair.shadowCastingMode);
                Assert.AreNotSame(original,ghostHair.sharedMaterial);
                Assert.AreEqual(mask,ghostHair.sharedMaterial.GetTexture("_BaseMap"));
                Assert.AreEqual("Barrel Rivals/Reins Ghost Hair",ghostHair.sharedMaterial.shader.name);
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly,ponytail.shadowCastingMode);
                Assert.AreSame(original,ponytail.sharedMaterial);
                var ghostSource=ghost.GetComponent<ReinsHorsePresentation>();
                ghostSource.ResetFrame(new HorsePresentationFrame(0,new Vector3(4,0,-2),Quaternion.Euler(0,40,0),8,0,0,1,false,false));
                ghost.GetComponentInChildren<ReinsRiderTackPresentation>(true).RenderImmediate();ghostRider.RenderImmediate();
                Assert.That(Vector3.Distance(ghostRider.transform.position,rider.transform.position),Is.GreaterThan(2));
            }
            finally{if(ghost)Object.Destroy(ghost.gameObject);Object.Destroy(tint);}
            yield return null;yield return null;LogAssert.NoUnexpectedReceived();
        }
    }
}
