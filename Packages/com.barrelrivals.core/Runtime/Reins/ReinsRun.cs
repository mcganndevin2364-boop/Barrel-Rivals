using System;

namespace BarrelRivals.Core.Reins
{
    /// <summary>Local Reins Racing v2. Fixed 20ms simulation, prototype double geometry, no trusted rewards.
    /// A Step consumes exactly one frame; callers must not use variable render delta as simulation time.</summary>
    public sealed class ReinsRun
    {
        public const int RulesVersion = 2;
        public const int StepMs = 20;
        public const int MaximumTicks = 7500;
        public const int ApproachMs = 4000;
        public const double ApproachSpeed = 1.5;
        public const int FirstLaunchCueMs = 2000;
        public const int LaunchCueIntervalMs = 1000;
        public const int LaunchDeadlineMs = 400;
        public const int LaunchAccelerationMs = 1200;
        public const int FirstCadenceBeatMs = 600;
        public const int DriveWindowMs = 4000;
        public const double AbsoluteSpeedCap = 14.0;
        public const double AbsoluteYawRateCap = 3.4;
        public const int WrapBudgetPerBarrelMs = 2000;
        public ReinsManifest Manifest { get; }
        public ReinsPhase Phase { get; private set; } = ReinsPhase.Ready;
        public int Tick { get; private set; }
        public long ElapsedMs => Tick * (long)StepMs;
        public long PhaseElapsedMs => (Tick - _phaseStartedTick) * (long)StepMs;
        public long RaceTimeMs { get; private set; }
        public long FinalTimeMs => Phase == ReinsPhase.Complete ? RaceTimeMs + KnockCount * 5000L : 0;
        public double X { get; private set; }
        public double Z { get; private set; } = -6;
        public double HeadingRadians { get; private set; }
        public double SpeedMetresPerSecond { get; private set; }
        public double YawRateRadiansPerSecond { get; private set; }
        public int BarrelIndex => _course.BarrelIndex;
        public int KnockCount => _course.KnockCount;
        public int StylePoints => _course.StylePoints;
        public ReinsLaunchOutcome LaunchOutcome { get; private set; } = ReinsLaunchOutcome.Pending;
        public int? LaunchReleaseErrorMs { get; private set; }
        public bool LaunchResolved => LaunchOutcome != ReinsLaunchOutcome.Pending;
        public long NextCadenceBeatMs => _nextBeatMs;
        public int CadencePeriodMs { get; private set; } = 500;
        public double CadencePhase01 => ReinsMath.Clamp(1 - (_nextBeatMs - RaceTimeMs) / (double)CadencePeriodMs, 0, 1);
        public int CadenceStreak { get; private set; }
        public int CadenceAwards { get; private set; }
        public int CadenceMisses { get; private set; }
        public double CadencePace01 => _pace;
        public ReinsTimingGrade LastCadenceGrade { get; private set; }
        public bool HotHooves => RaceTimeMs < _hotUntilMs && RaceTimeMs >= _blazingUntilMs;
        public bool BlazingHooves => RaceTimeMs < _blazingUntilMs;
        public bool WrapActive { get; private set; }
        public int WrapRemainingMs => _wrapRemainingTicks * StepMs;
        public int DriveAcceptedTaps { get; private set; }
        public long DriveRemainingMs => Phase == ReinsPhase.Drive ? Math.Max(0, DriveWindowMs - (RaceTimeMs - _driveStartedMs)) : 0;
        public double TurnProgress01 => _course.TurnProgress01;
        public bool TurnActive => _course.TurnActive;
        public ReinsSurface CurrentSurface => Manifest.SurfaceAt(X, Z);
        public double DistanceToBarrel
        {
            get
            {
                var target = BarrelIndex < 3 ? StandardCourse.Barrel(BarrelIndex) : new StandardCourse.Point(0, 0);
                return ReinsMath.Distance(X, Z, target.X, target.Z);
            }
        }
        public ReinsPocketZone PocketZone => BarrelIndex < 3 ? ReinsCourseJudge.ZoneForDistance(DistanceToBarrel) : ReinsPocketZone.None;
        public string LastFeedback { get; private set; } = "Hold to approach.";
        public int FeedbackTick { get; private set; }
        public bool IsTerminal => Phase == ReinsPhase.Complete || Phase == ReinsPhase.Cancelled || Phase == ReinsPhase.TimedOut;
        private readonly ReinsCourseJudge _course = new ReinsCourseJudge();
        private int _phaseStartedTick, _raceStartedTick, _wrapRemainingTicks = WrapBudgetPerBarrelMs / StepMs;
        private long _nextBeatMs, _recoveryUntilMs, _hotUntilMs, _blazingUntilMs;
        private long _pocketBoostUntilMs, _driveStartedMs, _lastDriveTapMs = -1000;
        private double _pace = .90, _pocketBoost, _driveBoost;
        private int _driveStreak;
        private bool _beatResolved;
        private DriveSide _lastDriveSide;

