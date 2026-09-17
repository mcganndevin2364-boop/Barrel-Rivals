# Barrel Rivals development context

Read README-M1.md, Docs/Decisions/M1-Practice.md, Docs/Decisions/M0-Foundation.md and the relevant Docs/Plan engineering card before changing a system. Preserve the eight original categories and 53 section numbers. Docs/Archive contains superseded context, not current instructions or verified status. The master plan defines the full game; milestone work delivers connected increments with evidence.

- Unity reproduction baseline: 6000.6.0f1. Pin package versions and verify installed APIs.
- Engine-independent rules live in Packages/com.barrelrivals.core. They must compile against .NET Standard 2.1 without Unity, Fusion or web/database dependencies.
- Existing Assets/_Project/Scripts/Runtime/Race classes are earlier scaffolding, not verified complete systems. Instantiate them only after wiring and checking their contracts.
- FoundationPreview is a development preview, not the competitive movement/timing implementation. Do not attach rewards or ranked results to its frame-based movement.
- FoundationBuilder owns Assets/_Project/Generated/Foundation. Keep hand-authored production content outside this directory. Preserve all Unity metadata and validate persisted references after reopening scenes.
- PracticeBuilder owns Assets/_Project/Generated/Practice. The M1 scene is offline one-barrel practice; local seeds, grades and results must not award currency/rank or be treated as trusted authority.
- Full gameplay is automatic bodycam riding with timed launch, remembered drawing challenges, exit timing and sprint. All three requested multiplayer experiences remain in scope.
- Use focused tests for consequential rules, state, persistence and regressions; verify appearance, input and performance on the relevant runtime/device. Document exactly what ran and what remains unverified.
- Batch entry points are in Tools/run-unity.sh (M0) and Tools/run-m1.sh (M1). Do not run concurrent Editors against the same project. Never publish raw editor licensing logs or signing credentials.
- Current prototype identifiers and signing are development-only. Do not treat them as finalized store configuration.
