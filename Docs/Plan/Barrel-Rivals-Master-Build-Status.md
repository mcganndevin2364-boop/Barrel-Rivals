**Barrel Rivals — master build status**  
September 20, 2026 · Engineering plan revision 6 · Eight categories / 53 sections

The current source fits **mane/forelock strands and a contoured western saddle to the live horse mesh**, adds an original natural hair atlas and layered rocky scenery with sparse pines, and preserves the connected MyStable/Tack/Rider Gear flow. Read [the art-fit checkpoint](../Art/Reins-Art-Fit-Checkpoint.md) for this pass’s verification and limits. The preceding visual-depth source retains grounded-stance gait studies, torso-following presentation, softer stable lighting and drier surfaces. The 19-bone horse still needs improved anatomy, complete rider/turn animation and phone qualification; photographic reference quality and the approved 0.5/v2 gameplay remain unfinished.

The preceding character-motion source added **skinned mane/tail strands and synchronized riding motion**, plus a correction for Animator visibility culling that could advance the clock without updating bones. The moving Unity check, actual captures and current verification belong to [the character-motion checkpoint](../Art/Reins-Character-Motion-Checkpoint.md); earlier static screenshots remain historical. This continues the reference-directed MyStable/Tack/Rider Gear work below. Natural foot planting, reference-quality art, a complete rider, phone qualification and the approved gameplay-v2 launch remain unfinished.

The current source includes reference-driven **MyStable, Tack and Rider Gear**: the user has supplied the warm barn/roster, saddle-grid and glove-inspection reference. The implementation adds an enclosed 3D showroom, rendered item cards, 20 free local cosmetics across five slots, preview/equip/cancel, and cosmetic save version 2 with preserved v1 migration. Cosmetic v2 is separate from the still-unimplemented Reins gameplay v2. Read [the feature checkpoint](../Features/MyStable-and-Gear.md). It records current verification status and remaining gaps; the reference is a target, not achieved photographic quality. Paid ownership, earned progression, the ten-horse roster and a new phone installation remain pending.

**Current position: the last native/device artifact is Barrel Rivals 0.4.0/build 4: Classic plus Reins Lab using rules v1. Its iPhone installation and Android artifact checks remain historical evidence (77 Editor / 8 Play Mode / 46 local HTTP checks at that checkpoint). Newer graphics SOURCE adds the imported free horse/three basic clips, all-phase rider camera, photographic CC0 PBR maps/dusk sky, modeled arena structures/barrels/terrain, gloves/bridle/braided reins and a compact licensed-font HUD. Scene generation/save/reopen passed; see the [graphics checkpoint](../Art/Reins-Premium-Graphics-Checkpoint.md) for dated tests. It retains 0.4 identity, v1 rules/saves and Classic navigation and has no new native build/install evidence. The approved 0.5/v2 moving alley remains unimplemented; full visual/device acceptance and online services remain outstanding.**

The same recovered `Barrel-Rivals-M0` working copy retains the earlier Classic launch → remember/draw → graded turn → exit timing loop as historical source and saves. The 0.3.0 checkpoint added retry, bests, own-best ghosts, coaching and feedback. The implemented 0.4.0 Reins Lab connects three gate taps → rein/cadence riding → three legal barrel turns → Drive → physical finish and local replay. The user chose this system over Classic and supplied a realistic rider-view image as the appearance target throughout gameplay. The approved v2 update makes Reins the startup game and removes Classic navigation; this source checkpoint does not make that migration. [Reins implementation report](Reins-Lab-Implementation.md) records historical v1 evidence; [the approved 0.5 plan](Reins-Racing-0.5-Implementation.md) governs remaining gameplay changes. [M1](Barrel-Rivals-M1-Report.md) and [M0](Barrel-Rivals-M0-Report.md) remain historical evidence.

No original full section has passed all its integration/device acceptance requirements: **0 of 53 full sections verified complete**. This is a section-acceptance count, not an effort percentage. New source replaces the Reins procedural horse with an imported skinned art base and improves arena/camera/HUD presentation. Its three gait studies are not finished natural movement; a full rigged rider, refined tack/hands/reins, full-reference appearance and measured phone qualification remain ahead. Neither extraction nor a saved scene completes the visual milestone or the online competitive game.

**Work completed in this collaboration.**

