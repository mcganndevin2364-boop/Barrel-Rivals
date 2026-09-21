using System;
using System.Collections;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using BarrelRivals.Core.Stable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class HeroHorseStableTests
    {
        const string Scene = "Assets/_Project/Development/HeroHorse/Stable/HeroHorseStableReview.unity";
        const string RaceScene = "Assets/_Project/Development/HeroHorse/HeroHorseRaceReview.unity";
        string directory;
        static IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
#else
            Assert.Ignore("Development review is excluded from mobile builds."); yield break;
#endif
        }
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BarrelHeroStable-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); StableSession.UseForTests(new StableProfileStore(directory));
        }
        [TearDown] public void TearDown()
        {
            StableSession.UseForTests(null); if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [UnityTest] public IEnumerator SavedDrapesFollowIdleAndOwnTheirMeshesAcrossVisibilityAndDestruction()
        {
            yield return Load();
            var horse = GameObject.Find("Copper"); var model = horse.GetComponentInChildren<HorseRigBindings>().ModelSpace;
            var drapes = horse.GetComponentsInChildren<StableReinDrape>(); Assert.AreEqual(2, drapes.Length);
            var animator = horse.GetComponentInChildren<Animator>(); animator.enabled = false;
            var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle"));
            var body = horse.GetComponentInChildren<HorseRigBindings>().Body;
            var hair = horse.GetComponentInChildren<ReinsHairMotion>();
            var root = horse.transform.localToWorldMatrix;
            var mesh = new Mesh(); var shell = new Mesh(); var contact = new GameObject("Test-only rein clearance").AddComponent<MeshCollider>();
            float maximumEndError = 0, minimumSideGap = float.PositiveInfinity, motion = 0;
            Vector3[] initial = null; int surfaceSamples = 0;
            try
            {
                for (int frame = 0; frame < 48; frame++)
                {
                    idle.SampleAnimation(animator.gameObject, idle.length * frame / 48f);
                    horse.GetComponentInChildren<HeroHorseGrounding>().RenderImmediate(); if (hair) hair.RenderImmediate(); foreach (var d in drapes) d.RenderImmediate();
                    body.BakeMesh(mesh, true);
                    var toModel = model.worldToLocalMatrix * body.localToWorldMatrix;
                    shell.vertices = mesh.vertices.Select(toModel.MultiplyPoint3x4).ToArray(); shell.triangles = mesh.triangles;
                    contact.sharedMesh = null; contact.sharedMesh = shell; Physics.SyncTransforms();
                    foreach (var d in drapes)
                    {
                        var vertices = d.Instance.vertices;
                        Vector3 Center(int ring)
                        { var p = Vector3.zero; for (int j = 0; j < StableReinDrape.Sides; j++) p += vertices[ring * StableReinDrape.Sides + j]; return p / StableReinDrape.Sides; }
                        maximumEndError = Mathf.Max(maximumEndError, Vector3.Distance(d.transform.TransformPoint(Center(0)), d.StartWorld),
                            Vector3.Distance(d.transform.TransformPoint(Center(d.KnotCount - 1)), d.EndWorld));
                        int side = d.name.Contains("left") ? -1 : 1;
                        for (int i = 3; i < d.KnotCount - 3; i++)
                        {
                            var point = model.InverseTransformPoint(d.transform.TransformPoint(Center(i)));
                            if (contact.Raycast(new Ray(new Vector3(side * 2, point.y, point.z), Vector3.left * side), out var hit, 2))
                            { minimumSideGap = Mathf.Min(minimumSideGap, side * (point.x - hit.point.x)); surfaceSamples++; }
                        }
                    }
                    var current = drapes[0].Instance.vertices;
                    if (initial == null) initial = current;
                    else for (int i = 0; i < current.Length; i++) motion = Mathf.Max(motion, Vector3.Distance(initial[i], current[i]));
                    Assert.AreEqual(root, horse.transform.localToWorldMatrix, "Presentation must not move the horse root.");
                }
                Assert.That(maximumEndError, Is.LessThan(.00001f)); Assert.That(motion, Is.GreaterThan(.0005f));
                Assert.That(surfaceSamples, Is.GreaterThan(100));
                Assert.That(minimumSideGap, Is.GreaterThan(StableReinDrape.Radius + .002f), "Idle cord must remain outside sampled body cross-sections.");
                var owned = drapes.Select(d => d.Instance).ToArray(); var before = owned[0].vertices;
                horse.SetActive(false); horse.SetActive(true); foreach (var d in drapes) d.RenderImmediate();
                Assert.AreSame(owned[0], drapes[0].Instance, "Tab switches should reuse the private mesh.");
                var clone = Object.Instantiate(horse); clone.name = "Independent showroom copy";
                var copies = clone.GetComponentsInChildren<StableReinDrape>(); foreach (var d in copies) d.RenderImmediate();
                Assert.AreNotSame(owned[0], copies[0].Instance); Assert.AreNotSame(owned[1], copies[1].Instance);
                var copiedMeshes = copies.Select(d => d.Instance).ToArray(); Object.Destroy(clone);
                yield return null; yield return null;
                foreach (var m in copiedMeshes) Assert.IsTrue(m == null, "Destroyed showroom must release its private rein meshes.");
                Assert.IsTrue(before.SequenceEqual(owned[0].vertices), "Other instances must not alter the source horse's rein.");
                string output = Environment.GetEnvironmentVariable("BARREL_HERO_STABLE_OUTPUT");
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, "drape.json"), JsonUtility.ToJson(new DrapeProof { maximumEndpointErrorM = maximumEndError,
                        minimumSampledSideGapM = minimumSideGap, vertexTravelM = motion, crossSectionSamples = surfaceSamples }, true) + "\n");
                }
            }
            finally { Object.Destroy(contact.gameObject); Object.Destroy(mesh); Object.Destroy(shell); }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActualSavedShowroomFramesHorseAndRendersMovingInspection()
        {
            yield return Load();
            var controller = Object.FindFirstObjectByType<StableController>(); var horse = GameObject.Find("Copper");
            var animator = horse.GetComponentInChildren<Animator>(); animator.enabled = false;
            var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle"));
            var drapes = horse.GetComponentsInChildren<StableReinDrape>(); var hair = horse.GetComponentInChildren<ReinsHairMotion>();
            var skins = horse.GetComponentsInChildren<SkinnedMeshRenderer>(); var flags = skins.Select(s => s.forceMatrixRecalculationPerRender).ToArray();
            var camera = Camera.main; var canvas = GameObject.Find("Stable HUD").GetComponent<Canvas>();
            string output = Environment.GetEnvironmentVariable("BARREL_HERO_STABLE_OUTPUT");
            if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(output);
            try
            {
                foreach (var skin in skins) skin.forceMatrixRecalculationPerRender = true;
                void Capture(string file, int width = 1280, int height = 720)
                {
                    var image = OverlayEvidenceCapture.Render(camera, canvas, width, height, () => {
                        controller.RefreshLayout(width, height, new Rect(0, 0, width, height));
                        if (file.StartsWith("stable", StringComparison.Ordinal))
                        {
                            var body = horse.GetComponentInChildren<HorseRigBindings>().Body; var measured = new Mesh();
                            try
                            {
                                body.BakeMesh(measured, true);
                                var projected = measured.vertices.Select(v => camera.WorldToViewportPoint(body.transform.TransformPoint(v))).ToArray();
                                Assert.That(projected.Min(p => p.x), Is.GreaterThan(.205f), "Horse must clear the roster.");
                                Assert.That(projected.Max(p => p.x), Is.LessThan(.765f), "Horse must clear the traits panel.");
                                Assert.That(projected.Min(p => p.y), Is.GreaterThan(.245f), "Hooves must clear the action buttons.");
                                Assert.That(projected.Max(p => p.y), Is.LessThan(.89f), "Ears must clear the header.");
                            }
                            finally { Object.DestroyImmediate(measured); }
                        }
                    });
                    try { if (!string.IsNullOrEmpty(output)) File.WriteAllBytes(Path.Combine(output, file), image.EncodeToPNG()); }
                    finally { Object.DestroyImmediate(image); }
                }
                idle.SampleAnimation(animator.gameObject, 0); foreach (var d in drapes) d.RenderImmediate();
                Capture("stable.png"); Capture("stable-tablet.png", 1024, 768);
                int frames = string.IsNullOrEmpty(output) ? 1 : 120;
                for (int i = 0; i < frames; i++)
                {
                    idle.SampleAnimation(animator.gameObject, Mathf.Repeat(i / 30f, idle.length));
                    horse.GetComponentInChildren<HeroHorseGrounding>().RenderImmediate(); if (hair) hair.RenderImmediate(); foreach (var d in drapes) d.RenderImmediate();
                    Capture("frame-" + i.ToString("D4") + ".png", 960, 540);
                    if (i % 20 == 0) yield return null;
                }
                controller.ShowGear(); Capture("saddles.png"); controller.ShowRiderGear(); Capture("gloves.png");
                controller.ShowStable();
                Assert.AreEqual(3, Object.FindObjectsByType<Light>().Length, "Showcase retains its bounded lighting count.");
                if (!string.IsNullOrEmpty(output))
                {
                    File.WriteAllText(Path.Combine(output,"capture.json"), "{\"width\":960,\"height\":540,\"framesPerSecond\":30,\"frameCount\":" + frames + "}\n");
                    var renderers = horse.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                    int triangles = renderers.Sum(r => { var mesh = r is SkinnedMeshRenderer skin ? skin.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh; return mesh ? Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i) / 3) : 0; });
                    File.WriteAllText(Path.Combine(output, "showcase.json"), JsonUtility.ToJson(new ShowcaseProof {
                        clip = idle.name, clipDurationSeconds = idle.length, frames = frames, characterTriangles = triangles,
                        renderers = renderers.Length, materialSlots = renderers.Sum(r => r.sharedMaterials.Length) }, true) + "\n");
                }
            }
            finally { for (int i = 0; i < skins.Length; i++) if (skins[i]) skins[i].forceMatrixRecalculationPerRender = flags[i]; controller.RefreshLayout(); }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SavedReviewNavigationKeepsEquippedGearAndUsesMatchingThumbnails()
        {
            yield return Load();
            var stable=Object.FindFirstObjectByType<StableController>();
#if UNITY_EDITOR
            Assert.IsFalse(UnityEditor.EditorBuildSettings.scenes.Any(s=>s.path==Scene || s.path==RaceScene));
            var images=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Image>(true));
            var paths=images.Where(i=>i.sprite).Select(i=>UnityEditor.AssetDatabase.GetAssetPath(i.sprite)).ToArray();
            Assert.IsFalse(paths.Any(p=>p.StartsWith("Assets/_Project/Art/Reins/Stable/Thumbnails/",StringComparison.Ordinal)));
            Assert.AreEqual(21,paths.Where(p=>p.StartsWith("Assets/_Project/Development/HeroHorse/Stable/Thumbnails/",StringComparison.Ordinal)).Distinct().Count());
