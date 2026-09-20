using System;
using System.Linq;
using BarrelRivals.Core;
using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BarrelRivals.Tests
{
    public sealed class FoundationTests
    {
        [Test] public void RngVersionOneMatchesKnownSequence()
        {
            var rng = new DeterministicRng(1);
            uint[] expected = {270369u,67634689u,2647435461u,307599695u,2398689233u};
            foreach (uint value in expected) Assert.AreEqual(value,rng.NextUInt());
        }
        [Test] public void ZeroSeedIsReproducibleAndNonzero()
        {
            var left = new DeterministicRng(0); var right = new DeterministicRng(0);
            for (int i=0;i<100;i++) { uint value=left.NextUInt(); Assert.AreNotEqual(0u,value); Assert.AreEqual(value,right.NextUInt()); }
        }
        [Test] public void SeparateStreamsDoNotAffectEachOther()
        {
            var gameplay = new DeterministicRng(45); var reference = new DeterministicRng(45);
            var cosmetic = new DeterministicRng(7);
            for (int i=0;i<100;i++) { cosmetic.NextUInt(); cosmetic.NextUInt(); Assert.AreEqual(reference.NextUInt(),gameplay.NextUInt()); }
        }
        [Test] public void RangeHandlesNegativeAndExtremeBounds()
        {
            var rng = new DeterministicRng(923);
            for (int i=0;i<10000;i++)
            {
                Assert.That(rng.Range(-15,15),Is.InRange(-15f,15f));
                float value = rng.Range(-float.MaxValue,float.MaxValue);
                Assert.IsFalse(float.IsNaN(value) || float.IsInfinity(value));
                Assert.That(rng.NextUnitFloat(),Is.GreaterThanOrEqualTo(0).And.LessThan(1));
            }
        }
        [Test] public void InvalidBoundsAreRejected()
        {
            var rng = new DeterministicRng(1);
            Assert.Throws<ArgumentOutOfRangeException>(()=>rng.Range(float.NaN,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>rng.Range(1,float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(()=>rng.Range(5,4));
        }
        [Test] public void FixedRangeDoesNotConsumeTheStream()
        {
            var rng = new DeterministicRng(1);
            Assert.AreEqual(3f,rng.Range(3,3)); Assert.AreEqual(270369u,rng.NextUInt());
        }
        [Test] public void CourseUsesStandardCenterDistances()
        {
            var a=StandardCourse.Barrel(0); var b=StandardCourse.Barrel(1); var c=StandardCourse.Barrel(2);
            Assert.That(Distance(a,b),Is.EqualTo(27.432).Within(0.000001));
            Assert.That(Distance(a,c),Is.EqualTo(32.004).Within(0.000001));
            Assert.That(Distance(b,c),Is.EqualTo(32.004).Within(0.000001));
            Assert.That(a.Z,Is.EqualTo(18.288).Within(0.000001));
            Assert.That(b.Z,Is.EqualTo(a.Z));
        }
        private static double Distance(StandardCourse.Point a,StandardCourse.Point b) => Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Z-b.Z,2));
        [Test] public void UnknownBarrelIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>StandardCourse.Barrel(-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>StandardCourse.Barrel(3));
        }
        [Test] public void SavedSceneHasValidBindingsAndPersistentMaterials() => FoundationBuilder.Validate();
        [Test] public void EveryQualityTierUsesTheSavedPipeline()
        {
            bool reinsEnabled=EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==ReinsLabBuilder.ScenePath);
            bool practiceEnabled=EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==PracticeBuilder.ScenePath);
            string expectedPath=reinsEnabled ? ReinsPremiumArenaBuilder.Root+"/Premium mobile pipeline.asset"
                : practiceEnabled ? PracticePresentationBuilder.Root+"/Practice mobile pipeline.asset" : FoundationBuilder.PipelinePath;
            var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(expectedPath);
            Assert.IsNotNull(pipeline); Assert.AreEqual(pipeline,GraphicsSettings.defaultRenderPipeline);
            int previous=QualitySettings.GetQualityLevel();
            try { for(int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i,false); Assert.AreEqual(pipeline,QualitySettings.renderPipeline); } }
            finally { QualitySettings.SetQualityLevel(previous,false); }
        }
    }
}
