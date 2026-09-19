using System;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Core;
using BarrelRivals.Practice;
using NUnit.Framework;

namespace BarrelRivals.Tests
{
    public sealed class PracticeRecordsTests
    {
        [Test] public void OnlyTheExactSeedAndRulesVersionShareABest()
        {
            var records = new PracticeRecords();
            var first = Complete(104, 300);
            Assert.AreEqual(PracticeRecordOutcome.FirstBest, records.Record(first).Outcome);
            Assert.IsFalse(records.TryGetBest(105, out _));
            Assert.AreEqual(PracticeRecordOutcome.FirstBest, records.Record(Complete(105, 0)).Outcome);
            records.TryGetBest(104, out PracticeRecord original);
            Assert.AreEqual(first.Result.FinalTimeMs, original.FinalTimeMs);
            var future = new PracticeRecords(PracticeRun.RulesVersion + 1);
            Assert.AreEqual(PracticeRecordOutcome.Ignored, future.Record(first).Outcome);
            future.Restore(new[] { original });
            Assert.IsEmpty(future.Entries);
        }

        [Test] public void FasterReplacesBestTiesKeepItAndEachRunIsRecordedOnce()
        {
            var records = new PracticeRecords();
            var first = Complete(104, 300);
            records.Record(first);
            var faster = Complete(104, 0);
            var update = records.Record(faster);
            Assert.AreEqual(PracticeRecordOutcome.Improved, update.Outcome);
            Assert.AreEqual(first.Result.FinalTimeMs, update.PreviousBestTimeMs);
            Assert.AreEqual(faster.Result.FinalTimeMs, update.Best.FinalTimeMs);
            var tie = records.Record(Complete(104, 0));
            Assert.AreEqual(PracticeRecordOutcome.Tied, tie.Outcome);
            Assert.AreSame(update.Best, tie.Best);
            Assert.AreEqual(PracticeRecordOutcome.Slower, records.Record(Complete(104, 300)).Outcome);
            Assert.AreEqual(PracticeRecordOutcome.Ignored, records.Record(first).Outcome);
            Assert.AreEqual(PracticeRecordOutcome.Ignored, records.Record(faster).Outcome);
        }

        [Test] public void ComparisonIncludesKnockPenaltyAndNeverRewardsIncompleteRuns()
        {
            var records = new PracticeRecords();
            records.Restore(new[]
            {
                new PracticeRecord(PracticeRun.RulesVersion, 104, 11000, 0),
                new PracticeRecord(PracticeRun.RulesVersion, 104, 8000, 1)
            });
            records.TryGetBest(104, out PracticeRecord best);
            Assert.AreEqual(11000, best.FinalTimeMs);
            var incomplete = new PracticeRun(9);
            Assert.AreEqual(PracticeRecordOutcome.Ignored, records.Record(incomplete).Outcome);
            incomplete.StartHold(0); incomplete.Cancel(100);
            Assert.AreEqual(PracticeRecordOutcome.Ignored, records.Record(incomplete).Outcome);
            Assert.AreEqual(1, records.Entries.Count);
        }

        [Test] public void InvalidImportedValuesAreDroppedAndTheShelfIsBounded()
        {
            var records = new PracticeRecords(capacity: 2);
            records.Restore(new[]
            {
                null, new PracticeRecord(0, 1, 10000, 0), new PracticeRecord(PracticeRun.RulesVersion + 1, 1, 10000, 0),
                new PracticeRecord(PracticeRun.RulesVersion, 1, -1, 0), new PracticeRecord(PracticeRun.RulesVersion, 1, long.MaxValue, 0),
                new PracticeRecord(PracticeRun.RulesVersion, 1, 10000, -1), new PracticeRecord(PracticeRun.RulesVersion, 1, 10000, 2),
                new PracticeRecord(PracticeRun.RulesVersion, 1, 12000, 0), new PracticeRecord(PracticeRun.RulesVersion, 2, 12000, 0),
                new PracticeRecord(PracticeRun.RulesVersion, 3, 12000, 0)
            });
            Assert.AreEqual(2, records.Entries.Count);
            Assert.IsFalse(records.TryGetBest(1, out _));
            Assert.IsTrue(records.TryGetBest(2, out _));
            Assert.IsTrue(records.TryGetBest(3, out _));
            records.Restore(records.Entries);
            Assert.AreEqual(2, records.Entries.Count);
        }

