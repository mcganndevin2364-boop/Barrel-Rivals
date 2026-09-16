**Barrel Rivals — M0 implementation and verification**  
September 16, 2026 · Foundation checkpoint · Unity 6000.6.0f1

**The result.** The repaired project compiles and opens a saved three-barrel arena with valid URP materials, a placeholder horse, a temporary first-person approach and reset controls. The Editor tests, separate shared-core build and Android development APK build pass. This is the foundation for the planned arcade game; the launch/drawing/turn loop, finished horse animation and multiplayer remain to be implemented.

Open the [project instructions](../../README-M0.md) to try it. The project folder is `Barrel-Rivals-M0`; add that folder in Unity Hub and open `Assets/_Project/Generated/Foundation/Arena_Foundation.unity`.

**Source preservation.** GitHub main was fetched at `b77f4e5fc95fd0eec70a77625dcbabf79f54ea19`. The repaired copy uses local branch `codex/m0-foundation`, based on that commit, with the original local Assets, Packages and ProjectSettings reconciled on top. An archive, tracked diff and 147-file SHA-256 inventory preserve the original local work. The original `/Users/devinmcgann/Barrel-Rivals` and separate `/Users/devinmcgann/BarrelRivals_Final` project folders remain unchanged. No GitHub push or store submission was performed.

**What changed and why.**

| Area / sections | Implemented result | Remaining work |
|---|---|---|
| Project/stack · 1, 6, 7 | Pinned packages, corrected assembly references, retained Unity metadata/settings, saved build scene, added batch check/build commands. | Physical devices, iOS build environment, complete application architecture and measured quality budgets. |
| Source repair · 22, 36 | Replaced nonexistent `RunScoreBreakdown` with existing `RunResult`; added missing seeded RNG and input guards. | Weather authoring/integration and complete race timing/scoring. Existing scaffolding is not certified by a compilation pass. |
| Shared rules · 1, 16, 22 | Engine-independent .NET Standard 2.1 core with a versioned RNG and standard-pattern barrel-center geometry. | Launch, drawing, paths, penalties, manifest/replay contracts and trusted verification. |
| Scene/rendering · 16, 17, 19, 24 | Persistent URP renderer/pipeline/materials, measured barrel layout, saved barrel/horse proxy prefabs, scene floor/fences/score line/gate markers. | Production art, horse/rider rigs, legal route/collision rules, mobile appearance and effects. |
| Preview/input · 2, 23, 50 | Input System UI, safe-area layout, preview/reset buttons, automatic approach and temporary rider camera; simulated touch dispatch verified. | Actual phone touch, race gestures, introduction/comfort behavior and full game UI. |
| Documentation · 1 | Root README, build status, onboarding and project specifications now link to the current plan; earlier versions are preserved as historical material. | Keep evidence and section status current with each increment. |
| Reproduction · 1, 53 | Targeted tests, persisted-reference validation, separate core compile target, check commands and evidence. | CI, service diagnostics, gameplay analytics and release automation. |

**Verification actually performed.**

| Check | Observed result | Practical limit |
|---|---|---|
| Unity import from a project without a Library cache | Passed; Editor exited successfully with no source compilation errors. | Uses this Mac's installed Unity and available package cache. |
| Generate, save, reopen and validate scene | Passed; bindings/material assets/build registration checked. | Foundation scene only. |
| Edit Mode | **10 passed, 0 failed**; RNG fixtures/edge cases, course distances, saved references and quality pipeline assignments. | Focused foundation checks, not whole-game correctness. |
| Play Mode | **2 passed, 0 failed**; saved scene loads, preview moves/stops/resets, and simulated touch hits the rendered controls and reaches their UI handlers. | The batch test temporarily routes input to the game view and ignores Editor focus, restoring both settings afterward. Actual phone input/lifecycle remain untested. |
| Shared core outside Unity | Passed using .NET SDK 8.0.318; **0 warnings, 0 errors**, .NET Standard 2.1 target. | No backend service or cross-client synchronization exists yet. |
| Unity camera render | Generated and visually inspected the arena image; no pink materials in this scene. | Desktop Editor capture of prototype geometry; overlay HUD is excluded. No mobile visual/performance claim. |
| Android artifact | **Passed**, Unity reported 0 build errors. ARM64/IL2CPP APK produced; package/API metadata, debug signature, ZIP alignment and all seven native libraries’ 16 KiB load-segment alignment checked. | No connected Android device was found. Installation, physical input, lifecycle, thermals and performance remain untested. |
| iOS | Not built. iOS module is installed, but full compatible Xcode is unavailable on this Mac. | Requires a compatible Mac/Xcode environment, signing setup and iPhone testing. |
| Original project preservation | The 147 inventoried original source/settings/metadata files remained unchanged. | Generated caches/logs are outside the preservation inventory. |

[Edit Mode report](../../Evidence/EditMode.xml) · [Play Mode report](../../Evidence/PlayMode.xml) · [Actual Unity arena render](../../Evidence/Arena-Overview.png)

**Android development artifact.** [BarrelRivals-Foundation.apk](../../Builds/Android/BarrelRivals-Foundation.apk) is 40,396,128 bytes (38.5 MiB). Its SHA-256 is `4200091ecd6b5608e52ad12b029258e4691116e399b36e00096b0886c4df39fd`. [Artifact verification](../../Evidence/Android-Artifact.json) records the actual metadata and checks. It uses development/debug signing and has not been installed or run on a physical phone.

**Compatibility register for this checkpoint.**

| Component | Recorded version/configuration | Scope of evidence |
|---|---|---|
| Unity Editor | 6000.6.0f1, Intel macOS | Import, Editor tests and render passed. |
| Universal Render Pipeline | 17.6.0 | Saved foundation pipeline and all quality-tier references validated. |
| Input System | 1.20.0; new-input mode | Scene UI hit-testing and simulated touch dispatch passed; physical touch pending. |
| uGUI | 2.6.0 | Prototype text/buttons in saved scene. Final typography/UI remain later work. |
| Test Framework | 1.8.0 | Edit/Play Mode reports produced. |
| Shared core | .NET Standard 2.1, C# 9; SDK 8.0.318 compiler | Same source compiled without Unity references. |
| Android development config | ARM64, IL2CPP, minimum API 26, target API 36, landscape; `com.barrelrivals.foundation` | Artifact outcome above. Debug identity/signing only. |
| Host/toolchain limit | macOS 13.7.8; selected tools are Command Line Tools | Full iOS build/sign/device qualification remains open. |

`Packages/packages-lock.json` records transitive package versions. `Tools/run-unity.sh` provides the repeatable import/scene validation, test, render and Android build entry points. Raw Unity logs are retained outside source control because licensing output can include session data.

**Current milestone position.** M0's compilation, asset persistence, Editor runtime and Android artifact checks have passed. Its platform qualification gate remains open. No entire original section is marked verified complete: its requirements cover substantially more than this foundation. The [master status](Barrel-Rivals-Master-Build-Status.md) retains every original category and section.

**Next connected build: M1.** Implement phase-owned hold/release/tap/drawing input; a seeded third-beep launch; one remembered shape challenge with normalized trace scoring; a graded automatic barrel turn; fixed slow motion and exit timing; and a clear result/retry. Put rules and clock contracts in the shared core and presentation in Unity. Verify invalid/cancelled input, repeatable outcomes and scene integration before expanding to all three barrels. Then M1N proves two-client timing/authority before roster, environment and store expansion.

The prototype horse, camera and HUD establish a place to integrate that work. They are not the final visual standard. The complete ten-horse roster, three multiplayer experiences, progression/economy and publishable iOS/Android releases remain the destination.
