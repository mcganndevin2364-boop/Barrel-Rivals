**Barrel Rivals — M1 first-barrel practice**
Initial milestone: September 16, 2026 · Updated September 18, 2026 · Same recovered project · Local branch `codex/m1-skill-loop`

M1 adds a playable offline skill loop to the repaired project: hold/release launch, automatic approach, remember/draw, a graded barrel turn, a separate exit tap, results and retry. The original project folders remain preserved. The folder name `Barrel-Rivals-M0` identifies the existing working copy; it does not limit that copy to milestone M0.

**Current update — 0.3.0 / build 3.** The user confirmed that the earlier 0.2.0/build 2 app works on their iPhone 17 Pro and that the touch feel is good. This update preserves the same launch thresholds, drawing grader, four-second drawing window, automatic path and separate exit tap; the practice rules remain version 1. It adds reasons to repeat a practice challenge and improves feedback/presentation.

| 0.3.0 addition | Connected behavior |
|---|---|
| Challenge controls | Retry repeats the exact seed; New Challenge advances it; the last selected seed persists across restart |
| Local personal bests | Up to 64 records keyed by exact seed and rules version; comparisons include the five-second penalty; ties retain the prior best |
| Own-best ghost | A bounded input replay must reproduce its saved result before playback; the mint horse is the player's own best for this challenge, hides during overlap and has a persisted visibility toggle |
| Coaching/results | One specific next action from observed launch, drawing or exit performance, plus best-time comparison and visible save-failure feedback |
| Sound/haptics | Persisted toggles, bounded procedural hoof/tack/dirt/grade audio, unchanged scheduled launch beeps with mute, and optional short native iOS/Android impacts |
| Prototype presentation | Original generated dirt textures, arena structures/scenery/lighting and an articulated procedural horse; no final rigged horse/rider purchased |

**0.3.0 checkpoint verification.** These results apply to the current update, separately from the completed 0.2.0 checks below.

| Current check | Observed status |
|---|---|
| Independent core build | **Passed: 0 warnings, 0 errors**; shared C# remains independent of Unity |
| Saved scene | **Passed:** generated, saved, reopened and validated with current references |
| Editor tests | **55/55 passed** |
| Play Mode and updated renders | **5/5 passed; actual Unity renders reviewed** |
| 0.3.0 native build/signing/artifact inspection | **Passed: compiled, signed, profile/ARM64/IPA integrity checked** |
| 0.3.0 phone installation/playtest | **Installation/version query passed; manual playtest pending** |

The earlier user-confirmed iPhone launch is evidence for **0.2.0**, not for this new build. See [polish decisions](../Decisions/M1-Practice-Polish.md) and [horse/rider options](../Art/Horse-and-Rider-Options.md). The user chose free assets only; the current artwork was generated within the project.

**Try it in Unity.** Open the repaired project with Unity 6000.6.0f1, open `Assets/_Project/Generated/Practice/Arena_Practice.unity`, then press Play. Hold **HOLD TO BEGIN**, release on the third beep/GO, remember the gold shape, draw it in the pad, lift, then make a fresh tap when the exit bar reaches its center. Results show raw time, penalty, three skill grades, your best for the same challenge and one coaching tip. **Retry** repeats the seed; **New Challenge** changes it. Sound, haptics and the own-best ghost can each be toggled. Mouse and touch share the same input flow. This is a one-barrel practice segment, not a full-course finish.

**Implemented connections, including 0.3.0 source.**

| Layer | Implementation | Sections advanced |
|---|---|---|
| Shared C# rules | Explicit practice phases; seeded cues/three shape templates; normalized/resampled trace grading; independent response/simulation time; immutable local result | 3, 29–33, 35–36 |
| Unity input | One pointer owner; phase-specific hold, drawing and tap; cancellation/retry; normalized drawing coordinates | 2, 31, 33 |
| Riding/presentation | Automatic graded path; introductory camera blend; bodycam; scheduled tone cues; one visible knock event; fixed drawing window | 11, 19, 23, 30–31, 35, 48 |
| Player feedback | Phase/countdown, skill confirmation, target/trace comparison, best-time comparison, one coaching tip, separate Retry/New Challenge and settings controls | 49–50 |
| Local retention/replay | Persisted challenge/settings and up to 64 exact-seed/rules personal bests; bounded result-checked input replay for an own-best ghost; corrupt-data and failed-save handling | 5, 52 |
| Practice presentation polish | Articulated procedural horse, original dirt/arena scenery, fixed audio voice pool and optional native short haptics | 10, 12, 17–19, 48–49 |
| Tooling | Saved/reopened practice scene, M1 batch commands, rule/input/rendering tests, Android artifact inspection with separate milestone evidence | 1, 6 |

**Prototype tuning.** The first cue is 500 ms after hold, the second follows 1,000 ms later, and the third follows after a seeded 800–1,800 ms delay. Launch time starts at the third cue; early release waits for it, late delay counts, and a missed release auto-launches after 750 ms. Preview lasts 1,200 ms. Drawing always lasts 4,000 real milliseconds at one-quarter simulated speed, even if the trace is submitted early. Quality controls the turn line/duration; severe failure causes one five-second barrel penalty. The exit window is 1,000 ms with its target at the midpoint. These values are starting tuning, not proven difficulty or retention targets.

