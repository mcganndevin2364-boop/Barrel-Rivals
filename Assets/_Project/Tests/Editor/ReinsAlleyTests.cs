using System;
using BarrelRivals.Core.Reins;
using NUnit.Framework;

namespace BarrelRivals.Tests
{
    public sealed class ReinsAlleyTests
    {
        private const double Radius = ReinsCourseJudge.HorseRadius;
        [TestCase(-1)] [TestCase(1)]
        public void FastCrossingCannotTunnelAndObliqueMotionSlides(int side)
        {
            Assert.IsTrue(ReinsAlley.Sweep(0, -6, side * 20, -4, Radius, out double x, out double z));
            Assert.That(x, Is.EqualTo(side * (3 - .47)).Within(1e-6));
            Assert.That(z, Is.EqualTo(-4).Within(1e-6));
            Assert.IsFalse(ReinsAlley.Sweep(x, z, 0, -3, Radius, out x, out z));
            Assert.That(x, Is.EqualTo(0).Within(1e-6));
        }

        [TestCase(-1)] [TestCase(1)]
        public void WrongWayApproachHitsFrontCapsAndOutsideCannotEnterThroughRails(int side)
        {
            Assert.IsTrue(ReinsAlley.Sweep(side * 3, 1, side * 3, -8, Radius, out double x, out double z));
            Assert.That(z, Is.EqualTo(-1.03).Within(1e-6));
            Assert.IsTrue(ReinsAlley.Sweep(side * 8, -5, 0, -5, Radius, out x, out z));
            Assert.That(x, Is.EqualTo(side * 3.47).Within(1e-6));
        }

        [Test] public void FullFinishGateAndCenterAlleyRemainClearInBothDirections()
        {
            Assert.LessOrEqual(ReinsAlley.FrontZ + ReinsAlley.RailRadius + Radius, -1);
            for (int i = -60; i <= 60; i++)
            {
                double gateX = i / 10.0;
                Assert.IsFalse(ReinsAlley.Sweep(gateX, .8, gateX, -.8, Radius, out double x, out double z));
                Assert.AreEqual(gateX, x); Assert.AreEqual(-.8, z);
                Assert.IsFalse(ReinsAlley.Sweep(gateX, -.8, gateX, .8, Radius, out x, out z));
            }
            Assert.IsFalse(ReinsAlley.Sweep(0, -14, 0, 3, Radius, out _, out _));
            Assert.IsFalse(ReinsAlley.Sweep(0, 3, 0, -14, Radius, out _, out _));
        }

        [Test] public void InitialOverlapIsResolvedAndNonFiniteInputRejected()
        {
            Assert.IsTrue(ReinsAlley.Sweep(3, -5, 3, -5, Radius, out double x, out double z));
            Assert.That(x, Is.EqualTo(2.53).Within(1e-6)); Assert.AreEqual(-5, z);
            Assert.Throws<ArgumentOutOfRangeException>(() => ReinsAlley.Sweep(double.NaN, 0, 1, 1, Radius, out _, out _));
        }
    }
}
