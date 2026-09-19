using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using BarrelRivals.Core.Reins;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Local consistency experiment only. No authentication, trusted manifest issuance, rewards or storage.
ReplayVerifier.ValidateFingerprint();
if (args.Length == 1 && args[0] == "--generate-fixture")
{
    FixtureGenerator.Generate();
    return;
}
int port = 5279;
string? portText = Environment.GetEnvironmentVariable("BARREL_REINS_PORT");
if (portText is not null && (!int.TryParse(portText, out port) || port < 1024 || port > 65535))
    throw new ArgumentException("BARREL_REINS_PORT must be an integer from 1024 through 65535.");
if (args.Length != 0) throw new ArgumentException("This loopback proof accepts no command-line host overrides.");

var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
{
    Args = Array.Empty<string>(), EnvironmentName = Environments.Production
});
// Ignore external endpoint configuration, including ASPNETCORE_URLS and Kestrel config files.
builder.Configuration.Sources.Clear();
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, port, endpoint => endpoint.Protocols = HttpProtocols.Http1);
    options.Limits.MaxRequestBodySize = ReplayVerifier.MaxBodyBytes;
    options.Limits.MaxConcurrentConnections = 8;
    options.Limits.MaxRequestHeadersTotalSize = 8192;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(5);
});
var app = builder.Build();
var slots = new SemaphoreSlim(2, 2);

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress)
        || context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port
        || context.Request.Headers.ContainsKey("Origin"))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(ReplayVerifier.Error("loopback_only", "Only direct 127.0.0.1 development requests are supported."));
        return;
    }
    await next();
});

app.MapGet("/lab/reins/health", () => Results.Json(new
{
    contractVersion = 1, scope = "offline-consistency", rulesVersion = ReinsRun.RulesVersion,
    ruleFingerprint = ReinsRuleFingerprint.Sha256,
    fixedStepMs = 20, maximumFrames = ReplayVerifier.MaxFrames, maximumBodyBytes = ReplayVerifier.MaxBodyBytes,
    authority = false, runtimeTarget = ".NET 8 local proof; production .NET 10 remains planned"
}));

