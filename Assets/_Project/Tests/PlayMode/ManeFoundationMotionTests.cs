using System;
using System.Collections;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ManeFoundationMotionTests
    {
        [UnityTest] public IEnumerator ContinuousManeFollowsAnimatedSkinAcrossIdleWalkAndFastGaits()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindAnyObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;
            var source=horse.GetComponent<ReinsHorsePresentation>();var animator=horse.GetComponentInChildren<Animator>();
            var motion=horse.GetComponentInChildren<ReinsHairMotion>();motion.SetReducedMotion(false);
            var hair=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Horse strand hair");
            var body=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("HorseBody",StringComparison.Ordinal));
            var model=hair.rootBone;Assert.IsNotNull(model,"Use the renderer’s art space after runtime presentation reparenting.");
            var marker=hair.sharedMesh.uv3;var progress=hair.sharedMesh.uv2;
            var sampled=Enumerable.Range(0,marker.Length).Where(i=>marker[i].x>.5f).Where((value,index)=>index%4==0).ToArray();
            var hairMesh=new Mesh();var bodyMesh=new Mesh();var shell=new Mesh();
            var contact=new GameObject("Test-only moving mane surface").AddComponent<MeshCollider>();
            float minimumClearance=float.PositiveInfinity,maximumClearance=float.NegativeInfinity,travel=0;
            int samples=0;Vector3[] first=null;var root=horse.localToWorldMatrix;
            try
            {
                foreach(float speed in new[]{0f,1.5f,8f,14f})for(int frame=0;frame<8;frame++)
                {
                    source.ResetFrame(new HorsePresentationFrame(0,horse.position,horse.rotation,speed,.5f,0,0,false,false));
                    animator.Update(0);animator.Play(0,0,frame/8f);animator.Update(0);motion.RenderImmediate();
                    body.BakeMesh(bodyMesh,true);hair.BakeMesh(hairMesh,true);
                    var toModel=model.worldToLocalMatrix*body.localToWorldMatrix;
                    shell.vertices=bodyMesh.vertices.Select(toModel.MultiplyPoint3x4).ToArray();shell.triangles=bodyMesh.triangles;
                    contact.sharedMesh=null;contact.sharedMesh=shell;Physics.SyncTransforms();
                    var hairToModel=model.worldToLocalMatrix*hair.localToWorldMatrix;
                    var points=hairMesh.vertices.Select(hairToModel.MultiplyPoint3x4).ToArray();
                    if(first==null)first=points;
                    foreach(int i in sampled)
                    {
                        var p=points[i];Assert.IsTrue(float.IsFinite(p.x+p.y+p.z));
                        travel=Mathf.Max(travel,Vector3.Distance(first[i],p));
                        bool crown=progress[i].y<=.25f;
                        var ray=crown?new Ray(new Vector3(p.x,3,p.z),Vector3.down):new Ray(new Vector3(2,p.y,p.z),Vector3.left);
                        Assert.IsTrue(contact.Raycast(ray,out var hit,4),"An animated foundation sample must remain over the horse.");
                        float gap=crown?p.y-hit.point.y:p.x-hit.point.x;
                        minimumClearance=Mathf.Min(minimumClearance,gap);maximumClearance=Mathf.Max(maximumClearance,gap);samples++;
                    }
                    Assert.AreEqual(root,horse.localToWorldMatrix);Assert.AreEqual(0,controller.Run.Tick);
                }
                Assert.Greater(samples,4000);Assert.Greater(travel,.005f);
                Assert.Greater(minimumClearance,-.003f,"The dense foundation must not disappear into the animated neck at sampled vertices.");
                Assert.Less(maximumClearance,.045f,"The foundation must not float away from the neck.");
                string output=Environment.GetEnvironmentVariable("BARREL_MANE_FOUNDATION_OUTPUT");
                if(!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"motion-fit.json"),JsonUtility.ToJson(new Proof{
                        samples=samples,minimumClearanceM=minimumClearance,maximumClearanceM=maximumClearance,vertexTravelM=travel},true)+"\n");
                }
            }
            finally{Object.Destroy(contact.gameObject);Object.Destroy(hairMesh);Object.Destroy(bodyMesh);Object.Destroy(shell);}
            LogAssert.NoUnexpectedReceived();
        }
        [Serializable] class Proof
        {
            public int poses=32,samples;public float minimumClearanceM,maximumClearanceM,vertexTravelM;
            public string scope="Sampled vertices/cross-sections over four speeds/eight phases; no continuous collision, natural-motion or phone performance claim.";
        }
    }
}
