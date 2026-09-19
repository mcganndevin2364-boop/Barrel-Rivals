**Barrel Rivals — design decisions and review findings**  
September 19, 2026 · Gameplay revision 5 · Engineering revision 6

The full outcome remains a polished iOS/Android game with first-person Reins racing, live alternating turns and spectating, recorded challenges, simultaneous racing, at least ten horses, earned tiers, progression and purchases. The original eight categories and 53 sections remain intact.

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

**Decision register.** “Required” and “approved” reflect the user’s expressed choice; “working choice” remains a recommendation; experiments need measured evidence. Revision 5/6 supersedes the earlier automatic/drawing identity, opt-in Lab, third-person introduction and three-tap launch. Implemented 0.4.0/build 4 still uses v1; the [approved 0.5 plan](Reins-Racing-0.5-Implementation.md) is next work, not completed code/art.

| ID | Decision | Status and rationale | Validation / affected sections |
|---|---|---|---|
| D01 | Reins is the main game, first-person throughout; Classic leaves the player-facing build and remains historical source/saves. | Approved by the user after playing Reins. 0.4 still contains both modes; 0.5 has not been implemented. | Reins cold-start entry; history/data preserved; 2, 10–12, 23, 29–36, 50. |
| D02 | Quick Duel is the everyday live mode: one full run per rider. Three-round Championship retains the existing six-run format. | Working choice. Shorter commitments and faster rematches suit the main play loop; longer events offer recovery after mistakes. | Compare voluntary rematches, perceived fairness and abandonment for both formats; 3, 36–39, 50. |
| D03 | Build all three multiplayer experiences for full release; prove continuous-input authority before scaling content. | Required modes. Local verifier work is not production networking. | Two-device feasibility early, full-mode release suite later; 35–41, 52. |
| D04 | Horse ownership, upgrade strength and event eligibility are separate. Earned unlocks cannot be purchased directly. | Required earned access; working implementation. Premium horses may be acquired early, but store/UI explain current event limits. | A premium purchase never unlocks a trophy-gated event by itself; 8–9, 38, 42–47. |
| D05 | Horse/gear advantages remain bounded by event power limits and matchmaking bands. | Working progression choice. No paid launch/cadence windows, artificial input-delay removal, auto-steering or knock forgiveness. | A cleaner lower-power run can overcome legal stat advantage; 9, 33, 38, 43. |
| D06 | Paired riders receive equal manifests, footing and reference information. | Working fairness rule. Future private data remains undisclosed; no second-rider-specific ghost advantage. | Inspect payloads and UI; swap rider order; 32, 37, 39–40. |
| D07 | Race clock starts at beep three/GO after a fixed four-second approach, independent of release timing. | Approved 0.5 contract. No launch standstill, time credit or inherited drawing slowdown. | Early/late/missing release and render-rate parity; 29–30, 35–36, 40. |
| D08 | One authoritative result event per run and one settlement per match. Exact ties are draws. | Working choice. Compare integer time totals before display rounding; refund entry stakes on a draw, with no trophy transfer. | Duplicate, tie, DNF, forfeit and retry tests; 3, 36, 40–42. |
| D09 | Provide always-available free practice and a free-entry novice competition route with legitimate earned rewards. | Working choice. Running out of coins should lead to play and improvement. Reward limits prevent simple farming. | A zero-balance account can earn its way back into eligible events without purchase; 42, 45, 50–51. |
| D10 | Ten distinct horses across the existing five event tiers, with multiple viable profiles per tier. | Required ten-horse minimum; proposed allocation. Keep a horse useful in its permitted class as later horses unlock. | Validate the full free progression path and class-balance matrix; 8–9, 13, 21, 45. |
| D11 | Career/aging uses experience, maturity and milestones. Injury recovery is reversible and does not remove all ways to play. | Working direction; detailed effects require a later design test. These remain full-product work, with no irreversible loss presumed. | Prototype care/recovery before activating it in competition; 14–15, 42, 50. |
| D12 | Start paid offers with clearly specified horse/gear/cosmetic packs and a pass. Earned crates can supply progression; any paid randomness gets a separate policy/balance review. | Working choice. Store scope remains; purchased odds/content and entitlements must be transparent. | Store sandbox, fulfillment, refund and odds checks where applicable; 44, 46–47. |
| D13 | Replay comparison requires matching rules/contracts/config/course/horse class/footing/input mode. | Required compatibility. Separate Classic, Reins v1 and future v2 namespaces; no silent rescore. | Reject old/mixed hashes and compare Unity/.NET fixtures; 32, 36, 40, 52. |
| D14 | Rank and wallet authority live on trusted services; simulation/presentation remain separable. | Working architecture. Photon transport alone cannot certify results or balances. | Attempt forged results, impossible traces and duplicate payouts; 1, 5, 37, 40–42. |
| D15 | Full completion means verified systems plus the roster, arena content, all modes, stable services and store-ready device builds. | Required outcome, clarified gate. A working capsule or one-horse arena meets only an early milestone. | Trace every release requirement to sections, artifacts and observed checks. |

**Engineering additions — revision 4.** Your instruction is that each section guide precise coding and the appropriate specialist tools while preserving the complete-game outcome. The engineering playbook now supplies all 53 implementation contracts and a shared stack register.

