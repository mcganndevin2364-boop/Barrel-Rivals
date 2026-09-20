# Reins Lab — local ASP.NET replay check

This development adapter recompiles and executes the same `BarrelRivals.Core.Reins` sources used by Unity. It accepts a bounded manifest and ordered 20 ms input frames, reconstructs the run and returns the recomputed state/result. A complete replay can pass this **offline consistency check**; it cannot establish human input, authenticated identity, a trusted match assignment, anti-cheat protection, ranking or reward eligibility.

The proof targets the already installed **.NET SDK 8.0.318 / ASP.NET runtime 8.0.21**. The proposed production backend remains **.NET 10**. No SDK, package, service, account, credentials or hosting is installed by these scripts. The project uses one local project reference and clears NuGet feeds during restore.

From the repository root:

```bash
bash Tools/ReinsServerCheck/run.sh build
bash Tools/ReinsServerCheck/run.sh smoke
```

The smoke script starts a temporary `127.0.0.1` listener, exercises HTTP acceptance/rejection, saves evidence under the task's `work/reins-server-check-v2`, and stops the process even on failure. It requires a complete fixture before reporting success. Run `bash Tools/ReinsServerCheck/run.sh serve` for a manual session; stop with Ctrl-C. Default port is 5279. Optional `BARREL_REINS_PORT` accepts ports 1024–65535 but cannot change the network interface. `BARREL_DOTNET` can identify an existing compatible SDK binary; it never installs one.

The default SDK path is `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet`. `DOTNET_CLI_HOME` is scoped to the task's `work/m0/dotnet-home`. Builds use one MSBuild worker and disable shared compiler processes. Run this small compilation separately from other agent edits to the shared core.

```bash
curl --fail-with-body http://127.0.0.1:5279/lab/reins/health
curl --fail-with-body http://127.0.0.1:5279/lab/reins/verify \
  -H 'Content-Type: application/json' \
  --data-binary @Contracts/Reins/preview-request.v2.json
```

The v2 request requires the matching rule fingerprint, `courseId:"reins-v2"` and `launchInitiallyHeld:true`; frames use `launchHeld`, never `gateTap`. V1 verification and mixed envelopes are explicitly rejected. The preview fixture is intentionally incomplete and must return `accepted:false`, `status:"Approach"`, and `result:null`. A complete fixture supplies only inputs, with no claimed final time or position. Repeated equivalent JSON payloads produce the same canonical digests and result on this runtime.

The endpoint is explicitly bound to IPv4 loopback; external Kestrel/appsettings/URL configuration is cleared. Host must match `127.0.0.1` and the selected port. Browser Origin requests are rejected. There is no CORS, authentication endpoint, upload storage, wallet, analytics export or cloud connection. Do not expose this proof through a tunnel or reverse proxy.

Request limits are 2 MiB uncompressed JSON, nesting depth 8, 7,500 frames, two simultaneous verifications, five seconds for the whole request and two seconds for replay computation. Unknown/duplicate/missing fields, invalid enum names, non-finite/out-of-range numbers, frame gaps/repeats, and post-terminal inputs fail. Schema files supplement runtime validation; ordering and terminal-state rules require the shared simulation. The parser requires integer JSON tokens without decimal/exponent notation for integer fields.

| HTTP status | Meaning |
| --- | --- |
| 200 | Well-formed replay recomputed; inspect `accepted`, `status` and `result` |
| 400 | Invalid JSON, manifest, frame bounds/order or terminal sequence |
| 403 | Request is not a direct permitted loopback request |
| 408 | Request deadline exceeded or cancelled |
| 413 | Body size exceeded |
| 415 | Content type/encoding unsupported |
| 429 | Both local verification slots occupied |

The complete shared rules remain prototype double-precision geometry. Passing this adapter does not prove agreement across Mono/IL2CPP/server runtimes, clock capture correctness, authenticated assignments, privacy of future challenges, persistence, or simultaneous match settlement. Those require separate evidence. See [the wire contract](../../Contracts/Reins/README.md) and [the unapplied database draft](../../Backend/Schema/README.md).

Current v2 verification: .NET Release build succeeded with zero warnings/errors, and **78/78 HTTP smoke checks passed**. The saved complete fixture has 1,893 frames and recomputes to 33,860 ms, zero knocks, 300 style points and Perfect launch/error 0. Evidence is written to `verification-evidence.v2.json`; `verification-evidence.json` remains the unchanged historical v1 snapshot. The smoke suite covers signed launch boundaries, tick-0 arming, timeout/repress behavior, strict version/course/fingerprint rejection, canonical hashes, saved complete result, terminal ordering, body/concurrency/deadline limits and v1 byte preservation. No PostgreSQL server or independent JSON Schema validator is installed or run by this tool.

Before rebuilding after a reviewed rule change, run `bash Tools/ReinsServerCheck/run.sh fingerprint` to refresh the six-source constant and v2 preview request. Then build, run `generate-fixture` deliberately, and inspect the new `complete-request.v2.json`, `complete-response.v2.json` and `fixture-provenance.v2.json`. Smoke compares saved expected output without regenerating it. The extra fingerprint sources include alley and standard barrel geometry; exact byte encoding and canonical property order are in the contract README.

Implementation references: [Microsoft's explicit Kestrel loopback configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0) and [System.Text.Json object inspection](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/handle-overflow).
