using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BarrelRivals.Core
{
    public enum PracticeRecordOutcome { Ignored, FirstBest, Improved, Tied, Slower }

    /// <summary>A local practice best, never a trusted competitive result or reward.</summary>
    public sealed class PracticeRecord
    {
        public int RulesVersion { get; }
        public uint Seed { get; }
        public long RawTimeMs { get; }
        public int KnockCount { get; }
        public long FinalTimeMs => RawTimeMs + KnockCount * 5000L;
        public PracticeReplay Replay { get; }

        public PracticeRecord(int rulesVersion, uint seed, long rawTimeMs, int knockCount, PracticeReplay replay = null)
        {
            RulesVersion = rulesVersion; Seed = seed; RawTimeMs = rawTimeMs;
            KnockCount = knockCount; Replay = replay;
        }
    }

    public readonly struct PracticeRecordUpdate
    {
        public PracticeRecordOutcome Outcome { get; }
        public PracticeRecord Best { get; }
        public long? PreviousBestTimeMs { get; }
        public bool IsNewBest => Outcome == PracticeRecordOutcome.FirstBest || Outcome == PracticeRecordOutcome.Improved;
        internal PracticeRecordUpdate(PracticeRecordOutcome outcome, PracticeRecord best, long? previous)
        { Outcome = outcome; Best = best; PreviousBestTimeMs = previous; }
    }

    /// <summary>Bounded bests for exactly one rules version, keyed by challenge seed.</summary>
    public sealed class PracticeRecords
    {
        public const int DefaultCapacity = 64;
        public const int MaximumCapacity = 128;
        public int RulesVersion { get; }
        public int Capacity { get; }
        public IReadOnlyList<PracticeRecord> Entries => _view;
        private readonly List<PracticeRecord> _entries = new List<PracticeRecord>();
        private readonly IReadOnlyList<PracticeRecord> _view;
        private ConditionalWeakTable<PracticeRun, object> _recordedRuns = new ConditionalWeakTable<PracticeRun, object>();

        public PracticeRecords(int rulesVersion = PracticeRun.RulesVersion, int capacity = DefaultCapacity)
        {
            if (rulesVersion < 1) throw new ArgumentOutOfRangeException(nameof(rulesVersion));
            if (capacity < 1 || capacity > MaximumCapacity) throw new ArgumentOutOfRangeException(nameof(capacity));
            RulesVersion = rulesVersion; Capacity = capacity; _view = _entries.AsReadOnly();
        }

        public bool TryGetBest(uint seed, out PracticeRecord best)
        {
            best = _entries.Find(entry => entry.Seed == seed);
            return best != null;
        }

        public PracticeRecordUpdate Record(PracticeRun run, PracticeReplay replay = null)
        {
            if (run == null || run.Phase != PracticePhase.Complete || run.Result == null ||
                RulesVersion != PracticeRun.RulesVersion || _recordedRuns.TryGetValue(run, out _))
                return new PracticeRecordUpdate(PracticeRecordOutcome.Ignored, null, null);
            _recordedRuns.Add(run, new object());
            if (replay != null && !replay.Matches(run)) replay = null;
            return Insert(new PracticeRecord(RulesVersion, run.Seed, run.Result.RawTimeMs, run.Result.KnockCount, replay));
        }

        /// <summary>Replaces the shelf with validated local data; unknown rules and invalid entries are ignored.</summary>
        public void Restore(IReadOnlyList<PracticeRecord> entries)
        {
            // A caller may pass our own read-only view.
            var snapshot = new List<PracticeRecord>();
            if (entries != null)
                for (int i = 0; i < Math.Min(entries.Count, MaximumCapacity); i++) snapshot.Add(entries[i]);
            _entries.Clear(); _recordedRuns = new ConditionalWeakTable<PracticeRun, object>();
            // Persistence adapters also bound the document; do not process an unbounded imported collection.
            foreach (var entry in snapshot) Insert(entry);
        }

        private PracticeRecordUpdate Insert(PracticeRecord candidate)
        {
            if (candidate == null || candidate.RulesVersion != RulesVersion || candidate.RawTimeMs <= 0 ||
                candidate.RawTimeMs > PracticeReplay.MaximumDurationMs || candidate.KnockCount < 0 || candidate.KnockCount > 1)
                return new PracticeRecordUpdate(PracticeRecordOutcome.Ignored, null, null);
            if (candidate.Replay != null && (candidate.Replay.RulesVersion != RulesVersion ||
                candidate.Replay.Seed != candidate.Seed || candidate.Replay.Result.RawTimeMs != candidate.RawTimeMs ||
                candidate.Replay.Result.KnockCount != candidate.KnockCount))
                candidate = new PracticeRecord(RulesVersion, candidate.Seed, candidate.RawTimeMs, candidate.KnockCount);
            TryGetBest(candidate.Seed, out PracticeRecord previous);
            if (previous != null && candidate.FinalTimeMs >= previous.FinalTimeMs)
                return new PracticeRecordUpdate(candidate.FinalTimeMs == previous.FinalTimeMs ? PracticeRecordOutcome.Tied : PracticeRecordOutcome.Slower,
                    previous, previous.FinalTimeMs);
            if (previous != null) _entries.Remove(previous);
            else if (_entries.Count == Capacity) _entries.RemoveAt(0);
            _entries.Add(candidate);
            return new PracticeRecordUpdate(previous == null ? PracticeRecordOutcome.FirstBest : PracticeRecordOutcome.Improved,
                candidate, previous?.FinalTimeMs);
        }
    }
}
