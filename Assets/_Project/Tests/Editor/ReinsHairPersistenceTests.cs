using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine;

namespace BarrelRivals.Tests
{
    public sealed class ReinsHairPersistenceTests
    {
        private Mesh savedMesh;

        [OneTimeSetUp]
        public void ReloadSavedHair()
        {
            const string path=ReinsHairBuilder.Root+"/Skinned hair.asset";
            // Exercise the persisted asset, not the transient mesh made by the builder.
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Assert.IsNotNull(savedMesh,"Regenerate the Reins art before running this regression.");
            Assert.IsTrue(EditorUtility.IsPersistent(savedMesh));
        }

        [Test]
        public void ReloadedHairVerticesMatchBoundsAndClearTheBroadNeck()
        {
            var vertices=savedMesh.vertices;
            var weights=savedMesh.boneWeights;
            var uv=savedMesh.uv;
            Assert.That(vertices.Length,Is.GreaterThan(100));
            Assert.AreEqual(vertices.Length,weights.Length);
            Assert.AreEqual(vertices.Length,uv.Length);
            var minimum=vertices[0];var maximum=vertices[0];
            var landmarks=savedMesh.uv2;
            var normals=savedMesh.normals;
            Assert.AreEqual(vertices.Length,landmarks.Length,"Saved hair must retain attachment landmarks.");
            int maneRoots=0;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
                var model=GameObject.Find("Horse proxy").transform.Find("Reference horse");
                var animator=model.GetComponentInChildren<Animator>();
                var idle=AssetDatabase.LoadAllAssetsAtPath(ReinsReferenceArtBuilder.HorsePath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Single(c=>c.name=="Idle" || c.name.EndsWith("|Idle"));
                idle.SampleAnimation(animator.gameObject,0);
                var surface=new ReinsHorseSurface(model);
                for(int i=0;i<vertices.Length;i++)
                {
                    var point=vertices[i];
                    Assert.IsTrue(Finite(point.x)&&Finite(point.y)&&Finite(point.z),"Non-finite hair vertex "+i);
                    minimum=Vector3.Min(minimum,point);maximum=Vector3.Max(maximum,point);
                    if(landmarks[i].x>1.5f)continue; // Tail uses its own rig landmark.
                    if(landmarks[i].y<.0001f)
                    {
                        var skin=surface.Top(point.x,point.z);
                        Assert.That(Vector3.Distance(point,skin.Point),Is.LessThan(.02f),"Mane/forelock root is detached from actual local neutral skin.");
                        if(landmarks[i].x<.5f)maneRoots++;
                    }
                    else if(landmarks[i].x<.5f && landmarks[i].y>.20f)
                    {
                        Assert.That(normals[i].x,Is.GreaterThan(.05f),"Mane normals must face outward so lit strands do not shade as an inverted black sheet.");
                        var skin=surface.Side(point.y,point.z);
                        Assert.That(point.x-skin.Point.x,Is.InRange(.001f,.040f),"Draped mane must clear the actual neck, not a stale world-space envelope.");
                    }
                }
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            // A stale native Mesh buffer can coexist with newly copied bounds metadata.
            // Compare the actual imported channel, without RecalculateBounds masking it.
            Assert.That(Vector3.Distance(minimum,savedMesh.bounds.min),Is.LessThan(.0001f),"Saved minimum disagrees with the persisted vertex buffer.");
            Assert.That(Vector3.Distance(maximum,savedMesh.bounds.max),Is.LessThan(.0001f),"Saved maximum disagrees with the persisted vertex buffer.");
            Assert.That(maneRoots,Is.GreaterThan(20));
        }

        [Test]
        public void ReloadedHairRetainsFiniteBindposesAndNormalizedMobileSkinWeights()
        {
            var poses=savedMesh.bindposes;var weights=savedMesh.boneWeights;
            Assert.That(poses.Length,Is.InRange(6,32));
            for(int bone=0;bone<poses.Length;bone++)
                for(int element=0;element<16;element++)Assert.IsTrue(Finite(poses[bone][element]),"Invalid bindpose "+bone);
            Assert.AreEqual(savedMesh.vertexCount,weights.Length);
            var used=new bool[poses.Length];
            foreach(var weight in weights)
            {
                float[] influence={weight.weight0,weight.weight1,weight.weight2,weight.weight3};
                int[] bone={weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};
                float sum=0;int active=0;
                for(int i=0;i<influence.Length;i++)
                {
                    Assert.IsTrue(Finite(influence[i]));Assert.That(influence[i],Is.InRange(0f,1f));
                    sum+=influence[i];if(influence[i]<=0)continue;
                    Assert.That(bone[i],Is.InRange(0,poses.Length-1));used[bone[i]]=true;active++;
                }
                Assert.That(sum,Is.EqualTo(1f).Within(.0001f));
                Assert.That(active,Is.InRange(1,2),"The mobile renderer uses two bone influences.");
            }
            Assert.That(used.Count(value=>value),Is.GreaterThanOrEqualTo(6),"Fitted roots retain torso, neck, head/ear and tail influences.");
        }

        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
