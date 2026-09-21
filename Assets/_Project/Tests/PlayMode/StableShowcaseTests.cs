using System;
using System.Collections;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class StableShowcaseTests
    {
        string directory;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BarrelShowcase-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); StableSession.UseForTests(new StableProfileStore(directory));
        }
        [TearDown] public void TearDown()
        {
            StableSession.UseForTests(null); if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        [UnityTest] public IEnumerator SavedDrapesFollowIdleAndOwnTheirMeshesAcrossVisibilityAndDestruction()
        {
            yield return SceneManager.LoadSceneAsync(StableController.SceneName); yield return null;
            var horse = GameObject.Find("Copper"); var model = horse.transform.Find("Reference horse");
            var drapes = horse.GetComponentsInChildren<StableReinDrape>(); Assert.AreEqual(2, drapes.Length);
            var animator = horse.GetComponentInChildren<Animator>(); animator.enabled = false;
            var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle"));
            var body = horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s => s.name.StartsWith("HorseBody", StringComparison.Ordinal));
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
                    if (hair) hair.RenderImmediate(); foreach (var d in drapes) d.RenderImmediate();
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
                        int side = d.name.StartsWith("Left", StringComparison.Ordinal) ? -1 : 1;
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
                string output = Environment.GetEnvironmentVariable("BARREL_STABLE_SHOWCASE_OUTPUT");
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
            yield return SceneManager.LoadSceneAsync(StableController.SceneName); yield return null;
            var controller = Object.FindFirstObjectByType<StableController>(); var horse = GameObject.Find("Copper");
            var animator = horse.GetComponentInChildren<Animator>(); animator.enabled = false;
            var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle"));
            var drapes = horse.GetComponentsInChildren<StableReinDrape>(); var hair = horse.GetComponentInChildren<ReinsHairMotion>();
            var skins = horse.GetComponentsInChildren<SkinnedMeshRenderer>(); var flags = skins.Select(s => s.forceMatrixRecalculationPerRender).ToArray();
            var camera = Camera.main; var canvas = GameObject.Find("Stable HUD").GetComponent<Canvas>();
            string output = Environment.GetEnvironmentVariable("BARREL_STABLE_SHOWCASE_OUTPUT");
            if (!string.IsNullOrEmpty(output)) Directory.CreateDirectory(Path.Combine(output, "frames"));
            try
            {
                foreach (var skin in skins) skin.forceMatrixRecalculationPerRender = true;
                void Capture(string file, int width = 1280, int height = 720)
                {
                    var image = OverlayEvidenceCapture.Render(camera, canvas, width, height, () => {
                        controller.RefreshLayout(width, height, new Rect(0, 0, width, height));
                        if (file.StartsWith("stable", StringComparison.Ordinal))
                        {
                            var body = skins.Single(s => s.name.StartsWith("HorseBody", StringComparison.Ordinal)); var measured = new Mesh();
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
                    if (hair) hair.RenderImmediate(); foreach (var d in drapes) d.RenderImmediate();
                    Capture("frames/" + (i + 1).ToString("D3") + ".png", 960, 540);
                    if (i % 20 == 0) yield return null;
                }
                controller.ShowGear(); Capture("saddles.png"); controller.ShowRiderGear(); Capture("gloves.png");
                controller.ShowStable();
                Assert.AreEqual(3, Object.FindObjectsByType<Light>().Length, "Showcase retains its bounded lighting count.");
                if (!string.IsNullOrEmpty(output))
                {
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