| Work | Completed outcome |
|---|---|
| Project/code review | Inspected racing source, separate sample project, GitHub and Unity configuration; retained the earlier audit. |
| Design and engineering | Preserved eight categories/53 sections; documented the full gameplay, stack, dependencies and acceptance contracts. |
| Source recovery | Fetched GitHub main `b77f4e5`, preserved local files, and reconciled them in isolated branch `codex/m0-foundation`. Original project folders remain unchanged. |
| Compilation repair | Fixed the missing result type and seeded RNG dependency; repaired package and assembly references. Unity and independent core compilation passed. |
| Rendering and scene | Saved valid pipeline/renderer/material assets, a measured three-barrel course, proxy horse, fences and UI. Unity rendered the new scene without pink materials. The separate sample project remains unchanged. |
| Runtime proof | M1 adds phase-owned launch/draw/exit input, graded riding, fixed slow motion, one knock penalty and immutable practice results/retry. The user confirmed 0.2.0 works on iPhone and liked its touch feel. 0.3.0 adds local bests/ghost/coaching and feedback; its Editor/Play Mode and iPhone build/install checks passed. The user subsequently played 0.4.0 Reins and selected its mechanics; this is qualitative feedback, not a complete measured device acceptance pass. |
| Practice presentation | Historical procedural arena/horse and sound/haptics retained. New graphics SOURCE imports a free 19-bone horse with three original gait studies, persistent URP materials, Linear/warm grade, generated dirt/crowd, all-phase rider camera/reduced motion and charcoal/gold HUD. A second source pass adds photographic PBR maps/sky, modeled arena props and foreground gloves/bridle/reins. Generation/save/reopen passed; finished character motion/reference quality and phone evidence remain pending. |
| MyStable and Gear | Reference-driven enclosed 3D showroom, saddle grid and rider glove inspection; 20 local cosmetics/five slots, real mesh thumbnails, preview/equip/cancel, preserved migration into cosmetic save v2, and race appearance binding. 101/101 Editor and 17/17 Play Mode tests passed; realistic final art, earned upgrades and trusted progression remain open. |
| Reproduction tools | Added Unity batch entry points, pinned packages/metadata, test reports and development guidance. |

**Implementation baseline.** The original racing checkout remains at `b156a70` with its local work preserved. The repaired copy is based on the freshly fetched GitHub commit `b77f4e5`. It adds a saved foundation scene, persistent prefabs/materials and shared core/test assemblies. The Reins Lab now supplies an offline three-barrel race prototype; production horse/rider art and animation, full competitive presentation and multiplayer remain to be built. M1 continues on local branch `codex/m1-skill-loop` from the M0 commit `feabb99`; it is the same repository and remote. This document describes the checkpoint before publication; the root [AI handoff](../../Barrel-Rivals-AI-Handoff.md) and Git history identify its exact source commit and published branch. Do not infer push completion from this status document alone.

**How to read the list.** “Early code,” “helper” or “prototype” means a starting point exists but the section is not implemented and verified end to end. “Not built” means no implementation of the required system was found in the reviewed game project. All sections have a planning/engineering specification; none is release-ready.

**CATEGORY A: FOUNDATION & INFRASTRUCTURE (Sections 1–7)**

| # | Section | Current implementation status |
|---|---|---|
| 1 | Project Setup & Architecture | M0 repair implemented; clean import/core compilation/scene reopen passed; full architecture/platform acceptance pending |
| 2 | Input System Foundation | Reins v1 owned touch/input dispatch tested; user prefers its feel; v2 launch ownership, 50% rein travel and phone ergonomics qualification pending |
| 3 | Game State Machine | Classic practice state tested; Reins preview/gate/race/Drive/finish/cancel/timeout state tested; online match/reconnect states pending |
| 4 | Data Architecture (ScriptableObjects) | Data classes exist; authored assets/validation pending |
| 5 | Save/Load & Cloud Sync | Classic64-best records and five isolated Reins surface bests; isolated cosmetic-v2 save and preserved migration from v1, atomic replace/backup/session recovery; current migration verification, phone restart proof and cloud sync pending |
| 6 | iOS & Android Platform Layer | 0.4.0 iOS build/sign/install/version and Android APK signature/ARM64/16 KB checks verified; user played Reins and prefers it; measured iPhone qualification and Android handset test pending |
| 7 | Performance Budget & Quality Tiers | Targets proposed; device performance unmeasured |

**CATEGORY B: HORSE SYSTEM (Sections 8–15)**

