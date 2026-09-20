# Barrel Rivals

The current source also includes **MyStable and Gear**: an interactive 3D stable, six original free starter cosmetics, preview/equip, device-only saved choices and a return-to-race flow using the equipped appearance. The user will supply MyStable/Gear-specific photos later; their exact layout is not yet known. Read [the feature checkpoint](Docs/Features/MyStable-and-Gear.md). This does not implement paid ownership, competitive upgrades, cloud progression, the ten-horse roster or a new phone installation.

An iOS/Android arcade barrel-racing game in development. The user has selected Reins Racing: continuous rein steering, cadence, close barrel turns, changing footing and a final drive. Classic is preserved as historical code/evidence; its player-facing route will be removed in the approved 0.5 update. Live turns with spectating, recorded challenges and simultaneous duels remain in scope, alongside ten horses, progression and a trusted economy.

## Current build

**Classic Practice + Reins Lab development build 0.4.0:** Unity 6000.6.0f1, saved URP scenes, original prototype art, isolated rules/saves and a local shared-Core replay verifier. Reins adds a legal three-barrel course; it is an experimental offline game mode. Final horse/rider art, trusted online play, progression/economy and measured mobile qualification remain ahead. The 0.4.0 iPhone build is installed with its version verified; the Android APK passed artifact checks. The user has played and prefers Reins, while requesting substantially better graphics. This qualitative report does not establish measured performance or full device qualification. Version-labelled evidence distinguishes those stages. The new alley launch and visual rebuild are approved next work, not implemented in 0.4.

- [Start here: AI continuation handoff](Barrel-Rivals-AI-Handoff.md)
- [Copyable message for another AI](CONTINUE-WITH-ANOTHER-AI.md)
- [Approved Reins 0.5 launch and visual rebuild](Docs/Plan/Reins-Racing-0.5-Implementation.md)
- [Classic controls and historical checkpoint](README-M1.md)
- [Current graphics source and actual Unity captures](Docs/Art/Reins-Premium-Graphics-Checkpoint.md)
- [Play Reins Lab and inspect the cross-stack prototype](README-Reins.md)
- [All ten mechanics: stack contracts and delivery gates](Docs/Plan/Reins-Mechanics-Integration.md)
- [iPhone export and test setup](README-iPhone.md)
- [M1 implementation results and limits](Docs/Plan/Barrel-Rivals-M1-Report.md)
- [Master status: eight categories and 53 sections](Docs/Plan/Barrel-Rivals-Master-Build-Status.md)
- [Full build plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md)
- [Gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md)
- [Engineering stack and section contracts](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md)

The next work is R2/0.5: a first-person moving-alley hold/release start, forgiving controls and one properly rigged horse/rider/arena, followed by phone qualification and then M1N continuous-input authority proof. The existing Mac can compile/sign/install personal iPhone test builds; debugger compatibility and release toolchain qualification remain open. [README-M0.md](README-M0.md) preserves the foundation preview; `Arena_Practice` and `Arena_ReinsLab` are the playable development scenes. Original folders remain preserved. Continue the `codex/m1-skill-loop` branch; `main` is an older checkpoint. This is development work, with no store submission.

## Development context

Read [AGENTS.md](AGENTS.md) and the relevant engineering card before implementation. Preserve Unity metadata and the pinned package lockfile. Generated M0 assets have explicit ownership; future hand-authored production art belongs outside the generated directory.

Earlier documents are retained in [Docs/Archive/Before-M0](Docs/Archive/Before-M0). Their old completion grades, Unity 2022.3 baseline, course dimensions and alternate section numbering do not describe this branch’s current implementation.
