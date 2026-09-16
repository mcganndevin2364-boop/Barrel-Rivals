**Barrel Rivals — complete-product build plan**  
September 16, 2026 · Engineering revision 4 · Original 53 sections · Solo creator with AI-assisted development

This is the implementation plan for the complete Barrel Rivals game. It preserves your exact eight category ranges and 53 section titles. `Barrel-Rivals-Gameplay-Blueprint.md` defines player behavior and rules; `Barrel-Rivals-Design-Decisions.md` records choices and experiments. Engineering revision 4 adds `Barrel-Rivals-Engineering-Playbook.md`: the shared stack, architecture, coding contracts and primary resources for every section. The gameplay decisions remain those of revision 3.

**Full product scope.** Live 1v1 turn-based competition is the lead mode: one rider runs while the other spectates. Recorded challenges and simultaneous racing remain required by your earlier request. The game includes at least ten distinct horses, horse upgrades and customization, gear, earned arena tiers, currencies, rewards and a premium store. A short third-person introduction transitions into first-person riding. Launch release, drawing accuracy/speed and timed boosts determine performance while the horse follows the course automatically.


**The full-release contract.** Every row below must be verified for the complete product. Smaller test builds prove milestones without removing requirements from the destination. Counts beyond your ten-horse requirement are working content recommendations based on the existing five-tier outline.

| ID | Player outcome / minimum scope | Sections | Evidence required |
|---|---|---|---|
| R01 | First-person automatic riding with intro, gate timing, drawing/exit skills, three barrels, sprint and correct penalties. | 2–4, 10–12, 23, 29–36 | Repeatable complete runs; readable grades/results; graded errors agree with horse/barrel animation. |
| R02 | Live Quick Duel, three-round Championship format, recorded challenges and simultaneous duels; friends/private challenges and clear spectating. | 5, 35–41, 50, 52 | Cross-platform physical-device sessions, compatible ghost replay, interruption tests and verified settlement in every format. |
| R03 | At least ten distinct horses with appearance, identity, meaningful bounded stats, upgrade ceilings and a complete earned route. | 8–15, 43, 45 | Finished roster records/assets and a tested balance/progression matrix; career/recovery behavior specified and working. |
| R04 | Five earned event tiers and five coherent arena presentations, with verified courses and scalable environment/visual settings. | 16–28, 38, 45 | Playable versions of every tier; final-art checks on target devices; explicit class/difficulty rules. |
| R05 | Coins/diamonds/trophies, useful gear, reliable rewards, missions/pass and a clear premium store. | 9, 42–47 | Zero-spend progression/recovery scenarios, transaction tests, purchase sandbox and entitlement/refund validation. |
| R06 | A complete player journey: account start/recovery, tutorial, stable, event selection, lobby, race, results, replay, rematch and support/settings. | 5–6, 13, 38–39, 48–53 | Uncoached new-player sessions and account/reinstall/recovery tests across supported screens. |
| R07 | Cohesive horse/rider animation, bodycam comfort, environments, sound/haptics and responsive touch feedback. | 10–12, 16–28, 48–51 | Representative visual/audio benchmark plus whole-roster review; reduced-motion and accessibility checks. |
| R08 | Trusted results/economy, operational services and store-ready iOS/Android releases. | 1, 5–7, 37–47, 50–53 | Reproducible signed builds, data/store compliance, security/failure checks, monitored rollout and recovery procedures. |

No feature is counted twice as progress merely because it appears in several categories. For example, replay recording starts with race events but is only complete when playback/versioning and recorded challenges work. Maintain one evidence record per requirement and link its owning sections.

**Evidence baseline, updated after M0 implementation.** The review found 51 C# files, two source errors and no saved racing scene or horse art/animation. The isolated repaired project now compiles, renders its saved arena through URP, passes 10 Edit Mode tests and two Play Mode preview/input tests, and compiles its shared core separately. [M0 implementation report](Barrel-Rivals-M0-Report.md) records platform artifact status and remaining gates. The separate sample project is unchanged. The table now distinguishes these foundation increments from full section coverage; no full section is verified or release-ready.

