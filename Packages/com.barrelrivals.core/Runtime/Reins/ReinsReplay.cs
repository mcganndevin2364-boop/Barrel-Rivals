using System;
using System.Collections.Generic;

namespace BarrelRivals.Core.Reins
{
    /// <summary>Bounded canonical input frames for local playback. Result consistency is not proof of human input.</summary>
    public sealed class ReinsReplay
    {
        public int RulesVersion { get; }
        public ReinsManifest Manifest { get; }
        public IReadOnlyList<ReinsInput> Frames { get; }
        private ReinsReplay(ReinsManifest manifest, ReinsInput[] frames)
        { RulesVersion = ReinsRun.RulesVersion; Manifest = manifest; Frames = Array.AsReadOnly(frames); }

        public static bool TryCreate(int rulesVersion, ReinsManifest manifest, IReadOnlyList<ReinsInput> frames, out ReinsReplay replay)
        {
            replay = null;
            if (rulesVersion != ReinsRun.RulesVersion || manifest == null || frames == null || frames.Count < 1 || frames.Count > ReinsRun.MaximumTicks)
                return false;
            var run = new ReinsRun(manifest); run.Start();
            var copy = new ReinsInput[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                if (run.IsTerminal) return false;
                copy[i] = frames[i]; run.Step(copy[i]);
            }
            if (run.Phase != ReinsPhase.Complete) return false;
            replay = new ReinsReplay(manifest, copy); return true;
        }
        public ReinsReplayPlayer CreatePlayer() => new ReinsReplayPlayer(this);
    }

    public sealed class ReinsReplayPlayer
    {
        public ReinsRun Run { get; }
        public bool Finished => Run.Tick >= _replay.Frames.Count;
        private readonly ReinsReplay _replay;
        internal ReinsReplayPlayer(ReinsReplay replay) { _replay = replay; Run = new ReinsRun(replay.Manifest); Run.Start(); }
        public bool Step()
        {
            if (Finished) return false;
            Run.Step(_replay.Frames[Run.Tick]); return true;
        }
    }
}
