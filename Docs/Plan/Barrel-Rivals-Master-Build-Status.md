**Barrel Rivals — master build status**  
September 16, 2026 · Engineering plan revision 4 · Eight categories / 53 sections

**Current position: M1 — first-barrel practice implemented; 33 Editor/Play Mode tests passed and the Android APK built and passed artifact checks. Phone and iOS qualification remain open.**

The same recovered `Barrel-Rivals-M0` working copy now connects launch → remember/draw → graded turn → exit timing → practice result/retry. The pure C# rules and Unity adapter are separate. [M1 implementation report](Barrel-Rivals-M1-Report.md) records the checks, artifact and limits; [M0 report](Barrel-Rivals-M0-Report.md) preserves the foundation evidence.

No original full section has passed all its integration/device acceptance requirements: **0 of 53 full sections verified complete**. Individual foundation and practice checks have now passed; this is a section-acceptance count, not a percentage of effort. The project is a playable one-barrel prototype with placeholder geometry; the full competitive race remains ahead.

**Work completed in this collaboration.**

| Work | Completed outcome |
|---|---|
| Project/code review | Inspected racing source, separate sample project, GitHub and Unity configuration; retained the earlier audit. |
| Design and engineering | Preserved eight categories/53 sections; documented the full gameplay, stack, dependencies and acceptance contracts. |
| Source recovery | Fetched GitHub main `b77f4e5`, preserved local files, and reconciled them in isolated branch `codex/m0-foundation`. Original project folders remain unchanged. |
| Compilation repair | Fixed the missing result type and seeded RNG dependency; repaired package and assembly references. Unity and independent core compilation passed. |
| Rendering and scene | Saved valid pipeline/renderer/material assets, a measured three-barrel course, proxy horse, fences and UI. Unity rendered the new scene without pink materials. The separate sample project remains unchanged. |
| Runtime proof | M1 adds phase-owned launch/draw/exit input, graded riding, fixed slow motion, one knock penalty and immutable practice results/retry. Editor touch and visual checks are recorded in the M1 report; physical touch remains pending. |
| Reproduction tools | Added Unity batch entry points, pinned packages/metadata, test reports and development guidance. |

**Implementation baseline.** The original racing checkout remains at `b156a70` with its local work preserved. The repaired copy is based on the freshly fetched GitHub commit `b77f4e5`. It adds a saved foundation scene, persistent prefabs/materials and shared core/test assemblies. Horse art/animation and complete race/multiplayer systems remain to be built. M1 continues on local branch `codex/m1-skill-loop` from the M0 commit `feabb99`; it is the same repository and remote. No changes have been pushed to GitHub.

**How to read the list.** “Early code,” “helper” or “prototype” means a starting point exists but the section is not implemented and verified end to end. “Not built” means no implementation of the required system was found in the reviewed game project. All sections have a planning/engineering specification; none is release-ready.

**CATEGORY A: FOUNDATION & INFRASTRUCTURE (Sections 1–7)**

| # | Section | Current implementation status |
|---|---|---|
| 1 | Project Setup & Architecture | M0 repair implemented; clean import/core compilation/scene reopen passed; full architecture/platform acceptance pending |
| 2 | Input System Foundation | M1 single-pointer launch/draw/exit adapter wired; cancellation/retry tested; real-phone sampling and latency pending |
| 3 | Game State Machine | M1 pure practice state machine and immutable result tested; full run/match/DNF/reconnect states pending |
| 4 | Data Architecture (ScriptableObjects) | Data classes exist; authored assets/validation pending |
| 5 | Save/Load & Cloud Sync | Not built |
| 6 | iOS & Android Platform Layer | Android APK verified; iOS Xcode source export succeeded/inspected; Xcode installation, native compile/signing and physical phones pending |
| 7 | Performance Budget & Quality Tiers | Targets proposed; device performance unmeasured |

**CATEGORY B: HORSE SYSTEM (Sections 8–15)**

| # | Section | Current implementation status |
|---|---|---|
| 8 | Horse Data Model & Breeds | Breed/stat definitions only; ten-horse roster not built |
| 9 | Horse Stats & Leveling | Stat helpers exist; leveling not built |
| 10 | Horse Animation State Machine | Animation-parameter code only; rigs/controllers absent |
| 11 | Horse Physics & Movement Controller | M1 automatic one-barrel path responds to skill grades; complete course, stats and rig integration pending |
| 12 | Horse Gait System | Not built |
| 13 | Horse Visual Customization (Colors/Markings) | Coat definitions only; customization not built |
| 14 | Horse Aging & Career System | Not built |
| 15 | Horse Injury & Recovery System | Not built |

**CATEGORY C: ARENA & ENVIRONMENT (Sections 16–22)**

| # | Section | Current implementation status |
|---|---|---|
| 16 | Arena Geometry & WPRA Standards | Standard-pattern center distances implemented and tested; route legality and production arena pending |
| 17 | Arena Lighting System | URP foundation lighting renders; mobile/production lighting pending |
| 18 | Arena Ground Surface (Dirt/Footing) | Surface definitions and helper code |
| 19 | Arena Props (Barrels, Fences, Gates, Chutes) | Saved prototype props; M1 visible knock and penalty share one event; final art/animation pending |
| 20 | Crowd System (Stands, Fans, Animation) | Crowd definitions/calculations only |
| 21 | Arena Themes & Variants | Tier names only; racing arenas not built |
| 22 | Weather & Time-of-Day System | Missing RNG fixed with reproducibility tests; authored weather/runtime integration pending |

**CATEGORY D: BODYCAM GRAPHICS ENGINE (Sections 23–28)**

