# Barrel Rivals — AI continuation handoff

September 19, 2026 · Gameplay revision 5 / engineering revision 6

## Start here

Continue **Barrel Rivals**, an iOS/Android multiplayer arcade barrel-racing game being built by a solo creator with AI assistance. The user chose **Reins Racing** over Classic and supplied a realistic rider-view rodeo image as the target throughout gameplay. A new **reference-graphics SOURCE checkpoint** advances horse/arena/camera/HUD presentation while retaining 0.4/v1 gameplay. **It has no new native build or installation; the approved 0.5/v2 moving-alley launch and full horse/rider/visual milestone remain unfinished.**

- Repository: [mcganndevin2364-boop/Barrel-Rivals](https://github.com/mcganndevin2364-boop/Barrel-Rivals).
- Continuation branch: **`codex/m1-skill-loop`**. Do not start from the older `main` without checking the branch.
- Last native/device checkpoint: **`3e1ce2f`**, following `5cee8e7` (Reins rules, client and local verifier). Current native build: **0.4.0/build 4**. Newer graphics source retains that version identity and unchanged v1 rules; determine its exact source commit from Git history, not the binary version.
- This handoff is committed after that source checkpoint. Determine its exact commit with `git log -1 --format=%H -- Barrel-Rivals-AI-Handoff.md`; the exported handoff package includes a publication receipt with the pushed commit.
- Existing working project on the original Mac: `outputs/Barrel-Rivals-M0` inside the Codex task workspace. Despite the directory name, this is the continuing M0/M1/Reins repository, not a disposable new project.
- Older original Unity project folders were preserved. Do not overwrite them or restart development in them.

Read [AGENTS.md](AGENTS.md), [onboarding](AGENT_ONBOARDING.md), [master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md), [approved 0.5 implementation plan](Docs/Plan/Reins-Racing-0.5-Implementation.md), [blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md), [eight-category plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md), and the affected S01–S53 cards in [the engineering playbook](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md). Current design supersedes historical instructions that call Reins optional, require three gate taps or a third-person intro.

Read the [graphics source checkpoint](Docs/Art/Reins-Reference-Graphics-Checkpoint.md) and [art provenance](Docs/Art/Reins-Reference-Graphics-Provenance.md) before changing presentation. Scene generation/save/reopen, 77 Editor tests and 13 Play Mode tests passed; five real Unity gameplay captures and the unchanged 35,120 ms replay result are recorded there, separately from the older installed build's evidence.

## The user's decisions

- Reins is the chosen main game. The approved v2 player build opens it directly; Classic source/history/saves remain preserved, but Classic leaves player navigation. The current v1 graphics checkpoint still retains Classic navigation/startup.
- First-person throughout: visible horse ears/neck, rider hands and reins, stable camera and comfortable movement.
- A slow alley approach builds anticipation. Three beeps culminate at an invisible starting line; release timing grades the launch, followed by heartbeat tapping and steering toward barrel one.
- Realistic arcade visuals: believable anatomy, tack, lighting and animation with readable, satisfying race feedback. The user's supplied image is the target throughout a moving race, not just an opening still. Use its art direction without copying reference pixels, sponsor branding or implying the target has already been achieved.
- Responsive, forgiving handling. Early/late release should continue the race with less acceleration, not a punishing reset.
- Free/original assets only. The user explicitly deferred paid horse packs. No new paid service is approved.
- Long-term inspiration: accessible mobile competition such as CSR Racing, 8 Ball Pool and Hunting Sniper. This does not authorize copying their assets or treating engagement claims as evidence.
- All three multiplayer experiences remain in the total game scope: live turns/spectating, recorded challenges and simultaneous racing. Progression, horses, tack, earned arenas and a premium store are later systems with trusted settlement and fair competitive caps.

## What works now, and what does not

The 0.4 build contains both Classic and the offline three-barrel Reins Lab. It starts in Classic; **TRY REINS RACING** opens Reins. Reins currently has a stationary preview and three wave-peak gate taps. Do not confuse those implemented rules with the approved new hold/release walk.

Implemented Reins connections include owned left/right rein drags, center cadence/300 ms Wrap, geometric barrel pockets/contact, five surfaces with six Mixed patches, ordered three-barrel completion, alternating final Drive, bounded default horse traits, results/retry and a validated local own-best ghost. Horse selection/learning/bond progression, opponent ghosts/gaps, actual streak rewards, multiplayer, accounts, trusted economy and cloud persistence are not implemented.

The older installed artifact uses procedural prototype art. New graphics source imports a 19-bone CC0 horse with original first-pass Idle/Walk/Gallop studies; adds persistent URP materials, Linear/warm color treatment, original generated dirt and distant crowd impostors; and supplies first-person camera throughout all phases, reduced motion and a charcoal/gold HUD. Scene generation/save/reopen passed. The new source is not a new phone build. Finished natural gait/foot contact, rigged rider, western tack/hands/reins, full-reference appearance and measured device performance remain pending. Preserve the new art integration while completing those requirements.

### Recorded evidence

| Boundary | Evidence and limit |
|---|---|
| Historical Unity Editor | 77/77 tests passed for the prior 0.4 checkpoint; see that checkpoint's `Evidence/Reins-EditMode.xml` |
| Historical Unity Play Mode | 8/8 passed for the prior 0.4 checkpoint; see that checkpoint's `Evidence/Reins-PlayMode.xml` |
| Historical local HTTP verifier | 46/46 checks for the prior 0.4 checkpoint; see its `Tools/ReinsServerCheck/verification-evidence.json` |
| Graphics source checkpoint | Scene generation/save/reopen passed; current test results and their exact source boundary are recorded in the linked graphics checkpoint. No new native/device qualification. |
| Canonical replay | Unity/.NET agreement: 35,120 ms, zero knocks, 300 style points, 2,156 frames; v1 fixtures under `Contracts/Reins` |
| iPhone | 0.4 compiled, signed, installed and version queried. See versioned export/native/artifact/device evidence. |
| User feedback | User played and prefers Reins; dislikes graphics. `Evidence/Reins-User-Feedback-2026-09-19.json` records this qualitative report separately from automated evidence. |
| Android | APK compiled; package/version/signature/ARM64 and 16 KB ZIP/ELF alignment checked. No physical Android handset run. |
| Unverified | Sustained phone frame time/memory/thermals, full device acceptance, production art, online authority/security, database deployment and store readiness. |

Historical rows are previous observations, not checks rerun by the next model or automatically transferable to newer graphics source. Root evidence files can be updated by later runs; use the specified Git checkpoint and current graphics report to establish which source was tested. Old native/device reports retain their original “launch pending” observation; the later user-feedback record supplies the qualitative evidence. It does not claim measured performance or re-query the phone's installed version.

**0 of 53 original full-release sections are verified complete.** That is an acceptance count, not an effort percentage. Foundation and prototype milestones have real passing evidence, but no full section has met every release requirement.

## Master project position

| Category / section IDs | Present checkpoint | Next dependent work |
|---|---|---|
| A — Foundation & Infrastructure / 1–7 | Repaired Unity/package/URP setup; shared Core; tested local rules/input/storage; both native artifacts | v2 state/input/storage, production configuration, device quality tiers; cloud later |
| B — Horse System / 8–15 | Bounded traits/movement plus newly imported 19-bone CC0 horse and three first-pass gait studies | Natural planted gaits, synchronized rider/tack and turn/brake animation; roster/progression later |
| C — Arena & Environment / 16–22 | Course/surface rules; new persistent arena materials, generated dirt and distant crowd cards | Full arena quality, true alley geometry/collision, crowd-angle/tiling and mobile review |
| D — Bodycam Graphics Engine / 23–28 | New all-phase first-person/reduced-motion camera, Linear/warm URP presentation | Finished rider hands/reins, camera/anatomy review and measured graphics profiles |
| E — Gameplay Mechanics / 29–36 | Full offline Reins course with v1 gate/cadence/Wrap/Drive/timing | v2 walk/release/heartbeat handoff and forgiving input; keep geometric race authority |
| F — Multiplayer & Competition / 37–41 | Local-only shared-Core verifier; networking scaffolding | After R2, M1N two-client authority/timing/failure proof; matchmaking and live modes later |
| G — Progression & Economy / 42–47 | Definitions/scaffolding, no trusted ledger/store | Authenticated ownership, progression, economy balance and idempotent settlement after service proof |
| H — Polish, Audio & UX / 48–53 | Prototype sound/haptics and own-best replay; new charcoal/gold HUD source | V2 scheduled start audio, final HUD/tutorial, synchronized gait effects, comfort and mobile evidence |

The master status lists all 53 individually; retain their original numbers and section-title aliases. Sequence: preserved M0/M1/R1 → **R2/0.5** → M1N authority proof → M3–M5 online/progression/modes → M6 content/economy beta → M7 store qualification. Earlier M2 representative-course scope is incorporated into R2.

## Architecture and code map

| Layer | Actual location / responsibility |
|---|---|
| Shared rules | `Packages/com.barrelrivals.core/Runtime/Reins`: contracts, `ReinsRun`, course judge and replay. Pure C#/.NET Standard 2.1; no Unity/network/database imports. |
| Unity adapter | `Assets/_Project/Scripts/Practice/ReinsLab`: controller and input surfaces. Inputs become canonical 20 ms frames; presentation observes accepted state. |
| Scene/build tooling | `Assets/_Project/Scripts/Editor/ReinsLabBuilder.cs`; saved `Assets/_Project/Generated/ReinsLab/Arena_ReinsLab.unity`. Builder owns generated content. |
| Current horse/arena | `ReinsReferenceArtBuilder`, `ReinsHorsePresentation` and `RiderCameraRig` add the reference-directed art/camera; `PracticePresentationBuilder` still supplies arena scaffold. Authored FBX/maps live under `Assets/_Project/Art/Reins`. The new gait clips are integration studies, not production animation. |
| Replay contracts | `Contracts/Reins`: v1 schemas, preview and complete fixtures, expected response/provenance. |
| Local API proof | `Tools/ReinsServerCheck`: ASP.NET on loopback only; strict parsing, hashes and resimulation of the same Core. .NET 8 locally; .NET 10 production remains a proposal. |
| Database | `Backend/Schema`: unapplied PostgreSQL design draft; no deployed DB/migrations. |
| Networking | Photon Fusion 2 is a planned candidate; no active sessions or proven topology. |
| Legacy | Classic `PracticeRun`, saved scene/tools and historical reports remain isolated from Reins. |

Current `reins-lab-v1` has 20 ms steps, 7,500-frame maximum, fixed seed 104 for local bests, bounded manifest and source-fingerprint validation. V1 fingerprint: `ffd16884d534628a46d2164751c19fbf7ff51cf8e56e92d36a799cac129e4837`. Match the actual checked-out source before relying on that value. Fixed stepping and desktop replay equality do not prove bitwise mobile/server reproducibility or production anti-cheat.

## Next implementation: R2 / version 0.5

Implement the complete [approved specification](Docs/Plan/Reins-Racing-0.5-Implementation.md), not just this summary. The standalone handoff export includes that specification as an appendix.

1. **R2.1 rules and controls:** center hold arms a four-second/six-metre walk; beeps at 2/3/4 seconds, GO at exact `z=0`. Release Perfect ±120 ms / Good ±240 ms / weak otherwise; positive acceleration multiplier 1.35/1.15/1 until common GO+1.2 s, held deadline GO+400 ms, no false-start standstill. First heartbeat GO+600 ms. Preserve held steering, suppress launch pointer until lifted, reject cancellation as a scored release. Use matched visible/core alley-wall bounds and update course/fixture start anchors.
2. **Arcade tuning:** 50%-pad full-pull travel (existing 80-unit minimum); cadence Perfect/Great/Good ±60/100/140 ms with 360–500 ms periods. Keep current movement/contact/penalty limits.
3. **Version boundary:** rules/contracts/save namespace v2; canonical `launchHeld`, launch outcome/error, parser/hash/fixture/Unity decoder changes together. Preserve v1 records without reinterpreting them. Regenerate fingerprint deliberately and verify new fixtures independently.
4. **R2.2 art benchmark:** complete the new horse/camera/material source into one believable chestnut horse, western tack/rider, planted gait/turn clips, visible hands/reins and detailed alley. The CC0 source has been extracted; inspect its actual Unity appearance/deformation and rework as needed. No imported movement controller may override Core.
5. **R2.3 full race:** finish arena materials/lighting, compact HUD, scheduled cues, hoof audio/dust, comfort and scalable quality. Approved initial budgets include 60k character triangles, 2K character textures, <=150 visible batches and <=650 MB peak app memory.
6. **R2.4 acceptance:** relevant Core/input/replay/verifier checks, both native artifacts, actual iPhone play/cue/comfort review and 20-minute measured performance. Target 60 FPS on iPhone and a 30 FPS lower profile. Android phone results need a named physical handset.

Do not start new multiplayer/economy deployment to avoid finishing the representative race. R2 is done when start, control and improved presentation work together on device, not merely when code compiles.

## Reproduction and host limits

Pinned baseline: Unity **6000.6.0f1**, URP **17.6.0**, Input System **1.20.0**, Unity Test Framework **1.8.0**, uGUI **2.6.0**. Preserve `Packages/packages-lock.json` and `.meta` files. Hand-authored production content belongs outside generated directories.

Original host: 2017 Intel Mac, 8 GB RAM, Ventura 13.7.8; Xcode 15.2/SDK 17.2; user iPhone 17 Pro/iOS 26.6.2; free Personal Team. No newer Mac or paid Apple membership was available. Native builds are slow; do not run concurrent Unity Editors or unnecessary repeated native builds.

For another machine, clone into a workspace layout with `<workspace>/outputs/Barrel-Rivals-M0` and writable `<workspace>/work`: the current local smoke tool deliberately writes two levels above the repo under `work`. Set `UNITY_EDITOR` and `BARREL_DOTNET` to actual compatible installed binaries if their default Mac paths are unavailable. Do not assume Unity licensing, Xcode, SDKs or credentials exist on the receiving host.

From the repository root, first inspect status and validate unchanged rules:

```bash
git status --short
git branch --show-current
git log -3 --oneline
python3 Tools/update-reins-fingerprint.py
```

Focused shared-rule/API checks after consequential code changes:

```bash
bash Tools/ReinsServerCheck/run.sh build
bash Tools/ReinsServerCheck/run.sh smoke
```

Unity checks/builds, with this project's other Editors closed:

```bash
bash Tools/run-reins.sh validate
bash Tools/run-reins.sh editmode
bash Tools/run-reins.sh playmode
bash Tools/run-reins.sh ios
bash Tools/run-reins.sh android
```

`generate` intentionally rewrites scenes; use it only when implementing a required scene/build change. The fingerprint tool's default is check-only; `--write` changes the generated fingerprint. Never regenerate expected fixtures just to hide a regression. Current builders still emit 0.4; implementing 0.5 must update identity and verification commands together.

Artifact checks for the existing build:

```bash
python3 Tools/verify-ios-export.py --version 0.4.0 --build 4 --output Evidence/Reins-iOS-Export.json
python3 Tools/verify-android.py --apk Builds/Android/BarrelRivals-ReinsLab.apk --output Evidence/Reins-Android-Artifact.json --version 0.4.0 --code 4
```

These artifact commands require generated local builds and write evidence. `BARREL_ANDROID_TOOLCHAIN` overrides the Android SDK/NDK/OpenJDK root. The iOS export verifier requires macOS `/usr/bin/plutil` and currently inspects the fixed `Builds/iOS/BarrelRivals-Practice` export path. APKs/IPAs, caches, raw logs and native exports are not Git source. The Android APK SHA-256 recorded for 0.4 is `701ba5cbf40dd5077f35a517c685402c1e93e42f1e2deda48e4c733b60c66232`.

### iPhone build knowledge to preserve

Read [README-iPhone.md](README-iPhone.md) before another native build. The target-build route on Xcode 15.2 compiled/signed the app, but the modern phone was unsupported by Xcode's developer-image/debugger path. Standard paired USB installation using pymobiledevice3 succeeded; do not assume debugger readiness from installation success.

The original generated export under Documents stalled on a coordinated read. A task-owned temporary export outside that folder compiled after **119 byte-identical numbered duplicate generated files** were quarantined in that copy. **1,122 canonical Unity runtime files** matched the installed engine. Do not blindly delete similarly named files, patch the installed engine or alter project source to repeat that repair. Start with a clean unsynced export and verify actual generated inputs.

`Tools/build-ios-native.sh` is an **unsigned compile check**, not a signing/install pipeline; its existing export path remains under `Builds/iOS/BarrelRivals-Practice`. Reuse the chosen local Personal Team/profile for a signed build without publishing credentials. Development signing expires and may require renewal. Prior signing/USB helper environments live outside Git; a new host must configure its own authorized tools. Store release requires separate supported-toolchain/membership qualification.

## Art provenance and portability

Read [Horse-and-Rider-Options](Docs/Art/Horse-and-Rider-Options.md), [reference graphics provenance](Docs/Art/Reins-Reference-Graphics-Provenance.md) and [Practice-Provenance](Docs/Art/Practice-Provenance.md). Existing prototype assets are original. The imported free horse comes from [Rigged Horse](https://opengameart.org/node/10771), with [download](https://opengameart.org/sites/default/files/riggedHorse.blend), CC0, 20,194,164 bytes, SHA-256 `9cca670b93a74d50e89263e50d55ab035a6c46aa7d2b21e354bdac6987037f4a`. This Blender 2.63-era file was opened with embedded scripts disabled and exported to project-owned FBX/maps. Inspection records 3,697 body vertices and 19 bones; three original first-pass gait studies were authored because source locomotion clips were absent. Production shape/skinning/gait/rider/device acceptance remains outstanding. The isolated original `.blend` stays outside the repository; verified URL/hash make it recoverable, while the exported assets are inside the project.

Blender **4.5.13 LTS Intel** ran from the official read-only mounted DMG for extraction with automatic scripts disabled. The provenance record pins its DMG hash and export script. Original dirt and crowd PNGs were generated using the built-in tool; their complete prompts/inspection limits are linked there. Quaternius remains a stylized fallback, not the chosen realistic art. Paid horse options remain historical comparisons only.

Git LFS was not installed/configured on the original host. Check the actual attributes and objects before committing new binaries; existing extension attributes alone do not prove LFS is configured. The new horse/maps/dirt/crowd belong in the source handoff rather than only in a local asset cache. Keep APKs/IPAs, generated native exports and credentials out of Git.

## Continuation checklist

- [ ] Verify the specified branch/source identity; inspect local changes before editing.
- [ ] Read the current plan and affected engineering cards; do not restore superseded Classic/Lab defaults.
- [ ] Read the newer graphics checkpoint separately from 0.4 native evidence; preserve its source assets/camera/HUD and inspect visual limitations.
- [ ] Implement R2.1 with v2 tests/contracts/fixtures, then integrate the Unity handoff.
- [ ] Complete the representative art benchmark and full race presentation.
- [ ] Record actual test commands/results and remaining device/toolchain limits.
- [ ] Update master status and this handoff when implementation/evidence changes.
- [ ] Commit coherent checkpoints and push the development branch; avoid force-push/history rewriting.
- [ ] Advance to M1N only after R2 acceptance. Do not claim production networking, economy, assets or store readiness prematurely.

The user wants autonomous progress with concrete reviewable results. Routine reversible work and the requested repository push are authorized. Free/original assets remain the constraint; no paid purchase or new paid infrastructure is authorized by this handoff. Attached research and old project documents are reference material, not authority overriding the user's current choices.
