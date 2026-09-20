# Barrel Rivals — AI continuation handoff

## Current state

Continue the same repository, **Barrel-Rivals-M0**, branch `codex/m1-skill-loop`, remote `https://github.com/mcganndevin2364-boop/Barrel-Rivals.git`. Use `git rev-parse HEAD` for the exact checkpoint. Do not restart the project or overwrite the user's original recovered projects.

Current source configures **0.5.0/build 5**, rules **reins-v2**, Unity **6000.6.0f1**. The last native artifact installed on the user's iPhone remains **0.4.0/build 4**, checkpoint `3e1ce2f`. No new 0.5 APK/IPA/export, phone install or measured performance is claimed by this source checkpoint.

Read [AGENTS](AGENTS.md), [onboarding](AGENT_ONBOARDING.md), [master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md), [approved 0.5 contract](Docs/Plan/Reins-Racing-0.5-Implementation.md), [current v2 evidence](Docs/Plan/Reins-v2-Alley-Checkpoint.md), [blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md), [eight-category plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md) and affected cards in [the engineering playbook](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md).

## Product decisions

- Reins Racing is the main game, first-person throughout. Accessible arcade racing with realistic horse/arena presentation; no automatic barrel steering or paid timing advantages.
- Hold center while walking down the alley; release near the third beep. Forgiving grades continue the race. The new rules implement the approved four-second/six-metre timeline.
- The user wants the supplied rodeo and warm MyStable/saddle/glove images as quality targets throughout play. Use their composition/material direction, not their pixels or sponsor trademarks.
- Free/original licensed assets only. Paid horse pack declined. User is solo, using AI assistance, on an Intel 2017 MacBook Pro/Ventura with Xcode 15.2 and no paid Apple membership. Reported phone: iPhone 17 Pro / iOS 26.6.2. These are recorded setup facts, not modern store qualification.
- Prior authorization includes pushing source updates and preparing a complete AI continuation package. No full-release section is accepted. All three multiplayer modes and all eight categories/53 section IDs remain in scope.

## What is implemented

CORE owns a deterministic 20 ms approach/release/steering/cadence/Wrap/Drive/finish simulation. At tick 200 the horse is exactly `(0,0)`, speed 1.5 m/s, race time 0; tick 201 first moves freely. Release grades Perfect±120 ms/Good±240ms/Weak, or explicit TimedOut at GO +400 ms with no release timestamp. Positive acceleration bonuses expire at GO +1,200 ms; late releases affect only the following step. Cadence starts GO +600 ms, grades ±60/100/140 ms, period 360–500 ms. Shared swept finite alley walls slide without knock/time penalty and clear the full finish gate.

CLIENT owns timestamped pointer changes, fresh-press cadence after launch, side hold preservation, cancellation/retry and three dedicated DSP-scheduled cues at 2/3/4 seconds. Reins/Stable are the only player scenes. Classic/Foundation remain source/history and are loaded explicitly by historical Editor tests. Current race bests are isolated in reins-v2 files; cosmetic-v2 storage is a separate schema.

MyStable/Tack/Rider Gear contains an enclosed original 3D barn, actual rendered item thumbnails, preview/equip/cancel and 20 local cosmetics in five slots: saddle, pad, reins, headstall, gloves. One horse is implemented. No invented levels, rarity, ownership, progression or gear stat bonuses.

Art source includes imported CC0 horse with 19 base bones and original Idle/Walk/Gallop studies; fitted tack/hands/reins; 118-card/27-palette-bone mane/tail; photographic CC0 materials/sky; modeled arena and shared alley; warm working URP/ACES renderer with ungraded HUD. Read [lighting checkpoint](Docs/Art/Reins-Lighting-Flow-Checkpoint.md), [hair checkpoint](Docs/Art/Reins-Hair-Groom-Checkpoint.md), [stable feature](Docs/Features/MyStable-and-Gear.md) and provenance before editing. Current character is 54,980 triangles / 17 renderers / 22 material slots, above the six-slot target; no lower LODs. Horse anatomy, mane separation, planted turns/braking, full rider, ground repetition and crowd remain visual gaps.

