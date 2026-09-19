# Reins Lab — local ASP.NET replay check

This development adapter recompiles and executes the same `BarrelRivals.Core.Reins` sources used by Unity. It accepts a bounded manifest and ordered 20 ms input frames, reconstructs the run and returns the recomputed state/result. A complete replay can pass this **offline consistency check**; it cannot establish human input, authenticated identity, a trusted match assignment, anti-cheat protection, ranking or reward eligibility.

The proof targets the already installed **.NET SDK 8.0.318 / ASP.NET runtime 8.0.21**. The proposed production backend remains **.NET 10**. No SDK, package, service, account, credentials or hosting is installed by these scripts. The project uses one local project reference and clears NuGet feeds during restore.

From the repository root:

```bash
bash Tools/ReinsServerCheck/run.sh build
bash Tools/ReinsServerCheck/run.sh smoke
```

The smoke script starts a temporary `127.0.0.1` listener, exercises HTTP acceptance/rejection, saves evidence under the task's `work/reins-server-check`, and stops the process even on failure. It requires a complete fixture before reporting success. Run `bash Tools/ReinsServerCheck/run.sh serve` for a manual session; stop with Ctrl-C. Default port is 5279. Optional `BARREL_REINS_PORT` accepts ports 1024–65535 but cannot change the network interface. `BARREL_DOTNET` can identify an existing compatible SDK binary; it never installs one.

The default SDK path is `/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet`. `DOTNET_CLI_HOME` is scoped to the task's `work/m0/dotnet-home`. Builds use one MSBuild worker and disable shared compiler processes. Run this small compilation separately from other agent edits to the shared core.

```bash
curl --fail-with-body http://127.0.0.1:5279/lab/reins/health
curl --fail-with-body http://127.0.0.1:5279/lab/reins/verify \
  -H 'Content-Type: application/json' \
  --data-binary @Contracts/Reins/preview-request.v1.json
```

The preview fixture is intentionally incomplete and must return `accepted:false`, `status:"Preview"`, and `result:null`. A complete fixture supplies only inputs, with no claimed final time or position. Repeated equivalent JSON payloads produce the same canonical digests and result on this runtime.

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

Observed local verification: Release build succeeded with zero warnings/errors; **46/46 HTTP checks passed**. These include an exact complete-run expected response, independent Python canonical digests and rule-source fingerprint, unknown/duplicate/missing fields, invalid ranges/enums, NaN/overflow, forged time/position fields, frame gaps/repeats, 7,500-frame timeout, post-terminal frames, oversized declared requests, two-slot concurrency, five-second slow-body deadlines and slot recovery. Health and verification responses expose the compiled `ReinsRuleFingerprint.Sha256` as `ruleFingerprint`; startup rejects blank or malformed fingerprints. The checked-in `verification-evidence.json` is a snapshot; rerun smoke after rules or adapter changes. JSON files were parsed, but no independent JSON Schema validator or PostgreSQL server was installed or run.

Implementation references: [Microsoft's explicit Kestrel loopback configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0) and [System.Text.Json object inspection](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/handle-overflow).
