# Reins 0.5 Android checkpoint

Source baseline: `523f6f44570d9998b4e55044edd124af7bb1bee8`, Unity 6000.6.0f1, version 0.5.0/code 5, rules reins-v2. The same runtime/art source produced the signed 0.5 iPhone package. No gameplay, model, animation, shader or input source changed during this Android build.

## Result

Unity produced the current **0.5.0/code 5 Android APK with zero build errors**. The verified user-facing file is `BarrelRivals-Android-Reins-0.5.0.apk`, **88,456,278 bytes**. SHA-256: `d4caa3836355202b2178e71034b54af9531aadd7caadead96d5946c01ce0912c`.

This is a debug-signed ARM64 development APK, not a store submission. No Android handset was named or connected for this checkpoint. Installation, physical launch, rendering correctness on a phone, controls/audio/haptics, saved equipment and sustained performance remain unverified. The iPhone package is separately [compiled/signed/verified](Reins-0.5-iPhone-Checkpoint.md), also awaiting installation and phone review.

## What the package contains

The committed builder exports the Reins race and MyStable scenes only. It includes the four-second first-person alley and timed release, rein/cadence/Wrap/Drive loop, shared course contacts, local v2 recordings, current horse/rider/hands and the stable/tack/glove preview/equip/save flow. The serialized global game-manager data contains Arena_ReinsLab and MyStable scene names and no Arena_Practice scene name, corroborating the build scene list. Classic stays in source/history and is excluded from the player scene list. One horse and 20 local cosmetics remain the implemented stable scope; no multiplayer authority, earned ownership or progression is added by this build.

The build compiled both GLES3 and Vulkan shader variants. Successful shader compilation does not establish identical physical-device rendering or performance. The active horse/tack/rider still exceeds the character material/triangle targets, and the existing visuals remain below the photographic references.

## Verification boundaries

- Unity reported a successful Android build with zero build errors. The source Editor/Play Mode checks remain the existing **140/140 and 37/37** glove/rider checkpoint; they were not unnecessarily rerun for unchanged gameplay and assets.
- The package inspector checks the exact application ID, version 0.5.0/code 5, minimum API 26, target API 36 and ARM64-only native code.
- `apksigner verify --verbose`, `zipalign -c -P 16 4`, archive CRCs, ELF64/AArch64 identity, native load-segment alignment and offset/address congruence all pass. These are artifact checks, not a physical 16 KB-page handset test.
- The referenced `com.barrelrivals.practice.PracticeHaptics` Java class is present in compiled DEX. This does not prove vibration feel or device/view-setting behavior without running the app.
- The new APK signer matches the preceding development APK; its higher version code preserves the normal signed-update path. This is an artifact identity check, not a performed handset update.
- The copied user-facing APK matches the build output SHA-256. The preceding 0.4 APK was preserved and checked against its historical evidence before the new build.
- All **558** prior tracked PNG/JSON/XML/MP4 evidence files remain byte-identical, including the signed iPhone evidence. Core, input, replay rules and previous test records are unchanged.

[Artifact checks](../../Evidence/Reins05-Android-Artifact.json) contain exact sizes, hash, SDK/signature results and native-library alignment observations. Raw licensing and build logs remain ignored and are excluded from the handoff.

## Rebuild

With other Editors closed, run from the repository root:

```bash
bash Tools/run-reins.sh android
python3 Tools/verify-android.py --apk Builds/Android/BarrelRivals-ReinsLab.apk --output Evidence/Reins05-Android-Artifact.json --version 0.5.0 --code 5
```

The current `Tools/run-reins.sh` entry point is now executable in Git; its script bytes are unchanged and Bash syntax/executable checks passed. Both direct invocation and the explicit Bash command above work.

Use fresh evidence names and preserve the previous artifact when producing a later checkpoint. `BARREL_ANDROID_TOOLCHAIN` overrides the installed AndroidPlayer SDK/NDK/OpenJDK root for inspection. This build used the installed SDK build-tools 36.0.0 and the existing development package ID. A production upload bundle, release signing and store review require separate work.

## Next acceptance

Install on a named, compatible Android handset before asserting Android play or performance. Review the same moving-alley timing, two-thumb input, interruption/retry, gear persistence, sound/comfort and full-course behavior in [the phone review](Reins-0.5-Phone-Review.md), and measure a 20-minute session under recorded quality/thermal conditions. Keep the iPhone and Android evidence distinct.

Refine the representative horse/rider/arena benchmark. The next large visual work should address the horse's base anatomy, coat/eye material response and motion together; earlier isolated alternative-mesh diagnostics are not an accepted replacement rig. Preserve the established controls and shared Core authority throughout that work. Full R2, all three multiplayer modes, progression/economy, cloud saves and all eight categories/53 sections remain in scope and unfinished.

## Settings serialization

The Android Unity run serialized the inactive iOS graphics Automatic flag from 0 to 1; byte comparison showed no other ProjectSettings change. After the Android build succeeded, the exact tracked settings were restored. This did not alter any Android setting or built APK. The committed iPhone export method still explicitly selects Metal. The artifact record retains both hashes and the restoration boundary.