        [Test] public void ReplayNormalizesHoldTimeAndReproducesResultAcrossFrameSteps()
        {
            var run = Complete(104, 30, origin: 8000, outReplay: out PracticeReplay replay);
            Assert.NotNull(replay);
            Assert.AreEqual(0, replay.Events[0].TimeMs);
            Assert.IsTrue(replay.Matches(run));
            foreach (int step in new[] { 8, 33, 311 })
            {
                var player = replay.CreatePlayer();
                for (long time = 0; time < replay.CompletedAtMs; time += step) player.AdvanceTo(time);
                player.AdvanceTo(replay.CompletedAtMs);
                Assert.AreEqual(run.Result.FinalTimeMs, player.Run.Result.FinalTimeMs);
                Assert.AreEqual(run.Result.Drawing.Score, player.Run.Result.Drawing.Score);
                Assert.AreEqual(PracticePath.Sample(run).X, PracticePath.Sample(player.Run).X);
            }
        }

        [Test] public void AutomaticLaunchAndRejectedTraceInputReplayTheObservedPenalty()
        {
            var run = new PracticeRun(104);
            var recorder = new PracticeReplayRecorder(run.Seed);
            run.StartHold(1200); recorder.Record(PracticeInputKind.StartHold, 1200);
            ReachDrawing(run);
            var point = new TracePoint(2, .5);
            Assert.IsFalse(run.BeginTrace(run.NowMs, point));
            recorder.Record(PracticeInputKind.BeginTrace, run.NowMs, point);
            run.AdvanceTo(40000);
            var replay = recorder.Finish(run);
            Assert.NotNull(replay);
            Assert.IsTrue(replay.Matches(run));
            var player = replay.CreatePlayer(); player.AdvanceTo(replay.CompletedAtMs);
            Assert.IsTrue(player.Run.AutoLaunched);
            Assert.AreEqual(TraceReason.Invalid, player.Run.DrawingGrade.Reason);
            Assert.AreEqual(1, player.Run.KnockCount);
        }

        [Test] public void CorruptOrOverflowingReplaysNeverBecomeAGhost()
        {
            var run = Complete(104, 0, origin: 0, outReplay: out PracticeReplay replay);
            Assert.IsFalse(PracticeReplay.TryCreate(PracticeRun.RulesVersion + 1, 104, replay.Events, replay.CompletedAtMs, out _));
            Assert.IsFalse(PracticeReplay.TryCreate(PracticeRun.RulesVersion, 104, replay.Events, replay.CompletedAtMs + 1, out _));
            var inputs = new List<PracticeInputEvent>(replay.Events);
            inputs[1] = new PracticeInputEvent(PracticeInputKind.ReleaseHold, -1);
            Assert.IsFalse(PracticeReplay.TryCreate(PracticeRun.RulesVersion, 104, inputs, replay.CompletedAtMs, out _));
            inputs[1] = new PracticeInputEvent((PracticeInputKind)99, 1);
            Assert.IsFalse(PracticeReplay.TryCreate(PracticeRun.RulesVersion, 104, inputs, replay.CompletedAtMs, out _));
            inputs[1] = new PracticeInputEvent(PracticeInputKind.BeginTrace, 1, new TracePoint(double.NaN, .5));
            Assert.IsFalse(PracticeReplay.TryCreate(PracticeRun.RulesVersion, 104, inputs, replay.CompletedAtMs, out _));
            var overflow = new PracticeReplayRecorder(run.Seed);
            overflow.Record(PracticeInputKind.StartHold, 0);
            for (int i = 0; i < PracticeReplay.MaximumEvents; i++) overflow.Record(PracticeInputKind.AddTrace, i);
            Assert.IsNull(overflow.Finish(run));
            Assert.AreEqual(PracticeRecordOutcome.FirstBest, new PracticeRecords().Record(run, overflow.Finish(run)).Outcome);
        }

