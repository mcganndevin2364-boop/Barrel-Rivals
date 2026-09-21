using System.Linq;
using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class GloveTailoringTests
    {
        [Test] public void TailoringKeepsTheConnectedShellAndGripApertureAndMirrorsAllDetails()
        {
            foreach(bool open in new[]{true,false})
            {
                var raw=open?ReinsGloveBuilder.Inspection():ReinsGloveBuilder.Riding(-1);
                var tailored=GloveTailoringBuilder.Finish(Object.Instantiate(raw));
                var right=open?null:GloveTailoringBuilder.Finish(ReinsGloveBuilder.Riding(1),true);
                try
                {
                    // Retain exact anatomical vertices/triangles. The existing connectivity
                    // and rein-clearance regression still operates on this unchanged shell.
                    CollectionAssert.AreEqual(raw.vertices,tailored.vertices.Take(raw.vertexCount));
                    CollectionAssert.AreEqual(raw.triangles,tailored.triangles.Take(raw.triangles.Length));
                    Assert.AreEqual(1,tailored.subMeshCount);
                    Assert.That(tailored.triangles.Length/3,Is.LessThan(open?5500:4500));
                    Assert.That(tailored.colors.Count(c=>c.a==1),Is.GreaterThan(150),"Fixed sewn thread must be present.");
                    Assert.That(tailored.colors.Count(c=>c.a==0&&c.r<.9f),Is.GreaterThan(150),"Dyed reinforced panels must be present.");
                    for(int i=0;i<tailored.vertexCount;i++)
                    {
                        var p=tailored.vertices[i];var n=tailored.normals[i];var t=tailored.tangents[i];
                        Assert.IsTrue(float.IsFinite(p.x+p.y+p.z+n.x+n.y+n.z+t.x+t.y+t.z));
                        Assert.That(n.sqrMagnitude,Is.EqualTo(1).Within(.02f));
                        Assert.That(new Vector3(t.x,t.y,t.z).sqrMagnitude,Is.EqualTo(1).Within(.02f));
                        if(i>=raw.vertexCount)Assert.Greater(p.y,0,"Sewn details stay dorsal and cannot close the palm-side rein aperture.");
                        if(right)Assert.That(Vector3.Distance(new Vector3(-p.x,p.y,p.z),right.vertices[i]),Is.LessThan(.000001f));
                    }
                    if(right) {
                        CollectionAssert.AreEqual(tailored.colors,right.colors);
                        var v=right.vertices;var n=right.normals;var tri=right.triangles;
                        for(int i=raw.triangles.Length;i<tri.Length;i+=3)
                            Assert.Greater(Vector3.Dot(Vector3.Cross(v[tri[i+1]]-v[tri[i]],v[tri[i+2]]-v[tri[i]]),n[tri[i]]+n[tri[i+1]]+n[tri[i+2]]),0,"Mirrored detail faces must remain outward.");
                    }
                }
                finally{Object.DestroyImmediate(raw);Object.DestroyImmediate(tailored);if(right)Object.DestroyImmediate(right);}
            }
        }
        [Test] public void SavedDyesUseOneOpaqueShaderAndPreserveTheSameThreadAndPhotographicMaps()
        {
            var shader=Shader.Find("Barrel Rivals/Tailored Leather");Assert.IsNotNull(shader);Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
            var paths=new[]{ReinsPremiumArenaBuilder.Root+"/Materials/Worn chestnut gloves.mat"}.Concat(new[]{"Blackout","Whiskey","Rodeo red","Steelhide","Midnight"}.Select(n=>StableWardrobeArtBuilder.Root+"/"+n+" gloves.mat"));
            Material first=null;
            foreach(var path in paths)
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);Assert.IsNotNull(material,path);Assert.AreSame(shader,material.shader);
                Assert.Less(material.renderQueue,2500); // Active URP pass checks run in the real rendered-scene test.
                foreach(var property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
                {Assert.IsNotNull(material.GetTexture(property));if(first)Assert.AreSame(first.GetTexture(property),material.GetTexture(property));}
                if(first)Assert.AreEqual(first.GetColor("_ThreadColor"),material.GetColor("_ThreadColor"));
                first=material;
            }
        }
    }
}