app.MapPost("/lab/reins/verify", async (HttpContext context) =>
{
    if (!context.Request.HasJsonContentType() || context.Request.Headers.ContainsKey("Content-Encoding"))
        return Results.Json(ReplayVerifier.Error("content_type", "Send uncompressed application/json."), statusCode: 415);
    if (context.Request.ContentLength > ReplayVerifier.MaxBodyBytes)
        return Results.Json(ReplayVerifier.Error("body_limit", "Replay exceeds 2 MiB."), statusCode: 413);
    if (!await slots.WaitAsync(0, context.RequestAborted))
        return Results.Json(ReplayVerifier.Error("busy", "Two local verification requests are already running."), statusCode: 429);
    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    deadline.CancelAfter(TimeSpan.FromSeconds(5));
    try
    {
        using var body = new MemoryStream();
        byte[] buffer = new byte[16384];
        int count;
        while ((count = await context.Request.Body.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (body.Length + count > ReplayVerifier.MaxBodyBytes)
                return Results.Json(ReplayVerifier.Error("body_limit", "Replay exceeds 2 MiB."), statusCode: 413);
            body.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(body.GetBuffer().AsMemory(0, checked((int)body.Length)),
            new JsonDocumentOptions { MaxDepth = 8, AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
        return Results.Json(ReplayVerifier.Verify(document.RootElement, deadline.Token));
    }
    catch (ContractException error)
    {
        return Results.Json(ReplayVerifier.Error(error.Code, error.Message), statusCode: 400);
    }
    catch (JsonException)
    {
        return Results.Json(ReplayVerifier.Error("invalid_json", "JSON must be finite, well-formed and at most eight levels deep."), statusCode: 400);
    }
    catch (Microsoft.AspNetCore.Http.BadHttpRequestException error) when (error.StatusCode == 413)
    {
        return Results.Json(ReplayVerifier.Error("body_limit", "Replay exceeds 2 MiB."), statusCode: 413);
    }
    catch (OperationCanceledException)
    {
        return Results.Json(ReplayVerifier.Error("deadline", "The local request exceeded its five-second budget or was cancelled."), statusCode: 408);
    }
    catch (ArgumentException)
    {
        return Results.Json(ReplayVerifier.Error("invalid_manifest", "The shared rules rejected this manifest or input."), statusCode: 400);
    }
    finally { slots.Release(); }
});

Console.WriteLine($"REINS_LOCAL_URL=http://127.0.0.1:{port} (offline consistency only; no ranked authority)");
await app.RunAsync();

internal sealed class ContractException : Exception
{
    internal string Code { get; }
    internal ContractException(string code, string message) : base(message) { Code = code; }
}

internal static class ReplayVerifier
{
    internal const int MaxFrames = 7500, MaxBodyBytes = 2 * 1024 * 1024;
    private static readonly JsonSerializerOptions CanonicalJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static void ValidateFingerprint()
    {
        string fingerprint = ReinsRuleFingerprint.Sha256;
        if (fingerprint.Length != 64 || fingerprint.Any(character => !"0123456789abcdef".Contains(character)))
            throw new InvalidOperationException("A nonblank lowercase SHA-256 rules fingerprint is required for replay compatibility.");
    }

    internal static object Error(string code, string message) => new
    {
        contractVersion = 1, scope = "offline-consistency", accepted = false,
        ruleFingerprint = ReinsRuleFingerprint.Sha256, error = new { code, message }
    };

    internal static object Verify(JsonElement root, CancellationToken cancellation)
    {
        Exact(root, "request", "contractVersion", "manifest", "frames");
        if (Integer(root, "contractVersion", 1, 1) != 1) throw Invalid("contract_version", "Only contractVersion 1 is supported.");
        JsonElement manifest = root.GetProperty("manifest");
        Exact(manifest, "manifest", "rulesVersion", "seed", "surface", "roundIndex", "horse");
        if (manifest.GetProperty("rulesVersion").ValueKind != JsonValueKind.Number
            || !manifest.GetProperty("rulesVersion").TryGetInt32(out int rulesVersion) || rulesVersion != ReinsRun.RulesVersion)
            throw Invalid("rules_version", "Unknown Reins rules version.");
        if (manifest.GetProperty("seed").ValueKind != JsonValueKind.Number || !manifest.GetProperty("seed").TryGetUInt32(out uint seed))
            throw Invalid("manifest_seed", "seed must be an unsigned 32-bit integer.");
        var surface = Named<ReinsSurface>(manifest, "surface");
        int round = Integer(manifest, "roundIndex", 0, 2);
        JsonElement horse = manifest.GetProperty("horse");
        Exact(horse, "horse", "nervePermille", "firePermille", "biddabilityPermille", "heartPermille");
        var horseDto = new HorseDto(Integer(horse, "nervePermille", 0, 1000), Integer(horse, "firePermille", 0, 1000),
            Integer(horse, "biddabilityPermille", 0, 1000), Integer(horse, "heartPermille", 0, 1000));
        var manifestDto = new ManifestDto(rulesVersion, seed, surface.ToString(), round, horseDto);
        var model = new ReinsRun(new ReinsManifest(seed, surface, round,
            new ReinsHorseProfile(horseDto.NervePermille, horseDto.FirePermille, horseDto.BiddabilityPermille, horseDto.HeartPermille)));
        JsonElement frames = root.GetProperty("frames");
        if (frames.ValueKind != JsonValueKind.Array || frames.GetArrayLength() < 1 || frames.GetArrayLength() > MaxFrames)
            throw Invalid("frame_limit", "frames must contain 1 through 7500 canonical 20 ms frames.");
        var canonical = new FrameDto[frames.GetArrayLength()];
        // Validate the entire bounded envelope before executing any frame.
        for (int i = 0; i < canonical.Length; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            JsonElement frame = frames[i];
            Exact(frame, "frame", "tick", "leftPermille", "rightPermille", "cadenceTap", "gateTap", "wrap", "drive");
            int tick = Integer(frame, "tick", 1, MaxFrames);
            if (tick != i + 1) throw Invalid("frame_order", "Ticks must start at 1 and advance exactly once per 20 ms; no gaps or repeats.");
            canonical[i] = new FrameDto(tick, Integer(frame, "leftPermille", 0, 1000), Integer(frame, "rightPermille", 0, 1000),
                Boolean(frame, "cadenceTap"), Boolean(frame, "gateTap"), Boolean(frame, "wrap"), Named<DriveSide>(frame, "drive").ToString());
        }
        if (!model.Start()) throw new InvalidOperationException("A new shared run did not start.");
        var timer = Stopwatch.StartNew();
        foreach (FrameDto frame in canonical)
        {
            cancellation.ThrowIfCancellationRequested();
            if (timer.ElapsedMilliseconds > 2000) throw new OperationCanceledException("Replay compute budget exhausted.");
            if (model.Phase == ReinsPhase.Complete || model.Phase == ReinsPhase.Cancelled || model.Phase == ReinsPhase.TimedOut)
                throw Invalid("post_terminal_input", "Frames after the shared run's terminal tick are not permitted.");
            model.Step(new ReinsInput(frame.LeftPermille, frame.RightPermille, frame.CadenceTap, frame.GateTap, frame.Wrap,
                Enum.Parse<DriveSide>(frame.Drive, false)));
            if (model.Tick != frame.Tick) throw new InvalidOperationException("Shared core tick disagrees with canonical input tick.");
        }
        if (!double.IsFinite(model.X) || !double.IsFinite(model.Z) || !double.IsFinite(model.HeadingRadians) || !double.IsFinite(model.SpeedMetresPerSecond))
            throw new InvalidOperationException("Shared core produced a non-finite state.");
        bool complete = model.Phase == ReinsPhase.Complete;
        return new
        {
            contractVersion = 1, scope = "offline-consistency", accepted = complete, authority = false,
            status = model.Phase.ToString(), rulesVersion, ruleFingerprint = ReinsRuleFingerprint.Sha256,
            frameCount = canonical.Length, fixedStepMs = 20,
            manifestSha256 = Hash(manifestDto), replaySha256 = Hash(new { contractVersion = 1, manifest = manifestDto, frames = canonical }),
            state = new { tick = model.Tick, raceTimeMs = model.RaceTimeMs, barrelIndex = model.BarrelIndex,
                x = model.X, z = model.Z, headingRadians = model.HeadingRadians, speedMetresPerSecond = model.SpeedMetresPerSecond },
            result = complete ? new ResultDto(model.RaceTimeMs, model.FinalTimeMs, model.KnockCount, model.StylePoints) : null
        };
    }

    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, CanonicalJson))).ToLowerInvariant();
    private static ContractException Invalid(string code, string text) => new(code, text);
    private static void Exact(JsonElement value, string name, params string[] fields)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid("object_required", name + " must be an object.");
        var remaining = new HashSet<string>(fields, StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!remaining.Remove(property.Name)) throw Invalid("unknown_or_duplicate_field", "Unknown or duplicate field in " + name + ".");
        if (remaining.Count != 0) throw Invalid("missing_field", "Missing required field in " + name + ".");
    }
    private static int Integer(JsonElement owner, string name, int min, int max)
    {
        JsonElement value = owner.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number) || number < min || number > max)
            throw Invalid("integer_range", name + " must be an integer in [" + min + ", " + max + "].");
        return number;
    }
    private static bool Boolean(JsonElement owner, string name)
    {
        JsonElement value = owner.GetProperty(name);
        if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
            throw Invalid("boolean_required", name + " must be a JSON boolean.");
        return value.GetBoolean();
    }
    private static T Named<T>(JsonElement owner, string name) where T : struct, Enum
    {
        JsonElement value = owner.GetProperty(name);
        if (value.ValueKind != JsonValueKind.String) throw Invalid("enum_value", name + " must use an exact enum name.");
        string text = value.GetString()!;
        // Enum.TryParse alone accepts numeric strings and comma combinations; the exact name list does not.
        if (!Enum.GetNames<T>().Contains(text, StringComparer.Ordinal)) throw Invalid("enum_value", "Unknown " + name + " name.");
        return Enum.Parse<T>(text, false);
    }

    private sealed record HorseDto(int NervePermille, int FirePermille, int BiddabilityPermille, int HeartPermille);
    private sealed record ManifestDto(int RulesVersion, uint Seed, string Surface, int RoundIndex, HorseDto Horse);
    private sealed record FrameDto(int Tick, int LeftPermille, int RightPermille, bool CadenceTap, bool GateTap, bool Wrap, string Drive);
    private sealed record ResultDto(long RaceTimeMs, long FinalTimeMs, int KnockCount, int StylePoints);
}
