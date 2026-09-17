**Barrel Rivals — M1 first-barrel practice**
September 16, 2026 · Same recovered project · Local branch `codex/m1-skill-loop`

M1 adds a playable offline skill loop to the repaired project: hold/release launch, automatic approach, remember/draw, a graded barrel turn, a separate exit tap, results and retry. The original project folders remain preserved. The folder name `Barrel-Rivals-M0` identifies the existing working copy; it does not limit that copy to milestone M0.

**Try it in Unity.** Open the repaired project with Unity 6000.6.0f1, open `Assets/_Project/Generated/Practice/Arena_Practice.unity`, then press Play. Hold **HOLD TO BEGIN**, release on the third beep/GO, remember the gold shape, draw it in the pad, lift, then make a fresh tap when the exit bar reaches its center. Results show raw time, penalty and three skill grades. **Retry** resets the run and changes the challenge seed. Mouse and touch share the same input flow. This is a one-barrel practice segment, not a full-course finish.

**Implemented connections.**

| Layer | Implementation | Sections advanced |
|---|---|---|
| Shared C# rules | Explicit practice phases; seeded cues/three shape templates; normalized/resampled trace grading; independent response/simulation time; immutable local result | 3, 29–33, 35–36 |
| Unity input | One pointer owner; phase-specific hold, drawing and tap; cancellation/retry; normalized drawing coordinates | 2, 31, 33 |
| Riding/presentation | Automatic graded path; introductory camera blend; bodycam; scheduled tone cues; one visible knock event; fixed drawing window | 11, 19, 23, 30–31, 35, 48 |
| Player feedback | Phase instructions, countdown, grade feedback, target/trace comparison, raw time plus penalty and retry | 50 |
| Tooling | Saved/reopened practice scene, M1 batch commands, rule/input/rendering tests, Android artifact inspection with separate milestone evidence | 1, 6 |

**Prototype tuning.** The first cue is 500 ms after hold, the second follows 1,000 ms later, and the third follows after a seeded 800–1,800 ms delay. Launch time starts at the third cue; early release waits for it, late delay counts, and a missed release auto-launches after 750 ms. Preview lasts 1,200 ms. Drawing always lasts 4,000 real milliseconds at one-quarter simulated speed, even if the trace is submitted early. Quality controls the turn line/duration; severe failure causes one five-second barrel penalty. The exit window is 1,000 ms with its target at the midpoint. These values are starting tuning, not proven difficulty or retention targets.

**Verification.** Core compilation and the initial rule/input checks passed. Visual review then found a missing CanvasRenderer on the custom drawing graphic. The corrected scene was regenerated, reopened and rechecked; Play Mode now asserts visible target/trace pixels as well as input/results. The final results follow.

| Check | Observed result |
|---|---|
| Saved scene | Practice scene generated, reopened and validated with persistent bindings and drawing renderer |
| Edit Mode | **29/29 passed**, including 19 new practice cases and 10 foundation cases |
| Play Mode | **4/4 passed**, including real UI touch dispatch, visible target/trace pixels, result/retry and pointer cancellation |
| Independent core build | .NET Standard 2.1/C# 9, SDK 8.0.318; **0 warnings, 0 errors**; no Unity references |
| Android player build | **Succeeded, 0 reported errors**; ARM64 IL2CPP development APK |
| APK inspection | Package/version/signature, ZIP 16 KiB alignment and all seven native libraries' load-segment alignment passed |
| Original source preservation | 147 inventory files checked; **0 changed** |
| Physical phones / iOS | Not run; no device-performance, actual audio-latency or store-readiness claim |

The final Editor reports ran September 16 locally (September 17 UTC). Android package `com.barrelrivals.foundation`, version **0.2.0 / code 2**, minimum SDK 26, target SDK 36. Artifact: `Builds/Android/BarrelRivals-Practice.apk`, **40,445,028 bytes**. SHA-256: `39ae25f2c789f17f4e6bf2b2fe55613727da43ab471dd1137dc55574ef5ddbe9`. This debug-signed development APK replaces the older foundation app if installed with the same package ID; it is not a release upload.

Evidence is stored inside the project under `Evidence/`: M1-Core-Build.json, M1-EditMode.xml, M1-PlayMode.xml, M1-Ready.png, M1-Preview.png, M1-Result.png and M1-Android-Artifact.json. Images are actual Unity renders captured by the Play Mode test. Automated touch events exercise the real UI route; they are not physical-phone measurements. Core replay checks use identical accepted timestamps across different render steps; they do not prove hardware/input latency equality.

**Limits and next work.** Geometry is still a placeholder horse/arena. No rigged riding animation, second/third-barrel flow, sprint, full race, matchmaking, accounts, purchases, rewards or production authority is implemented by M1. The grader needs labelled traces from real phones, and camera/audio/controls need comfort and latency testing. A synthetic cross-template check scored matching outlines Perfect, triangle substitutions Bad, and circle/square substitutions Good at a 1-second drawing duration; corner sensitivity and thresholds remain calibration work. Pause/focus loss cancels offline practice; it is not the future reconnect policy. The seed and result are local and must never qualify ranked rewards. Mobile performance, accessibility across devices and iOS build/signing remain unverified.

1. Install the practice APK on a named Android device; measure launch cue latency, touch ownership, shape grading, safe areas and sustained frame time. Establish a compatible Mac/Xcode/iPhone build route.
2. Run M1N: two clients with equal manifests, private reveals, trusted timing/result validation, independent slow-motion windows and explicit disconnect/retry outcomes. Record measured topology/cost findings before adopting services.
3. Build M2: one complete three-barrel race with sprint, a finished animated horse/rider, representative arena and measured mobile presentation.

No full original section is release-verified solely by this increment. The eight-category/53-section plan and the complete game remain the destination. Changes are local; no GitHub push or store upload has occurred.

**iPhone preparation follow-up.** The Unity iOS source export now succeeds with zero reported errors and passes the export inspector. Xcode native compilation, signing and iPhone installation remain unfinished. The available 2017/Ventura Mac needs an experimental Xcode 15.2 personal-device attempt; Apple sign-in is pending for that download. See [the iPhone setup guide](../../README-iPhone.md).
