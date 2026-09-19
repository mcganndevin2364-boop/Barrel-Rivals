using System;
using System.Collections.Generic;

namespace BarrelRivals.Core
{
    public enum PracticeInputKind { StartHold, ReleaseHold, BeginTrace, AddTrace, SubmitTrace, TapExit }

    public readonly struct PracticeInputEvent
    {
        public PracticeInputKind Kind { get; }
        public long TimeMs { get; }
        public TracePoint Point { get; }
        public PracticeInputEvent(PracticeInputKind kind, long timeMs, TracePoint point = default)
        { Kind = kind; TimeMs = timeMs; Point = point; }
    }

    /// <summary>Versioned local input replay for an own-run ghost. No uploads, ranks or rewards.</summary>
    public sealed class PracticeReplay
    {
        public const int MaximumEvents = 1024;
        public const long MaximumDurationMs = 60000;
        public int RulesVersion { get; }
        public uint Seed { get; }
        public long CompletedAtMs { get; }
        public IReadOnlyList<PracticeInputEvent> Events { get; }
        public PracticeResult Result { get; }

        private PracticeReplay(int rulesVersion, uint seed, long completedAtMs, PracticeInputEvent[] events, PracticeResult result)
        {
            RulesVersion = rulesVersion; Seed = seed; CompletedAtMs = completedAtMs;
            Events = Array.AsReadOnly(events); Result = result;
        }

        public static bool TryCreate(int rulesVersion, uint seed, IReadOnlyList<PracticeInputEvent> events,
            long completedAtMs, out PracticeReplay replay)
        {
            replay = null;
            if (rulesVersion != PracticeRun.RulesVersion || events == null || events.Count < 1 || events.Count > MaximumEvents ||
                completedAtMs <= 0 || completedAtMs > MaximumDurationMs) return false;
            var copy = new PracticeInputEvent[events.Count];
            var run = new PracticeRun(seed);
            long previous = 0;
            for (int i = 0; i < events.Count; i++)
            {
                var input = events[i];
                if (input.Kind < PracticeInputKind.StartHold || input.Kind > PracticeInputKind.TapExit ||
                    input.TimeMs < previous || input.TimeMs > completedAtMs ||
                    !PatternScoring.Finite(input.Point.X) || !PatternScoring.Finite(input.Point.Y) ||
                    System.Math.Abs(input.Point.X) > 16 || System.Math.Abs(input.Point.Y) > 16 ||
                    (i == 0 && (input.Kind != PracticeInputKind.StartHold || input.TimeMs != 0)) ||
                    (i > 0 && input.Kind == PracticeInputKind.StartHold)) return false;
                previous = input.TimeMs; copy[i] = input; Apply(run, input);
                // A ghost records only the active attempt, not later result-screen input.
                if (run.Phase == PracticePhase.Complete && i != events.Count - 1) return false;
            }
            run.AdvanceTo(completedAtMs);
            if (run.Phase != PracticePhase.Complete || run.Result == null || run.PhaseStartedMs != completedAtMs) return false;
            replay = new PracticeReplay(rulesVersion, seed, completedAtMs, copy, run.Result);
            return true;
        }

        public PracticeReplayPlayer CreatePlayer() => new PracticeReplayPlayer(this);

        public bool Matches(PracticeRun run)
        {
            if (run == null || run.Phase != PracticePhase.Complete || run.Result == null ||
                run.Seed != Seed || RulesVersion != PracticeRun.RulesVersion) return false;
            var result = run.Result;
            return result.RawTimeMs == Result.RawTimeMs && result.KnockCount == Result.KnockCount &&
                result.Launch == Result.Launch && result.Exit == Result.Exit &&
                result.Drawing.Quality == Result.Drawing.Quality && result.Drawing.SpeedBonus == Result.Drawing.SpeedBonus &&
                result.Drawing.Reason == Result.Drawing.Reason;
        }

        internal static void Apply(PracticeRun run, PracticeInputEvent input)
        {
            switch (input.Kind)
            {
                case PracticeInputKind.StartHold: run.StartHold(input.TimeMs); break;
                case PracticeInputKind.ReleaseHold: run.ReleaseHold(input.TimeMs); break;
                case PracticeInputKind.BeginTrace: run.BeginTrace(input.TimeMs, input.Point); break;
                case PracticeInputKind.AddTrace: run.AddTrace(input.TimeMs, input.Point); break;
                case PracticeInputKind.SubmitTrace: run.SubmitTrace(input.TimeMs); break;
                case PracticeInputKind.TapExit: run.TapExit(input.TimeMs); break;
            }
        }
    }

    public sealed class PracticeReplayPlayer
    {
        public PracticeRun Run { get; }
        private readonly PracticeReplay _replay;
        private int _next;
        internal PracticeReplayPlayer(PracticeReplay replay) { _replay = replay; Run = new PracticeRun(replay.Seed); }

        /// <param name="elapsedSinceHoldMs">Monotonic elapsed time since the current challenge's hold began.</param>
        public void AdvanceTo(long elapsedSinceHoldMs)
        {
            if (elapsedSinceHoldMs < Run.NowMs) throw new ArgumentOutOfRangeException(nameof(elapsedSinceHoldMs));
            long target = Math.Min(elapsedSinceHoldMs, PracticeReplay.MaximumDurationMs);
            while (_next < _replay.Events.Count && _replay.Events[_next].TimeMs <= target)
                PracticeReplay.Apply(Run, _replay.Events[_next++]);
            Run.AdvanceTo(target);
        }
    }

    /// <summary>Record calls routed to PracticeRun, including rejected calls which may invalidate a trace.</summary>
    public sealed class PracticeReplayRecorder
    {
        private readonly uint _seed;
        private readonly List<PracticeInputEvent> _events = new List<PracticeInputEvent>();
        private long? _origin;
        private bool _invalid;
        public PracticeReplayRecorder(uint seed) { _seed = seed; }

        public void Record(PracticeInputKind kind, long timeMs, TracePoint point = default)
        {
            if (_invalid) return;
            if (!_origin.HasValue)
            {
                if (kind != PracticeInputKind.StartHold || timeMs < 0) { _invalid = true; return; }
                _origin = timeMs;
            }
            long elapsed = timeMs - _origin.Value;
            if (_events.Count >= PracticeReplay.MaximumEvents || elapsed < 0 || elapsed > PracticeReplay.MaximumDurationMs ||
                (_events.Count > 0 && elapsed < _events[_events.Count - 1].TimeMs))
            { _invalid = true; return; }
            _events.Add(new PracticeInputEvent(kind, elapsed, point));
        }

        public PracticeReplay Finish(PracticeRun completed)
        {
            if (_invalid || !_origin.HasValue || completed == null || completed.Seed != _seed || completed.Phase != PracticePhase.Complete)
                return null;
            return PracticeReplay.TryCreate(PracticeRun.RulesVersion, _seed, _events, completed.PhaseStartedMs - _origin.Value, out PracticeReplay replay)
                && replay.Matches(completed) ? replay : null;
        }
    }
}