**Execution standard.** For each section record intended behavior, files/assets changed, meaningful checks, observed results and remaining gaps. Use Planned → Implemented → Verified → Release-ready. A named class or a success message does not by itself pass a section. Keep source, metadata, package versions and build configurations reproducible. Work began with Section 1; continue through connected milestones, and existing “completed/A+” labels do not override failing evidence.

**Engineering execution.** Every section has a matching S01–S53 coding card in the [engineering playbook](Barrel-Rivals-Engineering-Playbook.md). Each card specifies expertise/resources, stack, inputs, deliverables, integration and proof. The Stack / card column below makes that contract part of the build list. CLIENT/CORE/CONTENT/ART/RENDER/NET/API/DB/PLATFORM/STORE/AUDIO/BUILD/OPS are defined in the playbook's stack register.

For each increment, use the correct language/tool for its layer, record actual dependency versions, wire the result into the running game and test the affected boundaries. Update both the section evidence and the relevant full-game requirement. Package, hosting and asset additions follow a demonstrated need and compatibility/cost check. The recommended backend is ASP.NET Core with PostgreSQL, to be validated at M1N before production adoption.

**CATEGORY A: FOUNDATION & INFRASTRUCTURE (Sections 1–7)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 1. Project Setup & Architecture | M0 repair verified in Editor | CLIENT, CORE, BUILD · S01 | Consolidate local/GitHub work, repair compiler/package/assembly failures, pin Unity and restore URP. A clean checkout must import and open the saved startup scene without errors. |
| 2. Input System Foundation | Preview UI wired; gestures pending | CLIENT, CORE · S02 | Capture hold, release, tap and single-stroke drawing with phase ownership and cancellation. Verify multi-touch, focus loss and differing screen sizes on real phones. |
| 3. Game State Machine | Phase-switch scaffold | CORE, CLIENT, API · S03 | Define player and match state machines, including Quick Duel and three-round Championship. Own active input phase, spectator turn, immutable finish and rematch; reject invalid/duplicate transitions. D01–D03, D08. |
| 4. Data Architecture (ScriptableObjects) | Data classes only | CONTENT, CORE, CLIENT · S04 | Create validated, versioned horse, rider, gear, arena, challenge and event data assets with stable IDs. Missing references and invalid values must fail validation before build. |
| 5. Save/Load & Cloud Sync | Not implemented | API, DB, CLIENT · S05 | Separate local settings/practice from authoritative profile/inventory; add account linking, versioned saves and cloud recovery. Test reinstall, conflicts, interrupted writes and recovery. |
| 6. iOS & Android Platform Layer | Android development APK passed; device/iOS qualification pending | PLATFORM, BUILD, CLIENT · S06 | Implement mobile lifecycle/safe areas, build configurations, signing and account/platform integration. Produce installable Android and iOS builds; resolve the modern Mac/Xcode build requirement early. |
| 7. Performance Budget & Quality Tiers | Unmeasured targets | RENDER, CLIENT, BUILD · S07 | Set device-specific CPU/GPU/memory/thermal budgets and visual presets. Profile representative sessions; quality settings must never change competitive timing or drawing difficulty. |

**Category A completion:** The complete input/state/data/account/platform foundation passes its acceptance checks with reproducible builds on both platforms. M0 is only the repair/bootstrap checkpoint.

