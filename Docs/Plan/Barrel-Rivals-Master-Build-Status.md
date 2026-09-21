# Barrel Rivals — master build status

Current source configures **0.5.0/build 5**, rules **reins-v2**: moving first-person alley, single-release launch, accessible rein/cadence tuning, shared alley walls, Reins-only startup and updated replay/verifier/save contracts. The warm MyStable/Tack/Rider Gear source includes one horse and 20 local cosmetics with real preview/equip behavior.

Preserved v2 gameplay checkpoint: 135/135 Editor tests, 32/32 Play Mode tests and 78/78 local HTTP checks passed. [Current source evidence and limits](Reins-v2-Alley-Checkpoint.md). Last verified installed phone app remains 0.4.0/build 4; the current mobile update below records the new native build; no new device install is verified. Photographic graphics/animation, sustained device performance and full R2 acceptance remain open. Eight categories and all 53 section IDs remain in scope.

Latest integrated art update: [connected glove anatomy](../Art/Reins-Glove-Anatomy-Checkpoint.md). Racing, MyStable inspection and six real item thumbnails now share CC0-derived hand anatomy, a fitted rein grip, mirrored winding and a restrained leather finish. Inspection falls from 4,192 triangles/three renderers to 2,537/two; the full racing character increases by 584 triangles to 78,614/26 slots and still exceeds its budget. That earlier checkpoint passed 140/140 Editor and 37/37 Play Mode, zero skipped. New evidence uses `GloveAnatomy-*`; all 515 prior records remain unchanged. Core/input/contracts/verifier and the [seated rider integration](../Art/Reins-Rider-Body-Checkpoint.md) remain intact. That art-only checkpoint did not establish native, device or photographic acceptance; the mobile update below records subsequent build verification. The gloves still need detailed sewn construction, folds and articulation; wider horse/rider/arena and performance work remains.

The [candidate rider/tack fit](../Art/Reins-Horse-Rider-Fit.md) adds the existing skinned rider/gloves, a surface-fitted headstall, measured boot/stirrup contacts and private flexible reins to the isolated Unity horse. The live first-person camera now frames both grips and restores inspection correctly. Seven focused Play Mode tests pass, including existing race/ghost regressions; 84 walk/pull samples retain seat, wrist, rein and boot alignment. A neutral unsaved showroom inspection is included. The candidate is 92,402 triangles/25 slots, above the 60k/six targets, and remains excluded from player scenes. The 0.364 mm animated-source discrepancy still exceeds its retained 0.2 mm target. Coat/groom/rider detail, missing natural gaits/actions, LODs and actual race/stable/ghost integration remain next; neither native package nor the installed phone app changed. This is not photographic, device or R2 acceptance.

**How to read the list.** “Early code,” “helper” or “prototype” means a starting point exists but the section is not implemented and verified end to end. “Not built” means no implementation of the required system was found in the reviewed game project. All sections have a planning/engineering specification; none is release-ready.

**CATEGORY A: FOUNDATION & INFRASTRUCTURE (Sections 1–7)**

| # | Section | Current implementation status |
|---|---|---|
| 1 | Project Setup & Architecture | M0 repair implemented; clean import/core compilation/scene reopen passed; full architecture/platform acceptance pending |
| 2 | Input System Foundation | Reins v2 initial hold/release, pointer ownership, cancellation and 50% pull travel implemented/tested in Unity; phone timing and ergonomics pending |
| 3 | Game State Machine | Ready/Approach/Racing/Drive/finish/cancel/timeout v2 states tested; historical Classic state preserved; online reconnect pending |
| 4 | Data Architecture (ScriptableObjects) | Data classes exist; authored assets/validation pending |
| 5 | Save/Load & Cloud Sync | Classic/v1 race files preserved; isolated reins-v2 bests/replays and independent cosmetic-v2 persistence tested; phone restart/cloud sync pending |
| 6 | iOS & Android Platform Layer | 0.5 iOS export/native compilation/signature/IPA and Android version/signature/ARM64/16 KB alignment verified; both new installs/play/performance pending; last verified iPhone installation remains 0.4.0; named Android handset still needed |
| 7 | Performance Budget & Quality Tiers | Targets proposed; device performance unmeasured |

