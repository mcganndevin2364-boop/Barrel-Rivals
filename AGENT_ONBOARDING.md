# Barrel Rivals development onboarding

The user has selected **Reins Racing**, first-person throughout, with realistic arcade presentation and forgiving controls. Continue this repository. The last device artifact is **0.4.0/build 4**. A newer **reference-graphics source checkpoint** retains that identity and v1 gameplay: imported free horse/three first-pass clips, persistent materials, Linear/warm lighting treatment, dirt/crowd assets, all-phase first-person/reduced-motion camera and charcoal/gold HUD. Its scene generation/save/reopen and 77 Editor / 13 Play Mode tests passed. It has no new native build or phone-install evidence. The approved **0.5.0/build 5** moving-alley/v2 launch remains unimplemented; finished rider/tack, production animation and the complete visual milestone remain pending.

Read in this order:

1. [Portable AI handoff](Barrel-Rivals-AI-Handoff.md): checkpoint, user choices, environment, evidence and next task.
2. [AGENTS.md](AGENTS.md): current development boundaries.
3. [Master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md), [graphics source checkpoint](Docs/Art/Reins-Reference-Graphics-Checkpoint.md) and [approved 0.5 plan](Docs/Plan/Reins-Racing-0.5-Implementation.md).
4. [Gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md), [eight-category plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md) and relevant S01–S53 cards in [the engineering playbook](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md).
5. [Current 0.4 controls/build commands](README-Reins.md), [implementation evidence](Docs/Plan/Reins-Lab-Implementation.md), [wire contracts](Contracts/Reins/README.md) and [local verifier](Tools/ReinsServerCheck/README.md).

Gameplay revision 5 / engineering revision 6 supersede the earlier additive-Lab, three-wave gate and third-person-intro defaults. Historical M0/M1/0.4 reports describe earlier implementation and evidence, not the current product direction. Do not overwrite their measured results with new claims.

Next gameplay implementation task: R2.1 in the approved 0.5 plan—shared approach/release rules, versioned replay/verifier and Unity pointer/audio handoff. Build on the new art source when completing R2.2's horse/rider/alley benchmark; do not restart the asset pipeline or treat its first-pass clips as finished animation. The reference image is a throughout-gameplay target, not copied game art. Classic navigation remains in this v1 checkpoint; Reins-only startup is still part of v2. Do not scale the roster before quality and device gates pass.

Unity 6000.6.0f1 and the package lockfile define the reproduction baseline. Core remains engine-independent .NET Standard 2.1. Photon, production backend/database, ranked economy and cloud services are future integration work. The user prefers the played Reins prototype, but sustained device performance and formal full-course acceptance are unmeasured.