**CATEGORY B: HORSE SYSTEM (Sections 8–15)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 8. Horse Data Model & Breeds | Breed/stat data sketches | CONTENT, CORE, ART · S08 | Produce ten distinct horse entries across the five proposed event classes, with visible identity, performance role, upgrade ceiling and earned access. One prototype establishes the pipeline; the full roster is required for R03. D10. |
| 9. Horse Stats & Leveling | Stat helpers; no leveling | CORE, API, DB · S09 | Separate owned/upgraded stats from event-effective stats. Test meaningful upgrade benefits, caps and a cleaner lower-power run beating a weaker-skill premium run. No stat changes recognizer/deadline/penalty rules. D04–D05. |
| 10. Horse Animation State Machine | Animator parameter facade | ART, CLIENT · S10 | Build horse/rider animation states for introduction, acceleration, turns, clean/wide/error lines, stumble/contact, sprint and finish. Verify blending, rider attachment and feet/ground alignment. |
| 11. Horse Physics & Movement Controller | Unused movement helper | CORE, CLIENT · S11 | Implement automatic, versioned course movement with grade-dependent lines and authoritative knock events. Separate gameplay path/collision outcomes from presentation physics. Test reproducibility before relying on replay verification. |
| 12. Horse Gait System | Not implemented | ART, CLIENT · S12 | Coordinate walk, trot, canter and gallop with speed, stride, hoof contacts and rider movement. Verify transitions do not create sliding or camera jolts. |
| 13. Horse Visual Customization (Colors/Markings) | Coat data only | ART, CONTENT, CLIENT, API · S13 | Implement coat/marking choices, ownership, previews and material variants with sensible asset reuse. Verify persisted customization in bodycam, intro, spectator and replay views. |
| 14. Horse Aging & Career System | Not implemented | CORE, API, DB · S14 | Implement maturity, experience and career milestones with preserved ownership. Define any age-related competitive effects explicitly and freeze them for a complete match. Verify save/migration behavior. D11. |
| 15. Horse Injury & Recovery System | Not implemented | CORE, API, DB, CLIENT · S15 | Prototype reversible condition/recovery with an always-available route to play. Set exact consequences before activation; verify no compulsory paid recovery, permanent loss or mid-match stat drift is introduced accidentally. D11. |

**Category B completion:** All ten horses and their validated stats, rigs/animation, visual customization, leveling, career and recovery behavior satisfy R03. One representative horse proves production feasibility earlier.

**CATEGORY C: ARENA & ENVIRONMENT (Sections 16–22)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 16. Arena Geometry & WPRA Standards | Standard center distances tested | CORE, CONTENT, ART · S16 | Correct the standard pattern using verified WPRA dimensions; author alley, boundaries and legal route. Validate distances, clearances and course completion; identify arcade-size variants explicitly. |
| 17. Arena Lighting System | URP foundation light renders | RENDER, ART · S17 | Create mobile-compatible URP lighting and stable quality variants. Verify materials, shadows and visibility across intro, first-person, spectator and low-quality views. |
| 18. Arena Ground Surface (Dirt/Footing) | Surface data/helper | RENDER, CONTENT, CORE · S18 | Build dirt/footing visuals and bounded handling effects. Match opponents under equivalent ground conditions; cosmetic ruts must not silently disadvantage the second rider. |
| 19. Arena Props (Barrels, Fences, Gates, Chutes) | Saved prototype props/scene | CONTENT, CLIENT, CORE · S19 | Create saved barrel/fence/gate/chute prefabs with correct scale and collision. Knock each barrel once, restore it on reset and validate the persisted scene after reopening. |
| 20. Crowd System (Stands, Fans, Animation) | Crowd data/math only | ART, RENDER, AUDIO · S20 | Add scalable stands/crowds and skill-reactive sound/animation. Bound reactions and avoid duplicate playback; verify visual budgets and reset between runs. |
| 21. Arena Themes & Variants | Tier names only | CONTENT, ART, RENDER · S21 | Produce coherent presentations for the five proposed event tiers with measured asset reuse. Validate every course, lighting profile and difficulty manifest; one completed arena is only the content-pipeline proof. |
| 22. Weather & Time-of-Day System | RNG repaired/tested; integration pending | CORE, CONTENT, RENDER · S22 | Repair the missing seeded dependency and author weather/time-of-day settings. Freeze comparable match conditions and test visibility, reproducibility and quality-tier behavior. |

