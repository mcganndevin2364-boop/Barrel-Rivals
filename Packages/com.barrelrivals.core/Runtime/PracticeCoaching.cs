namespace BarrelRivals.Core
{
    public enum PracticeCoachFocus { Ready, Launch, Drawing, Exit, Consistency, Interrupted }

    public readonly struct PracticeCoachTip
    {
        public PracticeCoachFocus Focus { get; }
        public string Message { get; }
        internal PracticeCoachTip(PracticeCoachFocus focus, string message) { Focus = focus; Message = message; }
    }

    public static class PracticeCoaching
    {
        /// <summary>One useful next action from observed practice input; never invents an early/late exit.</summary>
        public static PracticeCoachTip ForRun(PracticeRun run)
        {
            if (run == null || (run.Phase != PracticePhase.Complete && run.Phase != PracticePhase.Cancelled))
                return Tip(PracticeCoachFocus.Ready, "Finish a practice run to see your next tip.");
            if (run.Phase == PracticePhase.Cancelled)
                return Tip(PracticeCoachFocus.Interrupted, "Try again when you are ready; this attempt was not recorded.");
            // A knock or invalid trace is the most actionable large loss, even if another skill was also missed.
            if (run.DrawingGrade.Reason != TraceReason.Accepted)
                return Tip(PracticeCoachFocus.Drawing, DrawingHint(run.DrawingGrade.Reason));
            if (run.AutoLaunched)
                return Tip(PracticeCoachFocus.Launch, "You missed the release. Lift your finger on the third beep.");
            if (run.LaunchGrade <= run.DrawingGrade.Grade && run.LaunchGrade <= run.ExitGrade && run.LaunchGrade != SkillGrade.Perfect)
                return Tip(PracticeCoachFocus.Launch, run.LaunchErrorMs < 0
                    ? "You released early. Keep holding until the third beep."
                    : "You released late. Lift your finger as the third beep sounds.");
            if (run.DrawingGrade.Grade <= run.ExitGrade && run.DrawingGrade.Grade != SkillGrade.Perfect)
                return Tip(PracticeCoachFocus.Drawing, run.DrawingGrade.Quality < 90
                    ? "Match the shape's proportions and close the outline before lifting."
                    : "Your outline is clean. Finish it a little sooner without losing its shape.");
            if (!run.ExitAccepted)
                return Tip(PracticeCoachFocus.Exit, "Lift after drawing, then make a new tap at the exit bar's center.");
            if (run.ExitGrade != SkillGrade.Perfect)
                return Tip(PracticeCoachFocus.Exit, "Aim your fresh exit tap at the bar's center mark.");
            return Tip(PracticeCoachFocus.Consistency, "All three skills were Perfect. Try the same challenge to repeat that clean run.");
        }

        public static string DrawingHint(TraceReason reason)
        {
            switch (reason)
            {
                case TraceReason.Missing: return "Draw the whole shape before the timer ends, then lift your finger.";
                case TraceReason.Invalid: return "Keep one steady stroke inside the drawing pad, then lift.";
                case TraceReason.TooSmall: return "Use more of the pad so your outline is large and clear.";
                case TraceReason.OpenShape: return "Bring the end of your outline back to its starting point.";
                case TraceReason.ExcessLength: return "Draw the outline once, without retracing or adding loops.";
                case TraceReason.LowAccuracy: return "Remember the shape's proportions and follow its outline more closely.";
                default: return "Shape accepted.";
            }
        }

        private static PracticeCoachTip Tip(PracticeCoachFocus focus, string message) => new PracticeCoachTip(focus, message);
    }
}