**CATEGORY B: HORSE SYSTEM (Sections 8–15)**

| # | Section | Current implementation status |
|---|---|---|
| 8 | Horse Data Model & Breeds | One starter horse displayed in the 3D stable; four bounded Reins trait parameters unchanged; distinct ten-horse roster, selection and bond pending |
| 9 | Horse Stats & Leveling | Bounded Reins effective movement traits tested; leveling, ownership and trusted progression pending |
| 10 | Horse Animation State Machine | 19-bone horse Idle/Walk/Gallop studies plus CC0-derived skinned rider with saddle/wrist/boot alignment and ghost integration tested; separate 39-bone replacement now has a fitted rider/bridle/reins in an isolated Unity benchmark, with 84 attachment samples and focused live checks; source fidelity, natural motion and cost targets remain open; authored planted turns/braking/Wrap and device acceptance pending |
| 11 | Horse Physics & Movement Controller | Reins fixed-step three-barrel steering with bounded launch acceleration and swept barrel/alley contacts; production hull, rig and online authority pending |
| 12 | Horse Gait System | Original Idle/Walk/Gallop studies now verified moving in actual rendered frames; bone culling corrected, skinned hair and camera/hand phase connected. Seated rider follows actual gait/tack; new replacement study has independent hoof-plane/contact checks but is not integrated; natural foot planting, turns/braking, authored rider actions, hoof audio and device acceptance pending |
| 13 | Horse Visual Customization (Colors/Markings) | Twenty local styles preview/equip with persisted race bindings; connected glove anatomy shared by six styles, shared horse fiber shading and saved-material checks pass; coat/marking customization and trusted ownership pending |
| 14 | Horse Aging & Career System | Not built |
| 15 | Horse Injury & Recovery System | Not built |

**CATEGORY C: ARENA & ENVIRONMENT (Sections 16–22)**

| # | Section | Current implementation status |
|---|---|---|
| 16 | Arena Geometry & WPRA Standards | Standard pattern, ordered winding, full finish gate and shared finite alley-wall geometry tested; credited elevation backdrop and clear apron persist without gameplay colliders; final arena/hull acceptance pending |
| 17 | Arena Lighting System | Photographic sky, brighter warm/fill light, working persisted HDR/ACES renderer and exposure pixel regression; final lighting and phone appearance/performance pending |
| 18 | Arena Ground Surface (Dirt/Footing) | Photographic CC0 dirt albedo/normal/roughness with world UVs and six independently tintable patches; texture tiling/grazing-angle and mobile surface acceptance pending |
| 19 | Arena Props (Barrels, Fences, Gates, Chutes) | Original modeled covered stands, roof trusses, fences, signs, fixtures, booth and detailed barrels; visible knock/penalty share one event; final art and animation pending |
| 20 | Crowd System (Stands, Fans, Animation) | New original distant crowd-card strip with inspected alpha; oblique-angle/repetition review, animation and measured mobile costs pending |
| 21 | Arena Themes & Variants | Tier names only; racing arenas not built |
| 22 | Weather & Time-of-Day System | Missing RNG fixed with reproducibility tests; authored weather/runtime integration pending |

**CATEGORY D: BODYCAM GRAPHICS ENGINE (Sections 23–28)**

| # | Section | Current implementation status |
|---|---|---|
| 23 | Bodycam Camera Controller | First person throughout, with bodycam ahead of the new rider torso and connected sleeves/reins; reduced-motion tests pass, phone framing/comfort acceptance pending |
| 24 | Post-Processing Shader Pipeline | Persistent URP reference materials and warm presentation source integrated; scene save/reopen passed; full effects/device validation pending |
| 25 | Lens Effects (Fisheye, Chromatic, Flare) | Not integrated into a racing scene |
| 26 | Motion Effects (Blur, Speed Lines) | Not integrated into a racing scene |
| 27 | Environmental VFX (Dust, Particles, Sweat) | 0.3.0 bounded hoof-dust particles implemented and included in reviewed Unity renders; production VFX/device acceptance pending |
| 28 | Dynamic Exposure & Color Grading | Static warm grading in new graphics source; dynamic exposure and full lighting/readability/device acceptance pending |

