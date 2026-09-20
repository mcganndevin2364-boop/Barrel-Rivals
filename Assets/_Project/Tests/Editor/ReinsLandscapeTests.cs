using System;
using System.Linq;
using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BarrelRivals.Tests
{
    public sealed class ReinsLandscapeTests
    {
        private const string Root="Assets/_Project/Art/Reins/Premium/Mountains/";

        [Test] public void SavedTerrainStaysOutsideCourseAndRetainsFiniteContinuousSurface()
        {
            string path=Root+"Layered eroded ridges.asset";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);Assert.IsNotNull(mesh);
            Assert.That(mesh.vertexCount,Is.LessThan(16000));
            Assert.That(mesh.triangles.Length/3,Is.LessThan(28000));
            var vertices=mesh.vertices;var normals=mesh.normals;var colors=mesh.colors;
            Assert.AreEqual(vertices.Length,normals.Length);Assert.AreEqual(vertices.Length,colors.Length);
            int apron=0,rocks=0,soil=0;var min=vertices[0];var max=vertices[0];
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];
                Assert.IsTrue(float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z));
                min=Vector3.Min(min,v);max=Vector3.Max(max,v);
                float radius=new Vector2(v.x,v.z-27).magnitude;
                Assert.That(radius,Is.GreaterThan(100),"Backdrop cannot encroach on the arena.");
                if(radius<180){Assert.That(v.y,Is.LessThan(4),"The arena needs a low open apron, not a nearby cliff.");apron++;}
                Assert.That(normals[i].sqrMagnitude,Is.EqualTo(1).Within(.001f));
                Assert.That(normals[i].y,Is.GreaterThan(.3f),"Terrain must face upward without folded cliff faces.");
                for(int channel=0;channel<4;channel++)Assert.That(colors[i][channel],Is.InRange(0f,1f));
                if(colors[i].a>.6f)rocks++;if(colors[i].a<.1f)soil++;
            }
            Assert.That(apron,Is.GreaterThan(500));Assert.That(rocks,Is.GreaterThan(100));Assert.That(soil,Is.GreaterThan(500));
            Assert.That(Vector3.Distance(min,mesh.bounds.min),Is.LessThan(.001f));
            Assert.That(Vector3.Distance(max,mesh.bounds.max),Is.LessThan(.001f));
            // The periodic chart duplicates its first/last vertex per ring. Verify
            // both geometry and shading meet; a matching height alone leaves a seam.
            for(int row=0;row<vertices.Length/257;row++)
            {
                int a=row*257,b=a+256;
                Assert.That(Vector3.Distance(vertices[a],vertices[b]),Is.LessThan(.001f));
                Assert.That(Vector3.Distance(normals[a],normals[b]),Is.LessThan(.001f));
                for(int c=0;c<4;c++)Assert.AreEqual(colors[a][c],colors[b][c],.001f);
            }
        }

        [Test] public void SavedLandscapeUsesReviewedMapsWithoutAddingGameplayOrLightingAuthority()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
                var root=GameObject.Find("Original western mountain ranges");Assert.IsNotNull(root);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true));
                Assert.IsEmpty(root.GetComponentsInChildren<Light>(true));
                var renderers=root.GetComponentsInChildren<MeshRenderer>();Assert.AreEqual(2,renderers.Length);
                Assert.AreEqual(3,renderers.Sum(r=>r.sharedMaterials.Length));
                foreach(var r in renderers)
                {
                    Assert.AreEqual(ShadowCastingMode.Off,r.shadowCastingMode);Assert.IsFalse(r.receiveShadows);
                    foreach(var m in r.sharedMaterials){Assert.IsNotNull(m);Assert.IsTrue(m.shader.isSupported);Assert.IsFalse(ShaderUtil.ShaderHasError(m.shader));}
                }
                var terrain=root.transform.Find("Layered eroded ridges").GetComponent<MeshRenderer>().sharedMaterial;
                Assert.AreEqual("Barrel Rivals/Distant Terrain",terrain.shader.name);
                // Compiled pass selection depends on an active render pipeline;
                // ReinsLandscapeRenderTests checks passes and pixels in Play Mode.
                Assert.AreSame(AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/RockFace_Albedo_1K.jpg"),terrain.GetTexture("_BaseMap"));
                Assert.AreSame(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Reins/Premium/Textures/ArenaSoil_Albedo_2K.png"),terrain.GetTexture("_GroundMap"));
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
