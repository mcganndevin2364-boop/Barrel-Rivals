using System.Text.Json;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using BarrelRivals.Core;
using BarrelRivals.Core.Reins;

// Development-only steering policy: generates inputs, never edits state or bypasses the course judge.
// This is a regression fixture, not a production opponent or a demonstration of human skill.
internal static class FixtureGenerator
{
    internal static void Generate()
    {
        var manifest = new ReinsManifest(104, ReinsSurface.HardPack);
        var run = new ReinsRun(manifest);
        var inputs = new List<ReinsInput>();
        var frames = new List<object>();
        int routeIndex = -1, waypoint = 0;
        List<StandardCourse.Point> route = new();
        run.Start();
        while (!run.IsTerminal)
        {
            ReinsInput input;
            if (run.Phase == ReinsPhase.Approach)
                input = new ReinsInput(launchHeld: run.Tick + 1 < 200);
            else
            {
                if (routeIndex != run.BarrelIndex)
                {
                    routeIndex = run.BarrelIndex; waypoint = 0; route = Route(routeIndex);
                    Console.WriteLine($"Route {routeIndex}, tick {run.Tick}, X {run.X:F4}, Z {run.Z:F4}");
                }
                while (waypoint < route.Count - 1 && Distance(run, route[waypoint]) < 1.0) waypoint++;
                var drive = run.Phase == ReinsPhase.Drive && run.DriveRemainingMs % 200 == 0
                    ? (run.DriveAcceptedTaps % 2 == 0 ? DriveSide.Left : DriveSide.Right) : DriveSide.None;
                input = Toward(run, route[waypoint].X, route[waypoint].Z, run.BarrelIndex < 3 ? .55 : 0, drive);
            }
            inputs.Add(input);
            frames.Add(new { tick = inputs.Count, leftPermille = input.LeftPermille, rightPermille = input.RightPermille,
                cadenceTap = input.CadenceTap, launchHeld = input.LaunchHeld, wrap = input.Wrap, drive = input.Drive.ToString() });
            run.Step(input);
        }
        Console.WriteLine($"Terminal {run.Phase}; tick={run.Tick}; barrel={run.BarrelIndex}; waypoint={waypoint}/{route.Count}; X={run.X:R}; Z={run.Z:R}; finalTimeMs={run.FinalTimeMs}");
        if (run.Phase != ReinsPhase.Complete || run.BarrelIndex != 3
            || !ReinsReplay.TryCreate(ReinsRun.RulesVersion, manifest, inputs, out _))
            throw new InvalidOperationException("Steering policy did not produce a complete, independently replayable run. No fixture written.");
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Contracts", "Reins");
        if (!File.Exists(Path.Combine(directory, "verify-request.v2.schema.json")))
            throw new InvalidOperationException("Run through run.sh from the repository containing the Reins contracts.");
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(new
        {
            contractVersion = 2,
            ruleFingerprint = ReinsRuleFingerprint.Sha256,
            launchInitiallyHeld = true,
            manifest = new { rulesVersion = ReinsRun.RulesVersion, courseId = "reins-v2", seed = manifest.Seed, surface = manifest.Surface.ToString(),
                roundIndex = manifest.RoundIndex, horse = new { nervePermille = 500, firePermille = 500, biddabilityPermille = 500, heartPermille = 500 } },
            frames
        });
        using var document = JsonDocument.Parse(json);
        var response = ReplayVerifier.Verify(document.RootElement, CancellationToken.None);
        File.WriteAllBytes(Path.Combine(directory, "complete-request.v2.json"), json);
        File.WriteAllText(Path.Combine(directory, "complete-response.v2.json"), JsonSerializer.Serialize(response,
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) + "\n");
        string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        string[] sources = { "ReinsContracts.cs", "ReinsCourseJudge.cs", "ReinsRun.cs", "ReinsReplay.cs", "ReinsAlley.cs", "../StandardCourse.cs" };
        string coreDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Packages/com.barrelrivals.core/Runtime/Reins");
        var provenance = new
        {
            contractVersion = 2, rulesVersion = ReinsRun.RulesVersion, courseId = "reins-v2",
            generatedAtUtc = DateTimeOffset.UtcNow, ruleFingerprint = ReinsRuleFingerprint.Sha256,
            scope = "offline-consistency", runtime = RuntimeInformation.FrameworkDescription,
            generationCommand = "bash Tools/ReinsServerCheck/run.sh generate-fixture",
            generatorSource = "Tools/ReinsServerCheck/FixtureGenerator.cs",
            generatorSourceSha256 = Digest("Tools/ReinsServerCheck/FixtureGenerator.cs"),
            compiledCoreAssemblySha256 = Digest(typeof(ReinsRun).Assembly.Location),
            sourceSnapshotSha256 = sources.ToDictionary(name => name, name => Digest(Path.Combine(coreDirectory, name))),
            requestFileSha256 = Digest(Path.Combine(directory, "complete-request.v2.json")),
            responseFileSha256 = Digest(Path.Combine(directory, "complete-response.v2.json")),
            frameCount = frames.Count, launchInitiallyHeld = true,
            result = new { run.RaceTimeMs, run.FinalTimeMs, run.KnockCount, run.StylePoints,
                launchOutcome = run.LaunchOutcome.ToString(), launchReleaseErrorMs = run.LaunchReleaseErrorMs },
            limits = new[] { "Development steering policy, not player telemetry or proof of human input.",
                "Expected response uses the same shared rules; HTTP/Python verification is recorded separately.",
                "Unity and native IL2CPP parity, phone acceptance, trusted service and persistence are separate gates." }
        };
        File.WriteAllText(Path.Combine(directory, "fixture-provenance.v2.json"), JsonSerializer.Serialize(provenance,
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) + "\n");
        Console.WriteLine($"Wrote {frames.Count} frames ({json.Length} bytes), expected response and provenance to {directory}");
    }

    private static ReinsInput Toward(ReinsRun run, double x, double z, double brake, DriveSide drive)
    {
        double angle = Math.Atan2(x - run.X, z - run.Z) - run.HeadingRadians;
        while (angle > Math.PI) angle -= Math.PI * 2;
        while (angle < -Math.PI) angle += Math.PI * 2;
        double steering = Math.Max(-1, Math.Min(1, angle * 1.8));
        int left = (int)Math.Round(1000 * (brake + Math.Max(0, -steering) * (1 - brake)));
        int right = (int)Math.Round(1000 * (brake + Math.Max(0, steering) * (1 - brake)));
        return new ReinsInput(left, right, cadenceTap: run.RaceTimeMs + ReinsRun.StepMs == run.NextCadenceBeatMs, drive: drive);
    }
    private static List<StandardCourse.Point> Route(int index)
    {
        if (index == 3) return new List<StandardCourse.Point> { new(0, -5) };
        var center = StandardCourse.Barrel(index);
        var source = index == 0 ? new StandardCourse.Point(0, 0) : StandardCourse.Barrel(index - 1);
        var next = index == 2 ? new StandardCourse.Point(0, 0) : StandardCourse.Barrel(index + 1);
        double angle = Math.Atan2(source.X - center.X, source.Z - center.Z);
        double outgoing = Math.Atan2(next.X - center.X, next.Z - center.Z);
        int direction = ReinsCourseJudge.TurnDirection(index);
        double travel = PositiveAngle((outgoing - direction * Math.PI / 2 - angle) * direction);
        if (travel < ReinsCourseJudge.RequiredWindingRadians + .3) travel += Math.PI * 2;
        var points = new List<StandardCourse.Point> { new(center.X + Math.Sin(angle) * 6, center.Z + Math.Cos(angle) * 6) };
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
}