**Category C completion:** All five proposed arena presentations, course/prop checks and fair environmental profiles pass. The first arena is a benchmark, not completion of the category.

**CATEGORY D: BODYCAM GRAPHICS ENGINE (Sections 23–28)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 23. Bodycam Camera Controller | Temporary first-person preview | CLIENT, ART · S23 | Replace the proposed primary chase view with the requested rider bodycam; add third-person intro transition, rider/horse framing and comfort settings. Test barrel readability and clipping. |
| 24. Post-Processing Shader Pipeline | Foundation URP renders; final effects pending | RENDER, BUILD · S24 | Restore the render pipeline, renderer assets and post-processing profiles. Verify the real racing scene on both platforms with no magenta materials or missing shaders. |
| 25. Lens Effects (Fisheye, Chromatic, Flare) | Not integrated | RENDER, CLIENT · S25 | Tune fisheye, chromatic effects and flares conservatively, with a reduced-effects preset. The world may be stylized; drawing targets and input must stay legible. |
| 26. Motion Effects (Blur, Speed Lines) | Not integrated | RENDER, CLIENT · S26 | Layer blur, speed cues and camera motion within device budgets. Verify comfort settings and stable input feedback during every gait and slow-motion transition. |
| 27. Environmental VFX (Dust, Particles, Sweat) | Particle references only | RENDER, ART, CLIENT · S27 | Produce pooled dust, hoof contact and other appropriate horse/environment effects. Test clean turns, knocks and sprint effects for visual clarity and bounded cost. |
| 28. Dynamic Exposure & Color Grading | Not implemented | RENDER, ART · S28 | Create consistent color grading and bounded exposure changes. Verify bright/dark arena transitions without hiding barrels, touch prompts or the timer. |

**Category D completion:** Bodycam and configured effect profiles work throughout the final roster/arenas on the supported device tiers, including a comfortable reduced-motion profile with unchanged competitive rules.

**CATEGORY E: GAMEPLAY MECHANICS (Sections 29–36)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 29. Phase 1 — Beep Gate System | Not implemented | CORE, CLIENT, AUDIO · S29 | Hold through two one-second-spaced beeps, release on a bounded randomized third. Define official clock origin at launch cue, early/late/missed release behavior, grades and audio/visual timing. Validate device latency. D07. |
| 30. Phase 2 — Alley Run | Movement helper only | CORE, CLIENT · S30 | Drive automatic alley acceleration and the first barrel approach, using legal horse stats and launch grade. Verify course timing and preview trigger positions. |
| 31. Phase 3 — Barrel Turn Pattern System | Accuracy helper; no loop | CORE, CLIENT · S31 | Build approach preview, pattern recall/drawing, slow-motion turn, graded riding line and exit-timing boost. Make severe failure visibly agree with the single knock penalty. |
| 32. Pattern Library & Generation | Not implemented | CORE, CONTENT, API · S32 | Create curated shapes and controlled tier difficulty. The server assigns equal per-round challenge schedules while retaining future targets until their reveal points. |
| 33. Touch Input & Path Scoring Algorithm | Not implemented | CORE, CLIENT, API · S33 | Implement a transparent grade combining geometry, coverage and completion with a speed bonus only above a quality floor. Calibrate real-phone traces and reject incomplete/scribbled input. Recognition confidence alone is not a grade. |
| 34. Phase 4 — Home Sprint | Whip/boost helper only | CORE, CLIENT, AUDIO · S34 | Implement the final readable timing/rhythm challenge and bounded sprint bonus. Test that charge, cooldown and phase transitions prevent repeated-input exploits. |
| 35. Slow Motion System | Not implemented | CORE, CLIENT, NET, API · S35 | Maintain trusted input deadlines and a validated simulation clock with fixed equal slow-motion windows. Prove two independent rider clocks early and test delay/pause/failure paths before content expansion. D07. |
| 36. Run Timer & Penalty Calculator | Result type repaired; rule integration pending | CORE, API, CLIENT · S36 | Compare integer run times plus one five-second penalty per knocked barrel. Quick Duel compares one run; Championship compares three-run totals/averages. Cover ties, DNF, forfeit and display rounding. D02/D08. |

