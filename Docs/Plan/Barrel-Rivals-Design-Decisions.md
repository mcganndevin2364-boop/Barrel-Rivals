**Barrel Rivals — design decisions and review findings**  
September 16, 2026 · Design revision 3 with engineering revision 4 additions

The full outcome remains a polished iOS/Android game with automatic first-person racing, live alternating turns and spectating, recorded challenges, simultaneous racing, at least ten horses, earned tiers, progression and purchases. The original eight categories and 53 sections remain intact.

You asked me to recommend the more replayable match format and to base purchases on comparable games. The choices below are working design recommendations under that direction. They are ready to prototype; they are not claims of implemented features or proven player retention.

**What needed improvement.** The previous documents left normal match length, timer origin, ties, entry charges, progression recovery and full-release content too vague. They also postponed network feasibility until after substantial content work, and some category completion statements described only a one-horse prototype. This revision defines those connections and separates prototype evidence from full-product completion.

**What the reference games actually support.** These observations come from developer-maintained descriptions/support pages, not measured retention or monetization data. They do not establish which feature caused those games’ commercial success.

| Reference | Verified feature | Barrel Rivals application |
|---|---|---|
| Hunting Sniper | Its official listing identifies PvP play and offers bundles and gems. | Clear rival competition, a premium currency and understandable packs. Exact opponent-matching and power formulas were not verified. [Official listing](https://apps.apple.com/us/app/hunting-sniper/id6446005634). |
| CSR Racing 2 | Collection, customization, upgrades/tuning and live racing are central features. | Horses should be desirable to own and improve, with meaningful performance differences inside appropriate event classes. [Developer overview](https://www.zynga.com/games/csr-racing-2/). |
| 8 Ball Pool | Standard and premium Victory Cues can be upgraded, increasing Force, Aim, Spin or Time. | Gear can provide visible, bounded stat advantages. Skill execution and event eligibility still matter. [Miniclip’s cue documentation](https://support.miniclip.com/hc/en-us/articles/115015644088-Victory-Cues-8-Ball-Pool). |
| CSR2 Race Pass | Tasks advance a pass through normal activities including racing, upgrading and customization. | Missions and a pass should support ordinary play and collection goals. [Developer support](https://zyngasupport.helpshift.com/hc/en/55-csr-2/faq/19545-race-pass/). |

My recommendation is collection plus meaningful, bounded progression advantages, rather than making every purchase cosmetic. The event rules prevent an endgame purchase from overwhelming the beginner pool. That balance is a Barrel Rivals design choice; the sources above do not establish equivalent caps in those games.

**Decision register.** “Required” comes from your expressed vision. “Working choice” is a recommendation made for this review. “Experiment” has a defined test before permanent commitment.

| ID | Decision | Status and rationale | Validation / affected sections |
|---|---|---|---|
| D01 | Automatic movement; rider bodycam; hold/release launch; recalled drawings and exit timing. | Required. These define the game’s identity. | Complete one convincing run with these exact interactions; 2, 10–12, 23, 29–36. |
| D02 | Quick Duel is the everyday live mode: one full run per rider. Three-round Championship retains the existing six-run format. | Working choice. Shorter commitments and faster rematches suit the main play loop; longer events offer recovery after mistakes. | Compare voluntary rematches, perceived fairness and abandonment for both formats; 3, 36–39, 50. |
| D03 | Build all three requested multiplayer experiences for the full release; test networking and independent slow motion before scaling art/content. | Required modes, improved work order. A prototype can ship to testers with fewer modes without redefining the final goal. | Two-device feasibility test early; all-mode release suite later; 35–41, 52. |
| D04 | Horse ownership, upgrade strength and event eligibility are separate. Earned unlocks cannot be purchased directly. | Required earned access; working implementation. Premium horses may be acquired early, but store/UI explain current event limits. | A premium purchase never unlocks a trophy-gated event by itself; 8–9, 38, 42–47. |
| D05 | Horse/gear advantages remain meaningful within an event’s effective power limits and matchmaking bands. | Working choice based on comparable progression systems. No paid change to drawing deadlines, recognition thresholds or knock penalties. | A stronger skill trace must overcome a legal premium advantage in defined balance scenarios; 9, 33, 38, 43. |
| D06 | Paired riders receive the same challenge manifest and conditions. Waiting players see racing and completed grades, with drawing targets/traces private until both finish. | Working choice. Limits the spectator’s advance-knowledge advantage. | Check network payloads as well as UI; test first/second-rider outcomes; 32, 37, 39–40. |
| D07 | Official clock begins at the launch cue; fixed slow-motion windows affect simulated riding time identically for both riders. | Experiment and explicit arcade rule. Launch hesitation remains part of performance; this is not a claim of regulation timing. | Identical accepted inputs yield the same validated result across supported render rates; 29, 35–36, 40. |
| D08 | One authoritative result event per run and one settlement per match. Exact ties are draws. | Working choice. Compare integer time totals before display rounding; refund entry stakes on a draw, with no trophy transfer. | Duplicate, tie, DNF, forfeit and retry tests; 3, 36, 40–42. |
| D09 | Provide always-available free practice and a free-entry novice competition route with legitimate earned rewards. | Working choice. Running out of coins should lead to play and improvement. Reward limits prevent simple farming. | A zero-balance account can earn its way back into eligible events without purchase; 42, 45, 50–51. |
| D10 | Ten distinct horses across the existing five event tiers, with multiple viable profiles per tier. | Required ten-horse minimum; proposed allocation. Keep a horse useful in its permitted class as later horses unlock. | Validate the full free progression path and class-balance matrix; 8–9, 13, 21, 45. |
| D11 | Career/aging uses experience, maturity and milestones. Injury recovery is reversible and does not remove all ways to play. | Working direction; detailed effects require a later design test. These remain full-product work, with no irreversible loss presumed. | Prototype care/recovery before activating it in competition; 14–15, 42, 50. |
| D12 | Start paid offers with clearly specified horse/gear/cosmetic packs and a pass. Earned crates can supply progression; any paid randomness gets a separate policy/balance review. | Working choice. Store scope remains; purchased odds/content and entitlements must be transparent. | Store sandbox, fulfillment, refund and odds checks where applicable; 44, 46–47. |
| D13 | Replay/leaderboard comparison requires matching course, challenge manifest, ruleset and event class. | Working technical contract. Randomly different shapes must not share an unqualified fastest-time board. | Reject incompatible challenges and replay versions; 32, 36, 40, 52. |
| D14 | Rank and wallet authority live on trusted services; simulation/presentation remain separable. | Working architecture. Photon transport alone cannot certify results or balances. | Attempt forged results, impossible traces and duplicate payouts; 1, 5, 37, 40–42. |
| D15 | Full completion means verified systems plus the roster, arena content, all modes, stable services and store-ready device builds. | Required outcome, clarified gate. A working capsule or one-horse arena meets only an early milestone. | Trace every release requirement to sections, artifacts and observed checks. |

**Engineering additions — revision 4.** Your instruction is that each section guide precise coding and the appropriate specialist tools while preserving the complete-game outcome. The engineering playbook now supplies all 53 implementation contracts and a shared stack register.

| ID | Engineering decision | Status / adoption proof |
|---|---|---|
| D16 | Every section names its stack, expertise/resources, inputs, concrete deliverables, integration and evidence. | Required development standard from your latest instruction. All 53 cards exist; implementation status remains evidence-based. |
| D17 | Shared engine-independent C# rules; Unity application/presentation/integration boundaries; trusted result and economy services. | Working architecture. Verify common API/language compatibility, cross-runtime fixtures and mobile builds before relying on shared code. |
| D18 | ASP.NET Core on .NET 10 LTS with PostgreSQL is the working backend target; Fusion 2 handles live session/presentation transport. | Recommendation, not provisioned infrastructure. M1N proves timing, verification, concurrency, recovery and cost; document any provider/topology replacement. |
| D19 | Pin compatible tools/packages and keep versioned rules, content, schema and API contracts. | Required engineering gate. New dependencies need an actual use, compatible mobile builds and maintainable ownership; premium quality is verified in the game. |

**Critical experiments, in order.**

| Experiment | Why it can change the design | Evidence to collect |
|---|---|---|
| Gesture readability and bodycam comfort | The signature interaction might feel like an interruption if the preview, drawing surface and camera compete for attention. | Diverse real-phone traces; observed misses; comfort feedback; stable-view/reduced-motion comparison. |
| Slow-motion clock and online input | A beautiful local demo can conceal unfair timing across devices/networks. | Paired deterministic scenarios, delayed inputs, disconnections and independent rider clocks. Define bounded latency handling before ranked use. |
| Quick Duel versus Championship | Shorter is a hypothesis, not proof of stronger retention. | Run complete sessions of both; compare voluntary replay/rematch, wait frustration and results understanding. Later compare return visits without mistaking long forced sessions for enjoyment. |
| Premium power versus skill | Large stat gaps can make practice feel pointless; overly strict equalization can make upgrades feel meaningless. | Same-input comparisons and better-skill/lower-power comparisons across all event classes. |
| Free progression and coin recovery | Entry fees, training costs and consumable use can accidentally trap a player after losses. | Simulated loss streaks, normal play progression and a complete zero-spend route to upper tiers. |

**Provisional tuning rules.** No exact boost percentages, prices, drop rates, mastery thresholds or calendar deadlines are approved by this document. Set initial values in versioned data, expose the player-facing effects clearly, then tune against the experiments. Do not change live competitive rules midway through a match or silently mix incompatible versions on a leaderboard.

**Commercial and technical boundaries.** The initial research did not establish reference-game retention figures, profitable price points, hidden matchmaking formulas or per-user operating costs. We need our own playtests and cost model. Server verification can check rules and plausibility; it cannot guarantee that a trace was drawn by a human or eliminate collusion. Treat these as operating risks with detection/review controls, not problems solved by an “anti-cheat” class name.

**Next decisions that need evidence:** gesture acceptance thresholds; slowdown/window durations; exact horse/class power bands; injury effects; target device budgets; network topology and operating cost; store prices and bundle contents. These do not block the source repairs, but each has an explicit gate before its dependent feature is treated as finished.