        [Test] public void ReplayForAnotherSeedOrResultCannotAttachToABest()
        {
            Complete(105, 0, origin: 0, outReplay: out PracticeReplay other);
            var records = new PracticeRecords();
            Assert.IsNull(records.Record(Complete(104, 0), other).Best.Replay);
            Complete(104, 0, origin: 0, outReplay: out PracticeReplay fast);
            records.Restore(new[] { new PracticeRecord(PracticeRun.RulesVersion, 104, 5000, 0, fast) });
            records.TryGetBest(104, out PracticeRecord restored);
            Assert.IsNull(restored.Replay);
        }

        [Test] public void CoachingNamesAnObservedLaunchMistakeAndAnAction()
        {
            Assert.That(PracticeCoaching.ForRun(Complete(104, -100)).Message, Does.Contain("early"));
            Assert.That(PracticeCoaching.ForRun(Complete(104, 300)).Message, Does.Contain("late"));
            var automatic = Complete(104, null);
            Assert.That(PracticeCoaching.ForRun(automatic).Message, Does.Contain("missed the release"));
            Assert.AreEqual(PracticeCoachFocus.Consistency, PracticeCoaching.ForRun(Complete(104, 0)).Focus);
        }

        [TestCase(TraceReason.Missing, "timer")]
        [TestCase(TraceReason.Invalid, "inside")]
        [TestCase(TraceReason.TooSmall, "more of the pad")]
        [TestCase(TraceReason.OpenShape, "starting point")]
        [TestCase(TraceReason.ExcessLength, "once")]
        [TestCase(TraceReason.LowAccuracy, "proportions")]
        public void DrawingFailuresHaveSpecificNextActions(TraceReason reason, string action)
            => Assert.That(PracticeCoaching.DrawingHint(reason), Does.Contain(action));

        [Test] public void MissingExitAndCancellationReceiveHonestCoaching()
        {
            var missing = Complete(104, 0, exitOffset: null);
            Assert.AreEqual(PracticeCoachFocus.Exit, PracticeCoaching.ForRun(missing).Focus);
            Assert.That(PracticeCoaching.ForRun(missing).Message, Does.Contain("new tap"));
            var cancelled = new PracticeRun(104); cancelled.StartHold(0); cancelled.Cancel(100);
            Assert.AreEqual(PracticeCoachFocus.Interrupted, PracticeCoaching.ForRun(cancelled).Focus);
            Assert.AreEqual(PracticeCoachFocus.Ready, PracticeCoaching.ForRun(new PracticeRun(104)).Focus);
        }

        [Test] public void DiskRoundTripKeepsEquivalentBestAndValidatedGhost()
        {
            WithStorePath(path =>
            {
                var first = Complete(104, 300, origin: 0, outReplay: out PracticeReplay initialReplay);
                var store = new PracticeRecordStore(path);
                store.Record(first, initialReplay); Assert.IsTrue(store.LastSaveSucceeded);
                var faster = Complete(104, 0, origin: 4000, outReplay: out PracticeReplay fastReplay);
                store.Record(faster, fastReplay); Assert.IsTrue(store.LastSaveSucceeded);
                var reloaded = new PracticeRecordStore(path);
                Assert.IsFalse(reloaded.DiscardedInvalidData);
                Assert.IsTrue(reloaded.Records.TryGetBest(104, out PracticeRecord best));
                Assert.AreEqual(faster.Result.FinalTimeMs, best.FinalTimeMs);
                Assert.NotNull(best.Replay); Assert.IsTrue(best.Replay.Matches(faster));
                Assert.IsFalse(File.Exists(path + ".tmp"));
            });
        }

        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("{\"schemaVersion\":99,\"rulesVersion\":1,\"entries\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"rulesVersion\":99,\"entries\":[]}")]
        public void CorruptOrOldFilesDoNotPreventPlay(string json)
        {
            WithStorePath(path =>
            {
                File.WriteAllText(path, json);
                var store = new PracticeRecordStore(path);
                Assert.IsEmpty(store.Records.Entries); Assert.IsTrue(store.DiscardedInvalidData);
                Assert.DoesNotThrow(() => store.Record(Complete(104, 0)));
                Assert.IsTrue(store.LastSaveSucceeded);
            });
        }