**Category E completion:** Every complete run phase, skill grade, clock, penalty and failure case works across all supported rulesets and event difficulty levels; expected outcomes are covered by reproducible tests.

**CATEGORY F: MULTIPLAYER & COMPETITION (Sections 37–41)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 37. Multiplayer Networking (Photon Fusion 2) | Placeholder; no networking | NET, API, CLIENT · S37 | Prototype Photon Fusion 2 mobile topology and trusted result-service boundaries early. Demonstrate live-turn spectating and independent simultaneous state before large content production; later harden sessions/auth/regions/costs. D03/D14. |
| 38. Matchmaking & ELO/Trophy System | No matchmaking | API, DB, NET, CLIENT · S38 | Combine earned eligibility, region/connection quality, skill and effective loadout bands. Feature Quick Duel and schedule Championships to manage population. Show explicit alternatives after queue timeout; never silently substitute a bot/replay. |
| 39. Spectator Mode | Not implemented | NET, CLIENT · S39 | Stream the active horse/run for the waiting rival. Use a separate spectator HUD; keep unrevealed shape targets and trace data private until both competitors complete the round. |
| 40. Anti-Cheat & Server Validation | Not implemented | CORE, API, DB · S40 | Validate manifest/version, accepted input timing, legal effective stats and results; own settlement on trusted services. Test forgery, impossible/replayed traces and duplicates. Document macro/collusion limits and operating controls. D14. |
| 41. Disconnect Handling & Reconnection | Not implemented | CORE, API, NET, CLIENT · S41 | Specify cancellation-before-start, reconnect, player forfeit, double-DNF and service void separately. No input-window rewind; refunds/restorations depend on cause and execute once. Test failures at each phase. D08. |

**Category F completion:** Live Quick Duel/Championship, recorded challenges and simultaneous duels work across supported physical iOS/Android devices with verified outcomes, population-aware matchmaking and tested interruption handling.

**CATEGORY G: PROGRESSION & ECONOMY (Sections 42–47)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 42. Currency System (Coins, Diamonds, Trophies) | In-memory coins/trophies | API, DB, CORE · S42 | Implement coins/diamonds and earned trophies with reservation/settlement records, overflow/negative guards and idempotence. Reserve fees once per match; test draws, losses, voids, retries and simultaneous account sessions. D08/D09. |
| 43. Gear & Equipment System | Tack data/slot helpers | CORE, API, DB, CLIENT · S43 | Implement clear owned gear, bounded stat/stacking limits and one-match consumable use covering all Championship runs. Display effective benefits and restoration/forfeit rules before entry. Test concurrency and double consumption. |
| 44. Loot Crate & Reward System | Not implemented | API, DB, CONTENT, CLIENT · S44 | Specify crate contents, progression rewards, duplicate handling and drop tables. Verify grants exactly once; if randomized rewards are sold, disclose actual odds before purchase. |
| 45. Trophy Road & Arena Unlocks | Tier constants only | CORE, API, DB, CLIENT · S45 | Deliver earned access through the five-tier journey and a complete free progression route. Keep a free-entry novice competition path for coin recovery; test repeated losses, anti-farming and absence of accidental paywalls. D04/D09/D10. |
| 46. Daily Missions & Season Pass | Not implemented | API, DB, CONTENT, CLIENT · S46 | Make missions/pass progress through ordinary racing, skill and collection goals. Specify free/premium rewards, versioned schedule and rollover/claim rules. Validate that normal play remains useful without buying the pass. |
| 47. In-App Purchase & Store | Not implemented | STORE, API, DB, CLIENT · S47 | Implement mobile storefronts, clear pack contents, purchase/entitlement verification and restoration where applicable. Test pending/failed/duplicate/refunded transactions in store sandboxes before sales. |