| # | Section | Current implementation status |
|---|---|---|
| 8 | Horse Data Model & Breeds | One starter horse displayed in the 3D stable; four bounded Reins trait parameters unchanged; distinct ten-horse roster, selection and bond pending |
| 9 | Horse Stats & Leveling | Bounded Reins effective movement traits tested; leveling, ownership and trusted progression pending |
| 10 | Horse Animation State Machine | New graphics source imports 19-bone CC0 horse and movement-driven Idle/Walk/Gallop studies; production clips/blending, rigged rider/tack and device acceptance pending |
| 11 | Horse Physics & Movement Controller | Classic automatic one-barrel path and Reins analog fixed-step three-barrel course with swept contacts implemented; final rig and online authority pending |
| 12 | Horse Gait System | Original Idle/Walk/Gallop studies now verified moving in actual rendered frames; bone culling corrected, skinned hair and camera/hand phase connected. Natural foot planting, turns/braking, full rider/hoof audio and device acceptance pending |
| 13 | Horse Visual Customization (Colors/Markings) | Twenty local tack/rider cosmetic styles preview/equip with persisted race bindings; current reference-update verification, coat/marking customization and trusted ownership pending |
| 14 | Horse Aging & Career System | Not built |
| 15 | Horse Injury & Recovery System | Not built |

**CATEGORY C: ARENA & ENVIRONMENT (Sections 16–22)**

| # | Section | Current implementation status |
|---|---|---|
| 16 | Arena Geometry & WPRA Standards | Standard-pattern geometry plus tested Reins ordered winding and physical finish; production hull/arena validation pending |
| 17 | Arena Lighting System | Photographic dusk sky, Linear HDR/ACES, warm directional light and original fixtures integrated; saved/reopened scene passed; final lighting and phone appearance/performance pending |
| 18 | Arena Ground Surface (Dirt/Footing) | Photographic CC0 dirt albedo/normal/roughness with world UVs and six independently tintable patches; texture tiling/grazing-angle and mobile surface acceptance pending |
| 19 | Arena Props (Barrels, Fences, Gates, Chutes) | Original modeled covered stands, roof trusses, fences, signs, fixtures, booth and detailed barrels; visible knock/penalty share one event; final art and animation pending |
| 20 | Crowd System (Stands, Fans, Animation) | New original distant crowd-card strip with inspected alpha; oblique-angle/repetition review, animation and measured mobile costs pending |
| 21 | Arena Themes & Variants | Tier names only; racing arenas not built |
| 22 | Weather & Time-of-Day System | Missing RNG fixed with reproducibility tests; authored weather/runtime integration pending |

**CATEGORY D: BODYCAM GRAPHICS ENGINE (Sections 23–28)**

| # | Section | Current implementation status |
|---|---|---|
| 23 | Bodycam Camera Controller | New source keeps first person through every phase with reduced motion; final horse/rider framing, comfort and physical-phone acceptance pending |
| 24 | Post-Processing Shader Pipeline | Persistent URP reference materials and warm presentation source integrated; scene save/reopen passed; full effects/device validation pending |
| 25 | Lens Effects (Fisheye, Chromatic, Flare) | Not integrated into a racing scene |
| 26 | Motion Effects (Blur, Speed Lines) | Not integrated into a racing scene |
| 27 | Environmental VFX (Dust, Particles, Sweat) | 0.3.0 bounded hoof-dust particles implemented and included in reviewed Unity renders; production VFX/device acceptance pending |
| 28 | Dynamic Exposure & Color Grading | Static warm grading in new graphics source; dynamic exposure and full lighting/readability/device acceptance pending |

**CATEGORY E: GAMEPLAY MECHANICS (Sections 29–36)**

| # | Section | Current implementation status |
|---|---|---|
| 29 | Phase 1 — Beep Gate System | Reins v1 three-peak gate/standstill tested; 0.5 four-second moving alley, three beeps and single-release grades approved, not implemented |
| 30 | Phase 2 — Alley Run | Reins v1 steering/cadence/launch acceleration tested; 0.5 approach-to-steering/heartbeat handoff and forgiving tuning pending |
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
| 40 | Anti-Cheat & Server Validation | Local ASP.NET shared-Core verifier passes 46 HTTP checks; Unity canonical replay matches; authenticated manifests, input admission and production anti-cheat pending |
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
| 48 | Adaptive Audio Engine | Classic hoof/tack/dirt/grade audio plus Reins scheduled gate/cadence cues; physical latency, final sound assets and adaptive mix pending |
| 49 | Haptics & Feedback System | 0.3.0 optional short native iOS/Android feedback observes accepted outcomes; persisted toggle and no editor vibration; native bridges built; phone feel pending |
| 50 | UI/UX Design System | Compact v1 race HUD plus reference-driven MyStable/Tack/Rider Gear navigation, filtered item grid, 3D orbit, true base-trait information and preview/equip/cancel; desktop layout/navigation checks passed; v2 startup and phone ergonomics pending |
| 51 | Tutorial & Onboarding Flow | Not built |
| 52 | Replay System & Highlights | Classic and Reins own-best replay formats isolated; Reins full save/reload/replay/fingerprint/session-failure checks pass; opponent authorization, live gap, sharing/highlights pending |
| 53 | Analytics & Telemetry | Not built |

