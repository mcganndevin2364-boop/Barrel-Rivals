using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    /// <summary>Persisted assets and real rider-view evidence; not a visual-quality or phone-performance verdict.</summary>
    public sealed class PremiumGraphicsTests
    {
        private static readonly string[] Stages =
        { "Ready", "Approach-1", "Turn-1", "Cross-2", "Turn-2", "Approach-3", "Turn-3", "Drive", "Finish" };

        [UnityTest]
        public IEnumerator SavedPremiumSceneRetainsMappedFootingAndNonAuthoritativeRiderTack()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName, LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<ReinsLabController>();
            Assert.IsNotNull(controller); controller.enabled = false;
            var arena = GameObject.Find("Premium rodeo arena");
            Assert.IsNotNull(arena, "Regenerate and save the premium scene before evaluating its art.");
            Assert.IsEmpty(arena.GetComponentsInChildren<Collider>(true),
                "New arena art must not add unverified gameplay collision.");
            var patchCenters = new List<Vector2>();
            // Fractional powers at the last mountain ring previously generated NaN vertices.
            foreach (var filter in arena.GetComponentsInChildren<MeshFilter>(true))
            {
                // Unity may upload static combined meshes as non-readable; inspect their resulting bounds.
                var bounds = filter.sharedMesh.bounds;
                Assert.IsTrue(Finite(bounds.center.x) && Finite(bounds.center.y) && Finite(bounds.center.z)
                    && Finite(bounds.extents.x) && Finite(bounds.extents.y) && Finite(bounds.extents.z),
                    "Invalid decorative mesh bounds: " + filter.name);
            }
            for (int i = 0; i < 6; i++)
            {
                var patch = GameObject.Find("Footing patch " + i); Assert.IsNotNull(patch);
                Assert.IsNull(patch.GetComponent<Collider>(), "A cosmetic footing patch must not become a second ground collider.");
                var filter = patch.GetComponent<MeshFilter>(); Assert.IsNotNull(filter);
                Assert.IsNotNull(filter.sharedMesh); Assert.Greater(filter.sharedMesh.vertexCount, 0);
                var renderer = patch.GetComponent<Renderer>(); Assert.IsNotNull(renderer);
                var center = new Vector2(patch.transform.position.x, patch.transform.position.z);
                var geometryCenter = new Vector2(renderer.bounds.center.x, renderer.bounds.center.z);
                Assert.Less(Vector2.Distance(center, geometryCenter), .001f,
                    "SurfaceAt samples each patch transform; its XZ origin must remain at the geometry center.");
                foreach (var previous in patchCenters)
                    Assert.Greater(Vector2.Distance(center, previous), .001f,
                        "Footing patches must retain distinct SurfaceAt sampling positions after mesh replacement.");
                patchCenters.Add(center);
                var material = renderer.sharedMaterial;
                Assert.IsNotNull(material); Assert.IsNotNull(material.shader);
                Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
                Assert.IsTrue(material.shader.isSupported);
                Assert.IsTrue(material.IsKeywordEnabled("_NORMALMAP"), "A normal texture must actually participate in shading.");
                Assert.IsTrue(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"), "The roughness-derived surface mask is not enabled.");
                foreach (string slot in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
                {
                    var texture = material.GetTexture(slot);
                    Assert.IsNotNull(texture, "Saved footing lost " + slot + " on patch " + i);
#if UNITY_EDITOR
                    Assert.IsFalse(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)),
                        "Texture must survive scene reload, not exist only as an editor-generated temporary object: " + slot);
#endif
                }
#if UNITY_EDITOR
                Assert.IsFalse(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)), "Footing material was not saved.");
                Assert.IsFalse(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)), "Footing mesh was not saved.");
