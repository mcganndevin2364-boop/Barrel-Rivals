using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
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
            float maneMaxX=float.NegativeInfinity,rootMinZ=float.PositiveInfinity,rootMaxZ=float.NegativeInfinity;
            int broadNeckVertices=0,maneRoots=0;
            for(int i=0;i<vertices.Length;i++)
            {
                var point=vertices[i];
                Assert.IsTrue(Finite(point.x)&&Finite(point.y)&&Finite(point.z),"Non-finite hair vertex "+i);
                minimum=Vector3.Min(minimum,point);maximum=Vector3.Max(maximum,point);
                // Neck is bone 0; forelock and tail have other primary bones.
                if(weights[i].boneIndex0!=0 || weights[i].weight0<=0)continue;
                maneMaxX=Mathf.Max(maneMaxX,point.x);
                if(point.z>.65f && point.z<1.10f && point.y>1.45f && point.y<1.72f && point.x>.32f)
                    broadNeckVertices++;
                if(uv[i].y>.0001f)continue;
                maneRoots++;
                rootMinZ=Mathf.Min(rootMinZ,point.z);rootMaxZ=Mathf.Max(rootMaxZ,point.z);
                Assert.That(Mathf.Abs(point.x),Is.LessThan(.025f),"Mane roots must join the dorsal crest.");
            }
            // A stale native Mesh buffer can coexist with newly copied bounds metadata.
            // Compare the actual imported channel, without RecalculateBounds masking it.
            Assert.That(Vector3.Distance(minimum,savedMesh.bounds.min),Is.LessThan(.0001f),"Saved minimum disagrees with the persisted vertex buffer.");
            Assert.That(Vector3.Distance(maximum,savedMesh.bounds.max),Is.LessThan(.0001f),"Saved maximum disagrees with the persisted vertex buffer.");
            Assert.That(maneRoots,Is.GreaterThan(20));
            Assert.That(rootMinZ,Is.InRange(.70f,.82f));
            Assert.That(rootMaxZ,Is.InRange(1.73f,1.83f),"Neck mane must stop before the ears; the forelock covers the poll.");
            Assert.That(maneMaxX,Is.InRange(.34f,.46f),"Draped mane must reach outside the measured broad neck envelope.");
            Assert.That(broadNeckVertices,Is.GreaterThan(8),"The rear mane curtain is still buried inside the neck.");
        }

        [Test]
        public void ReloadedHairRetainsFourFiniteBindposesAndNormalizedSkinWeights()
        {
            var poses=savedMesh.bindposes;var weights=savedMesh.boneWeights;
            Assert.AreEqual(4,poses.Length);
            for(int bone=0;bone<poses.Length;bone++)
                for(int element=0;element<16;element++)Assert.IsTrue(Finite(poses[bone][element]),"Invalid bindpose "+bone);
            Assert.AreEqual(savedMesh.vertexCount,weights.Length);
            var used=new bool[4];
            foreach(var weight in weights)
            {
                float[] influence={weight.weight0,weight.weight1,weight.weight2,weight.weight3};
                int[] bone={weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};
                float sum=0;int active=0;
                for(int i=0;i<influence.Length;i++)
                {
                    Assert.IsTrue(Finite(influence[i]));Assert.That(influence[i],Is.InRange(0f,1f));
                    sum+=influence[i];if(influence[i]<=0)continue;
                    Assert.That(bone[i],Is.InRange(0,3));used[bone[i]]=true;active++;
                }
                Assert.That(sum,Is.EqualTo(1f).Within(.0001f));
                Assert.That(active,Is.InRange(1,2),"The mobile renderer uses two bone influences.");
            }
            Assert.That(used,Is.All.True,"Neck, head, tail base and tail must remain bound after persistence.");
        }

        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