**Position in the delivery sequence.**

| Milestone | Result to deliver | Current position |
|---|---|---|
| M0 | Reconciled source, compilation/package/URP repair, saved arena and first build checks | Foundation/Editor/Android checks passed; later 0.2.0 iPhone build/install/launch confirmed; detailed platform qualification remains open |
| M1 | One playable launch → draw → turn → exit-boost loop | 0.2.0: 33 tests/artifact checks and user-confirmed iPhone play. 0.3.0 polish: 55 Editor tests passed; Play Mode passed 5/5 tests; native build/install verified; phone acceptance pending |
| R1 | Additive Reins Lab and shared replay-verifier prototype | Core, saved scene, desktop replay and local verifier checks passed; 0.4.0 iOS build/install/version and Android APK checks passed; user played and selected Reins; detailed device acceptance pending |
| R2 / 0.5 | Reins main-game startup, moving first-person alley/release launch, forgiving controls, one rigged horse/rider/arena, v2 replay/verifier | Partial art/camera/HUD SOURCE work now exists on v1; moving launch/v2/startup and full character/visual/device acceptance remain pending; exact contract unchanged |
| M1N | Continuous-input two-client timing/authority proof | Local verifier component tested; transport, authenticated authority and service persistence pending |
| M2 | Complete representative race with one finished horse/arena | Scope incorporated into R2 / 0.5; not a duplicate implementation milestone |
| M3 | Live duels/Championships and saved progression | Planned |
| M4 | Recorded challenges | Planned |
| M5 | Simultaneous racing | Planned |
| M6 | Full content/economy beta | Planned |
| M7 | Release qualification and store launch | Planned |

**iPhone preparation.** Xcode 15.2 built and signed development apps on the existing Ventura Mac. The signed IPA was installed on the connected iPhone 17 Pro running iOS 26.6.2 through standard USB installation; Xcode’s developer image lacks this phone variant. The user resolved development-certificate trust and confirmed 0.2.0 opens and feels good. Versions 0.3.0 and 0.4.0 were subsequently built, signed, installed and version-queried. The user has now played the second/Reins mode and prefers it; they want substantially better graphics and more forgiving arcade feel. This confirms mode use and product preference, not a measured launch-latency, sustained-performance or complete lifecycle acceptance pass. [iPhone setup guide](../../README-iPhone.md).

**Next concrete work.** Review the [graphics source checkpoint](../Art/Reins-Premium-Graphics-Checkpoint.md), preserve its imported/authored assets and address visual limitations. Follow [Reins Racing 0.5](Reins-Racing-0.5-Implementation.md): make Reins the sole player-facing game, implement the four-second first-person alley with beeps at 2/3/4 seconds and a single release, then heartbeat/steering handoff and approved forgiving tuning. Upgrade rules/contracts/saves/verifier fixtures together to v2. Complete the free/original horse/rider/arena with natural planted animation and refined foreground tack/hands/reins and actual phone timing/visual/performance evidence before scaling content. Classic and v1 records remain preserved; M1N follows R2 acceptance. [Asset research](../Art/Horse-and-Rider-Options.md) and [provenance](../Art/Reins-Reference-Graphics-Provenance.md) distinguish inspected source from completed character quality.

**Companion documents.**

- [Full master build plan](Barrel-Rivals-8-Category-Build-Plan.md)
- [Engineering playbook](Barrel-Rivals-Engineering-Playbook.md)
- [Gameplay blueprint](Barrel-Rivals-Gameplay-Blueprint.md)
- [Technical review and evidence](Barrel-Rivals-Technical-Review.md)

This snapshot incorporates M0/M1, Classic 0.3.0, Reins 0.4.0, the newer reference-graphics source and remaining approved 0.5 work. The historical 77/8/46 checks belong to the prior 0.4.0 source; consult the graphics checkpoint for later tests rather than transferring old results. There is no new native/device artifact for the graphics source. Sustained device performance, finished character/reference quality, v2 replay and multiplayer remain pending. Update each section only when its evidence changes.
