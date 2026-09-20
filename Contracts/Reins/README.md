# Reins local replay contracts — version 2

`verify-request.v2.schema.json` and `verify-response.v2.schema.json` describe `POST /lab/reins/verify`. The ASP.NET adapter in `Tools/ReinsServerCheck` is an **offline consistency experiment**, not a production trust boundary or award contract. It explicitly rejects v1 verification; all `*.v1.*` files remain unchanged historical evidence.

The required request envelope is `contractVersion:2`, the exact compiled `ruleFingerprint`, `launchInitiallyHeld:true`, a manifest and ordered frames. The manifest specifies `rulesVersion:2`, `courseId:"reins-v2"`, an unsigned 32-bit seed (including zero), an exact surface name, round index 0–2 and four temperament values in permille 0–1000. Caller-supplied manifests remain untrusted. Future competition requires authenticated server-issued assignments, frozen loadouts and course/footing versions; those services are not implemented.

The adapter constructs the shared run and calls `Start()` once, arming the launch at tick 0. Each frame is one 20 ms step, starting at tick 1. `launchHeld` replaces v1 `gateTap`; it is current held state, not a tap event. A false first frame records an intentional release at tick 1 even when touch-down/up happened within one rendering interval. The first release is graded once; represses cannot rearm it. All approach frames must be retained. Inputs cannot omit the approach, move GO or start the race clock early.

GO is tick 200: the horse reaches `(0,0)` at 1.5 m/s and race time is zero. The first free movement step is tick 201. Release timing uses signed error relative to GO: Perfect ±120 ms, Good through ±240 ms, otherwise Weak. Still-held input at tick 220 resolves as TimedOut; intentional release on that same tick wins. Timeout carries `launchReleaseErrorMs:null`, never a fabricated zero. The response includes launch outcome/error in the state and, when complete, the result. Initial arming is explicit in the wire format even though the Core's `Start()` always arms it.

Every declared property is required. Unknown, duplicate or missing properties, mixed v1 fields/namespaces, incorrect fingerprints, gaps/repeated ticks and input after a terminal state fail. Enum strings are case-sensitive names, not ordinals/combinations. Integer fields require integer JSON tokens, without decimal/exponent notation. Rein pressure and Wrap are current state; cadence/Drive taps are events. The adapter accepts only input, not claimed positions, final times, rewards or results. Maximum request is 2,097,152 uncompressed bytes and 7,500 frames (150 seconds including the approach). Schemas supplement runtime validation; source compatibility, integer-token syntax and terminal order also require parser/Core checks.

`accepted:true` means only that these inputs reached Complete in the shared local simulation. Complete has a recomputed result; other phases have `result:null`. Responses retain `authority:false` and `scope:"offline-consistency"`. Digests do not prove human input, trusted assignment, anti-cheat protection or reward eligibility.

## Fingerprint and canonical hashes

Version 2 fingerprints these source labels in this exact order, relative to `Packages/com.barrelrivals.core/Runtime/Reins`:

1. `ReinsContracts.cs`
2. `ReinsCourseJudge.cs`
3. `ReinsRun.cs`
4. `ReinsReplay.cs`
5. `ReinsAlley.cs`
6. `../StandardCourse.cs`

For each label, concatenate its UTF-8 bytes, one NUL byte and the corresponding file's exact bytes; SHA-256 the concatenation. The literal `../StandardCourse.cs` label includes barrel geometry formerly omitted by the historical four-source fingerprint. The helper, Unity builder validator and smoke suite use the same six labels. The request fingerprint must equal the compiled constant. This is compatibility metadata, not authentication.

Canonical hashes use compact UTF-8 JSON, no BOM/trailing newline, decimal integer tokens, lowercase booleans and exact enum names. Runtime DTOs reconstruct these property orders after strict validation:

- Manifest: `rulesVersion`, `courseId`, `seed`, `surface`, `roundIndex`, `horse`.
- Horse: `nervePermille`, `firePermille`, `biddabilityPermille`, `heartPermille`.
- Frame: `tick`, `leftPermille`, `rightPermille`, `cadenceTap`, `launchHeld`, `wrap`, `drive`.
- Replay: `contractVersion`, `ruleFingerprint`, `launchInitiallyHeld`, `manifest`, `frames`.

`manifestSha256` covers the canonical manifest; `replaySha256` covers the entire canonical request. Equivalent property order/whitespace at input does not alter either hash. This narrow encoding is not a general JSON canonicalization standard.

## Fixtures and preservation

`preview-request.v2.json` retains the familiar filename but represents three **Approach** frames, not the removed Preview phase. It must be incomplete with a pending launch. `complete-request.v2.json` is generated solely through valid inputs from a development steering policy, using `(0,0)` as the incoming source for barrel one. It holds through tick 199 and releases on GO. The saved `complete-response.v2.json` and `fixture-provenance.v2.json` record the actual result/runtime/hashes; use that exact input file for cross-runtime comparisons. A generated expected answer is not independent physics validation.

After a deliberate, reviewed rules edit, regenerate only v2:

```bash
bash Tools/ReinsServerCheck/run.sh fingerprint
bash Tools/ReinsServerCheck/run.sh build
bash Tools/ReinsServerCheck/run.sh generate-fixture
bash Tools/ReinsServerCheck/run.sh smoke
```

Smoke consumes the saved expected response; it does not regenerate it. Its evidence is `Tools/ReinsServerCheck/verification-evidence.v2.json`. Compare the same fixture in Unity and native IL2CPP separately before claiming parity/device acceptance. The v1 request/response/schema/provenance files and original verifier evidence are preserved byte for byte. Old Classic/`reins-lab-v1` saves are not migrated into v2 bests.

The unapplied PostgreSQL draft has a separate future persistence/settlement role. The loopback endpoint has no database writer, account services, ranked authority or rewards.