**Category G completion:** Full earned progression, gear, wallet/rewards/pass/store and free recovery routes pass balance, persistence and failure tests; purchases cannot bypass earned event access.

**CATEGORY H: POLISH, AUDIO & UX (Sections 48–53)**

| Section | Starting point | Stack / card | Work and acceptance check |
|---|---|---|---|
| 48. Adaptive Audio Engine | Basic sound methods | AUDIO, CLIENT, CONTENT · S48 | Build gait/surface-linked hoofbeats, crowd response, gate cues, skill feedback, music and audio settings. Test synchronization, interruptions and duplicate-source prevention. |
| 49. Haptics & Feedback System | Not implemented | PLATFORM, CLIENT · S49 | Add optional haptics for cues, good timing, turns and knocks. Verify the experience remains usable with haptics disabled or unsupported. |
| 50. UI/UX Design System | Unwired legacy HUD | CLIENT, CONTENT, API · S50 | Build the complete journey with a clear primary Play action, collection, event eligibility/effective stats, results/rematch, friends and help/account settings. Validate clarity after wins, losses, empty wallet and network failures. |
| 51. Tutorial & Onboarding Flow | Not implemented | CORE, CLIENT, CONTENT · S51 | Teach hold/release, pattern recall/drawing, turn-exit timing, sprint and penalties through practice. New testers must complete and understand a run unaided. |
| 52. Replay System & Highlights | Breadcrumb/material helpers | CORE, CONTENT, API, CLIENT · S52 | Record versioned inputs/events plus visual samples; implement playback/highlights and verified compatible ghost challenges. Group fastest-time boards by comparable manifest/rules/class. Clearly distinguish recorded and live rivals. D13. |
| 53. Analytics & Telemetry | Not implemented | OPS, API, CLIENT, BUILD · S53 | Instrument onboarding, grades, voluntary rematches/returns, queue/interruptions, crashes and economic corrections. Verify events and privacy controls; distinguish evidence of enjoyment from simply longer sessions. Compare format cohorts only with adequate samples. |

**Category H completion:** The complete player journey, adaptive audio/haptics, tutorial, replay/highlights and verified operational telemetry work across the full released content and supported settings.

**Dependencies and early risk tests.** Implement a small complete experience across categories. The section numbering organizes ownership; it must not postpone networking, platform, economy or art feasibility until the end.

| Before investing in… | Prove first | Owners |
|---|---|---|
| Final drawing UI, complex templates or higher-tier difficulty | Bodycam readability, graded trace accuracy and touch-phase boundaries on real phones. | 2, 23, 31–33, 50–51 |
| Ranked racing or elaborate spectator presentation | Trusted input timing, fixed slow-motion clocks, private challenges and two-client result agreement. | 29, 32, 35–41 |
| Ten finished horses or five finished arenas | One representative rig/animation/material/environment pipeline meeting a real-device visual/performance benchmark. | 7–13, 16–28, 48 |
| Paid packs, entry-fee tuning or deep upgrade costs | Effective power/class model, free progression/loss recovery and authoritative ledger. | 5, 9, 38, 42–47 |
| Broad beta / marketing submission | Signed builds, account recovery, compatible backend/versioning and operational diagnosis. | 1, 5–7, 37–47, 50–53 |

**Build milestones.** Each has a deliverable and an exit check. A failed high-risk experiment prompts a recorded revision before dependent content expands. All R01–R08 requirements remain in the full-release contract.

