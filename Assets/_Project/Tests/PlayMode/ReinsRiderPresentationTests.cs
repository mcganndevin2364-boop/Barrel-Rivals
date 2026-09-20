using System;
using System.Collections;
using System.IO;
using BarrelRivals.Core.Reins;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsRiderPresentationTests
    {
        [UnityTest]
        public IEnumerator RiderViewStaysWithTheHorseBeforeGoAndReturnsExactlyOnRetry()
        {
            yield return SceneManager.LoadSceneAsync("Arena_ReinsLab", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<ReinsLabController>();
            controller.enabled = false;
            controller.ResetRun(); controller.RefreshPresentation();
            var camera = Camera.main;
            var horse = GameObject.Find("Horse proxy").transform;
            AssertRiderView(camera, horse);
            Vector3 initialCameraPosition = camera.transform.position;
            Quaternion initialCameraRotation = camera.transform.rotation;

            controller.Begin();
            Assert.AreEqual(ReinsPhase.Approach, controller.Run.Phase);
            for(int i=0;i<100;i++)controller.Step(new ReinsInput(launchHeld:true));
            controller.RefreshPresentation();horse.GetComponent<ReinsHorsePresentation>().ApplyInterpolation(1);
            camera.GetComponent<RiderCameraRig>().RenderImmediate();AssertRiderView(camera,horse);
            Assert.That(controller.Run.Z,Is.EqualTo(-3).Within(.000001));
            Assert.That(camera.transform.position.z-initialCameraPosition.z,Is.EqualTo(3).Within(.01f),
                "The rider must travel with the horse down the actual six-metre alley.");
            for(int i=100;i<199;i++)controller.Step(new ReinsInput(launchHeld:true));
            controller.Step(new ReinsInput(launchHeld:false));
            Assert.AreEqual(ReinsPhase.Racing,controller.Run.Phase);
            Assert.AreEqual(ReinsLaunchOutcome.Perfect,controller.Run.LaunchOutcome);
            Assert.AreEqual(0,controller.Run.RaceTimeMs);
            for (int i = 0; i < 150; i++) controller.Step(new ReinsInput(450, 0));
            controller.RefreshPresentation(); AssertRiderView(camera, horse);
            Assert.Greater(Vector3.Distance(initialCameraPosition, camera.transform.position), .5f);
            controller.ResetRun(); controller.RefreshPresentation();
            Assert.Less(Vector3.Distance(initialCameraPosition, camera.transform.position), .001f,
                "Retry must reset interpolation; the previous run cannot leak into the first captured frame.");
            Assert.Less(Quaternion.Angle(initialCameraRotation, camera.transform.rotation), .001f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ReducedMotionAndRepeatedPresentationCannotChangeAcceptedRunOrOwnedInput()
        {
            yield return SceneManager.LoadSceneAsync("Arena_ReinsLab", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<ReinsLabController>();
            controller.enabled = false;
            var rig = Camera.main.GetComponent<RiderCameraRig>();
            bool originalSetting = rig.ReducedMotion;
            string directory = Path.Combine(Application.temporaryCachePath, "reins-view-proof-" + Guid.NewGuid().ToString("N"));
            controller.SetRecordDirectory(directory);
            try
            {
                var fixture = JsonUtility.FromJson<Fixture>(File.ReadAllText(Path.Combine(Application.dataPath,
                    "../Contracts/Reins/complete-request.v2.json")));
                var expected = new ReinsRun(new ReinsManifest(ReinsLabController.ChallengeSeed, ReinsSurface.HardPack));
                expected.Start(); controller.Begin();
                bool checkedOwnership = false;
                for (int i = 0; i < fixture.frames.Length; i++)
                {
                    var input = fixture.frames[i].Input();
                    expected.Step(input); controller.Step(input);
                    if (i % 137 == 0)
                    {
                        rig.SetReducedMotion(!rig.ReducedMotion);
                        controller.RefreshPresentation(); controller.RefreshPresentation();
                        Assert.AreEqual(expected.Tick, controller.Run.Tick);
                        Assert.AreEqual(expected.X, controller.Run.X);
                        Assert.AreEqual(expected.Z, controller.Run.Z);
                        Assert.AreEqual(expected.HeadingRadians, controller.Run.HeadingRadians);
                        Assert.AreEqual(expected.SpeedMetresPerSecond, controller.Run.SpeedMetresPerSecond);
                        Assert.AreEqual(expected.RaceTimeMs, controller.Run.RaceTimeMs);
                    }
                    if (!checkedOwnership && controller.Run.Phase == ReinsPhase.Racing)
                    {
                        Assert.IsTrue(controller.Press(ReinsPad.Left, 923));
                        controller.Pull(ReinsPad.Left, 923, .7f);
                        rig.SetReducedMotion(!rig.ReducedMotion);
                        Assert.IsFalse(controller.Press(ReinsPad.Right, 923), "Changing comfort settings cannot steal the held finger.");
                        Assert.AreEqual(700, controller.ConsumeInput(Time.realtimeSinceStartupAsDouble).LeftPermille);
                        controller.Release(ReinsPad.Left, 923);
                        Assert.AreEqual(0, controller.ConsumeInput(Time.realtimeSinceStartupAsDouble).LeftPermille);
                        checkedOwnership = true;
                    }
                }
                Assert.IsTrue(checkedOwnership);
                Assert.AreEqual(ReinsPhase.Complete, controller.Run.Phase);
                Assert.AreEqual(V2FixtureExpected.Result.finalTimeMs, controller.Run.FinalTimeMs);
                Assert.AreEqual(expected.FinalTimeMs, controller.Run.FinalTimeMs);
                Assert.AreEqual(expected.StylePoints, controller.Run.StylePoints);
                Assert.AreEqual(expected.KnockCount, controller.Run.KnockCount);
                rig.SetReducedMotion(true); controller.RefreshPresentation();
                float fixedFov = Camera.main.fieldOfView;
                Vector3 fixedView = Camera.main.transform.position;
                yield return null;
                Assert.AreEqual(fixedFov, Camera.main.fieldOfView);
                Assert.Less(Vector3.Distance(fixedView, Camera.main.transform.position), .001f);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                rig.SetReducedMotion(originalSetting);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator ImportedCharacterGhostKeepsItsSkinButCannotActivateEffectsOrInteraction()
        {
            var source = new GameObject("Imported ghost regression fixture");
            source.SetActive(false);
            var model = new GameObject("Skinned model"); model.transform.SetParent(source.transform, false);
            var bone = new GameObject("Neck bone"); bone.transform.SetParent(model.transform, false);
            var mesh = new Mesh { name = "Ghost skin test triangle" };
            mesh.vertices = new[] { Vector3.zero, Vector3.up, Vector3.right };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.bindposes = new[] { Matrix4x4.identity };
            mesh.boneWeights = new[] { Weight(), Weight(), Weight() }; mesh.RecalculateBounds();
            var shader = Shader.Find("Universal Render Pipeline/Unlit"); Assert.IsNotNull(shader);
            var sourceMaterial = new Material(shader); sourceMaterial.SetColor("_BaseColor", Color.red);
            var ghostMaterial = new Material(shader); ghostMaterial.SetColor("_BaseColor", Color.cyan);
            var skin = model.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh = mesh;
            skin.sharedMaterial = sourceMaterial; skin.rootBone = bone.transform; skin.bones = new[] { bone.transform };
            var collider = source.AddComponent<BoxCollider>();
            var body = source.AddComponent<Rigidbody>(); body.isKinematic = false;
            source.AddComponent<PracticeModeLink>();
            ReinsGhostAwakeProbe.AwakeCalls=0;ReinsGhostDependentProbe.AwakeCalls=0;
            var awakeProbe=source.AddComponent<ReinsGhostAwakeProbe>();
            var dependentProbe=source.AddComponent<ReinsGhostDependentProbe>();
            Assert.AreEqual(0,ReinsGhostAwakeProbe.AwakeCalls);
            Assert.AreEqual(0,ReinsGhostDependentProbe.AwakeCalls);
            var camera = source.AddComponent<Camera>(); camera.enabled = true;
            var sound = source.AddComponent<AudioSource>(); sound.playOnAwake = true;
            source.AddComponent<AudioListener>();
            var emitter = new GameObject("Imported dust"); emitter.transform.SetParent(source.transform, false);
            var particles = emitter.AddComponent<ParticleSystem>(); var main = particles.main; main.playOnAwake = true;
            var animator = model.AddComponent<Animator>(); animator.applyRootMotion = true;
            Transform ghost = null;
            try
            {
                ghost = ReinsLabController.CreatePresentationGhost(source.transform, ghostMaterial);
                ghost.gameObject.SetActive(true);
                yield return null;
                var ghostSkin = ghost.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.IsTrue(ghostSkin.enabled); Assert.AreSame(mesh, ghostSkin.sharedMesh);
                Assert.AreSame(ghostMaterial, ghostSkin.sharedMaterial);
                Assert.IsTrue(ghostSkin.bones[0].IsChildOf(ghost), "Skinning must reference the cloned skeleton, not the player's bones.");
                Assert.AreNotSame(bone.transform, ghostSkin.bones[0]);
                Assert.IsFalse(ghost.GetComponent<Camera>().enabled);
                Assert.IsFalse(ghost.GetComponent<AudioListener>().enabled);
                Assert.IsFalse(ghost.GetComponent<AudioSource>().enabled);
                Assert.IsFalse(ghost.GetComponent<AudioSource>().isPlaying);
                Assert.AreEqual(0,ReinsGhostAwakeProbe.AwakeCalls,"Inactive cloned scripts must be removed before activation; disabling still allows Awake.");
                Assert.AreEqual(0,ReinsGhostDependentProbe.AwakeCalls);
                Assert.IsNull(ghost.GetComponent<PracticeModeLink>());
                Assert.IsNull(ghost.GetComponent<ReinsGhostAwakeProbe>());
                Assert.IsNull(ghost.GetComponent<ReinsGhostDependentProbe>());
                Assert.AreSame(awakeProbe,source.GetComponent<ReinsGhostAwakeProbe>());
                Assert.AreSame(dependentProbe,source.GetComponent<ReinsGhostDependentProbe>());
                Assert.IsTrue(awakeProbe.enabled);Assert.IsTrue(dependentProbe.enabled);
                Assert.IsFalse(ghost.GetComponent<Collider>().enabled);
                Assert.IsTrue(ghost.GetComponent<Rigidbody>().isKinematic);
                Assert.IsFalse(ghost.GetComponent<Rigidbody>().detectCollisions);
                Assert.IsFalse(ghost.GetComponentInChildren<Animator>().applyRootMotion);
                var ghostParticles = ghost.GetComponentInChildren<ParticleSystem>();
                Assert.IsFalse(ghostParticles.isPlaying); Assert.AreEqual(0, ghostParticles.particleCount);
                Assert.IsFalse(ghostParticles.GetComponent<Renderer>().enabled);
                Assert.AreSame(sourceMaterial, skin.sharedMaterial);
                Assert.AreEqual(Color.red, sourceMaterial.GetColor("_BaseColor"));
                Assert.IsTrue(camera.enabled); Assert.IsTrue(sound.enabled); Assert.IsTrue(sound.playOnAwake);
                Assert.IsTrue(collider.enabled); Assert.IsFalse(body.isKinematic); Assert.IsTrue(animator.applyRootMotion);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (ghost) Object.Destroy(ghost.gameObject);
                Object.Destroy(source); Object.Destroy(mesh); Object.Destroy(sourceMaterial); Object.Destroy(ghostMaterial);
            }
        }

        private static BoneWeight Weight() => new BoneWeight { boneIndex0 = 0, weight0 = 1 };
        private static void AssertRiderView(Camera camera, Transform horse)
        {
            Assert.Greater(camera.transform.position.y - horse.position.y, 1.5f);
            Assert.Less(Vector3.Distance(camera.transform.position, horse.position), 4,
                "The player should remain seated at the horse, including before GO.");
            Assert.Greater(Vector3.Dot(camera.transform.forward, horse.forward), .9f);
            Assert.Greater(Vector3.Dot(camera.transform.up, Vector3.up), .95f);
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

    public sealed class ReinsGhostAwakeProbe : MonoBehaviour
    {
        public static int AwakeCalls;
        private void Awake() { AwakeCalls++; }
    }

    [RequireComponent(typeof(ReinsGhostAwakeProbe))]
    public sealed class ReinsGhostDependentProbe : MonoBehaviour
    {
        public static int AwakeCalls;
        private void Awake() { AwakeCalls++; }
    }
}
