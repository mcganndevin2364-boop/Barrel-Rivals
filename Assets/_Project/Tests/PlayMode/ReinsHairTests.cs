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
            Assert.IsNotNull(skin.sharedMesh);
            Assert.That(skin.sharedMesh.triangles.Length/3,Is.LessThan(3000));
            Assert.That(skin.sharedMesh.vertexCount,Is.LessThan(4000));
            AssertLayeredHairMaterials(skin);
            Assert.That(skin.bones.Length,Is.InRange(6,32));
            foreach(var b in skin.bones){Assert.IsNotNull(b);Assert.IsTrue(b.IsChildOf(horse));}
            foreach(var w in skin.sharedMesh.boneWeights){Assert.That(w.weight0+w.weight1,Is.EqualTo(1).Within(.0001f));Assert.That(w.boneIndex0,Is.InRange(0,skin.bones.Length-1));Assert.That(w.boneIndex1,Is.InRange(0,skin.bones.Length-1));}
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

        [UnityTest] public IEnumerator SceneGhostKeepsBothHairMasksPrivateAndReleasesBothCopies()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);yield return null;
            var controller=Object.FindAnyObjectByType<ReinsLabController>();controller.enabled=false;
            var horse=GameObject.Find("Horse proxy").transform;
            var player=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Horse strand hair");
            AssertLayeredHairMaterials(player);
            var originals=player.sharedMaterials;
            var maps=originals.Select(m=>m.GetTexture("_BaseMap")).ToArray();
            var cutoffs=originals.Select(m=>m.GetFloat("_Cutoff")).ToArray();
            var colors=originals.Select(m=>m.GetColor("_BaseColor")).ToArray();
            var scales=originals.Select(m=>m.GetTextureScale("_BaseMap")).ToArray();
            var offsets=originals.Select(m=>m.GetTextureOffset("_BaseMap")).ToArray();
            var culls=originals.Select(m=>m.GetFloat("_Cull")).ToArray();
            // Inspect the actual scene/controller clone, including the no-best-record
            // inactive path, rather than a fixture with a single material slot.
            var ghost=SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Own best — Reins recording");
            Material[] copies=null;
            try
            {
                var ghostSkin=ghost.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Horse strand hair");
                Assert.AreSame(player.sharedMesh,ghostSkin.sharedMesh);
                copies=ghostSkin.sharedMaterials;
                Assert.AreEqual(2,copies.Length);
                Assert.AreNotSame(copies[0],copies[1],"The dense undercoat and separated outer strands require independent ghost materials.");
                for(int i=0;i<2;i++)
                {
                    Assert.IsNotNull(copies[i]);
                    foreach(var original in originals)Assert.AreNotSame(original,copies[i],"A ghost cannot mutate player material assets.");
                    Assert.AreEqual("Barrel Rivals/Reins Ghost Hair",copies[i].shader.name);
                    Assert.AreSame(maps[i],copies[i].GetTexture("_BaseMap"),"Each submesh must retain its own alpha atlas.");
                    Assert.AreEqual(cutoffs[i],copies[i].GetFloat("_Cutoff"),.00001f);
                    Assert.AreEqual(culls[i],copies[i].GetFloat("_Cull"),.00001f);
                    Assert.AreEqual(1,copies[i].GetFloat("_AlphaClip"),.00001f);
                    Assert.That(Vector2.Distance(scales[i],copies[i].GetTextureScale("_BaseMap")),Is.LessThan(.00001f));
                    Assert.That(Vector2.Distance(offsets[i],copies[i].GetTextureOffset("_BaseMap")),Is.LessThan(.00001f));
                }

                var otherColor=copies[1].GetColor("_BaseColor");
                copies[0].SetFloat("_Cutoff",Mathf.Clamp01(cutoffs[0]+.1f));
                copies[0].SetColor("_BaseColor",Color.magenta);
                copies[0].SetTextureOffset("_BaseMap",offsets[0]+new Vector2(.013f,.027f));
                Assert.AreEqual(cutoffs[1],copies[1].GetFloat("_Cutoff"),.00001f,"Changing one ghost layer must not alter the other.");
                AssertColor(otherColor,copies[1].GetColor("_BaseColor"));
                for(int i=0;i<2;i++)
                {
                    Assert.AreSame(originals[i],player.sharedMaterials[i]);
                    Assert.AreSame(maps[i],originals[i].GetTexture("_BaseMap"));
                    Assert.AreEqual(cutoffs[i],originals[i].GetFloat("_Cutoff"),.00001f);
                    Assert.AreEqual(culls[i],originals[i].GetFloat("_Cull"),.00001f);
                    Assert.That(Vector2.Distance(scales[i],originals[i].GetTextureScale("_BaseMap")),Is.LessThan(.00001f));
                    Assert.That(Vector2.Distance(offsets[i],originals[i].GetTextureOffset("_BaseMap")),Is.LessThan(.00001f));
                    AssertColor(colors[i],originals[i].GetColor("_BaseColor"));
                }
            }
            finally {if(ghost)Object.Destroy(ghost);}
            yield return null;yield return null;
            for(int i=0;i<2;i++)
            {
                Assert.IsTrue(copies[i]==null,"Every private ghost hair material must be released, including an inactive ghost.");
                Assert.IsTrue(originals[i],"Player material assets must survive ghost disposal.");
                Assert.IsTrue(maps[i],"Source alpha atlases must survive ghost disposal.");
                Assert.AreSame(originals[i],player.sharedMaterials[i]);
            }
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertLayeredHairMaterials(SkinnedMeshRenderer skin)
        {
            Assert.AreEqual(2,skin.sharedMesh.subMeshCount,"Dense undercoat and loose strands need separate material ranges.");
            var materials=skin.sharedMaterials;Assert.AreEqual(2,materials.Length);
            Assert.AreNotSame(materials[0],materials[1]);
            for(int i=0;i<2;i++)
            {
                Assert.IsNotNull(materials[i]);
                Assert.That(skin.sharedMesh.GetIndexCount(i),Is.GreaterThan(0),"Both groom layers must contain real geometry.");
                Assert.IsTrue(materials[i].HasProperty("_BaseMap"));
                Assert.IsNotNull(materials[i].GetTexture("_BaseMap"));
                Assert.That(materials[i].GetFloat("_AlphaClip"),Is.EqualTo(1));
                Assert.IsTrue(materials[i].IsKeywordEnabled("_ALPHATEST_ON"));
                Assert.That(materials[i].GetFloat("_Cutoff"),Is.GreaterThan(0).And.LessThan(1));
                Assert.That(materials[i].GetFloat("_Cull"),Is.EqualTo(0),"Both strand layers must remain two-sided.");
            }
            Assert.AreNotSame(materials[0].GetTexture("_BaseMap"),materials[1].GetTexture("_BaseMap"));
            CollectionAssert.AreEquivalent(new[]{"Original natural strand atlas","Original separated strand atlas"},
                materials.Select(m=>m.GetTexture("_BaseMap").name).ToArray(),"The undercoat and outer locks must use the reviewed original alpha maps.");
        }

        private static void AssertColor(Color expected,Color actual)
        {for(int channel=0;channel<4;channel++)Assert.AreEqual(expected[channel],actual[channel],.00001f);}
    }
}