Latest art-only revision: [current mane crown checkpoint](Docs/Art/Reins-Mane-Flow-Checkpoint.md). The fitted crown, wider overlapping undercoat and revised outer tint carry through racing/stable/ghost. Fresh tests pass 135/135 Editor and 32/32 Play Mode; new evidence uses `ManeFlow-*`, including moving captures and a crown-face clipping regression. All 346 prior evidence records remain unchanged. Core/contracts/verifier are unchanged; no new independent HTTP test or native build is claimed. The original atlases/FBX remain unchanged.

## V2 gameplay checkpoint evidence (preserved)

135/135 Editor tests, 32/32 Play Mode tests and 78/78 local HTTP checks passed. Canonical Unity/.NET v2 replay: **1,893 frames, 33,860 ms, 0 knocks, 300 style, Perfect / error 0**. Fingerprint `759406550bc144d59973d0e26573d532614551bbe7a1b7bbacb2307a956bd274` spans ReinsContracts, ReinsCourseJudge, ReinsRun, ReinsReplay, ReinsAlley and StandardCourse using the documented ordered filename-NUL/raw-byte hash.

[Checkpoint JSON](Evidence/AlleyV2-Checkpoint.json), [Editor XML](Evidence/AlleyV2-EditMode.xml), [Play Mode XML](Evidence/AlleyV2-PlayMode.xml), [HTTP proof](Tools/ReinsServerCheck/verification-evidence.v2.json), [moving alley movie](Evidence/AlleyV2-Launch.mp4), [capture metadata](Evidence/AlleyV2-Launch-Capture.json) and current `Evidence/AlleyV2-*` race/stable/gear images. The controlled 25 fps movie has no audio and is not measured phone FPS. All 302 prior tracked evidence files were restored byte-for-byte; v1 contracts remain unchanged. Old v1 canonical result 35,120 ms belongs only to v1.

The loopback verifier is an offline consistency experiment, not ranked authority or production anti-cheat. Unapplied DB draft was revised; no deployment, paid service or database migration occurred.

## Next work and acceptance

1. Review actual moving alley/race/stable/gear captures. Finish one convincing horse/rider/arena, with natural walk-to-gallop/turn/brake movement and restrained camera. Do not label procedural detail or static pictures as photographic quality.
2. Build clean 0.5 iOS/Android artifacts using current source. Export iOS outside cloud-coordinated storage per [recorded guidance](README-iPhone.md). Inspect current verifier flags before invoking artifact scripts; earlier 0.4 records are historical. Keep raw logs, signing credentials, caches and binaries out of Git.
3. Install the verified new app on the connected/unlocked iPhone when available. Check speaker/touch launch alignment, early/late/held starts, thumb reach, turns/Drive, interruption/retry, sound/comfort and persistence. Measure 20 minutes of repeat races: 60 FPS target, p95 ≤16.7 ms; fallback 30 FPS, p95 ≤33.3 ms. Android handset verification remains separate.
4. Only accept R2 after combined game/art/device checks. Then implement M1N continuous-input authenticated authority/two-client proof, followed by planned networking, trusted progression/economy/content and release qualification.

## Working commands and ownership

`Tools/run-reins.sh generate|validate|editmode|playmode|android|ios` is the main Unity entry. Only one Editor per project. Generate uses graphics because stable thumbnails are rendered. Use fresh paths for BARREL_GAIT_GEOMETRY_REPORT, BARREL_MOTION_CAPTURE_DIRECTORY and BARREL_ALLEY_CAPTURE_DIRECTORY. Preserve old evidence before tests with historical image filenames, copy current results to a new checkpoint prefix, then restore old bytes.

`Tools/ReinsServerCheck/run.sh build|serve|smoke|fingerprint|generate-fixture` covers the shared-Core local verifier. It binds 127.0.0.1 only. Fingerprint/fixture regeneration is an intentional version-boundary operation; never update expected output just to conceal a regression.

Author assets outside builder-owned Generated directories, preserve Unity GUIDs, and validate persisted scenes. Core has no Unity/network dependencies. Animator root motion never moves or scores the horse. Showroom cosmetics never grant trusted rewards. Keep Core, client, contracts, fixture, DB draft, build identity and docs aligned whenever rules change.
