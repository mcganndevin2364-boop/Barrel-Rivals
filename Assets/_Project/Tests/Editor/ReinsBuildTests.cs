using System.Linq;
using BarrelRivals.Core.Reins;
using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BarrelRivals.Tests
{
    public sealed class ReinsBuildTests
    {
        [Test] public void ReinsV2IsTheOnlyMobileRaceEntryAndItsFinishIsInvisible()
        {
            CollectionAssert.AreEqual(new[]{ReinsLabBuilder.ScenePath,StableBuilder.ScenePath},
                EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray());
            Assert.AreEqual("0.5.0",PlayerSettings.bundleVersion);
            Assert.AreEqual("5",PlayerSettings.iOS.buildNumber);
            Assert.AreEqual(5,PlayerSettings.Android.bundleVersionCode);
            EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
            Assert.IsNull(GameObject.Find("Score line"));
            Assert.IsNull(GameObject.Find("Classic practice"));
            Assert.IsNull(GameObject.Find("Begin"),"The center hold owns the launch; no separate click-start path.");
        }

        [Test] public void SavedAlleyRailSurfaceMatchesSharedWallBoundsAndClearsTheFinish()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(ReinsPremiumArenaBuilder.Root+"/Meshes/Arena Galvanized arena rails.asset");
            Assert.IsNotNull(mesh);
            // Ordinary perimeter rails are outside this x/z region. Inspect the real saved
            // batched vertex data, not generator intent or empty marker transforms.
            var alley=mesh.vertices.Where(v=>Mathf.Abs(v.x)<4 && v.z<0).ToArray();
            Assert.Greater(alley.Length,100);
            Assert.That(alley.Min(v=>v.x),Is.EqualTo(-ReinsAlley.HalfWidth-ReinsAlley.RailRadius).Within(.002));
            Assert.That(alley.Max(v=>v.x),Is.EqualTo(ReinsAlley.HalfWidth+ReinsAlley.RailRadius).Within(.002));
            Assert.That(alley.Min(v=>v.z),Is.EqualTo(ReinsAlley.BackZ-ReinsAlley.RailRadius).Within(.002));
            Assert.That(alley.Max(v=>v.z),Is.EqualTo(ReinsAlley.FrontZ+ReinsAlley.RailRadius).Within(.002));
            Assert.LessOrEqual(alley.Max(v=>v.z)+ReinsCourseJudge.HorseRadius,-1,
                "Rail caps and horse radius must leave the complete score plane clear.");
        }
    }
}