| ID | Engineering decision | Status / adoption proof |
|---|---|---|
| D16 | Every section names its stack, expertise/resources, inputs, concrete deliverables, integration and evidence. | Required development standard from your latest instruction. All 53 cards exist; implementation status remains evidence-based. |
| D17 | Shared engine-independent C# rules; Unity application/presentation/integration boundaries; trusted result and economy services. | Working architecture. Verify common API/language compatibility, cross-runtime fixtures and mobile builds before relying on shared code. |
| D18 | ASP.NET Core on .NET 10 LTS with PostgreSQL is the working backend target; Fusion 2 handles live session/presentation transport. | Recommendation, not provisioned infrastructure. M1N proves timing, verification, concurrency, recovery and cost; document any provider/topology replacement. |
| D19 | Pin compatible tools/packages and keep versioned rules, content, schema and API contracts. | Required engineering gate. New dependencies need an actual use, compatible mobile builds and maintainable ownership; premium quality is verified in the game. |

**Approved Reins direction — revision 5 / engineering 6.** These values are approved starting implementation defaults; validation can motivate a documented revision, not silent substitution. None of D20–D24 is implemented in the 0.4 checkpoint.

| ID | Decision | Status / adoption proof |
|---|---|---|
| D20 | Hold center starts a 6 m / 4 s walking alley; beeps at 2/3/4 s; third beep crosses invisible z=0 and enables race clock/steering. | Approved. Verify fixed approach, visible horse/rider, scheduled audio and steering handoff. |
| D21 | First release: Perfect ±120 ms, Good ±240 ms, otherwise normal launch; acceleration 1.35/1.15/default until common GO+1.2 s; missing-release deadline GO+400 ms. | Approved grade-and-continue. No stall/restart, time credit, early motion advantage, cadence/Wrap leakage or release spam. |
| D22 | Responsive, forgiving control: full rein pull at 50% pad travel; cadence ±60/100/140 ms, periods 360–500 ms, first beat GO+600 ms. | Approved starting tuning. Measure two-thumb reach, recovery, input latency and fatigue on phones. |
| D23 | Realistic arcade art: one skinned horse/rider with western tack/reins, in-place clips, stabilized first-person camera and one detailed arena. | Approved free/original-only milestone. Inspect assets/rights, view walk-to-gallop and full-race captures; further primitive-only polish is insufficient. |
| D24 | Rules/contracts v2, canonical launchHeld/release outcome, fresh fingerprint/fixtures, updated local verifier/schema and separate v2 saves. | Required compatibility boundary. Preserve Classic/v1 data and historic results; no cloud deployment or actual DB migration in R2. |
| D25 | Initial R2 budgets: 60k character triangles, 2K character textures, 150 visible batches, 650 MB peak memory; iPhone 60 FPS target with 30 FPS fallback. | Approved target, unmeasured. Qualify a 20-minute phone session without quality settings changing rules. |

**Critical experiments, in order.**

| Experiment | Why it can change the design | Evidence to collect |
|---|---|---|
| Gesture readability and bodycam comfort | The moving release/heartbeat handoff, thumb reach and first-person camera must communicate without hiding the horse or next barrel. | Real-phone launch/heartbeat timing and steering samples; observed misses; handoff/cancel tests and reduced-motion comparison. |
| Fixed-step clock and online input | A beautiful local demo can conceal unfair timing across devices/networks. | Paired deterministic scenarios, delayed inputs, disconnections and independent rider clocks. Define bounded latency handling before ranked use. |
| Quick Duel versus Championship | Shorter is a hypothesis, not proof of stronger retention. | Run complete sessions of both; compare voluntary replay/rematch, wait frustration and results understanding. Later compare return visits without mistaking long forced sessions for enjoyment. |
| Premium power versus skill | Large stat gaps can make practice feel pointless; overly strict equalization can make upgrades feel meaningless. | Same-input comparisons and better-skill/lower-power comparisons across all event classes. |
| Free progression and coin recovery | Entry fees, training costs and consumable use can accidentally trap a player after losses. | Simulated loss streaks, normal play progression and a complete zero-spend route to upper tiers. |

**Tuning rules.** D20–D22 lock the approved 0.5 starting launch/control values, and D25 sets initial performance budgets. Prices, drop rates, progression thresholds and launch dates remain open. Keep values versioned, explain player-facing effects and revise only against recorded evidence. Do not change live competitive rules midway through a match or silently mix incompatible versions on a leaderboard.

**Commercial and technical boundaries.** The initial research did not establish reference-game retention figures, profitable price points, hidden matchmaking formulas or per-user operating costs. We need our own playtests and cost model. Server verification can check rules and plausibility; it cannot guarantee that a trace was drawn by a human or eliminate collusion. Treat these as operating risks with detection/review controls, not problems solved by an “anti-cheat” class name.

**Next decisions that need evidence:** measured 0.5 timing/comfort and whether approved tuning needs revision; authored horse/collision dimensions; exact horse/class power bands; injury effects; device qualification against the initial budgets; network topology/cost; store prices and bundle contents. These do not block the source repairs, but each has an explicit gate before its dependent feature is treated as finished.