#endif
            }
            var horse = GameObject.Find("Horse proxy"); Assert.IsNotNull(horse);
            var tack = horse.GetComponentInChildren<ReinsRiderTackPresentation>(true); Assert.IsNotNull(tack);
            Assert.IsTrue(tack.enabled && tack.gameObject.activeInHierarchy);
            Assert.IsEmpty(tack.GetComponentsInChildren<Collider>(true), "Rider hands/reins have no race authority.");
            Assert.AreSame(horse.GetComponent<ReinsHorsePresentation>(), Binding<ReinsHorsePresentation>(tack, "source"));
            foreach (string field in new[] { "leftHand", "rightHand", "leftGrip", "rightGrip", "leftBit", "rightBit" })
            {
                var transform = Binding<Transform>(tack, field);
                Assert.IsNotNull(transform, "Saved tack lost reference: " + field);
                Assert.IsTrue(transform.IsChildOf(horse.transform), "Rider/bit reference escaped its horse: " + field);
            }
            Assert.AreNotSame(Binding<Transform>(tack, "leftGrip"), Binding<Transform>(tack, "rightGrip"));
            var left = Binding<MeshFilter>(tack, "leftRein"); var right = Binding<MeshFilter>(tack, "rightRein");
            Assert.IsNotNull(left); Assert.IsNotNull(right); Assert.AreNotSame(left, right);
            foreach (var rein in new[] { left, right })
            {
                Assert.IsNotNull(rein.sharedMesh); Assert.Greater(rein.sharedMesh.vertexCount, 0);
                var renderer = rein.GetComponent<MeshRenderer>(); Assert.IsNotNull(renderer); Assert.IsTrue(renderer.enabled);
                Assert.IsNotEmpty(renderer.sharedMaterials); Assert.IsTrue(renderer.sharedMaterials.All(m => m && m.shader));
            }
            // Ensure the loaded references can be exercised, rather than only checking visible object names.
            tack.RenderImmediate();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CanonicalV1ReplayCapturesNinePremiumGameplayStagesWithoutChangingTheResult()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName, LoadSceneMode.Single);
            yield return null;
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType,
                "Actual Unity captures require a graphics device; a no-graphics result is not visual evidence.");
            var controller = Object.FindFirstObjectByType<ReinsLabController>();
            Assert.IsNotNull(controller); controller.enabled = false;
            var horse = GameObject.Find("Horse proxy"); Assert.IsNotNull(horse);
            var animator = horse.GetComponentInChildren<Animator>();
            var tack = horse.GetComponentInChildren<ReinsRiderTackPresentation>();
            Assert.IsNotNull(animator); Assert.IsNotNull(tack);
            Assert.IsFalse(animator.applyRootMotion, "The renderer cannot replace fixed-step race movement.");
            var camera = Camera.main; Assert.IsNotNull(camera);
            var rig = camera.GetComponent<RiderCameraRig>(); Assert.IsNotNull(rig);
            bool previousReducedMotion = rig.ReducedMotion;
            string directory = Path.Combine(Application.temporaryCachePath, "premium-graphics-" + Guid.NewGuid().ToString("N"));
            var captures = new List<CaptureEvidence>();
            var phaseCounts = new Dictionary<ReinsPhase, int>();
            var minDistances = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
            var captured = new HashSet<string>();
            controller.SetRecordDirectory(directory);
            try
            {
                rig.SetReducedMotion(true);
                controller.RefreshPresentation(); animator.Update(0); tack.RenderImmediate(); controller.RefreshPresentation();
                Capture("Ready", controller, camera, tack, captures); captured.Add("Ready");
                var fixture = JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,
                    "../Contracts/Reins/complete-request.v1.json")));
                Assert.IsNotNull(fixture.frames); Assert.IsNotEmpty(fixture.frames);
                controller.Begin();
                // No yields: animation advances exactly once per accepted 20ms replay input, not by extra Editor frames.
                foreach (var frame in fixture.frames)
                {
                    controller.Step(frame.Input()); controller.RefreshPresentation();
                    animator.Update(ReinsRun.StepMs / 1000f); tack.RenderImmediate(); controller.RefreshPresentation();
                    var run = controller.Run;
                    phaseCounts[run.Phase] = phaseCounts.TryGetValue(run.Phase, out int count) ? count + 1 : 1;
                    if (run.Phase == ReinsPhase.Racing && run.BarrelIndex < 3)
                    {
                        int barrel = run.BarrelIndex;
                        minDistances[barrel] = Math.Min(minDistances[barrel], run.DistanceToBarrel);
                        if (barrel == 0 && run.DistanceToBarrel <= 12)
                            CaptureOnce("Approach-1", controller, camera, tack, captures, captured);
                        if (barrel == 1 && run.DistanceToBarrel >= 15)
                            CaptureOnce("Cross-2", controller, camera, tack, captures, captured);
                        if (barrel == 2 && run.DistanceToBarrel <= 12)
                            CaptureOnce("Approach-3", controller, camera, tack, captures, captured);
                        // The canonical v1 fixture uses approximately 3.4m Risk turns; a 2.5m assertion would invent a new route.
                        if (run.TurnActive && run.DistanceToBarrel <= 3.5 && run.TurnProgress01 >= .20)
                            CaptureOnce("Turn-" + (barrel + 1), controller, camera, tack, captures, captured);
                    }
                    if (run.Phase == ReinsPhase.Drive && run.PhaseElapsedMs >= 500)
                        CaptureOnce("Drive", controller, camera, tack, captures, captured);
                }
                Assert.AreEqual(ReinsPhase.Complete, controller.Run.Phase);
                Assert.AreEqual(35120L, controller.Run.FinalTimeMs);
                Assert.AreEqual(0, controller.Run.KnockCount); Assert.AreEqual(300, controller.Run.StylePoints);
                CaptureOnce("Finish", controller, camera, tack, captures, captured);
                foreach (string stage in Stages) Assert.IsTrue(captured.Contains(stage), "Missing canonical gameplay stage: " + stage);
                Assert.AreEqual(Stages.Length, captures.Count);
                var evidence = new RunEvidence
                {
                    unityVersion = Application.unityVersion, ruleset = "reins-lab-v1",
                    rulesFingerprint = ReinsRuleFingerprint.Sha256, finalPhase = controller.Run.Phase.ToString(),
                    finalTimeMs = controller.Run.FinalTimeMs, knocks = controller.Run.KnockCount,
                    stylePoints = controller.Run.StylePoints, replayFrames = fixture.frames.Length,
                    captureWidth = 1280, captureHeight = 720, reducedMotion = true,
                    graphicsDeviceName = SystemInfo.graphicsDeviceName, graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
                    graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(), graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                    reportedGraphicsMemoryMB = SystemInfo.graphicsMemorySize, operatingSystem = SystemInfo.operatingSystem,
                    devicePerformanceMeasured = false, visualQualityAcceptance = false,
                    minimumBarrelDistancesMetres = minDistances, captures = captures.ToArray(),
                    phaseFrameCounts = phaseCounts.Select(p => new PhaseEvidence { phase = p.Key.ToString(), frames = p.Value }).ToArray()
                };
                File.WriteAllText(Path.Combine(EvidenceDirectory(), "PremiumGraphics-Run.json"), JsonUtility.ToJson(evidence, true) + "\n");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                rig.SetReducedMotion(previousReducedMotion);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static T Binding<T>(ReinsRiderTackPresentation tack, string name) where T : Object
        {
            var field = typeof(ReinsRiderTackPresentation).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Serialized tack binding disappeared: " + name);
            return field.GetValue(tack) as T;
        }

        private static void CaptureOnce(string stage, ReinsLabController controller, Camera camera,
            ReinsRiderTackPresentation tack, List<CaptureEvidence> captures, HashSet<string> captured)
        {
            if (captured.Contains(stage)) return;
            Capture(stage, controller, camera, tack, captures); captured.Add(stage);
        }

        private static void Capture(string stage, ReinsLabController controller, Camera camera,
            ReinsRiderTackPresentation tack, List<CaptureEvidence> captures)
        {
            tack.RenderImmediate(); controller.RefreshPresentation();
            var horse = GameObject.Find("Horse proxy").transform;
            Assert.Less(Vector3.Distance(horse.position, camera.transform.position), 4,
                "Evidence must use the actual rider camera rather than a disconnected art-preview camera.");
            Assert.Greater(Vector3.Dot(camera.transform.up, Vector3.up), .95f);
            var canvas = GameObject.Find("Reins HUD").GetComponent<Canvas>();
            Assert.IsTrue(canvas.enabled && canvas.gameObject.activeInHierarchy);
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance; var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + .01f;
                Canvas.ForceUpdateCanvases();
                // Changing the canvas render target invalidates cached screen-space text geometry.
                foreach (var label in canvas.GetComponentsInChildren<Text>(true))
                { label.cachedTextGenerator.Invalidate(); label.SetAllDirty(); }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(EvidenceDirectory(), "PremiumGraphics-" + stage + ".png"), image.EncodeToPNG());
                var run = controller.Run;
                captures.Add(new CaptureEvidence
                {
                    stage = stage, phase = run.Phase.ToString(), tick = run.Tick, raceTimeMs = run.RaceTimeMs,
                    barrelIndex = run.BarrelIndex, distanceToTargetMetres = run.DistanceToBarrel,
                    turnProgress01 = run.TurnProgress01, speedMetresPerSecond = run.SpeedMetresPerSecond,
                    horsePosition = horse.position, cameraPosition = camera.transform.position,
                    cameraFieldOfView = camera.fieldOfView
                });
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                Object.Destroy(target); Object.Destroy(image); Canvas.ForceUpdateCanvases();
            }
        }

        private static string EvidenceDirectory()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Evidence"));
            Directory.CreateDirectory(directory); return directory;
        }

        [Serializable] private sealed class RunEvidence
        {
            public string unityVersion, ruleset, rulesFingerprint, finalPhase;
            public string graphicsDeviceName, graphicsDeviceVendor, graphicsDeviceType, graphicsDeviceVersion, operatingSystem;
            public long finalTimeMs;
            public int knocks, stylePoints, replayFrames, captureWidth, captureHeight, reportedGraphicsMemoryMB;
            public bool reducedMotion, devicePerformanceMeasured, visualQualityAcceptance;
            public double[] minimumBarrelDistancesMetres;
            public CaptureEvidence[] captures;
            public PhaseEvidence[] phaseFrameCounts;
        }
        [Serializable] private sealed class CaptureEvidence
        {
            public string stage, phase;
            public int tick, barrelIndex;
            public long raceTimeMs;
            public double distanceToTargetMetres, turnProgress01, speedMetresPerSecond;
            public Vector3 horsePosition, cameraPosition;
            public float cameraFieldOfView;
        }
        [Serializable] private sealed class PhaseEvidence { public string phase; public int frames; }
        [Serializable] private sealed class Fixture { public Frame[] frames; }
        [Serializable] private sealed class Frame
        {
            public int leftPermille, rightPermille;
            public bool cadenceTap, gateTap, wrap;
            public string drive;
            public ReinsInput Input() => new ReinsInput(leftPermille, rightPermille, cadenceTap, gateTap, wrap,
                (DriveSide)Enum.Parse(typeof(DriveSide), drive));
        }
    }
}
