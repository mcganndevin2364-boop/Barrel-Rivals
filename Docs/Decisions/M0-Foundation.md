# M0 foundation decision record

This isolated implementation branch starts at GitHub `b77f4e5`, with the user's local Assets, Packages and ProjectSettings preserved on top. The original `/Users/devinmcgann/Barrel-Rivals` checkout is unchanged. A file archive, tracked diff and SHA-256 inventory were retained in the task's work directory before editing.

Unity 6000.6.0f1 is the reproduction baseline. URP 17.6.0, uGUI 2.6.0, Input System 1.20.0 and Test Framework 1.8.0 match packages found on this machine. `Packages/packages-lock.json` records resolved dependencies. Actual Unity import/build results, rather than package availability, determine support.

The runtime assembly now references the data assembly, engine-independent core and the UI assembly it actually uses. Unused Fusion and render-pipeline references were removed from that assembly. Fusion remains planned for M1N; no network integration is claimed. Editor rendering tools have their own assembly and references.

`RunScoreBreakdown` was a nonexistent type. Score reveal now accepts the existing `RunResult`. The missing seeded generator is an explicit versioned xorshift32 stream in `com.barrelrivals.core`, with bounds checks and zero-seed handling. It is not used for security or paid rewards. Weather owns a supplied generator; visual randomness must remain separate from competitive challenge generation.

The shared core compiles against .NET Standard 2.1, without Unity references. `Tools/CoreCheck/BarrelRivals.Core.csproj` compiles the same source separately. The future service's runtime is not installed or implemented by this milestone.

The old scene generator created transient materials and unwired components. Its menu entry now delegates to an explicit foundation builder. Generated content belongs only in `Assets/_Project/Generated/Foundation`; running the generator recreates that development scene and its owned assets. Place future hand-authored content outside this directory.

The standard-pattern geometry uses 90 feet between the first two barrel centers, 105 feet from each to the third, and 60 feet from the score line to their baseline. Layout data is independent of rendering. This does not yet implement legal route/turn scoring.

The scene is a course preview: primitive horse geometry, persistent URP materials, three barrel prefabs, fence, score line, camera, safe-area HUD and touch/mouse preview/reset buttons. Its automatic approach stops before barrel one. It has no launch skill, drawing, full race, penalties, rewards or multiplayer. Those remain later milestones. The preview controller is intentionally separate from competitive rules.

Landscape and `com.barrelrivals.foundation` are development settings, not a final store bundle-ID decision. Android uses ARM64/IL2CPP for the test artifact. No store submission, production signing, spending or GitHub push is part of this local recovery increment.

Validation results are recorded in the accompanying M0 report; raw logs stay in ignored Logs or the task work directory because editor licensing output can contain sensitive session data.