**Historical 0.2.0 verification.** Core compilation and the initial rule/input checks passed. Visual review then found a missing CanvasRenderer on the custom drawing graphic. The corrected scene was regenerated, reopened and rechecked; Play Mode now asserts visible target/trace pixels as well as input/results. The completed 0.2.0 results follow; they do not describe the 0.3.0 build currently being tested.

| Check | Observed result |
|---|---|
| Saved scene | Practice scene generated, reopened and validated with persistent bindings and drawing renderer |
| Edit Mode | **29/29 passed**, including 19 new practice cases and 10 foundation cases |
| Play Mode | **4/4 passed**, including real UI touch dispatch, visible target/trace pixels, result/retry and pointer cancellation |
| Independent core build | .NET Standard 2.1/C# 9, SDK 8.0.318; **0 warnings, 0 errors**; no Unity references |
| Android player build | **Succeeded, 0 reported errors**; ARM64 IL2CPP development APK |
| APK inspection | Package/version/signature, ZIP 16 KiB alignment and all seven native libraries' load-segment alignment passed |
| Original source preservation | 147 inventory files checked; **0 changed** |
| Physical phones / iOS | **0.2.0/build 2:** iOS native build, development signing and iPhone installation verified; launch/basic operation confirmed and touch feel approved by user. Detailed gameplay, performance and audio latency remain unmeasured |

The 0.2.0 final Editor reports ran September 16 locally (September 17 UTC). Android package `com.barrelrivals.foundation`, version **0.2.0 / code 2**, minimum SDK 26, target SDK 36. Artifact: `Builds/Android/BarrelRivals-Practice.apk`, **40,445,028 bytes**. SHA-256: `39ae25f2c789f17f4e6bf2b2fe55613727da43ab471dd1137dc55574ef5ddbe9`. This debug-signed development APK replaces the older foundation app if installed with the same package ID; it is not a release upload.

The initial 0.2.0 evidence was recorded inside the project under `Evidence/`: M1-Core-Build.json, M1-EditMode.xml, M1-PlayMode.xml, M1-Ready.png, M1-Preview.png, M1-Result.png and M1-Android-Artifact.json. Images are actual Unity renders captured by the Play Mode test. Reproduction commands reuse the M1 report/image filenames, so check their recorded run/version before associating them with an artifact; the historical results and APK identity above remain specific to 0.2.0. Automated touch events exercise the real UI route; they are not physical-phone measurements. Core replay checks use identical accepted timestamps across different render steps; they do not prove hardware/input latency equality.

**Limits and next work.** The 0.3.0 procedural horse is articulated and the arena has new original presentation assets. Production horse/rider rigs and synchronized riding animation still need selection/integration. No second/third-barrel flow, sprint, full race, matchmaking, accounts, purchases, rewards or production authority is implemented by M1. The grader needs labelled traces from real phones, and camera/audio/controls need comfort and latency testing. A synthetic cross-template check scored matching outlines Perfect, triangle substitutions Bad, and circle/square substitutions Good at a 1-second drawing duration; corner sensitivity and thresholds remain calibration work. Pause/focus loss cancels offline practice; it is not the future reconnect policy. The seed and result are local and must never qualify ranked rewards. Mobile performance and accessibility across devices remain unverified. The 0.2.0 iOS build/sign/install route is established; the 0.3.0 native artifact and installation are verified, while manual gameplay and performance remain unverified. Local record and ghost validation is for practice consistency and grants no competitive trust or rewards.

1. The 0.3.0 Play Mode/render/native/signature/install checks passed. Playtest that installed update on the iPhone. Check same-seed retry, new challenge, restart persistence, own-best ghost, one-tip coaching, sound/haptic toggles and appearance while preserving the approved controls. Measure cue latency, safe areas, comfort and sustained frame time; Xcode debugger compatibility remains unresolved. Qualify Android separately on a named Android device.
2. Run M1N: two clients with equal manifests, private reveals, trusted timing/result validation, independent slow-motion windows and explicit disconnect/retry outcomes. Record measured topology/cost findings before adopting services.
3. Build M2: one complete three-barrel race with sprint, a finished animated horse/rider, representative arena and measured mobile presentation.

No full original section is release-verified solely by this increment. The eight-category/53-section plan and the complete game remain the destination. Changes are local; no GitHub push or store upload has occurred.

**Historical iPhone preparation — 0.2.0/build 2.** The iOS app compiled and was development-signed successfully on Xcode 15.2. The signed IPA passed signature/profile/archive checks and was installed on the connected iPhone 17 Pro using standard USB installation after Xcode’s device-image path failed. The phone confirmed version 0.2.0/build 2. The user resolved development-certificate trust and confirmed that the app opens and works. The user also approved the touch feel. Detailed practice-flow, latency and performance testing remain pending, and 0.3.0 now has separate successful build/install verification; its manual phone playtest remains pending. See [the iPhone setup guide](../../README-iPhone.md).

**Subsequent design request.** The user requested the ten Reins mechanics after this checkpoint. The [Reins integration plan](Reins-Mechanics-Integration.md) governs that separate ruleset and future stack work. This report describes classic one-barrel practice; it does not qualify the new lab.