| Milestone | Main sections | Deliverable and exit check |
|---|---|---|
| M0 — Recover and establish builds | 1–4, 6–7, 17, 24 | Preserve/reconcile source, repair compilation/dependencies/URP, save a minimal arena. Clean import and first Android build pass; establish a compatible iOS build machine and named test devices. |
| M1 — Prove one skill turn | 2–3, 8, 10–12, 19, 23, 29–33, 35–36, 50 | Bodycam launch, automatic approach, recall/draw, fixed slow motion, exit timing, error/penalty and retry. Collect real-phone gesture/comfort evidence. |
| M1N — Prove network timing and authority | 5, 32, 35–41, 52 | Minimal two-client experiment: equal manifests, private reveals, accepted input timing, matching scores, independent simultaneous windows and failure outcomes. No finished art is required. Record topology/cost decision. |
| M2 — Complete the representative race | 7–13, 16–36, 48–51 | Three barrels/sprint/results with one production-quality horse/rider and arena, tutorial and measured budgets. Compare Quick Duel/Championship pacing and visible premium-skill balance with prototype loadouts. |
| M3 — Live competition and saved progression | 5, 37–43, 45, 50, 52–53 | Cross-platform Quick Duel and six-run Championship, spectating, eligible matching, one-entry settlement, cloud profile, free novice route and rematch. Match failures recover correctly. |
| M4 — Recorded challenges | 32, 36–40, 52–53 | Compatible recorded rivals, friend/common daily challenges and correctly grouped comparisons. Verify replay versioning and submitted outcomes. |
| M5 — Simultaneous competition | 11, 19, 35–41, 50, 52 | Two active riders with independent course/barrels/windows, stable remote presentation and authoritative results including penalties. Pass the network test matrix. |
| M6 — Complete content and economy beta | Full 8–28, 42–53 scope | Ten finished horses, five coherent arena presentations/event tiers, upgrades/gear/career/recovery, free progression, rewards/missions/pass/store, coherent audio/UX and all three multiplayer experiences. Every R01–R08 row has evidence or a named release blocker. |
| M7 — Release qualification and launch | 6–7, 37–47, 50–53 | Physical-device closed beta, purchase/privacy/store checks, production capacity/costs, staged rollout and rollback. Close release blockers; complete actual store submissions and approval tracking. |

**Provisional quality targets and required checks.** Targets are design goals to calibrate at the early milestones, not achieved measurements or external industry benchmarks. Select exact supported device models before approving the final art budget. A small playtest catches problems; it is not statistical proof of market success.

| Area | Test / provisional target | Evidence record |
|---|---|---|
| First-time understanding | In an initial 10-person uncoached sample, at least 8 complete a legal practice run and explain the five-second knock rule after onboarding. Revise the tutorial if they cannot. | Observed outcomes and specific confusion points, not only completion telemetry. |
| Repeat play | Compare full Quick Duel and Championship sessions; look at voluntary rematches, abandonment, waiting frustration and later return visits. Do not claim a retention uplift from a tiny sample. | Test protocol, sample characteristics and findings; keep chosen format revisable. |
| Input/clock correctness | Replay the same accepted traces with 30/60/120 Hz rendering; validated result and event order remain identical. Include early/late/missing inputs, every barrel and all failure states. | Versioned fixtures, expected result and test outputs. |
| Visual performance | Target stable 60 FPS on the agreed baseline/high profile and a supported 30 FPS fallback if needed; initial frame-time targets are p95 ≤16.7/33.3 ms respectively during a representative 20-minute session. | Named devices/builds, frame-time/thermal/memory captures and budgets; revise scope/profile support from evidence. |
| Network behavior | Exercise 50/150/300 ms round-trip delay and 0/2/5% packet loss, plus reconnect/backgrounding. Define the supported competitive envelope from results; degraded networks get explicit recovery/eligibility behavior. | Both devices’ authoritative results and transition logs. No duplicate awards or stuck matches in any tested case. |
| Skill versus power | Test same-input stat differences and better-skill/lower-power victories in each class; legal premium advantages should not routinely erase a full barrel penalty. | Controlled run matrix for horse levels/gear, with measured times and revised caps. |
| Economy/recovery | Test zero balance, repeated losses, refunded/duplicate purchases, concurrent sessions, cancellation, draw, double-DNF and service void. | Ledger reconciliation: one charge/settlement per match, accurate entitlements and a playable free recovery route. |
| Content completeness | Exercise all ten horses, five arena presentations, quality/comfort profiles and legal equipment combinations. | Asset/rights inventory, missing-reference checks and visual signoff against the benchmark. |
| Production readiness | Reproduce signed builds, recover accounts, operate alerts/support, roll back configuration and address actual store-review requirements. | Release checklist with artifact links and owners; no open critical corruption, payment or progression-loss defects. |

