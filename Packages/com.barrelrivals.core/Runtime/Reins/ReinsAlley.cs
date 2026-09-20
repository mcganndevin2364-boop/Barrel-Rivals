using System;

namespace BarrelRivals.Core.Reins
{
    /// <summary>Shared visible rails and deterministic swept horse contact. No barrel penalty.
    /// Two finite capsules leave more than one metre clear behind the entire finish plane.</summary>
    public static class ReinsAlley
    {
        public const double HalfWidth = 3;
        public const double BackZ = -12;
        public const double FrontZ = -1.5;
        public const double RailRadius = .05;
        private const double Separation = 1e-8;

        public static bool Sweep(double fromX, double fromZ, double toX, double toZ, double horseRadius,
            out double x, out double z)
        {
            if (!ReinsMath.Finite(fromX) || !ReinsMath.Finite(fromZ) || !ReinsMath.Finite(toX) ||
                !ReinsMath.Finite(toZ) || !ReinsMath.Finite(horseRadius) || horseRadius <= 0 || horseRadius >= HalfWidth - RailRadius)
                throw new ArgumentOutOfRangeException(nameof(horseRadius));
            double radius = horseRadius + RailRadius;
            double dx = toX - fromX, dz = toZ - fromZ;
            x = fromX; z = fromZ;
            bool contact = Depenetrate(-HalfWidth, radius, ref x, ref z);
            contact |= Depenetrate(HalfWidth, radius, ref x, ref z);
            // Each collision removes inward motion; the bounded iterations also handle a
            // displacement crossing both rails. Unlike endpoint clamping, this cannot tunnel.
            for (int iteration = 0; iteration < 4; iteration++)
            {
                double time = 2, nx = 0, nz = 0;
                Hit(-HalfWidth, radius, x, z, dx, dz, ref time, ref nx, ref nz);
                Hit(HalfWidth, radius, x, z, dx, dz, ref time, ref nx, ref nz);
                if (time > 1) { x += dx; z += dz; return contact; }
                contact = true;
                x += dx * time + nx * Separation; z += dz * time + nz * Separation;
                dx *= 1 - time; dz *= 1 - time;
                double inward = Math.Min(0, dx * nx + dz * nz);
                dx -= inward * nx; dz -= inward * nz;
                if (dx * dx + dz * dz < 1e-20) return true;
            }
            // Conservative stop at the last safe contact if the iteration bound is reached.
            return contact;
        }

        private static bool Depenetrate(double wallX, double radius, ref double x, ref double z)
        {
            double nearestZ = ReinsMath.Clamp(z, BackZ, FrontZ);
            double nx = x - wallX, nz = z - nearestZ;
            double distance = Math.Sqrt(nx * nx + nz * nz);
            if (distance >= radius) return false;
            if (distance < 1e-12) { nx = wallX < 0 ? 1 : -1; nz = 0; }
            else { nx /= distance; nz /= distance; }
            x = wallX + nx * (radius + Separation); z = nearestZ + nz * (radius + Separation);
            return true;
        }

        private static void Hit(double wallX, double radius, double x, double z, double dx, double dz,
            ref double first, ref double normalX, ref double normalZ)
        {
            if (Math.Abs(dx) > 1e-14)
            {
                double nx = dx > 0 ? -1 : 1;
                double time = (wallX + nx * radius - x) / dx;
                double atZ = z + dz * time;
                if (time >= 0 && time <= 1 && time < first && atZ >= BackZ && atZ <= FrontZ)
                { first = time; normalX = nx; normalZ = 0; }
            }
            Endcap(wallX, BackZ, radius, x, z, dx, dz, ref first, ref normalX, ref normalZ);
            Endcap(wallX, FrontZ, radius, x, z, dx, dz, ref first, ref normalX, ref normalZ);
        }

        private static void Endcap(double cx, double cz, double radius, double x, double z, double dx, double dz,
            ref double first, ref double normalX, ref double normalZ)
        {
            double ox = x - cx, oz = z - cz, a = dx * dx + dz * dz;
            if (a < 1e-20) return;
            double b = ox * dx + oz * dz, c = ox * ox + oz * oz - radius * radius;
            double discriminant = b * b - a * c;
            if (discriminant < 0) return;
            double time = (-b - Math.Sqrt(discriminant)) / a;
            if (time < 0 || time > 1 || time >= first) return;
            double nx = (ox + dx * time) / radius, nz = (oz + dz * time) / radius;
            if (dx * nx + dz * nz >= -1e-12) return;
            first = time; normalX = nx; normalZ = nz;
        }
    }
}
