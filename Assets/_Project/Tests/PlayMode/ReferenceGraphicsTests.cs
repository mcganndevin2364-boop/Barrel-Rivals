using System;
using System.Collections;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    /// <summary>Saved-scene import and real Unity rendering evidence, not a device performance benchmark.</summary>
    public sealed class ReferenceGraphicsTests
    {
        [UnityTest]
        public IEnumerator PersistedReinsSceneContainsTheRiggedHorseAndUsableMaterials()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName, LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<ReinsLabController>(); controller.enabled = false;
            controller.ResetRun(); controller.RefreshPresentation();
            var model = ReferenceHorse();
            var animator = model.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "The saved scene must retain the imported character animator.");
            Assert.IsNotNull(animator.runtimeAnimatorController);
            Assert.IsFalse(animator.applyRootMotion, "Core owns horse position and scoring.");
            Assert.IsNotNull(animator.avatar); Assert.IsTrue(animator.avatar.isValid);
            Assert.IsTrue(animator.parameters.Any(p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float));
            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (string gait in new[] { "Idle", "Walk", "Gallop" })
            {
                var clip = clips.FirstOrDefault(c => c && c.name.Contains(gait));
                Assert.IsNotNull(clip, "Missing imported gait: " + gait);
                Assert.Greater(clip.length, .1f); Assert.IsTrue(clip.isLooping, gait + " must not stop after one cycle.");
            }
            Assert.IsEmpty(model.GetComponentsInChildren<Collider>(true), "Imported art cannot add competing collision geometry.");
            Assert.IsFalse(GameObject.Find("Horse proxy").GetComponent<PracticeHorseVisual>().enabled,
                "The old procedural horse animator must not compete with the imported rig.");
            Assert.IsNotNull(model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Rider seat anchor"));
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.IsNotEmpty(skins);
            foreach (var skin in skins)
            {
                Assert.IsNotNull(skin.sharedMesh); Assert.IsTrue(skin.enabled);
                Assert.Greater(skin.sharedMesh.vertexCount, 0); Assert.IsNotEmpty(skin.bones);
                foreach (var bone in skin.bones) Assert.IsTrue(bone && bone.IsChildOf(model), "Skin bone reference escaped its character.");
                foreach (var material in skin.sharedMaterials)
                {
                    AssertMaterial(material, skin.name);
                    // The original felt hat uses an authored solid material. All
                    // imported horse and rider surfaces must retain their maps.
                    if (!(skin.name == "Original cowboy hat" && skin.GetComponentInParent<ReinsRiderBodyPresentation>()))
                        Assert.IsNotNull(material.mainTexture, "Imported character material lost its source texture: " + material.name);
                }
            }
            animator.Update(0);
            var head = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bone.002");
            Assert.IsNotNull(head, "The imported head bone must be available for the rider-facing check.");
            Vector3 headInModel = model.InverseTransformPoint(head.position);
            var body = skins.FirstOrDefault(s => s.name.StartsWith("HorseBody"));
            Assert.IsNotNull(body, "The imported body must retain a distinct skinned renderer.");
            var baked = new Mesh();
            try
            {
                // Use the actual posed vertices: imported animation-culling bounds can be deliberately oversized.
                // Compensate imported renderer scale before TransformPoint applies the full hierarchy.
                // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html
                body.BakeMesh(baked, true);
                var points = baked.vertices; Assert.IsNotEmpty(points);
                var bounds = new Bounds(body.transform.TransformPoint(points[0]), Vector3.zero);
                for (int i = 1; i < points.Length; i++) bounds.Encapsulate(body.transform.TransformPoint(points[i]));
                WriteEvidence("ArtInspection", new ArtEvidence
                {
                    unityVersion = Application.unityVersion, posedBodyBoundsMetres = bounds.size,
                    headPositionInModelMetres = headInModel, gaitClips = clips.Select(c => c.name).ToArray()
                });
                Assert.That(bounds.size.y, Is.InRange(1.75f, 2.65f),
                    "Expected a roughly 2.12m horse; inspect FBX root conversion if metres/orientation were lost.");
                Assert.Greater(headInModel.z, 0,
                    "The head must face ahead of the rider in +Z; FBX axis conversion must not show the rump while racing forward.");
            }
            finally { Object.Destroy(baked); }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.IsNotNull(script, "The saved Reins scene contains a missing script.");
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                        foreach (var material in renderer.sharedMaterials) AssertMaterial(material, renderer.name);
            }
            Assert.IsNotNull(Camera.main.GetComponent<RiderCameraRig>());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CanonicalRunRendersRiderViewAtEveryBarrelAndDuringDrive()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName, LoadSceneMode.Single);
            yield return null;
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType,
                "Reference screenshots require a graphics device; a no-graphics run is not rendering evidence.");
            var controller = Object.FindFirstObjectByType<ReinsLabController>(); controller.enabled = false;
            var model = ReferenceHorse(); var animator = model.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator); Assert.IsNotNull(animator.runtimeAnimatorController);
            var camera = Camera.main; var rig = camera.GetComponent<RiderCameraRig>();
            bool originalReducedMotion = rig.ReducedMotion;
            string directory = Path.Combine(Application.temporaryCachePath, "reference-graphics-" + Guid.NewGuid().ToString("N"));
            controller.SetRecordDirectory(directory);
            try
            {
                // This is a real user setting. Fixing it makes framing independent of the Editor's last frame time.
                rig.SetReducedMotion(true);
                controller.RefreshPresentation(); animator.Update(0);
                Capture("Ready", controller, camera);
                var fixture = JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,
                    "../Contracts/Reins/complete-request.v2.json")));
                var captured = new bool[3]; bool capturedDrive = false;
                controller.Begin();
                // No yield in this loop: only these explicit 20ms advances update animation during the replay.
                // Normal Unity animation would otherwise add render-time updates on top of the recorded timeline.
                foreach (var frame in fixture.frames)
                {
                    controller.Step(frame.Input()); controller.RefreshPresentation();
                    animator.Update(ReinsRun.StepMs / 1000f);
                    var run = controller.Run;
                    if (run.Phase == ReinsPhase.Racing && run.BarrelIndex < 3
                        && !captured[run.BarrelIndex] && run.DistanceToBarrel <= 8)
                    {
                        Capture("Barrel-" + (run.BarrelIndex + 1), controller, camera);
                        captured[run.BarrelIndex] = true;
                    }
                    if (!capturedDrive && run.Phase == ReinsPhase.Drive && run.PhaseElapsedMs >= 500)
                    { Capture("Drive", controller, camera); capturedDrive = true; }
                }
                Assert.IsTrue(captured.All(value => value), "The canonical replay must provide all three barrel captures.");
                Assert.IsTrue(capturedDrive, "Missing final Drive capture.");
                Assert.AreEqual(ReinsPhase.Complete, controller.Run.Phase);
                Assert.AreEqual(V2FixtureExpected.Result.finalTimeMs, controller.Run.FinalTimeMs);
                Assert.AreEqual(0, controller.Run.KnockCount);
                Assert.AreEqual(300, controller.Run.StylePoints);
                WriteEvidence("Run", new RunEvidence
                {
                    unityVersion = Application.unityVersion, finalTimeMs = controller.Run.FinalTimeMs,
                    knocks = controller.Run.KnockCount, stylePoints = controller.Run.StylePoints,
                    replayFrames = fixture.frames.Length, captureWidth = 1280, captureHeight = 720,
                    reducedMotion = true, devicePerformanceMeasured = false,
                    captures = new[] { "Ready", "Barrel-1", "Barrel-2", "Barrel-3", "Drive" }
                });
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                rig.SetReducedMotion(originalReducedMotion);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static Transform ReferenceHorse()
        {
            var horse = GameObject.Find("Horse proxy"); Assert.IsNotNull(horse);
            var model = horse.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Reference horse");
            Assert.IsNotNull(model, "The persisted scene still contains only the old procedural horse. Regenerate with the imported art.");
            return model;
        }

        private static void AssertMaterial(Material material, string owner)
        {
            Assert.IsNotNull(material, "Missing material on " + owner);
            Assert.IsNotNull(material.shader, "Missing shader on " + owner);
            Assert.IsFalse(material.shader.name.Contains("InternalError"), "Error/pink shader on " + owner);
            Assert.IsTrue(material.shader.isSupported, "Unsupported shader on " + owner + ": " + material.shader.name);
        }

        private static void WriteEvidence(string name, object evidence)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Evidence"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "ReferenceGraphics-" + name + ".json"), JsonUtility.ToJson(evidence, true) + "\n");
        }

        private static void Capture(string stage, ReinsLabController controller, Camera camera)
        {
            controller.RefreshPresentation();
            var horse = GameObject.Find("Horse proxy").transform;
            horse.GetComponentInChildren<ReinsRiderTackPresentation>().RenderImmediate();
            horse.GetComponentInChildren<ReinsRiderBodyPresentation>().RenderImmediate();
            Assert.That(Vector3.Distance(horse.position, camera.transform.position), Is.LessThan(4),
                "A gameplay screenshot must use the rider camera, not an art-preview camera.");
            Assert.Greater(Vector3.Dot(camera.transform.up, Vector3.up), .95f);
            var canvas = GameObject.Find("Reins HUD").GetComponent<Canvas>();
            Assert.IsTrue(canvas.enabled && canvas.gameObject.activeInHierarchy, "Capture must include actual gameplay HUD.");
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
                foreach(var label in canvas.GetComponentsInChildren<Text>(true)){label.cachedTextGenerator.Invalidate();label.SetAllDirty();}
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                string evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../Evidence"));
                Directory.CreateDirectory(evidence);
                File.WriteAllBytes(Path.Combine(evidence, "ReferenceGraphics-" + stage + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                Object.Destroy(target); Object.Destroy(image); Canvas.ForceUpdateCanvases();
            }
        }

        [Serializable] private sealed class ArtEvidence
        {
            public string unityVersion;
            public Vector3 posedBodyBoundsMetres, headPositionInModelMetres;
            public string[] gaitClips;
        }
        [Serializable] private sealed class RunEvidence
        {
            public string unityVersion;
            public long finalTimeMs;
            public int knocks, stylePoints, replayFrames, captureWidth, captureHeight;
            public bool reducedMotion, devicePerformanceMeasured;
            public string[] captures;
        }
        [Serializable] private sealed class Fixture { public Frame[] frames; }
        [Serializable] private sealed class Frame
        {
            public int leftPermille, rightPermille;
            public bool cadenceTap, launchHeld, wrap;
            public string drive;
            public ReinsInput Input() => new ReinsInput(leftPermille, rightPermille, cadenceTap, launchHeld, wrap,
                (DriveSide)Enum.Parse(typeof(DriveSide), drive));
        }
    }
}
