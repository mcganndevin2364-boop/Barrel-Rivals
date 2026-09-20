using System.Collections;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsHairTests
    {
        [UnityTest] public IEnumerator StrandHairIsSkinnedPersistentAndFollowsTheRigWithoutMovingTheRaceRoot()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindFirstObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;var rootPosition=horse.position;
            var skin=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Horse strand hair");
            Assert.IsNotNull(skin.sharedMesh);Assert.IsNotNull(skin.sharedMaterial.mainTexture);
            Assert.That(skin.sharedMesh.triangles.Length/3,Is.LessThan(3000));
            Assert.That(skin.sharedMesh.vertexCount,Is.LessThan(4000));
            Assert.That(skin.sharedMaterial.GetFloat("_AlphaClip"),Is.EqualTo(1));
            Assert.IsTrue(skin.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON"));
            Assert.AreEqual(4,skin.bones.Length);
            foreach(var b in skin.bones){Assert.IsNotNull(b);Assert.IsTrue(b.IsChildOf(horse));}
            foreach(var w in skin.sharedMesh.boneWeights){Assert.That(w.weight0+w.weight1,Is.EqualTo(1).Within(.0001f));Assert.That(w.boneIndex0,Is.InRange(0,3));Assert.That(w.boneIndex1,Is.InRange(0,3));}
            foreach(var renderer in horse.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("HorseHair")))Assert.IsFalse(renderer.gameObject.activeSelf,"Frozen source clumps must stay hidden, including in thumbnails.");
            var animator=horse.GetComponentInChildren<Animator>();var presentation=horse.GetComponent<ReinsHorsePresentation>();
            presentation.ResetFrame(new HorsePresentationFrame(0,horse.position,horse.rotation,8,0,0,0,false,false));
            var first=new Mesh();var second=new Mesh();
            try {
                animator.Play(0,0,.1f);animator.Update(0);skin.BakeMesh(first);
                animator.Play(0,0,.6f);animator.Update(0);skin.BakeMesh(second);
                var a=first.vertices;var b=second.vertices;float maximumMovement=0;
                Assert.AreEqual(a.Length,b.Length);
                for(int i=0;i<a.Length;i++){
                    Assert.IsFalse(float.IsNaN(a[i].x)||float.IsNaN(a[i].y)||float.IsNaN(a[i].z));
                    maximumMovement=Mathf.Max(maximumMovement,Vector3.Distance(a[i],b[i]));
                }
                Assert.That(maximumMovement,Is.GreaterThan(.001f),"The new hair must actually follow animated bones.");
                Assert.That(Vector3.Distance(rootPosition,horse.position),Is.LessThan(.0001f));
                Assert.AreEqual(0,controller.Run.Tick);LogAssert.NoUnexpectedReceived();
            } finally {Object.Destroy(first);Object.Destroy(second);}
        }
    }
}
