using System;

namespace BarrelRivals.Core
{
    public readonly struct PracticePose
    {
        public double X { get; }
        public double Z { get; }
        public double HeadingDegrees { get; }
        public PracticePose(double x,double z,double heading) { X=x; Z=z; HeadingDegrees=heading; }
    }

    /// <summary>Version 1 one-barrel practice path. Kinematic rules; Unity physics does not decide results.</summary>
    public static class PracticePath
    {
        public static PracticePose Sample(PracticeRun run)
        {
            var center=StandardCourse.Barrel(0); double p=run.PhaseProgress;
            double radius=FinalRadius(run.DrawingGrade.Grade);
            switch(run.Phase)
            {
                case PracticePhase.Alley:
                    double u=p*p*(2-p), v=1-u, x=center.X+2.2, z=center.Z-5;
                    double px=3*v*u*u*x+u*u*u*x;
                    double pz=v*v*v*(-10)+3*v*u*u*(z-6)+u*u*u*z;
                    double dx=6*v*u*x;
                    double dz=30*v*v+6*v*u*(z-6)+18*u*u;
                    return new PracticePose(px,pz,Math.Atan2(dx,dz)*180/Math.PI);
                case PracticePhase.Preview: return new PracticePose(center.X+2.2,center.Z-5+5*p,0);
                case PracticePhase.Drawing: return Arc(center.X,center.Z,2.2,Math.PI*.5*p,Math.PI*.5*p);
                case PracticePhase.Turn:
                    var here=TurnPoint(p,run.DrawingGrade);
                    var before=TurnPoint(Math.Max(0,p-.001),run.DrawingGrade);
                    var after=TurnPoint(Math.Min(1,p+.001),run.DrawingGrade);
                    return new PracticePose(here.X,here.Y,Math.Atan2(after.X-before.X,after.Y-before.Y)*180/Math.PI);
                case PracticePhase.Exit: return new PracticePose(center.X+3*p,center.Z-radius,90);
                case PracticePhase.RunOut: return new PracticePose(center.X+3+4*p,center.Z-radius,90);
                case PracticePhase.Complete: return new PracticePose(center.X+7,center.Z-radius,90);
                default: return new PracticePose(0,-10,0);
            }
        }
        private static PracticePose Arc(double x,double z,double radius,double angle,double heading)
            => new PracticePose(x+radius*Math.Cos(angle),z+radius*Math.Sin(angle),-heading*180/Math.PI);
        private static TracePoint TurnPoint(double p,TraceEvaluation grade)
        {
            var center=StandardCourse.Barrel(0); double end=FinalRadius(grade.Grade);
            double radius=2.2+(end-2.2)*Smooth(p);
            if(grade.Quality<35)
                radius=p<=1.0/3 ? 2.2+(.7-2.2)*Smooth(p*3) : .7+(end-.7)*Smooth((p-1.0/3)*1.5);
            double angle=Math.PI*.5+Math.PI*p;
            return new TracePoint(center.X+radius*Math.Cos(angle),center.Z+radius*Math.Sin(angle));
        }
        private static double Smooth(double value) => value*value*(3-2*value);
        public static double FinalRadius(SkillGrade grade) => grade==SkillGrade.Perfect ? 2.2 : grade==SkillGrade.Great ? 2.65 : grade==SkillGrade.Good ? 3.2 : 4.1;
    }
}
