using System;
using System.Collections.Generic;

namespace BarrelRivals.Core
{
    public enum PracticePhase { Ready, Gate, Alley, Preview, Drawing, Turn, Exit, RunOut, Complete, Cancelled }

    public sealed class PracticeResult
    {
        public long RawTimeMs { get; }
        public int KnockCount { get; }
        public long FinalTimeMs => RawTimeMs + KnockCount * 5000L;
        public SkillGrade Launch { get; }
        public TraceEvaluation Drawing { get; }
        public SkillGrade Exit { get; }
        internal PracticeResult(long rawTime, int knocks, SkillGrade launch, TraceEvaluation drawing, SkillGrade exit)
        { RawTimeMs=rawTime; KnockCount=knocks; Launch=launch; Drawing=drawing; Exit=exit; }
    }

    /// <summary>Offline, one-barrel practice rules v1. Absolute monotonic millisecond inputs;
    /// phase-boundary integration makes results independent of render stepping. No online trust or rewards.</summary>
    public sealed class PracticeRun
    {
        public const int RulesVersion = 1;
        public const int DrawingWindowMs = 4000;
        public const int PreviewDurationMs = 1200;
        public const int AutoLaunchGraceMs = 750;
        public const int ExitWindowMs = 1000;
        public uint Seed { get; }
        public PatternKind Pattern { get; }
        public int ThirdBeepDelayMs { get; }
        public PracticePhase Phase { get; private set; }
        public long NowMs { get; private set; }
        public long PhaseStartedMs { get; private set; }
        public long FirstBeepMs { get; private set; }
        public long SecondBeepMs => FirstBeepMs+1000;
        public long LaunchCueMs => SecondBeepMs+ThirdBeepDelayMs;
        public long DrawingClosesMs { get; private set; }
        public long PhaseDurationMs { get; private set; }
        public long LaunchErrorMs { get; private set; }
        public bool AutoLaunched { get; private set; }
        public bool TraceSubmitted { get; private set; }
        public bool TraceActive { get; private set; }
        public bool ExitAccepted { get; private set; }
        public int KnockCount { get; private set; }
        public long RawTimeMs => _simulationMicroseconds/1000;
        public SkillGrade LaunchGrade { get; private set; }
        public SkillGrade ExitGrade { get; private set; }
        public TraceEvaluation DrawingGrade { get; private set; }
        public PracticeResult Result { get; private set; }
        public double PhaseProgress => PhaseDurationMs>0 ? Math.Max(0,Math.Min(1,(NowMs-PhaseStartedMs)/(double)PhaseDurationMs)) : 0;
        public IReadOnlyList<TracePoint> Trace => _traceView;
        private readonly List<TracePoint> _trace = new List<TracePoint>(PatternScoring.MaximumPoints);
        private readonly IReadOnlyList<TracePoint> _traceView;
        private long _simulationMicroseconds, _traceStartedMs, _knockAtMs = long.MaxValue;
        private bool _launchReleased, _clockStarted;

        public PracticeRun(uint seed)
        {
            Seed=seed; var rng=new DeterministicRng(seed);
            ThirdBeepDelayMs=800+(int)(rng.NextUInt()%1001);
            Pattern=(PatternKind)(rng.NextUInt()%3);
            Phase=PracticePhase.Ready; DrawingGrade=PatternScoring.Failure(TraceReason.Missing);
            _traceView=_trace.AsReadOnly();
        }

