using System;
using System.Collections;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class ReinsHairMotionTests
    {
        [UnityTest]
        public IEnumerator HairSeeksWithoutDriftAndReducedMotionRestoresNeutralWithoutChangingTheRig()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);
            yield return null;
            using (var sample = new ControlledHorse(GameObject.Find("Horse proxy").transform))
            {
                yield return null; // Flush the last nonzero frame delta before manual sampling.
                Assert.That(Time.deltaTime, Is.EqualTo(0));
                var view = Camera.main.GetComponent<RiderCameraRig>();
                bool reduced = view.ReducedMotion;
                try
                {
                    // Do not manually bind here: the scene/controller must have connected
                    // the generated player component to the live comfort setting.
                    view.SetReducedMotion(true);
                    Assert.IsTrue(sample.Hair.ReducedMotion);
                    sample.Hair.RenderImmediate();
                    var neutral = Capture(sample.Helpers);
                    view.SetReducedMotion(false);
                    Assert.IsFalse(sample.Hair.ReducedMotion);

                    sample.Seek(.125f, 8, .8f);
                    var protectedPose = Capture(sample.Protected);
                    sample.Hair.RenderImmediate();
                    var first = Capture(sample.Helpers);
                    Assert.That(MaximumRotation(neutral, first), Is.GreaterThan(.2f), "The added helpers must actually move.");
                    AssertHelperBounds(neutral, first);
                    for (int i = 0; i < 64; i++) sample.Hair.RenderImmediate();
                    AssertPose(first, Capture(sample.Helpers), "Repeated evaluation must not accumulate rotation");
                    AssertPose(protectedPose, Capture(sample.Protected), "Hair cannot mutate imported bones or the horse root");

                    sample.Seek(.625f, 8, .8f);
                    protectedPose = Capture(sample.Protected);
                    sample.Hair.RenderImmediate();
                    var second = Capture(sample.Helpers);
                    Assert.That(MaximumRotation(first, second), Is.GreaterThan(.1f), "Changing the rendered stride must change helper motion.");
                    AssertHelperBounds(neutral, second);
                    AssertPose(protectedPose, Capture(sample.Protected), "Phase-driven hair must remain presentation-only");
                    sample.Seek(.125f, 8, .8f);
                    sample.Hair.RenderImmediate();
                    AssertPose(first, Capture(sample.Helpers), "Seeking back to a phase must reproduce its pose");

                    view.SetReducedMotion(true);
                    sample.Seek(.625f, 14, -1.5f);
                    protectedPose = Capture(sample.Protected);
                    sample.Hair.RenderImmediate();
                    AssertPose(neutral, Capture(sample.Helpers), "Reduced motion uses the authored helper pose");
                    AssertPose(protectedPose, Capture(sample.Protected), "Reduced mode must not erase the imported gait");
                    LogAssert.NoUnexpectedReceived();
                }
                finally { view.SetReducedMotion(reduced); }
            }
        }

        [UnityTest]
        public IEnumerator GhostClonedFromPosedPlayerKeepsAuthoredRestAndOnlyMovesItsOwnHelpers()
        {
            yield return SceneManager.LoadSceneAsync(ReinsLabController.SceneName);
            yield return null;
            using (var sample = new ControlledHorse(GameObject.Find("Horse proxy").transform))
            {
                yield return null;
                sample.Hair.SetReducedMotion(true);
                var neutral = Capture(sample.Helpers);
                sample.Hair.SetReducedMotion(false);
                sample.Seek(.125f, 8, .8f);
                sample.Hair.RenderImmediate();
                var posedPlayer = Capture(sample.Helpers);
                Assert.That(MaximumRotation(neutral, posedPlayer), Is.GreaterThan(.2f));
                Transform ghost = null;
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                Assert.IsNotNull(shader);
                var tint = new Material(shader);
                tint.SetColor("_BaseColor", new Color(.35f, .88f, .77f, .28f));
                try
                {
                    ghost = ReinsLabController.CreatePresentationGhost(sample.Root, tint);
                    var hair = ghost.GetComponentInChildren<ReinsHairMotion>(true);
                    Assert.IsNotNull(hair, "The private ghost behavior allowlist must retain secondary hair.");
                    var helpers = Helpers(ghost);
                    AssertPose(posedPlayer, Capture(sample.Helpers), "Cloning must not restore or mutate player helpers");
                    ghost.gameObject.SetActive(true);
                    var presentation = ghost.GetComponent<ReinsHorsePresentation>();
                    var animator = ghost.GetComponentInChildren<Animator>(true);
                    Assert.IsNotNull(presentation);
                    Assert.IsNotNull(animator);
                    presentation.Initialize();
                    presentation.enabled = false;
                    hair.enabled = false;
                    hair.SetReducedMotion(true);
                    AssertPose(neutral, Capture(helpers), "Awake must not adopt the player's posed rotation as ghost rest");

                    hair.SetReducedMotion(false);
                    Seek(presentation, animator, ghost, .125f, 0, 0);
                    hair.RenderImmediate();
                    var idleGhost = Capture(helpers);
                    Seek(presentation, animator, ghost, .125f, 8, -1.1f);
                    var protectedGhost = ImportedBones(ghost).Concat(new[] { ghost }).ToArray();
                    var protectedPose = Capture(protectedGhost);
                    hair.RenderImmediate();
                    var runningGhost = Capture(helpers);
                    Assert.That(MaximumRotation(idleGhost, runningGhost), Is.GreaterThan(.2f), "Ghost effort must come from its own presenter.");
                    AssertHelperBounds(neutral, runningGhost);
                    AssertPose(protectedPose, Capture(protectedGhost), "Ghost hair cannot mutate its imported rig/root");
                    AssertPose(posedPlayer, Capture(sample.Helpers), "Ghost sampling must not write player helper transforms");
                    LogAssert.NoUnexpectedReceived();
                }
                finally
                {
                    if (ghost) Object.Destroy(ghost.gameObject);
                    Object.Destroy(tint);
                }
            }
            yield return null;
            yield return null; // Includes the existing private ghost-material owner's cleanup.
        }

        [UnityTest]
        public IEnumerator SavedStableUsesIdleAnimatorAfterRacePresenterIsRemovedAndResetsWhenHidden()
        {
            yield return SceneManager.LoadSceneAsync(StableController.SceneName);
            yield return null;
            using (var sample = new ControlledHorse(GameObject.Find("Copper").transform))
            {
                yield return null;
                Assert.IsNull(sample.Root.GetComponentInChildren<ReinsHorsePresentation>(true), "The showroom must not retain race presentation authority.");
                sample.Hair.SetReducedMotion(true);
                var neutral = Capture(sample.Helpers);
                sample.Hair.SetReducedMotion(false);
                sample.Seek(.125f, 0, 0);
                var protectedPose = Capture(sample.Protected);
                sample.Hair.RenderImmediate();
                var first = Capture(sample.Helpers);
                AssertPose(protectedPose, Capture(sample.Protected), "Stable secondary motion cannot change imported rig/root");
                sample.Seek(.625f, 0, 0);
                sample.Hair.RenderImmediate();
                var second = Capture(sample.Helpers);
                Assert.That(MaximumRotation(first, second), Is.GreaterThan(.05f), "Stable Idle must use its retained Animator clock without a race presenter.");
                AssertHelperBounds(neutral, second);
                sample.Seek(.125f, 0, 0);
                sample.Hair.RenderImmediate();
                AssertPose(first, Capture(sample.Helpers), "Stable phase seeks must also be repeatable");

                sample.Hair.enabled = true;
                sample.Hair.RenderImmediate();
                sample.Root.gameObject.SetActive(false);
                AssertPose(neutral, Capture(sample.Helpers), "Hiding the stable horse must restore helper rest");
                sample.Root.gameObject.SetActive(true);
                sample.Hair.enabled = false;
                sample.Seek(.125f, 0, 0);
                sample.Hair.RenderImmediate();
                AssertPose(first, Capture(sample.Helpers), "Showing the horse must not recapture a posed rest rotation");
                LogAssert.NoUnexpectedReceived();
            }
        }

        private static void Seek(ReinsHorsePresentation source, Animator animator, Transform root, float cycle, float speed, float turn)
        {
            if (source)
                source.ResetFrame(new HorsePresentationFrame(0, root.position, root.rotation, speed, turn, 0, 0, false, false));
            animator.Update(0);
            animator.Play(0, 0, cycle);
            animator.Update(0);
        }

        private static Transform[] Helpers(Transform root)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            var helpers = Enumerable.Range(0, 8).Select(i => all.Single(t => t.name == "Hair motion " + i)).ToArray();
            Assert.AreEqual(8, all.Count(t => t.name.StartsWith("Hair motion ", StringComparison.Ordinal)));
            foreach (var helper in helpers)
            {
                Assert.IsTrue(helper.IsChildOf(root));
                Assert.IsNotNull(helper.parent);
                Assert.IsFalse(helper.parent.name.StartsWith("Hair motion ", StringComparison.Ordinal), "Helpers must be independent, not an accumulating chain.");
            }
            return helpers;
        }

        private static Transform[] ImportedBones(Transform root) => root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Single(r => r.name.StartsWith("HorseBody", StringComparison.Ordinal)).bones.Distinct().ToArray();

        private static Pose[] Capture(Transform[] transforms) => transforms.Select(t => new Pose(t)).ToArray();

        private static float MaximumRotation(Pose[] first, Pose[] second)
        {
            float maximum = 0;
            for (int i = 0; i < first.Length; i++) maximum = Mathf.Max(maximum, Quaternion.Angle(first[i].Rotation, second[i].Rotation));
            return maximum;
        }

        private static void AssertHelperBounds(Pose[] neutral, Pose[] actual)
        {
            for (int i = 0; i < neutral.Length; i++)
            {
                Assert.That(Vector3.Distance(neutral[i].Position, actual[i].Position), Is.LessThan(.000001f), "Helper roots stay fitted");
                Assert.That(Vector3.Distance(neutral[i].Scale, actual[i].Scale), Is.LessThan(.000001f), "Hair never scales helpers");
                Assert.That(Quaternion.Angle(neutral[i].Rotation, actual[i].Rotation), Is.LessThanOrEqualTo(10.02f), "Independent helper rotation stays within the declared bound");
                for (int channel = 0; channel < 4; channel++)
                    Assert.IsFalse(float.IsNaN(actual[i].Rotation[channel]) || float.IsInfinity(actual[i].Rotation[channel]), "All helper channels must remain finite");
            }
        }

        private static void AssertPose(Pose[] expected, Pose[] actual, string message)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(Vector3.Distance(expected[i].Position, actual[i].Position), Is.LessThan(.000001f), message + " position " + i);
                Assert.That(Vector3.Distance(expected[i].Scale, actual[i].Scale), Is.LessThan(.000001f), message + " scale " + i);
                // Quaternion.Angle can magnify last-bit normalization differences near
                // zero. Channel tolerance directly verifies these repeatable local poses.
                for (int channel = 0; channel < 4; channel++)
                    Assert.AreEqual(expected[i].Rotation[channel], actual[i].Rotation[channel], .00001f, message + " rotation " + i);
            }
        }

        private readonly struct Pose
        {
            public readonly Vector3 Position, Scale;
            public readonly Quaternion Rotation;
            public Pose(Transform transform) { Position = transform.localPosition; Rotation = transform.localRotation; Scale = transform.localScale; }
        }

        private sealed class ControlledHorse : IDisposable
        {
            public readonly Transform Root;
            public readonly ReinsHairMotion Hair;
            public readonly Transform[] Helpers, Protected;
            private readonly ReinsHorsePresentation source;
            private readonly Animator animator;
            private readonly Behaviour[] paused;
            private readonly bool[] enabled;
            private readonly float timeScale;

            public ControlledHorse(Transform root)
            {
                Root = root;
                Assert.IsNotNull(root);
                Hair = root.GetComponentInChildren<ReinsHairMotion>(true);
                Assert.IsNotNull(Hair, "Generate the updated groom and save/reopen its component.");
                Assert.AreEqual("Horse strand hair", Hair.name);
                source = root.GetComponent<ReinsHorsePresentation>();
                animator = root.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(animator);
                Helpers = ReinsHairMotionTests.Helpers(root);
                Protected = ImportedBones(root).Concat(new[] { root }).ToArray();
                paused = new Behaviour[] { Hair, source, root.GetComponentInChildren<ReinsRiderTackPresentation>(true),
                    Object.FindAnyObjectByType<ReinsLabController>(), Camera.main.GetComponent<RiderCameraRig>() }.Where(b => b).ToArray();
                enabled = paused.Select(b => b.enabled).ToArray();
                timeScale = Time.timeScale;
                Time.timeScale = 0;
                foreach (var behaviour in paused) behaviour.enabled = false;
            }

            public void Seek(float cycle, float speed, float turn) => ReinsHairMotionTests.Seek(source, animator, Root, cycle, speed, turn);

            public void Dispose()
            {
                Time.timeScale = timeScale;
                if (Root) Root.gameObject.SetActive(true);
                for (int i = 0; i < paused.Length; i++) if (paused[i]) paused[i].enabled = enabled[i];
            }
        }
    }
}
