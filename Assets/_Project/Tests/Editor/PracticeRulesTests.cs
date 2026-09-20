using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using BarrelRivals.Core;
using BarrelRivals.Editor;
using NUnit.Framework;

namespace BarrelRivals.Tests
{
    public sealed class PracticeRulesTests
    {
        [TestCase(PatternKind.Circle)] [TestCase(PatternKind.Triangle)] [TestCase(PatternKind.Square)]
        public void AssignedShapesAcceptScaleTranslationStartPointAndDirection(PatternKind kind)
        {
            var template=PatternScoring.Template(kind); var trace=new List<TracePoint>();
            int count=template.Length-1;
            for(int i=0;i<=count;i++)
            {
                var p=template[(count+1-i%count)%count]; trace.Add(new TracePoint(p.X*.7+.1,p.Y*.7+.15));
            }
            var result=PatternScoring.Evaluate(trace,kind,1000);
            Assert.That(result.Quality,Is.GreaterThanOrEqualTo(95));
            Assert.AreEqual(SkillGrade.Perfect,result.Grade);
        }
        [TestCase(16)] [TestCase(32)] [TestCase(128)]
        public void CircleSamplingDensityDoesNotChangeTheGrade(int count)
        {
            var points=new List<TracePoint>();
            for(int i=0;i<=count;i++) points.Add(new TracePoint(.5+.34*Math.Cos(2*Math.PI*i/count),.5+.34*Math.Sin(2*Math.PI*i/count)));
            Assert.AreEqual(SkillGrade.Perfect,PatternScoring.Evaluate(points,PatternKind.Circle,1200).Grade);
        }
        [Test] public void PartialCircleAndTinyTraceCannotEarnSpeed()
        {
            var circle=PatternScoring.Template(PatternKind.Circle); var half=new List<TracePoint>();
            for(int i=0;i<=32;i++) half.Add(circle[i]);
            var partial=PatternScoring.Evaluate(half,PatternKind.Circle,100);
            Assert.AreEqual(0,partial.Score); Assert.AreEqual(TraceReason.OpenShape,partial.Reason);
            var tiny=new List<TracePoint>(); foreach(var p in circle) tiny.Add(new TracePoint(.4+p.X*.1,.4+p.Y*.1));
            Assert.AreEqual(0,PatternScoring.Evaluate(tiny,PatternKind.Circle,100).SpeedBonus);
        }
        [Test] public void ExtraLoopsAndNonFiniteSamplesAreRejected()
        {
            var shape=PatternScoring.Template(PatternKind.Square); var loops=new List<TracePoint>(shape); loops.AddRange(shape);
            Assert.AreEqual(TraceReason.ExcessLength,PatternScoring.Evaluate(loops,PatternKind.Square,1000).Reason);
            shape[1]=new TracePoint(double.NaN,.5);
            Assert.AreEqual(TraceReason.Invalid,PatternScoring.Evaluate(shape,PatternKind.Square,1000).Reason);
        }
        [Test] public void SeedReproducesBoundedChallengeSchedule()
        {
            for(uint seed=0;seed<100;seed++)
            {
                var a=new PracticeRun(seed); var b=new PracticeRun(seed);
                Assert.That(a.ThirdBeepDelayMs,Is.InRange(800,1800));
                Assert.AreEqual(a.Pattern,b.Pattern); Assert.AreEqual(a.ThirdBeepDelayMs,b.ThirdBeepDelayMs);
            }
        }
        [Test] public void EarlyReleaseWaitsForCueAndDoesNotAddABarrelPenalty()
        {
            var run=new PracticeRun(5); run.StartHold(0); run.ReleaseHold(1000);
            Assert.AreEqual(PracticePhase.Gate,run.Phase); Assert.AreEqual(0,run.RawTimeMs);
            run.AdvanceTo(run.LaunchCueMs); Assert.AreEqual(PracticePhase.Alley,run.Phase);
            Assert.AreEqual(SkillGrade.Bad,run.LaunchGrade); Assert.AreEqual(0,run.KnockCount); Assert.AreEqual(0,run.RawTimeMs);
            Assert.IsFalse(run.ReleaseHold(run.NowMs));
        }
        [Test] public void LateAndMissingReleasesIncludeTheDelay()
        {
            var late=new PracticeRun(7); late.StartHold(0); late.ReleaseHold(late.LaunchCueMs+300);
            Assert.AreEqual(300,late.RawTimeMs); Assert.AreEqual(SkillGrade.Good,late.LaunchGrade);
            var missing=new PracticeRun(7); missing.StartHold(0); missing.AdvanceTo(missing.LaunchCueMs+PracticeRun.AutoLaunchGraceMs);
            Assert.IsTrue(missing.AutoLaunched); Assert.AreEqual(PracticeRun.AutoLaunchGraceMs,missing.RawTimeMs);
        }
        [Test] public void EarlyDrawingCompletionDoesNotShortenSlowMotion()
        {
            var run=AtDrawing(); long start=run.NowMs, before=run.RawTimeMs;
            Draw(run,10); Assert.IsTrue(run.TraceSubmitted); Assert.AreEqual(PracticePhase.Drawing,run.Phase);
            run.AdvanceTo(start+PracticeRun.DrawingWindowMs-1); Assert.AreEqual(PracticePhase.Drawing,run.Phase);
            run.AdvanceTo(start+PracticeRun.DrawingWindowMs); Assert.AreEqual(PracticePhase.Turn,run.Phase);
            Assert.AreEqual(1000,run.RawTimeMs-before);
        }
        [Test] public void InputAtDeadlineCannotReopenOrRescoreTheTrace()
        {
            var run=AtDrawing(); run.BeginTrace(run.NowMs,new TracePoint(.2,.2));
            run.AdvanceTo(run.DrawingClosesMs);
            Assert.IsFalse(run.AddTrace(run.NowMs,new TracePoint(.8,.8))); Assert.IsFalse(run.SubmitTrace(run.NowMs));
            Assert.IsFalse(run.BeginTrace(run.NowMs,new TracePoint(.2,.2))); Assert.AreEqual(0,run.DrawingGrade.Score);
        }
        [Test] public void MissingDrawingKnocksOnceAndProducesAnImmutableResult()
        {
            var run=AtDrawing(); run.AdvanceTo(30000);
            Assert.AreEqual(PracticePhase.Complete,run.Phase); Assert.AreEqual(1,run.Result.KnockCount);
            Assert.AreEqual(run.Result.RawTimeMs+5000,run.Result.FinalTimeMs);
            var result=run.Result; run.AdvanceTo(40000); run.Cancel(40000); run.TapExit(40000);
            Assert.AreSame(result,run.Result); Assert.AreEqual(1,run.KnockCount);
        }
        [Test] public void ExitRequiresItsOwnWindowAndCanOnlyBeAcceptedOnce()
        {
            var run=AtDrawing(); Assert.IsFalse(run.TapExit(run.NowMs)); Draw(run,16);
            run.AdvanceTo(run.DrawingClosesMs); run.AdvanceTo(run.PhaseStartedMs+run.PhaseDurationMs);
            Assert.AreEqual(PracticePhase.Exit,run.Phase);
            Assert.IsTrue(run.TapExit(run.NowMs+500)); Assert.AreEqual(SkillGrade.Perfect,run.ExitGrade);
            Assert.IsFalse(run.TapExit(run.NowMs+300)); Assert.AreEqual(SkillGrade.Perfect,run.ExitGrade);
        }
        [Test] public void CancellationCannotFinishARunOrResumeItsOldInput()
        {
            var run=AtDrawing(); Assert.IsTrue(run.Cancel(run.NowMs+60000)); run.AdvanceTo(run.NowMs+5000);
            Assert.AreEqual(PracticePhase.Cancelled,run.Phase); Assert.IsNull(run.Result);
            Assert.IsFalse(run.BeginTrace(run.NowMs,new TracePoint(.5,.5)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>run.AdvanceTo(run.NowMs-1));
        }
        [Test] public void IdenticalTimestampedInputsHaveIdenticalResultsAcrossRenderSteps()
        {
            var fast=Replay(8); var medium=Replay(16); var slow=Replay(33); var sparse=Replay(311);
            foreach(var result in new[]{medium,slow,sparse})
            {
                Assert.AreEqual(fast.RawTimeMs,result.RawTimeMs); Assert.AreEqual(fast.FinalTimeMs,result.FinalTimeMs);
                Assert.AreEqual(fast.Drawing.Score,result.Drawing.Score); Assert.AreEqual(fast.Exit,result.Exit);
            }
        }
        [Test] public void PracticePathIsContinuousAtPhaseBoundaries()
        {
            var run=new PracticeRun(9); run.StartHold(0); run.ReleaseHold(run.LaunchCueMs+20);
            while(run.Phase!=PracticePhase.Complete)
            {
                long end=run.PhaseStartedMs+run.PhaseDurationMs;
                run.AdvanceTo(end-1); var a=PracticePath.Sample(run); run.AdvanceTo(end); var b=PracticePath.Sample(run);
                Assert.That(Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z)),Is.LessThan(.05),run.Phase.ToString());
            }
        }
        [Test] public void SavedPracticeSceneHasRequiredBindings()
        {
            var shipping=EditorBuildSettings.scenes;
            try {
                EditorBuildSettings.scenes=shipping.Where(x=>x.path!=PracticeBuilder.ScenePath)
                    .Concat(new[]{new EditorBuildSettingsScene(PracticeBuilder.ScenePath,true)}).ToArray();
                PracticeBuilder.Validate();
            } finally { EditorBuildSettings.scenes=shipping; }
        }
        private static PracticeRun AtDrawing()
        {
            var run=new PracticeRun(104); run.StartHold(0); run.ReleaseHold(run.LaunchCueMs+30);
            while(run.Phase!=PracticePhase.Drawing) run.AdvanceTo(run.PhaseStartedMs+run.PhaseDurationMs);
            return run;
        }
        private static List<TracePoint> Points(PatternKind kind)
        {
            var vertices=PatternScoring.Template(kind); var points=new List<TracePoint>(); int count=kind==PatternKind.Circle ? 1 : 16;
            for(int i=1;i<vertices.Length;i++) for(int j=0;j<count;j++)
            { double t=j/(double)count; points.Add(new TracePoint(vertices[i-1].X+(vertices[i].X-vertices[i-1].X)*t,vertices[i-1].Y+(vertices[i].Y-vertices[i-1].Y)*t)); }
            points.Add(vertices[vertices.Length-1]); return points;
        }
        private static void Draw(PracticeRun run,int step)
        {
            var points=Points(run.Pattern); long start=run.NowMs+100;
            Step(run,start,step); run.BeginTrace(start,points[0]);
            for(int i=1;i<points.Count;i++) { long at=start+i*15; Step(run,at,step); run.AddTrace(at,points[i]); }
            run.SubmitTrace(run.NowMs+30);
        }
        private static PracticeResult Replay(int step)
        {
            var run=new PracticeRun(104); run.StartHold(0); long launch=run.LaunchCueMs+30;
            Step(run,launch,step); run.ReleaseHold(launch);
            while(run.Phase!=PracticePhase.Drawing) Step(run,run.PhaseStartedMs+run.PhaseDurationMs,step);
            Draw(run,step); Step(run,run.DrawingClosesMs,step); Step(run,run.PhaseStartedMs+run.PhaseDurationMs,step);
            long exit=run.PhaseStartedMs+500; Step(run,exit,step); run.TapExit(exit); Step(run,30000,step);
            return run.Result;
        }
        private static void Step(PracticeRun run,long end,int step)
        { while(run.NowMs+step<end) run.AdvanceTo(run.NowMs+step); run.AdvanceTo(end); }
    }
}