        public ReinsRun(ReinsManifest manifest) { Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest)); }
        public bool IsBarrelKnocked(int index) => _course.IsBarrelKnocked(index);
        public bool Start()
        {
            if (Phase != ReinsPhase.Ready) return false;
            SpeedMetresPerSecond = ApproachSpeed;
            Enter(ReinsPhase.Approach); Say("Hold through the alley. Release on the third beep."); return true;
        }
        public bool Cancel()
        {
            if (Phase == ReinsPhase.Ready || IsTerminal) return false;
            SpeedMetresPerSecond = YawRateRadiansPerSecond = 0; WrapActive = false;
            Enter(ReinsPhase.Cancelled); Say("Lab run cancelled. No race result or reward."); return true;
        }
        public void Step(ReinsInput input)
        {
            if (Phase == ReinsPhase.Ready || IsTerminal) return;
            Tick++;
            if (Tick >= MaximumTicks)
            {
                if (_raceStartedTick > 0) RaceTimeMs = (Tick - _raceStartedTick) * (long)StepMs;
                SpeedMetresPerSecond = YawRateRadiansPerSecond = 0; WrapActive = false;
                Enter(ReinsPhase.TimedOut); Say("Practice time limit reached. Retry the route."); return;
            }
            if (Phase == ReinsPhase.Approach)
            {
                // The accepted center down arms the hold at tick zero. The first false sample
                // is a release even when down/up happened inside the first 20ms interval.
                ResolveLaunch(input.LaunchHeld);
                X = 0; Z = -6 + ApproachSpeed * Math.Min(ElapsedMs, ApproachMs) / 1000.0;
                HeadingRadians = YawRateRadiansPerSecond = 0;
                SpeedMetresPerSecond = ApproachSpeed;
                if (ElapsedMs >= ApproachMs) BeginRace();
                return;
            }
            RaceTimeMs = (Tick - _raceStartedTick) * (long)StepMs;
            if (Phase == ReinsPhase.Drive && RaceTimeMs - _driveStartedMs >= DriveWindowMs)
            { _driveBoost = 0; Enter(ReinsPhase.Racing); Say("Drive window complete. Cross the finish between the gate posts."); }
            WrapActive = input.Wrap && BarrelIndex < 3 && DistanceToBarrel <= ReinsCourseJudge.RiskOuterRadius && _wrapRemainingTicks > 0;
            if (WrapActive) _wrapRemainingTicks--;
            if (BarrelIndex < 3) StepCadence(input.CadenceTap && !WrapActive);
            if (Phase == ReinsPhase.Drive && input.Drive != DriveSide.None) StepDrive(input.Drive);
            Move(input);
            // Resolve AFTER this tick's movement: late releases never alter a consumed step.
            ResolveLaunch(input.LaunchHeld);
        }
        private void ResolveLaunch(bool held)
        {
            if (LaunchResolved) return;
            if (!held)
            {
                int error = (int)(ElapsedMs - ApproachMs);
                LaunchReleaseErrorMs = error;
                int distance = Math.Abs(error);
                LaunchOutcome = distance <= 120 ? ReinsLaunchOutcome.Perfect
                    : distance <= 240 ? ReinsLaunchOutcome.Good : ReinsLaunchOutcome.Weak;
                Say(LaunchOutcome == ReinsLaunchOutcome.Perfect ? "PERFECT RELEASE!"
                    : LaunchOutcome == ReinsLaunchOutcome.Good ? "GOOD RELEASE!" : "Keep riding. Release on the third beep next time.");
            }
            else if (ElapsedMs >= ApproachMs + LaunchDeadlineMs)
            {
                LaunchOutcome = ReinsLaunchOutcome.TimedOut;
                Say("Keep riding. Lift, then tap the heartbeat.");
            }
        }
        private void BeginRace()
        {
            X = Z = HeadingRadians = YawRateRadiansPerSecond = 0;
            Enter(ReinsPhase.Racing); _raceStartedTick = Tick; RaceTimeMs = 0;
            SpeedMetresPerSecond = ApproachSpeed;
            _nextBeatMs = FirstCadenceBeatMs;
            if (!LaunchResolved) Say("GO! Release and ride toward the left barrel.");
        }
        private void StepCadence(bool tap)
        {
            if (RaceTimeMs > _nextBeatMs + 140)
            {
                if (!_beatResolved && !HotHooves && !BlazingHooves) CadenceMiss();
                double fraction = ReinsMath.Clamp(SpeedMetresPerSecond / 10.0, 0, 1);
                // Accessible 20ms-quantized cadence, from 2.0 to approximately 2.78 Hz.
                CadencePeriodMs = RaceTimeMs < _recoveryUntilMs ? 500 : (int)Math.Round((500 - 140 * fraction) / StepMs) * StepMs;
                CadencePeriodMs = Math.Max(360, Math.Min(500, CadencePeriodMs));
                _nextBeatMs += CadencePeriodMs; _beatResolved = false;
            }
            if (!tap || _beatResolved) return;
            _beatResolved = true;
            long error = Math.Abs(RaceTimeMs - _nextBeatMs);
            if (error > 140) { CadenceMiss(); return; }
            LastCadenceGrade = error <= 60 ? ReinsTimingGrade.Perfect : error <= 100 ? ReinsTimingGrade.Great : ReinsTimingGrade.Good;
            CadenceAwards++;
            if (LastCadenceGrade == ReinsTimingGrade.Perfect)
            {
                _pace = Math.Min(1, _pace + .025); CadenceStreak++;
                if (CadenceStreak == 5) { _hotUntilMs = RaceTimeMs + 2000; Say("HOT HOOVES! Two seconds of full cadence pace."); }
                else if (CadenceStreak == 10) { _blazingUntilMs = RaceTimeMs + 3000; Say("BLAZING HOOVES! Three seconds with a capped burst."); }
                else Say("Perfect cadence.");
            }
            else
            {
                CadenceStreak = 0;
                if (LastCadenceGrade == ReinsTimingGrade.Great) _pace = Math.Min(1, _pace + .01);
                Say(LastCadenceGrade == ReinsTimingGrade.Great ? "Great cadence." : "Good cadence. Aim at the ring's peak.");
            }
        }
        private void CadenceMiss()
        {
            LastCadenceGrade = ReinsTimingGrade.Miss; CadenceMisses++; CadenceStreak = 0;
            _pace = Math.Max(.72, _pace - .04); _recoveryUntilMs = RaceTimeMs + 1500;
            _hotUntilMs = _blazingUntilMs = 0; Say("Cadence missed. Follow the slower ring to recover.");
        }
        private void StepDrive(DriveSide side)
        {
            // Faster tapping cannot exceed this accessible ~5.5 Hz accepted-event rate.
            if (RaceTimeMs - _lastDriveTapMs < 180 || DriveAcceptedTaps >= 20) return;
            if (side == _lastDriveSide) { _driveStreak = 0; _driveBoost = 0; Say("Alternate left, then right for the drive."); return; }
            _lastDriveSide = side; _lastDriveTapMs = RaceTimeMs; DriveAcceptedTaps++; _driveStreak++;
            _driveBoost = Math.Min(.20, _driveStreak * .025);
            Say(_driveStreak >= 8 ? "FULL DRIVE! Keep the reins aimed at the finish." : "Drive: alternate at a steady rhythm.");
        }
        private void Move(ReinsInput input)
        {
            const double dt = StepMs / 1000.0;
            var horse = Manifest.Horse; var surface = Manifest.TuningAt(X, Z);
            double left = input.LeftPermille / 1000.0, right = input.RightPermille / 1000.0;
            double brake = Math.Min(left, right);
            // Counter-rein pressure can slow a turn while preserving useful steering range.
            double steering = ReinsMath.Clamp((right - left) / (1 - brake * .8), -1, 1);
            double top = 10 * (.96 + .08 * horse.FirePermille / 1000.0) *
                (1 + Manifest.RoundIndex * .01 * (horse.HeartPermille / 1000.0 - .5));
            double boost = Math.Max(RaceTimeMs < _pocketBoostUntilMs ? _pocketBoost : 0, Math.Max(BlazingHooves ? .08 : 0, _driveBoost));
            double pace = HotHooves || BlazingHooves ? 1 : _pace;
            double targetSpeed = top * surface.SpeedMultiplier * pace * (1 + Math.Min(.25, boost)) *
                (1 - .35 * Math.Abs(steering)) * (1 - brake) * (WrapActive ? .90 : 1);
            targetSpeed = ReinsMath.Clamp(targetSpeed, 0, AbsoluteSpeedCap);
            double acceleration = targetSpeed > SpeedMetresPerSecond ? 4.8 : 7.5 * (1.04 - .08 * horse.FirePermille / 1000.0);
            if (targetSpeed > SpeedMetresPerSecond && RaceTimeMs < LaunchAccelerationMs)
                acceleration *= LaunchOutcome == ReinsLaunchOutcome.Perfect ? 1.35 : LaunchOutcome == ReinsLaunchOutcome.Good ? 1.15 : 1;
            SpeedMetresPerSecond += ReinsMath.Clamp(targetSpeed - SpeedMetresPerSecond, -acceleration * dt, acceleration * dt);
            double grip = surface.GripMultiplier * (.94 + .12 * horse.NervePermille / 1000.0);
            double yaw = 2.8 * (.92 + .16 * horse.BiddabilityPermille / 1000.0) * grip * (WrapActive ? 1.15 : 1);
            yaw *= 1 - .35 * ReinsMath.Clamp(SpeedMetresPerSecond / Math.Max(1, top), 0, 1);
            double minimumRadius = WrapActive ? .98 : 1.15;
            double yawLimit = Math.Min(AbsoluteYawRateCap, SpeedMetresPerSecond / minimumRadius);
            YawRateRadiansPerSecond = ReinsMath.Clamp(steering * yaw, -yawLimit, yawLimit);
            HeadingRadians = ReinsMath.Angle(HeadingRadians + YawRateRadiansPerSecond * dt);
            double previousX = X, previousZ = Z;
            double nextX = X + Math.Sin(HeadingRadians) * SpeedMetresPerSecond * dt;
            double nextZ = Z + Math.Cos(HeadingRadians) * SpeedMetresPerSecond * dt;
            X = ReinsMath.Clamp(nextX, -26.5, 26.5); Z = ReinsMath.Clamp(nextZ, -15, 62.5);
            if (X != nextX || Z != nextZ) SpeedMetresPerSecond = Math.Min(SpeedMetresPerSecond, 1);
            ReinsAlley.Sweep(previousX, previousZ, X, Z, ReinsCourseJudge.HorseRadius, out double slideX, out double slideZ);
            X = slideX; Z = slideZ;
            int previousKnocks = KnockCount;
            bool completedTurn = _course.Step(previousX, previousZ, X, Z);
            if (KnockCount > previousKnocks)
            { SpeedMetresPerSecond *= .85; _pocketBoostUntilMs = 0; Say("Barrel contact: +5 seconds, once for this barrel."); }
            if (completedTurn)
            {
                ReinsPocketZone zone = _course.LastCompletedZone;
                _pocketBoost = zone == ReinsPocketZone.Kiss ? .25 : zone == ReinsPocketZone.Pocket ? .18 : zone == ReinsPocketZone.Risk ? .08 : 0;
                _pocketBoostUntilMs = RaceTimeMs + 1500; _wrapRemainingTicks = WrapBudgetPerBarrelMs / StepMs;
                Say(zone == ReinsPocketZone.Kiss ? "BARREL KISS! Clean turn and 1,000 local style points." : "Barrel " + BarrelIndex + " complete. " + zone + " exit.");
                if (BarrelIndex == 3)
                {
                    Enter(ReinsPhase.Drive); _driveStartedMs = RaceTimeMs; _lastDriveTapMs = RaceTimeMs - 180;
                    Say("HOME DRIVE! Alternate left and right; steer through the finish posts.");
                }
            }
            // A valid three-turn route must cross the actual line from the course side through its gate.
            if (BarrelIndex == 3 && previousZ > 0 && Z <= 0)
            {
                double fraction = previousZ / (previousZ - Z);
                double crossingX = previousX + (X - previousX) * fraction;
                if (Math.Abs(crossingX) <= 6)
                { Enter(ReinsPhase.Complete); WrapActive = false; SpeedMetresPerSecond = YawRateRadiansPerSecond = 0; Say("Three legal turns. Lab run complete."); }
            }
        }
        private void Enter(ReinsPhase phase) { Phase = phase; _phaseStartedTick = Tick; }
        private void Say(string message) { LastFeedback = message; FeedbackTick = Tick; }
    }
}
