using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarrelRivals.Tests
{
    public sealed class ReinsPostProcessingTests
    {
        [Test]
        public void ReloadedPremiumRendererRetainsPostProcessingResources()
        {
            const string pipelinePath=ReinsPremiumArenaBuilder.Root+"/Premium mobile pipeline.asset";
            const string rendererPath=ReinsPremiumArenaBuilder.Root+"/Premium mobile renderer.asset";
            const string postPath="Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
            AssetDatabase.ImportAsset(rendererPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(pipelinePath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var pipeline=AssetDatabase.LoadAssetAtPath<ScriptableObject>(pipelinePath);
            Assert.IsNotNull(pipeline,"Regenerate the Reins presentation before running this regression.");
            var pipelineData=new SerializedObject(pipeline);
            var renderers=pipelineData.FindProperty("m_RendererDataList");
            int index=pipelineData.FindProperty("m_DefaultRendererIndex").intValue;
            Assert.That(index,Is.InRange(0,renderers.arraySize-1));
            var renderer=renderers.GetArrayElementAtIndex(index).objectReferenceValue;
            Assert.IsNotNull(renderer);
            Assert.AreEqual(rendererPath,AssetDatabase.GetAssetPath(renderer),"The premium pipeline must not reuse or mutate the legacy renderer.");
            Assert.IsTrue(EditorUtility.IsPersistent(renderer));

            // URP disables its LUT, grading and final FXAA pass when this resource
            // is absent, even when the camera and Volume have post effects enabled.
            var post=new SerializedObject(renderer).FindProperty("postProcessData").objectReferenceValue;
            Assert.IsNotNull(post,"Missing PostProcessData silently disables the configured ACES grade.");
            Assert.IsTrue(EditorUtility.IsPersistent(post));
            Assert.AreEqual(postPath,AssetDatabase.GetAssetPath(post));
            var resources=new SerializedObject(post);
            foreach(string field in new[]{"lutBuilderHdrPS","uberPostPS","finalPostPassPS"})
            {
                var property=resources.FindProperty("shaders."+field);
                Assert.IsNotNull(property,field);
                var shader=property.objectReferenceValue as Shader;
                Assert.IsNotNull(shader,"Missing URP post shader: "+field);
                Assert.IsTrue(EditorUtility.IsPersistent(shader),"Post shaders must survive scene reload and player export.");
            }
        }
    }
}
