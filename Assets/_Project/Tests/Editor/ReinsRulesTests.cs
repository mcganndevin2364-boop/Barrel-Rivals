using System;
using System.Collections.Generic;
using BarrelRivals.Core;
using BarrelRivals.Core.Reins;
using NUnit.Framework;

namespace BarrelRivals.Tests
{
    public sealed class ReinsRulesTests
    {
        [Test] public void ManifestAndInputsRejectInvalidRanges()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinsInput(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinsInput(rightPermille: 1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinsInput(drive: (DriveSide)8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinsManifest(1, (ReinsSurface)8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinsManifest(1, roundIndex: 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReinsHorseProfile(heartPermille: -1));
            Assert.Throws<ArgumentNullException>(() => new ReinsRun(null));
        }

        [Test] public void AlleyApproachOwnsPoseAndClockUntilExactGoBoundary()
        {
            var run = new ReinsRun(new ReinsManifest(1));
            Assert.AreEqual(-6, run.Z); Assert.AreEqual(0, run.SpeedMetresPerSecond);
            Assert.IsTrue(run.Start()); Assert.IsFalse(run.Start());
            for (int tick = 1; tick <= 200; tick++)
            {
                run.Step(new ReinsInput(1000, 700, true, true, true, DriveSide.Right));
                Assert.AreEqual(0, run.X); Assert.That(run.Z, Is.EqualTo(-6 + tick * .03).Within(1e-12));
                Assert.AreEqual(0, run.HeadingRadians); Assert.AreEqual(1.5, run.SpeedMetresPerSecond);
                Assert.AreEqual(0, run.RaceTimeMs); Assert.AreEqual(0, run.CadenceAwards);
                Assert.IsFalse(run.WrapActive); Assert.AreEqual(0, run.DriveAcceptedTaps);
                Assert.AreEqual(tick == 200 ? ReinsPhase.Racing : ReinsPhase.Approach, run.Phase);
            }
            Assert.AreEqual(600, run.NextCadenceBeatMs);
            run.Step(new ReinsInput(leftPermille: 500, launchHeld: true));
            Assert.AreEqual(20, run.RaceTimeMs); Assert.Less(run.X, 0); Assert.Greater(run.Z, 0);
        }

        [TestCase(-260, ReinsLaunchOutcome.Weak)] [TestCase(-240, ReinsLaunchOutcome.Good)]
        [TestCase(-140, ReinsLaunchOutcome.Good)] [TestCase(-120, ReinsLaunchOutcome.Perfect)]
        [TestCase(0, ReinsLaunchOutcome.Perfect)] [TestCase(120, ReinsLaunchOutcome.Perfect)]
        [TestCase(140, ReinsLaunchOutcome.Good)] [TestCase(240, ReinsLaunchOutcome.Good)]
        [TestCase(260, ReinsLaunchOutcome.Weak)] [TestCase(400, ReinsLaunchOutcome.Weak)]
        public void FirstReleaseUsesSignedGoErrorAndInclusiveWindows(int error, ReinsLaunchOutcome outcome)
        {
            var run = ReleaseAt(4000 + error);
            Assert.AreEqual(outcome, run.LaunchOutcome); Assert.AreEqual(error, run.LaunchReleaseErrorMs);
            for (int i = 0; i < 230; i++) run.Step(new ReinsInput(launchHeld: i % 2 == 0));
            Assert.AreEqual(outcome, run.LaunchOutcome); Assert.AreEqual(error, run.LaunchReleaseErrorMs);
        }

        [Test] public void SubStepReleaseTimeoutAndCancellationCannotFabricateOrRepeatLaunch()
        {
            var shortTouch = ReleaseAt(20);
            Assert.AreEqual(ReinsLaunchOutcome.Weak, shortTouch.LaunchOutcome);
            Assert.AreEqual(-3980, shortTouch.LaunchReleaseErrorMs);
            while (shortTouch.Tick < 200) shortTouch.Step(new ReinsInput(launchHeld: true));
            Assert.AreEqual(0, shortTouch.Z); Assert.AreEqual(0, shortTouch.RaceTimeMs);
            var timeout = new ReinsRun(new ReinsManifest(1)); timeout.Start();
            while (timeout.Tick < 219) timeout.Step(new ReinsInput(launchHeld: true));
            Assert.IsFalse(timeout.LaunchResolved);
            timeout.Step(new ReinsInput(launchHeld: true));
            Assert.AreEqual(ReinsLaunchOutcome.TimedOut, timeout.LaunchOutcome);
            Assert.IsNull(timeout.LaunchReleaseErrorMs); Assert.AreEqual(400, timeout.RaceTimeMs);
            timeout.Step(default); Assert.IsNull(timeout.LaunchReleaseErrorMs);
            var cancelled = new ReinsRun(new ReinsManifest(1)); cancelled.Start();
            while (cancelled.Tick < 219) cancelled.Step(new ReinsInput(launchHeld: true));
            cancelled.Cancel(); cancelled.Step(default);
            Assert.AreEqual(219, cancelled.Tick); Assert.AreEqual(ReinsLaunchOutcome.Pending, cancelled.LaunchOutcome);
            Assert.IsNull(cancelled.LaunchReleaseErrorMs);
        }

        [Test] public void LaunchBoostStartsAfterReleaseNeverChangesBrakingAndExpiresAtFixedGoTime()
        {
            var early = ReleaseAt(4000); var late = ReleaseAt(4000, release: false);
            early.Step(default); late.Step(default);
            Assert.That(early.SpeedMetresPerSecond - 1.5, Is.EqualTo(4.8 * .02 * 1.35).Within(1e-12));
            Assert.That(late.SpeedMetresPerSecond - 1.5, Is.EqualTo(4.8 * .02).Within(1e-12), "This tick consumed the late release; its boost starts next tick.");
            double speed = late.SpeedMetresPerSecond; late.Step(default);
            Assert.That(late.SpeedMetresPerSecond - speed, Is.EqualTo(4.8 * .02 * 1.35).Within(1e-12));
            var weak = ReleaseAt(20); while (weak.Tick < 200) weak.Step(default);
            var perfectBrake = ReleaseAt(4000);
            perfectBrake.Step(new ReinsInput(1000, 1000)); weak.Step(new ReinsInput(1000, 1000));
            Assert.AreEqual(weak.SpeedMetresPerSecond, perfectBrake.SpeedMetresPerSecond);
            // Hold both reins to ensure positive acceleration is available near expiry.
            while (early.RaceTimeMs < 1160) early.Step(new ReinsInput(1000, 1000));
            speed = early.SpeedMetresPerSecond; early.Step(default);
            Assert.That(early.SpeedMetresPerSecond - speed, Is.EqualTo(4.8 * .02 * 1.35).Within(1e-12));
            speed = early.SpeedMetresPerSecond; early.Step(default);
            Assert.AreEqual(1200, early.RaceTimeMs);
            Assert.That(early.SpeedMetresPerSecond - speed, Is.EqualTo(4.8 * .02).Within(1e-12));
        }

        private static ReinsRun ReleaseAt(int milliseconds, bool release = true)
        {
            var run = new ReinsRun(new ReinsManifest(1)); run.Start();
            while (run.ElapsedMs + ReinsRun.StepMs < milliseconds) run.Step(new ReinsInput(launchHeld: true));
            run.Step(new ReinsInput(launchHeld: !release)); return run;
        }

        [TestCase(-140, ReinsTimingGrade.Good)] [TestCase(-100, ReinsTimingGrade.Great)] [TestCase(-80, ReinsTimingGrade.Great)]
        [TestCase(-60, ReinsTimingGrade.Perfect)] [TestCase(-40, ReinsTimingGrade.Perfect)] [TestCase(0, ReinsTimingGrade.Perfect)]
        [TestCase(40, ReinsTimingGrade.Perfect)] [TestCase(60, ReinsTimingGrade.Perfect)] [TestCase(80, ReinsTimingGrade.Great)] [TestCase(100, ReinsTimingGrade.Great)] [TestCase(120, ReinsTimingGrade.Good)]
        [TestCase(140, ReinsTimingGrade.Good)] [TestCase(-160, ReinsTimingGrade.Miss)]
        public void CadenceGradesTheExactFixedTickWindow(int offset, ReinsTimingGrade expected)
        {
            var run = Racing(); long target = run.NextCadenceBeatMs + offset;
            while (run.RaceTimeMs + ReinsRun.StepMs < target) run.Step(default);
            run.Step(new ReinsInput(cadenceTap: true));
            Assert.AreEqual(target, run.RaceTimeMs); Assert.AreEqual(expected, run.LastCadenceGrade);
            int awards = run.CadenceAwards;
            run.Step(new ReinsInput(cadenceTap: true));
            Assert.AreEqual(awards, run.CadenceAwards, "At most one grade per beat, including duplicate calls in its window.");
        }

        [Test] public void CadenceSpamCannotAccelerateAndUnattendedBeatsMiss()
        {
            var spam = Racing(); var silent = Racing();
            for (int i = 0; i < 100; i++) { spam.Step(new ReinsInput(cadenceTap: true)); silent.Step(default); }
            Assert.AreEqual(0, spam.CadenceAwards); Assert.Greater(spam.CadenceMisses, 0);
            Assert.Greater(silent.CadenceMisses, 0); Assert.Less(silent.CadencePace01, .9);
            Assert.LessOrEqual(spam.CadencePace01, silent.CadencePace01);
            Assert.AreEqual(0, spam.CadenceStreak);
        }

        [Test] public void HotAndBlazingRewardsAreBoundedAndAMissEndsThem()
        {
            var run = Racing();
            for (int i = 0; i < 5; i++) HitNextBeat(run);
            Assert.IsTrue(run.HotHooves); Assert.AreEqual(5, run.CadenceStreak);
            for (int i = 0; i < 5; i++) HitNextBeat(run);
            Assert.IsTrue(run.BlazingHooves); Assert.AreEqual(10, run.CadenceStreak);
            Assert.LessOrEqual(run.SpeedMetresPerSecond, ReinsRun.AbsoluteSpeedCap);
            // Earned protection expires after three seconds. Unattended beats must then miss;
            // a tap immediately after a fast beat rollover is already inside the next Good window.
            for (int i = 0; i < 4000 / ReinsRun.StepMs; i++) run.Step(default);
            Assert.AreEqual(ReinsTimingGrade.Miss, run.LastCadenceGrade);
            Assert.IsFalse(run.HotHooves); Assert.IsFalse(run.BlazingHooves); Assert.AreEqual(0, run.CadenceStreak);
        }

        [Test] public void ReinsTurnImmediatelyAndBothReinsBrakeWithoutSteering()
        {
            var left = Racing(); var right = Racing(); var brake = Racing(); var free = Racing();
            for (int i = 0; i < 40; i++)
            {
                left.Step(new ReinsInput(leftPermille: 650)); right.Step(new ReinsInput(rightPermille: 650));
                brake.Step(new ReinsInput(1000, 1000)); free.Step(default);
            }
            Assert.Less(left.HeadingRadians, 0); Assert.Greater(right.HeadingRadians, 0);
            Assert.That(left.X, Is.EqualTo(-right.X).Within(1e-10));
            Assert.AreEqual(0, brake.HeadingRadians); Assert.Less(brake.SpeedMetresPerSecond, free.SpeedMetresPerSecond);
            Assert.That(brake.SpeedMetresPerSecond, Is.EqualTo(0).Within(1e-9));
        }

        [Test] public void WrapConsumesFiniteBudgetAndExcludesCadenceAwards()
        {
            var run = Racing(); var barrel = StandardCourse.Barrel(0);
            for (int i = 0; i < 1500 && run.DistanceToBarrel > 3.2; i++)
                run.Step(Toward(run, barrel.X, barrel.Z, .25));
            Assert.LessOrEqual(run.DistanceToBarrel, 3.2, "Test approach must enter the actual near-barrel region.");
            int awards = run.CadenceAwards, budget = run.WrapRemainingMs;
            run.Step(new ReinsInput(leftPermille: 650, cadenceTap: true, wrap: true));
            Assert.IsTrue(run.WrapActive); Assert.AreEqual(awards, run.CadenceAwards);
            Assert.AreEqual(budget - ReinsRun.StepMs, run.WrapRemainingMs);
            int activeTicks = 1;
            for (int i = 0; i < 200; i++)
            {
                awards = run.CadenceAwards;
                run.Step(new ReinsInput(1000, 1000, cadenceTap: true, wrap: true));
                if (run.WrapActive) { activeTicks++; Assert.AreEqual(awards, run.CadenceAwards); }
            }
            Assert.AreEqual(ReinsRun.WrapBudgetPerBarrelMs / ReinsRun.StepMs, activeTicks);
            Assert.AreEqual(0, run.WrapRemainingMs); Assert.IsFalse(run.WrapActive);
        }

        [Test] public void SurfaceSelectionAndRoundDegradationAreFairAndReproducible()
        {
            var a = new ReinsManifest(78, ReinsSurface.Mixed, 1);
            var b = new ReinsManifest(78, ReinsSurface.Mixed, 1);
            foreach (double x in new[] { -20.0, 0, 20 }) foreach (double z in new[] { -5.0, 20, 50 })
            {
                Assert.AreEqual(a.SurfaceAt(x, z), b.SurfaceAt(x, z));
                Assert.AreEqual(a.TuningAt(x, z).GripMultiplier, b.TuningAt(x, z).GripMultiplier);
                Assert.AreNotEqual(ReinsSurface.Mixed, a.SurfaceAt(x, z));
            }
            var fresh = new ReinsManifest(78, ReinsSurface.TackyClay, 0).TuningAt(0, 0);
            var third = new ReinsManifest(78, ReinsSurface.TackyClay, 2).TuningAt(0, 0);
            Assert.Less(third.SpeedMultiplier, fresh.SpeedMultiplier); Assert.Less(third.GripMultiplier, fresh.GripMultiplier);
        }

        [Test] public void SweptContactKnocksEachBarrelAtMostOnceAndKissHasClearance()
        {
            var judge = new ReinsCourseJudge(); var barrel = StandardCourse.Barrel(0);
            for (int i = 0; i < 20; i++) judge.Step(barrel.X - .9, barrel.Z, barrel.X + .9, barrel.Z);
            Assert.AreEqual(1, judge.KnockCount); Assert.IsTrue(judge.IsBarrelKnocked(0));
            Assert.AreEqual(0, judge.BarrelIndex); Assert.AreEqual(0, judge.StylePoints);
            Assert.Less(ReinsCourseJudge.ContactRadius, ReinsCourseJudge.KissInnerRadius);
            Assert.AreEqual(ReinsPocketZone.Kiss, ReinsCourseJudge.ZoneForDistance(1.1));
            Assert.AreEqual(ReinsPocketZone.None, ReinsCourseJudge.ZoneForDistance(.8));
        }

        [Test] public void StraightPassSkippingAndReverseArcsCannotCompleteABarrel()
        {
            var judge = new ReinsCourseJudge(); var barrel = StandardCourse.Barrel(0);
            for (int i = 0; i < 24; i++) judge.Step(barrel.X + 3, barrel.Z - 6 + i * .5, barrel.X + 3, barrel.Z - 5.5 + i * .5);
            Assert.AreEqual(0, judge.BarrelIndex);
            var wrongBarrel = StandardCourse.Barrel(1);
            FeedLoop(judge, wrongBarrel, 3, 1, Math.PI, Math.PI * 2);
            Assert.AreEqual(0, judge.BarrelIndex);
            var reverse = new ReinsCourseJudge();
            double angle = Math.Atan2(-barrel.X, -barrel.Z);
            FeedApproach(reverse, barrel, angle, 3);
            FeedLoop(reverse, barrel, 3, 1, angle, Math.PI * 2);
            Assert.AreEqual(0, reverse.BarrelIndex); Assert.AreEqual(0, reverse.StylePoints);
        }

        [Test] public void OrderedContinuousLoopsCompleteOnlyOnANearbyOutgoingExit()
        {
            var judge = new ReinsCourseJudge();
            for (int index = 0; index < 3; index++)
            {
                var barrel = StandardCourse.Barrel(index);
                var source = index == 0 ? new StandardCourse.Point(0, 0) : StandardCourse.Barrel(index - 1);
                var destination = index == 2 ? new StandardCourse.Point(0, 0) : StandardCourse.Barrel(index + 1);
                double angle = Math.Atan2(source.X - barrel.X, source.Z - barrel.Z);
                double outbound = Math.Atan2(destination.X - barrel.X, destination.Z - barrel.Z);
                int direction = ReinsCourseJudge.TurnDirection(index);
                double exitAngle = outbound - direction * Math.PI / 2;
                double travel = PositiveAngle((exitAngle - angle) * direction);
                if (travel < ReinsCourseJudge.RequiredWindingRadians + .1) travel += Math.PI * 2;
                FeedApproach(judge, barrel, angle, 3);
                double last = FeedLoop(judge, barrel, 3, direction, angle, travel);
                Assert.AreEqual(index, judge.BarrelIndex, "Pure circling has no validated outward exit.");
                double x = barrel.X + Math.Sin(last) * 3, z = barrel.Z + Math.Cos(last) * 3;
                for (int step = 0; step < 8 && judge.BarrelIndex == index; step++)
                {
                    double nextX = x + Math.Sin(outbound) * .4, nextZ = z + Math.Cos(outbound) * .4;
                    judge.Step(x, z, nextX, nextZ); x = nextX; z = nextZ;
                }
                Assert.AreEqual(index + 1, judge.BarrelIndex);
            }
            Assert.AreEqual(300, judge.StylePoints); Assert.AreEqual(0, judge.KnockCount);
        }

        [Test] public void CancellationAndTimeoutFreezeFiniteStateAndHaveNoResult()
        {
            var cancelled = Racing(); Assert.IsTrue(cancelled.Cancel());
            int tick = cancelled.Tick; cancelled.Step(new ReinsInput(1000, 1000));
            Assert.AreEqual(tick, cancelled.Tick); Assert.AreEqual(0, cancelled.FinalTimeMs);
            var timeout = new ReinsRun(new ReinsManifest(9, ReinsSurface.MuddySlop, 2, new ReinsHorseProfile(1000, 1000, 1000, 1000)));
            timeout.Start();
            while (!timeout.IsTerminal)
            {
                timeout.Step(new ReinsInput(cadenceTap: true));
                Assert.IsFalse(double.IsNaN(timeout.X) || double.IsNaN(timeout.Z) || double.IsInfinity(timeout.SpeedMetresPerSecond));
                Assert.LessOrEqual(timeout.SpeedMetresPerSecond, ReinsRun.AbsoluteSpeedCap);
                Assert.LessOrEqual(Math.Abs(timeout.YawRateRadiansPerSecond), ReinsRun.AbsoluteYawRateCap);
            }
            Assert.AreEqual(ReinsPhase.TimedOut, timeout.Phase); Assert.AreEqual(ReinsRun.MaximumTicks, timeout.Tick);
            Assert.AreEqual(0, timeout.FinalTimeMs);
        }

        [Test] public void IdenticalFramesProduceIdenticalLocalSimulation()
        {
            var manifest = new ReinsManifest(789, ReinsSurface.Mixed, 2);
            var a = new ReinsRun(manifest); var b = new ReinsRun(manifest); a.Start(); b.Start();
            for (int i = 0; i < 1400; i++)
            {
                var input = new ReinsInput(i % 47 < 9 ? 400 : 0, i % 59 < 12 ? 300 : 0,
                    cadenceTap: i % 21 == 0, launchHeld: i < 199, wrap: i % 77 < 5);
                a.Step(input); b.Step(input);
                Assert.AreEqual(a.X, b.X); Assert.AreEqual(a.Z, b.Z); Assert.AreEqual(a.HeadingRadians, b.HeadingRadians);
            }
            Assert.AreEqual(a.RaceTimeMs, b.RaceTimeMs); Assert.AreEqual(a.KnockCount, b.KnockCount); Assert.AreEqual(a.BarrelIndex, b.BarrelIndex);
            Assert.IsFalse(ReinsReplay.TryCreate(ReinsRun.RulesVersion, manifest, new[] { default(ReinsInput) }, out _));
            Assert.IsFalse(ReinsReplay.TryCreate(99, manifest, new[] { default(ReinsInput) }, out _));
        }

        [Test] public void ASteeredThreeBarrelFixtureCompletesAndItsReplayRejectsExtraFrames()
        {
            var manifest = new ReinsManifest(104, ReinsSurface.HardPack);
            var run = new ReinsRun(manifest); var frames = new List<ReinsInput>(); run.Start();
            int routeIndex = -1, waypoint = 0;
            List<StandardCourse.Point> route = null;
            while (!run.IsTerminal)
            {
                ReinsInput input;
                if (run.Phase == ReinsPhase.Approach) input = new ReinsInput(launchHeld: run.ElapsedMs + ReinsRun.StepMs < ReinsRun.ApproachMs);
                else
                {
                    if (routeIndex != run.BarrelIndex) { routeIndex = run.BarrelIndex; waypoint = 0; route = Route(routeIndex); }
                    // One metre of lookahead avoids demanding a waypoint inside the horse's turning radius.
                    while (waypoint < route.Count - 1 && Distance(run, route[waypoint]) < 1.0) waypoint++;
                    input = Toward(run, route[waypoint].X, route[waypoint].Z, run.BarrelIndex < 3 ? .55 : 0,
                        run.Phase == ReinsPhase.Drive && run.DriveRemainingMs % 200 == 0 ? (run.DriveAcceptedTaps % 2 == 0 ? DriveSide.Left : DriveSide.Right) : DriveSide.None);
                }
                frames.Add(input); run.Step(input);
            }
            Assert.AreEqual(ReinsPhase.Complete, run.Phase, "The conservative steering fixture must be able to finish all three legal turns. Last barrel: " + run.BarrelIndex);
            Assert.AreEqual(3, run.BarrelIndex); Assert.Greater(run.FinalTimeMs, 0);
            Assert.That(run.DriveAcceptedTaps, Is.InRange(1, 20));
            Assert.IsTrue(ReinsReplay.TryCreate(ReinsRun.RulesVersion, manifest, frames, out ReinsReplay replay));
            var player = replay.CreatePlayer(); while (player.Step()) { }
            Assert.AreEqual(run.FinalTimeMs, player.Run.FinalTimeMs); Assert.AreEqual(run.StylePoints, player.Run.StylePoints);
            var sameSide = AtDrive(manifest, frames);
            for (int i = 0; i < 200; i++) sameSide.Step(new ReinsInput(drive: DriveSide.Left));
            Assert.AreEqual(1, sameSide.DriveAcceptedTaps, "Repeating one side cannot build drive rewards.");
            var rapid = AtDrive(manifest, frames);
            for (int i = 0; i < 250; i++) rapid.Step(new ReinsInput(drive: i % 2 == 0 ? DriveSide.Left : DriveSide.Right));
            Assert.LessOrEqual(rapid.DriveAcceptedTaps, 20); Assert.AreNotEqual(ReinsPhase.Drive, rapid.Phase);
            int accepted = rapid.DriveAcceptedTaps;
            for (int i = 0; i < 30; i++) rapid.Step(new ReinsInput(drive: i % 2 == 0 ? DriveSide.Left : DriveSide.Right));
            Assert.AreEqual(accepted, rapid.DriveAcceptedTaps, "No drive awards after its bounded four-second window.");
            frames.Add(default);
            Assert.IsFalse(ReinsReplay.TryCreate(ReinsRun.RulesVersion, manifest, frames, out _));
        }

        private static ReinsRun AtDrive(ReinsManifest manifest, IReadOnlyList<ReinsInput> frames)
        {
            var run = new ReinsRun(manifest); run.Start();
            for (int i = 0; i < frames.Count && run.Phase != ReinsPhase.Drive; i++) run.Step(frames[i]);
            Assert.AreEqual(ReinsPhase.Drive, run.Phase); return run;
        }

        private static ReinsRun Racing()
        {
            var run = new ReinsRun(new ReinsManifest(104)); run.Start();
            while (run.Phase != ReinsPhase.Racing)
                run.Step(new ReinsInput(launchHeld: run.ElapsedMs + ReinsRun.StepMs < ReinsRun.ApproachMs));
            return run;
        }
        private static void HitNextBeat(ReinsRun run)
        {
            while (run.NextCadenceBeatMs <= run.RaceTimeMs) run.Step(default);
            while (run.RaceTimeMs + ReinsRun.StepMs < run.NextCadenceBeatMs) run.Step(default);
            run.Step(new ReinsInput(cadenceTap: true));
        }
        private static ReinsInput Toward(ReinsRun run, double x, double z, double brake, DriveSide drive = DriveSide.None)
        {
            double angle = Math.Atan2(x - run.X, z - run.Z) - run.HeadingRadians;
            while (angle > Math.PI) angle -= Math.PI * 2;
            while (angle < -Math.PI) angle += Math.PI * 2;
            double steering = Math.Max(-1, Math.Min(1, angle * 1.8));
            int left = (int)Math.Round(1000 * (brake + Math.Max(0, -steering) * (1 - brake)));
            int right = (int)Math.Round(1000 * (brake + Math.Max(0, steering) * (1 - brake)));
            bool beat = run.RaceTimeMs + ReinsRun.StepMs == run.NextCadenceBeatMs;
            return new ReinsInput(left, right, cadenceTap: beat, drive: drive);
        }
        private static List<StandardCourse.Point> Route(int index)
        {
            if (index == 3) return new List<StandardCourse.Point> { new StandardCourse.Point(0, -5) };
            var center = StandardCourse.Barrel(index);
            var source = index == 0 ? new StandardCourse.Point(0, 0) : StandardCourse.Barrel(index - 1);
            var next = index == 2 ? new StandardCourse.Point(0, 0) : StandardCourse.Barrel(index + 1);
            double angle = Math.Atan2(source.X - center.X, source.Z - center.Z);
            double outgoing = Math.Atan2(next.X - center.X, next.Z - center.Z);
            int direction = ReinsCourseJudge.TurnDirection(index);
            double travel = PositiveAngle((outgoing - direction * Math.PI / 2 - angle) * direction);
            if (travel < ReinsCourseJudge.RequiredWindingRadians + .3) travel += Math.PI * 2;
            var points = new List<StandardCourse.Point> { new StandardCourse.Point(center.X + Math.Sin(angle) * 6, center.Z + Math.Cos(angle) * 6) };
            const double radius = 3.4;
            for (double progress = 0; progress < travel; progress += .16)
                points.Add(new StandardCourse.Point(center.X + Math.Sin(angle + direction * progress) * radius, center.Z + Math.Cos(angle + direction * progress) * radius));
            double end = angle + direction * travel;
            double endX = center.X + Math.Sin(end) * radius, endZ = center.Z + Math.Cos(end) * radius;
            points.Add(new StandardCourse.Point(endX, endZ));
            points.Add(new StandardCourse.Point(endX + Math.Sin(outgoing) * 8, endZ + Math.Cos(outgoing) * 8));
            return points;
        }
        private static double Distance(ReinsRun run, StandardCourse.Point point)
            => Math.Sqrt((run.X - point.X) * (run.X - point.X) + (run.Z - point.Z) * (run.Z - point.Z));
        private static double PositiveAngle(double value)
        { while (value < 0) value += Math.PI * 2; while (value >= Math.PI * 2) value -= Math.PI * 2; return value; }
        private static void FeedApproach(ReinsCourseJudge judge, StandardCourse.Point center, double angle, double radius)
        {
            for (double r = 6; r > radius; r -= .25)
                judge.Step(center.X + Math.Sin(angle) * r, center.Z + Math.Cos(angle) * r,
                    center.X + Math.Sin(angle) * (r - .25), center.Z + Math.Cos(angle) * (r - .25));
        }
        private static double FeedLoop(ReinsCourseJudge judge, StandardCourse.Point center, double radius, int direction, double angle, double travel)
        {
            int count = (int)Math.Ceiling(travel / .04);
            for (int i = 0; i < count; i++)
            {
                double a = angle + direction * travel * i / count, b = angle + direction * travel * (i + 1) / count;
                judge.Step(center.X + Math.Sin(a) * radius, center.Z + Math.Cos(a) * radius,
                    center.X + Math.Sin(b) * radius, center.Z + Math.Cos(b) * radius);
            }
            return angle + direction * travel;
        }
    }
}