        public bool StartHold(long timeMs)
        {
            AdvanceTo(timeMs); if(Phase!=PracticePhase.Ready) return false;
            FirstBeepMs=timeMs+500; _clockStarted=true; Enter(PracticePhase.Gate,LaunchCueMs-timeMs+AutoLaunchGraceMs);
            return true;
        }
        public bool ReleaseHold(long timeMs)
        {
            AdvanceTo(timeMs); if(Phase!=PracticePhase.Gate || _launchReleased) return false;
            _launchReleased=true; LaunchErrorMs=NowMs-LaunchCueMs;
            LaunchGrade=LaunchErrorMs<0 ? SkillGrade.Bad : TimingGrade(LaunchErrorMs);
            if(NowMs>=LaunchCueMs) BeginAlley();
            return true;
        }
        public bool BeginTrace(long timeMs, TracePoint point)
        {
            AdvanceTo(timeMs); if(Phase!=PracticePhase.Drawing || TraceActive || TraceSubmitted) return false;
            TraceActive=true; _traceStartedMs=NowMs; return AddTrace(timeMs,point);
        }
        public bool AddTrace(long timeMs, TracePoint point)
        {
            AdvanceTo(timeMs); if(Phase!=PracticePhase.Drawing || !TraceActive || TraceSubmitted) return false;
            if(!PatternScoring.Finite(point.X)||!PatternScoring.Finite(point.Y)||point.X<0||point.X>1||point.Y<0||point.Y>1||_trace.Count>=PatternScoring.MaximumPoints)
            { RejectTrace(); return false; }
            if(_trace.Count==0 || PatternScoring.Distance(_trace[_trace.Count-1],point)>.002) _trace.Add(point);
            return true;
        }
        public bool SubmitTrace(long timeMs)
        {
            AdvanceTo(timeMs); if(Phase!=PracticePhase.Drawing || !TraceActive || TraceSubmitted) return false;
            CloseTrace(); return true;
        }
        public bool TapExit(long timeMs)
        {
            AdvanceTo(timeMs); if(Phase!=PracticePhase.Exit || ExitAccepted) return false;
            ExitAccepted=true; ExitGrade=TimingGrade(Math.Abs(NowMs-PhaseStartedMs-ExitWindowMs/2)); return true;
        }
        public bool Cancel(long timeMs)
        {
            CheckTime(timeMs);
            if(Phase==PracticePhase.Ready || Phase==PracticePhase.Complete || Phase==PracticePhase.Cancelled) return false;
            NowMs=timeMs; TraceActive=false; Enter(PracticePhase.Cancelled,0); return true;
        }
        public void AdvanceTo(long timeMs)
        {
            CheckTime(timeMs);
            while(true)
            {
                long deadline=Deadline();
                if(deadline>timeMs) { Integrate(timeMs); break; }
                Integrate(deadline); Transition();
            }
        }
        private void CheckTime(long timeMs)
        {
            if(timeMs<NowMs || timeMs>1000000000000L) throw new ArgumentOutOfRangeException(nameof(timeMs),"Use monotonic milliseconds within one practice session.");
        }
        private long Deadline()
        {
            if(Phase==PracticePhase.Ready || Phase==PracticePhase.Complete || Phase==PracticePhase.Cancelled) return long.MaxValue;
            if(Phase==PracticePhase.Gate && _launchReleased) return LaunchCueMs;
            return PhaseStartedMs+PhaseDurationMs;
        }
        private void Integrate(long next)
        {
            if(_clockStarted && Phase!=PracticePhase.Complete && Phase!=PracticePhase.Cancelled && next>LaunchCueMs)
                _simulationMicroseconds+=(next-Math.Max(NowMs,LaunchCueMs))*(Phase==PracticePhase.Drawing ? 250L : 1000L);
            NowMs=next;
            if(Phase==PracticePhase.Turn && NowMs>=_knockAtMs) KnockCount=1;
        }
        private void Transition()
        {
            switch(Phase)
            {
                case PracticePhase.Gate:
                    if(!_launchReleased) { AutoLaunched=true; LaunchErrorMs=AutoLaunchGraceMs; LaunchGrade=SkillGrade.Bad; }
                    BeginAlley(); break;
                case PracticePhase.Alley: Enter(PracticePhase.Preview,PreviewDurationMs); break;
                case PracticePhase.Preview:
                    Enter(PracticePhase.Drawing,DrawingWindowMs); DrawingClosesMs=NowMs+DrawingWindowMs; break;
                case PracticePhase.Drawing:
                    if(!TraceSubmitted) CloseTrace();
                    Enter(PracticePhase.Turn,Duration(DrawingGrade.Grade,2800,2100,1700,1400));
                    if(DrawingGrade.Quality<35) _knockAtMs=NowMs+PhaseDurationMs/3;
                    break;
                case PracticePhase.Turn: Enter(PracticePhase.Exit,ExitWindowMs); break;
                case PracticePhase.Exit: Enter(PracticePhase.RunOut,Duration(ExitGrade,1400,1150,950,750)); break;
                case PracticePhase.RunOut:
                    Result=new PracticeResult(RawTimeMs,KnockCount,LaunchGrade,DrawingGrade,ExitGrade);
                    Enter(PracticePhase.Complete,0); break;
                default: throw new InvalidOperationException("Invalid practice transition.");
            }
        }
        private void CloseTrace()
        {
            DrawingGrade=TraceActive ? PatternScoring.Evaluate(_trace,Pattern,NowMs-_traceStartedMs) : PatternScoring.Failure(TraceReason.Missing);
            TraceSubmitted=true; TraceActive=false;
        }
        private void RejectTrace() { DrawingGrade=PatternScoring.Failure(TraceReason.Invalid); TraceSubmitted=true; TraceActive=false; }
        private void BeginAlley() => Enter(PracticePhase.Alley,Duration(LaunchGrade,4100,3500,3200,3000));
        private void Enter(PracticePhase phase,long duration) { Phase=phase; PhaseStartedMs=NowMs; PhaseDurationMs=duration; }
        private static int Duration(SkillGrade grade,int bad,int good,int great,int perfect) => grade==SkillGrade.Perfect ? perfect : grade==SkillGrade.Great ? great : grade==SkillGrade.Good ? good : bad;
        private static SkillGrade TimingGrade(long errorMs) => errorMs<=100 ? SkillGrade.Perfect : errorMs<=220 ? SkillGrade.Great : errorMs<=420 ? SkillGrade.Good : SkillGrade.Bad;
    }
}