#endif
            var appearance=GameObject.Find("Copper").GetComponent<StableAppearance>();
            Click("Gear tab");Click("Filter Pad");Click("Select pad-turquoise");
            Assert.AreEqual("pad-turquoise",appearance.Applied.padId);Assert.AreEqual("pad-desert",stable.Equipped.padId);
            Click("MyStable tab");Assert.AreEqual("pad-desert",appearance.Applied.padId);
            Click("Gear tab");Click("Filter Pad");Click("Select pad-turquoise");Click("Equip selected");
            Click("Filter Reins");Click("Select reins-crimson");Click("Equip selected");
            Click("Filter Headstall");Click("Select headstall-midnight");Click("Equip selected");
            Click("Rider gear tab");Click("Select gloves-rodeo-red");Click("Equip rider selected");
            StableSession.UseForTests(new StableProfileStore(directory));
            yield return Load();stable=Object.FindFirstObjectByType<StableController>();
            Assert.AreEqual("pad-turquoise",stable.Equipped.padId);Assert.AreEqual("gloves-rodeo-red",stable.Equipped.glovesId);
            Click("Rider gear tab");Click("Select gloves-blackout");Click("Race");yield return null;yield return null;
            Assert.AreEqual(RaceScene,SceneManager.GetActiveScene().path);
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();Assert.IsNotNull(horse);
            var applied=GameObject.Find("Horse proxy").GetComponent<StableAppearance>().Applied;
            Assert.AreEqual("pad-turquoise",applied.padId);Assert.AreEqual("reins-crimson",applied.reinsId);
            Assert.AreEqual("headstall-midnight",applied.headstallId);Assert.AreEqual("gloves-rodeo-red",applied.glovesId);
            Assert.AreEqual("Crimson rein braid",horse.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Fitted left rein").sharedMaterials[1].name);
            Assert.AreEqual(2,horse.GetComponentsInChildren<Renderer>().Count(r=>r.name=="Glove shell" && r.sharedMaterial.name=="Rodeo red gloves"));
            var race=Object.FindFirstObjectByType<ReinsLabController>();race.enabled=false;race.Begin();race.OpenStable();yield return null;
            Assert.AreEqual(RaceScene,SceneManager.GetActiveScene().path,"Gear navigation must not interrupt an active attempt.");
            race.CancelRun();race.OpenStable();yield return null;yield return null;
            Assert.AreEqual(Scene,SceneManager.GetActiveScene().path);Assert.IsNotNull(Object.FindFirstObjectByType<HorseRigBindings>());
            Assert.AreEqual("gloves-rodeo-red",Object.FindFirstObjectByType<StableController>().Equipped.glovesId);
            LogAssert.NoUnexpectedReceived();
        }
        static void Click(string name)
        {
            var item=GameObject.Find(name);Assert.IsNotNull(item,name);
            var button=item.GetComponent<Button>();Assert.IsTrue(button.interactable,name);button.onClick.Invoke();
        }
        [Serializable] class ShowcaseProof
        {
            public string clip; public float clipDurationSeconds; public int frames, characterTriangles, renderers, materialSlots;
            public int capturedWidth = 960, capturedHeight = 540; public bool phoneFootage = false;
            public string scope = "Actual saved stable and gear with real UI. Idle sampled at 30 Hz for four seconds; encoded frame rate is not a measurement of runtime performance.";
        }
        [Serializable] class DrapeProof
        {
            public int poses = 48, crossSectionSamples; public float maximumEndpointErrorM, minimumSampledSideGapM, vertexTravelM;
            public string scope = "Saved MyStable Idle samples and independent mesh lifetime. Cross-section clearance is not full continuous collision or device performance.";
        }
    }
}
