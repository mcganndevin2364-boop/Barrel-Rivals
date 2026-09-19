using BarrelRivals.Core;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;

namespace BarrelRivals.Tests
{
    public sealed class PracticeFeedbackTests
    {
        [Test]
        public void ObservationsDoNotAdvanceRulesOrRepeatAcceptedFeedback()
        {
            var run = new PracticeRun(104);
            var tracker = new PracticeFeedbackTracker();
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            run.StartHold(0);
            Assert.AreEqual(PracticeFeedbackCue.Gate, tracker.Observe(run));
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            Assert.AreEqual(0, run.NowMs);
            run.ReleaseHold(run.LaunchCueMs);
            Assert.AreEqual(PracticeFeedbackCue.Launch, tracker.Observe(run));
            while (run.Phase != PracticePhase.Drawing)
            {
                run.AdvanceTo(run.PhaseStartedMs + run.PhaseDurationMs);
                tracker.Observe(run);
            }
            long now = run.NowMs;
            run.BeginTrace(now, new TracePoint(.5, .5));
            run.AddTrace(now + 100, new TracePoint(.51, .5));
            run.SubmitTrace(now + 110);
            Assert.AreEqual(PracticeFeedbackCue.Trace, tracker.Observe(run));
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            run.AdvanceTo(run.DrawingClosesMs);
            Assert.AreEqual(PracticeFeedbackCue.Turn, tracker.Observe(run));
            run.AdvanceTo(run.PhaseStartedMs + run.PhaseDurationMs / 3);
            Assert.AreEqual(PracticeFeedbackCue.Knock, tracker.Observe(run));
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            run.AdvanceTo(run.PhaseStartedMs + run.PhaseDurationMs);
            Assert.AreEqual(PracticeFeedbackCue.ExitReady, tracker.Observe(run));
            run.TapExit(run.NowMs + 500);
            Assert.AreEqual(PracticeFeedbackCue.Exit, tracker.Observe(run));
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            run.AdvanceTo(run.NowMs + 10000);
            Assert.AreEqual(PracticeFeedbackCue.Finish, tracker.Observe(run));
            long raw = run.Result.RawTimeMs, final = run.Result.FinalTimeMs, completeAt = run.NowMs;
            for (int i = 0; i < 100; i++) Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            Assert.AreEqual(completeAt, run.NowMs);
            Assert.AreEqual(raw, run.Result.RawTimeMs);
            Assert.AreEqual(final, run.Result.FinalTimeMs);
            Assert.AreEqual(raw + 5000, final);
        }

        [Test]
        public void LateAttachmentCancellationAndRetryDoNotReplayOldCues()
        {
            var run = new PracticeRun(7);
            run.StartHold(0); run.AdvanceTo(100000);
            var tracker = new PracticeFeedbackTracker();
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            tracker.Reset();
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(run));
            var retry = new PracticeRun(8);
            tracker.Observe(retry); retry.StartHold(0);
            Assert.AreEqual(PracticeFeedbackCue.Gate, tracker.Observe(retry));
            retry.Cancel(10);
            Assert.AreEqual(PracticeFeedbackCue.None, tracker.Observe(retry));
            Assert.IsNull(retry.Result);
        }

        [Test]
        public void PreferencesPersistAndRepeatedResetKeepsTheAudioPoolBounded()
        {
            string sound = PracticeFeedback.SoundPreference, haptics = PracticeFeedback.HapticsPreference;
            bool hadSound = PlayerPrefs.HasKey(sound), hadHaptics = PlayerPrefs.HasKey(haptics);
            int previousSound = PlayerPrefs.GetInt(sound), previousHaptics = PlayerPrefs.GetInt(haptics);
            GameObject first = null, second = null;
            try
            {
                first = new GameObject("Feedback preference test");
                var feedback = first.AddComponent<PracticeFeedback>();
                feedback.SoundEnabled = false; feedback.HapticsEnabled = false;
                int voices = first.GetComponents<AudioSource>().Length;
                Assert.AreEqual(5, voices);
                for (int i = 0; i < 30; i++)
                {
                    feedback.ResetFeedback(); feedback.Observe(new PracticeRun((uint)i));
                }
                Assert.AreEqual(voices, first.GetComponents<AudioSource>().Length);
                second = new GameObject("Reloaded feedback preferences");
                var reloaded = second.AddComponent<PracticeFeedback>();
                Assert.IsFalse(reloaded.SoundEnabled); Assert.IsFalse(reloaded.HapticsEnabled);
                var run = new PracticeRun(103);
                reloaded.Observe(run); run.StartHold(0); reloaded.Observe(run);
                Assert.AreEqual(0, run.NowMs);
            }
            finally
            {
                if (first) Object.DestroyImmediate(first);
                if (second) Object.DestroyImmediate(second);
                if (hadSound) PlayerPrefs.SetInt(sound, previousSound); else PlayerPrefs.DeleteKey(sound);
                if (hadHaptics) PlayerPrefs.SetInt(haptics, previousHaptics); else PlayerPrefs.DeleteKey(haptics);
                PlayerPrefs.Save();
            }
        }
    }
}