        [Test] public void BrokenGhostKeepsValidLocalBestButCannotPlay()
        {
            WithStorePath(path =>
            {
                File.WriteAllText(path, "{\"schemaVersion\":1,\"rulesVersion\":1,\"entries\":[{\"rulesVersion\":1,\"seed\":104,\"rawTimeMs\":10000,\"knockCount\":0,\"completedAtMs\":10000,\"events\":[{\"kind\":99,\"timeMs\":0,\"x\":0,\"y\":0}]}]}");
                var store = new PracticeRecordStore(path);
                Assert.IsTrue(store.Records.TryGetBest(104, out PracticeRecord best));
                Assert.AreEqual(10000, best.FinalTimeMs); Assert.IsNull(best.Replay);
                Assert.IsTrue(store.DiscardedInvalidData);
            });
        }

        [Test] public void StorageFailureRetainsSessionBestWithoutThrowing()
        {
            WithStorePath(path =>
            {
                File.WriteAllText(path, "This file prevents a child directory.");
                var store = new PracticeRecordStore(Path.Combine(path, "bests.json"));
                Assert.DoesNotThrow(() => store.Record(Complete(104, 0)));
                Assert.IsFalse(store.LastSaveSucceeded);
                Assert.IsTrue(store.Records.TryGetBest(104, out _));
            });
        }

        private static PracticeRun Complete(uint seed, int? launchOffset, int? exitOffset = 500)
            => Complete(seed, launchOffset, 0, out _, exitOffset);

        private static PracticeRun Complete(uint seed, int? launchOffset, long origin, out PracticeReplay outReplay, int? exitOffset = 500)
        {
            var run = new PracticeRun(seed); var recorder = new PracticeReplayRecorder(seed);
            run.StartHold(origin); recorder.Record(PracticeInputKind.StartHold, origin);
            if (launchOffset.HasValue)
            {
                long time = run.LaunchCueMs + launchOffset.Value;
                run.ReleaseHold(time); recorder.Record(PracticeInputKind.ReleaseHold, time);
            }
            ReachDrawing(run);
            var points = PatternScoring.Template(run.Pattern); long start = run.NowMs + 100;
            run.BeginTrace(start, points[0]); recorder.Record(PracticeInputKind.BeginTrace, start, points[0]);
            for (int i = 1; i < points.Length; i++)
            {
                long time = start + 1000 * i / (points.Length - 1);
                run.AddTrace(time, points[i]); recorder.Record(PracticeInputKind.AddTrace, time, points[i]);
            }
            run.SubmitTrace(run.NowMs + 10); recorder.Record(PracticeInputKind.SubmitTrace, run.NowMs);
            run.AdvanceTo(run.DrawingClosesMs); run.AdvanceTo(run.PhaseStartedMs + run.PhaseDurationMs);
            if (exitOffset.HasValue)
            { run.TapExit(run.NowMs + exitOffset.Value); recorder.Record(PracticeInputKind.TapExit, run.NowMs); }
            run.AdvanceTo(origin + 40000); outReplay = recorder.Finish(run); return run;
        }

        private static void ReachDrawing(PracticeRun run)
        {
            while (run.Phase != PracticePhase.Drawing) run.AdvanceTo(run.PhaseStartedMs + run.PhaseDurationMs);
        }

        private static void WithStorePath(Action<string> test)
        {
            string directory = Path.Combine(Path.GetTempPath(), "BarrelRivals-Records-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { test(Path.Combine(directory, "bests.json")); }
            finally { Directory.Delete(directory, true); }
        }
    }
}