**CATEGORY E: GAMEPLAY MECHANICS (Sections 29–36)**

| # | Section | Current implementation status |
|---|---|---|
| 29 | Phase 1 — Beep Gate System | Four-second/six-metre walk, scheduled beeps at 2/3/4 seconds, one release with signed grade/timeout and no forced standstill implemented; physical audio/input alignment pending |
| 30 | Phase 2 — Alley Run | Moving approach to GO/steering/heartbeat handoff and forgiving rein/cadence tuning implemented/tested; phone acceptance pending |
| 31 | Phase 3 — Barrel Turn Pattern System | Classic drawing turn preserved; Reins ordered geometric turns, Pocket/Kiss and bounded Leg Wrap implemented/tested; production animation/phone calibration pending |
| 32 | Pattern Library & Generation | Classic three-shape catalogue; Reins fixed rules fingerprint, surface/round/horse manifest and recording schema; authored content tools/server assignment pending |
| 33 | Touch Input & Path Scoring Algorithm | Classic trace scorer; Reins timestamped touch input, cadence windows and swept contact grading; Unity dispatch and full replay pass, phone calibration pending |
| 34 | Phase 4 — Home Sprint | Reins four-second alternating Drive after third legal barrel, capped events/boost and physical finish tested; actual-phone input/fatigue acceptance pending |
| 35 | Slow Motion System | Classic fixed drawing slow motion preserved; Reins separate constant 20 ms simulation without drawing slowdown; online clocks pending |
| 36 | Run Timer & Penalty Calculator | Classic immutable one-barrel result and Reins three-barrel time plus once-per-barrel 5s penalty tested; trusted match settlement pending |

**CATEGORY F: MULTIPLAYER & COMPETITION (Sections 37–41)**

| # | Section | Current implementation status |
|---|---|---|
| 37 | Multiplayer Networking (Photon Fusion 2) | Placeholder class; actual networking not built |
| 38 | Matchmaking & ELO/Trophy System | Matchmaking not built |
| 39 | Spectator Mode | Not built |
| 40 | Anti-Cheat & Server Validation | V2 shared-Core local verifier passes 78 HTTP checks; completed Unity/.NET replay agrees; authentication, trusted input admission and competitive authority pending |
| 41 | Disconnect Handling & Reconnection | Not built |

**CATEGORY G: PROGRESSION & ECONOMY (Sections 42–47)**

| # | Section | Current implementation status |
|---|---|---|
| 42 | Currency System (Coins, Diamonds, Trophies) | Temporary local coins/trophies; persistence/ledger absent |
| 43 | Gear & Equipment System | Five cosmetic slots (saddle/pad/reins/headstall/gloves), 20 free local styles, validation, real item thumbnails, 3D inspection, explicit equip and cosmetic-v2 persistence; desktop migration/equip checks passed; competitive modifiers/reservations/consumables pending |
| 44 | Loot Crate & Reward System | Not built |
| 45 | Trophy Road & Arena Unlocks | Tier constants only; progression path not built |
| 46 | Daily Missions & Season Pass | Not built |
| 47 | In-App Purchase & Store | Not built |

**CATEGORY H: POLISH, AUDIO & UX (Sections 48–53)**

| # | Section | Current implementation status |
|---|---|---|
| 48 | Adaptive Audio Engine | Three dedicated DSP-scheduled launch voices plus separate cadence and grade audio, lifecycle cleanup tested; final mix/hoof assets and physical latency pending |
| 49 | Haptics & Feedback System | 0.3.0 optional short native iOS/Android feedback observes accepted outcomes; persisted toggle and no editor vibration; native bridges built; phone feel pending |
| 50 | UI/UX Design System | Reins-only startup and pre-run settings plus reference-driven MyStable/Tack/Rider Gear inspection/equip; desktop checks passed; finished visual quality and phone ergonomics pending |
| 51 | Tutorial & Onboarding Flow | Not built |
| 52 | Replay System & Highlights | Isolated Classic/v1/v2 recordings; explicit armed-launch state/fingerprint, v2 replay/save/reload and historical-byte preservation tested; authorized rivals, sharing/highlights pending |
| 53 | Analytics & Telemetry | Not built |

**Position in the delivery sequence.**

