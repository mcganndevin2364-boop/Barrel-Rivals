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
            var normals=savedMesh.normals;var tangents=savedMesh.tangents;
            Assert.AreEqual(vertices.Length,tangents.Length,"Fiber lighting requires a persisted tangent frame.");
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
                    var tangent=tangents[i];var direction=new Vector3(tangent.x,tangent.y,tangent.z);
                    Assert.IsTrue(Finite(tangent.x)&&Finite(tangent.y)&&Finite(tangent.z)&&Finite(tangent.w));
                    Assert.That(direction.sqrMagnitude,Is.EqualTo(1).Within(.001f));
                    Assert.That(Mathf.Abs(tangent.w),Is.EqualTo(1).Within(.001f));
                    Assert.That(Mathf.Abs(Vector3.Dot(direction,normals[i])),Is.LessThan(.001f),"A degenerate frame breaks longitudinal lighting.");
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
                        Assert.That(point.x-skin.Point.x,Is.InRange(.001f,.040f),"Draped mane must clear actual skin: vertex "+i+", progress "+landmarks[i].y+", foundation "+savedMesh.uv3[i].x);
                    }
                }
                // Attached vertices alone do not prove a visible groom: a wide face
                // can take a chord through the convex crown. Check the actual faces.
                var triangles=savedMesh.triangles;int crownFaces=0;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                    if(landmarks[a].x>.5f || landmarks[b].x>.5f || landmarks[c].x>.5f)continue;
                    if(Mathf.Max(landmarks[a].y,Mathf.Max(landmarks[b].y,landmarks[c].y))>.251f)continue;
                    var center=(vertices[a]+vertices[b]+vertices[c])/3;
                    var body=surface.Top(center.x,center.z);
                    Assert.That(center.y-body.Point.y,Is.GreaterThan(-.002f),
                        "A mane crown face passes through the neck despite attached vertices.");
                    crownFaces++;
                }
                Assert.That(crownFaces,Is.GreaterThan(100),"Check a real fitted crown, not only the hanging tips.");
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
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

        [Test]
        public void FoundationIsOneConnectedSurfaceWithoutInteriorCracksAndOtherLocksRemainUnmarked()
        {
            var marker=savedMesh.uv3;var vertices=savedMesh.vertices;var indices=savedMesh.triangles;
            Assert.AreEqual(vertices.Length,marker.Length,"Foundation coverage must survive native mesh persistence.");
            var marked=Enumerable.Range(0,vertices.Length).Where(i=>marker[i].x>.5f).ToArray();
            Assert.That(marked.Length,Is.InRange(400,800));
            var parent=Enumerable.Range(0,vertices.Length).ToArray();
            var edges=new System.Collections.Generic.Dictionary<(int,int),int>();int faces=0;
            int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
            for(int i=0;i<indices.Length;i+=3)
            {
                int count=Enumerable.Range(0,3).Count(k=>marker[indices[i+k]].x>.5f);
                Assert.IsTrue(count==0 || count==3,"Coverage must never interpolate from foundation onto an unrelated lock.");
                if(count==0)continue;faces++;
                for(int k=0;k<3;k++)
                {
                    int a=indices[i+k],b=indices[i+(k+1)%3];parent[Find(a)]=Find(b);
                    var key=a<b?(a,b):(b,a);edges[key]=edges.TryGetValue(key,out int n)?n+1:1;
                    Assert.That(Vector3.Distance(vertices[a],vertices[b]),Is.LessThan(.08f),"Long chords could cut across the fitted neck: "+a+" to "+b);
                }
            }
            Assert.AreEqual(1,marked.Select(Find).Distinct().Count(),"Dense mane must form one surface rather than disconnected overlapping cards.");
            Assert.IsTrue(edges.Values.All(n=>n==1 || n==2));
            Assert.AreEqual(1,marked.Length-edges.Count+faces,"A connected rectangular foundation must not contain interior holes.");
            foreach(int i in marked)Assert.AreEqual(0,savedMesh.uv2[i].x,"Only the mane is covered; tail/forelock keep their original masks.");
        }

        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