**Failure/settlement matrix.** Detailed values belong to versioned rules, but the economic outcomes must be unambiguous before public competition.

| Event | Race outcome | Economic behavior |
|---|---|---|
| Cancel before match start | No result | Release fee/item reservations. |
| Valid win/loss | Settle once | Publish the predetermined prize/trophy changes; one match item charge covers its runs. |
| Exact tie on underlying times | Draw | Refund entry stakes, no winner prize/trophy transfer; normal consumed match items stay consumed. No repeatable tie-reward farming. |
| Player timeout/forfeit or double player-caused DNF | Loss / double-loss | Apply the published failure policy; no automatic service-void refund and no prize for double-DNF. |
| Confirmed service failure | Void | Restore fees/items exactly once through auditable correction; no trophy loss. |

**Release checks attached to the original sections.** Signed artifacts, privacy/support/account controls, accessibility, commercial-use asset/audio rights, age ratings, accurate store pages and purchase disclosures, closed testing, SDK compatibility and monitored operation are required work. They remain mapped to Sections 6–7, 40–47 and 50–53; this preserves the 53-section structure rather than leaving publishing as an unowned afterthought.

**Work ownership and cost decisions.** Implementation, tests, asset preparation and documentation can be handled incrementally in this project with AI assistance. You remain the product owner for actual play feel/visual review, physical-device access, developer-account identity and spending decisions. The reference art/rig pipeline, target build machine, backend and purchases need explicit cost estimates before commitment. The plan creates no paid accounts and assumes no hired team or automatic external testing. Record each decision and its evidence in the decision register rather than leaving it scattered across conversations.

**Store and standards references.** If randomized rewards are sold, their odds must be disclosed before purchase under both [Apple’s guidelines](https://developer.apple.com/app-store/review/guidelines/) and [Google Play’s payment policy](https://support.google.com/googleplay/android-developer/answer/9858738?hl=en). Section 16 should use [WPRA’s published standard-pattern description](https://wpra.com/the-proof-is-in-the-dirt-in-the-wilderness-circuit/) and the [current rule book](https://wpra.com/rule-book/) when authoring regulation-style arenas. Platform/toolchain findings from the initial inspection remain in `Barrel-Rivals-Technical-Review.md` and must be rechecked at release.

**Quality and solo-production constraints.** “Premium” means observable results: convincing horse/rider animation, a readable and comfortable bodycam, responsive touch grading, accurate clocks, stable sessions, recoverable saves/purchases, cohesive art/audio and measured device performance. A capsule or isolated script can demonstrate early logic but cannot satisfy the final visual or product requirement. Plan asset creation/sourcing and animation as real work. Build and approve one representative horse/arena before scaling the collection.

Available weekly time, device access, art sources and spending limits remain unknown, so this document does not promise a launch date. I can implement and validate incrementally with you; playtesting and product judgment determine whether the experience meets your standard. Update the section status only when its evidence exists.

**Next implementation:** M1’s actual first-person skill loop, followed by the small M1N networking/clock experiment before content expansion. M0 source repair, saved-scene rendering, automated preview checks and Android artifact checks have passed. Physical-device qualification and the compatible iOS build environment remain open; the M0 report and master status carry current evidence.