| Milestone | Result to deliver | Current position |
|---|---|---|
| M0 | Reconciled source, compilation/package/URP repair, saved arena and first build checks | Foundation/Editor/Android checks passed; later 0.2.0 iPhone build/install/launch confirmed; detailed platform qualification remains open |
| M1 | One playable launch → draw → turn → exit-boost loop | 0.2.0: 33 tests/artifact checks and user-confirmed iPhone play. 0.3.0 polish: 55 Editor tests passed; Play Mode passed 5/5 tests; native build/install verified; phone acceptance pending |
| R1 | Additive Reins Lab and shared replay-verifier prototype | Core, saved scene, desktop replay and local verifier checks passed; 0.4.0 iOS build/install/version and Android APK checks passed; user played and selected Reins; detailed device acceptance pending |
| R2 / 0.5 | Reins main-game startup, moving first-person alley/release launch, forgiving controls, one rigged horse/rider/arena, v2 replay/verifier | Moving launch/v2/startup and replay/verifier source checks pass; both 0.5 native artifacts verified; representative character/visual quality and device acceptance remain pending |
| M1N | Continuous-input two-client timing/authority proof | Local verifier component tested; transport, authenticated authority and service persistence pending |
| M2 | Complete representative race with one finished horse/arena | Scope incorporated into R2 / 0.5; not a duplicate implementation milestone |
| M3 | Live duels/Championships and saved progression | Planned |
| M4 | Recorded challenges | Planned |
| M5 | Simultaneous racing | Planned |
| M6 | Full content/economy beta | Planned |
| M7 | Release qualification and store launch | Planned |

**iPhone preparation.** Xcode 15.2 built and signed development apps on the existing Ventura Mac. The signed IPA was installed on the connected iPhone 17 Pro running iOS 26.6.2 through standard USB installation; Xcode’s developer image lacks this phone variant. The user resolved development-certificate trust and confirmed 0.2.0 opens and feels good. Versions 0.3.0 and 0.4.0 were subsequently built, signed, installed and version-queried. The user has now played the second/Reins mode and prefers it; they want substantially better graphics and more forgiving arcade feel. This confirms mode use and product preference, not a measured launch-latency, sustained-performance or complete lifecycle acceptance pass. [iPhone setup guide](../../README-iPhone.md).

**Next concrete work.** Review the actual moving alley and current stable/gear captures in [the latest art checkpoint](../Art/Reins-Glove-Anatomy-Checkpoint.md); the [v2 checkpoint](Reins-v2-Alley-Checkpoint.md) owns gameplay acceptance. Preserve and install/review the verified 0.5 artifacts when the target phone is available. Refine the separate fitted horse study before runtime replacement, while retaining the existing moving game benchmark. Measure actual timing, visual comfort, lifecycle behavior and sustained performance; natural planted turns/braking and full reference quality remain open. Preserve free/original asset provenance and historical evidence. Do not expand the roster before this quality gate. M1N follows R2 acceptance.

**Companion documents.**

- [Full master build plan](Barrel-Rivals-8-Category-Build-Plan.md)
- [Engineering playbook](Barrel-Rivals-Engineering-Playbook.md)
- [Gameplay blueprint](Barrel-Rivals-Gameplay-Blueprint.md)
- [Technical review and evidence](Barrel-Rivals-Technical-Review.md)

This snapshot incorporates historical M0/M1/0.4, current stable/graphics source and the implemented v2 alley rules/input boundary. Source checks do not replace native, device or photographic-quality acceptance. Multiplayer, trusted progression/economy and release qualification remain future work.

<!-- REINS05_MOBILE_START -->
Fresh **0.5.0/build 5 iPhone and Android development packages are verified**. iPhone export/native compilation/signature/IPA integrity and Android version/signature/ARM64/16 KB alignment checks passed. Neither new package has been installed or playtested on a phone; the last verified installed iPhone app remains 0.4.0/build 4. See [iPhone evidence](../Build/Reins-0.5-iPhone-Checkpoint.md) and [Android evidence](../Build/Reins-0.5-Android-Checkpoint.md). This advances S06 artifact verification; no section or R2 device gate is accepted.
<!-- REINS05_MOBILE_END -->