| # | Section | Current implementation status |
|---|---|---|
| 23 | Bodycam Camera Controller | M1 third-person intro blends into bodycam; phone comfort and final rider framing pending |
| 24 | Post-Processing Shader Pipeline | New racing URP assets render correctly; production effects/mobile validation pending; separate sample unchanged |
| 25 | Lens Effects (Fisheye, Chromatic, Flare) | Not integrated into a racing scene |
| 26 | Motion Effects (Blur, Speed Lines) | Not integrated into a racing scene |
| 27 | Environmental VFX (Dust, Particles, Sweat) | Particle references only; effects not integrated |
| 28 | Dynamic Exposure & Color Grading | Not built |

**CATEGORY E: GAMEPLAY MECHANICS (Sections 29–36)**

| # | Section | Current implementation status |
|---|---|---|
| 29 | Phase 1 — Beep Gate System | M1 seeded three-beep hold/release, early/late/missed rules and scheduled tones; device latency/authority pending |
| 30 | Phase 2 — Alley Run | M1 graded automatic alley and single preview transition; full-course/stat integration pending |
| 31 | Phase 3 — Barrel Turn Pattern System | M1 preview/draw/turn/exit loop wired with knock and retry; human playtesting and animation pending |
| 32 | Pattern Library & Generation | M1 circle/triangle/square catalogue with versioned local seed; tier library/private server reveals pending |
| 33 | Touch Input & Path Scoring Algorithm | M1 normalized/resampled closed-shape grader with quality-gated speed; synthetic edge cases pass; human-phone calibration pending |
| 34 | Phase 4 — Home Sprint | Boost helper only; sprint challenge not built |
| 35 | Slow Motion System | M1 fixed response window and separate integer simulation clock tested; independent network clocks pending |
| 36 | Run Timer & Penalty Calculator | M1 immutable one-barrel time plus one 5-second penalty tested; full-run/DNF/tie/server result pending |

**CATEGORY F: MULTIPLAYER & COMPETITION (Sections 37–41)**

| # | Section | Current implementation status |
|---|---|---|
| 37 | Multiplayer Networking (Photon Fusion 2) | Placeholder class; actual networking not built |
| 38 | Matchmaking & ELO/Trophy System | Matchmaking not built |
| 39 | Spectator Mode | Not built |
| 40 | Anti-Cheat & Server Validation | Not built |
| 41 | Disconnect Handling & Reconnection | Not built |

**CATEGORY G: PROGRESSION & ECONOMY (Sections 42–47)**

| # | Section | Current implementation status |
|---|---|---|
| 42 | Currency System (Coins, Diamonds, Trophies) | Temporary local coins/trophies; persistence/ledger absent |
| 43 | Gear & Equipment System | Tack definitions/slot helpers only |
| 44 | Loot Crate & Reward System | Not built |
| 45 | Trophy Road & Arena Unlocks | Tier constants only; progression path not built |
| 46 | Daily Missions & Season Pass | Not built |
| 47 | In-App Purchase & Store | Not built |

**CATEGORY H: POLISH, AUDIO & UX (Sections 48–53)**

| # | Section | Current implementation status |
|---|---|---|
| 48 | Adaptive Audio Engine | M1 scheduled launch tone cues; audio latency, licensed sound and adaptive mix pending |
| 49 | Haptics & Feedback System | Not built |
| 50 | UI/UX Design System | M1 phase HUD, drawing/countdown, grade/result comparison and retry; full player journey pending |
| 51 | Tutorial & Onboarding Flow | Not built |
| 52 | Replay System & Highlights | Position-recording/material helpers; replay playback absent |
| 53 | Analytics & Telemetry | Not built |

**Position in the delivery sequence.**

| Milestone | Result to deliver | Current position |
|---|---|---|
| M0 | Reconciled source, compilation/package/URP repair, saved arena and first build checks | Foundation/Editor checks and Android APK passed; physical-device/iOS qualification still open |
| M1 | One playable launch → draw → turn → exit-boost loop | Implemented; 33 tests and Android artifact checks passed; physical-phone acceptance pending |
| M1N | Early two-client timing/authority and independent-clock proof | Planned |
| M2 | Complete representative race with one finished horse/arena | Planned |
| M3 | Live duels/Championships and saved progression | Planned |
| M4 | Recorded challenges | Planned |
| M5 | Simultaneous racing | Planned |
| M6 | Full content/economy beta | Planned |
| M7 | Release qualification and store launch | Planned |

**iPhone preparation.** The user’s phone is an iPhone 17 Pro (reported iOS 26.6.2). Unity exported an unsigned Xcode project successfully, and its structural checks passed. The available Mac has no full Xcode; a free Personal Team/Xcode 15.2 attempt is pending Apple sign-in and compatibility testing. No signed iPhone app or phone run is claimed. [iPhone setup guide](../../README-iPhone.md).

**Next concrete work.** Playtest M1 on physical phones and calibrate timing/drawing/comfort, while preparing the M1N two-client authority/clock proof. Establish a compatible iOS build path before closing the platform gate. Then M2 expands to the complete three-barrel race, sprint and representative final art.

**Companion documents.**

- [Full master build plan](Barrel-Rivals-8-Category-Build-Plan.md)
- [Engineering playbook](Barrel-Rivals-Engineering-Playbook.md)
- [Gameplay blueprint](Barrel-Rivals-Gameplay-Blueprint.md)
- [Technical review and evidence](Barrel-Rivals-Technical-Review.md)

This snapshot incorporates M0 recovery and the M1 first-barrel implementation and recorded evidence. Device gameplay, performance and multiplayer checks remain pending. Update each section only when its evidence changes.
