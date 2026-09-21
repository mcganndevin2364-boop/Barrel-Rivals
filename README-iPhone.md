# Barrel Rivals — iPhone development build

Fresh 0.5 iOS export, native compilation, signing and IPA integrity are verified; installation/play/performance remain pending. The last verified installed phone app remains 0.4.0/build 4.

The updated game contains Reins-only startup, a four-second first-person alley with one timed release, revised horse/rider/hands, and MyStable/Tack/Rider Gear. One horse and 20 local cosmetics are implemented. Multiplayer, trusted rewards/progression and store release remain unfinished. The artwork is still below the photographic references.

Read [the current mobile checkpoint](Docs/Build/Reins-0.5-iPhone-Checkpoint.md) for exact build evidence and [the phone review](Docs/Build/Reins-0.5-Phone-Review.md) for controls, equipment persistence and device acceptance.

## Rebuild

With other Editors closed, from the repository root:

```bash
bash Tools/run-reins.sh ios
python3 Tools/verify-ios-export.py --version 0.5.0 --build 5 --output Evidence/Reins05-iOS-Export.json
```

Use fresh evidence filenames for later checkpoints. Unity writes `Builds/iOS/BarrelRivals-Practice/Unity-iPhone.xcodeproj`. On this host, relocate the completed export outside cloud-coordinated Documents before native compilation, preserving earlier working exports. Unity can replace a directory symlink during export; check the actual destination afterward. Verify the project hash and generated sources before compiling. The current fresh export needed no duplicate-source repair.

`Tools/build-ios-native.sh` performs an unsigned compiler check only. For an installable app, use the resolved external export with the Unity-iPhone target, ReleaseForRunning, iphoneos SDK, automatic signing, the user's selected Personal Team and two compiler jobs. Regeneration can replace manual signing selections. Do not commit credentials, provisioning profiles or private device identifiers.

Verify signing, profile validity/phone inclusion, ARM64, version/build and IPA integrity before installing. The previously proven local install route is `python -m pymobiledevice3 apps install --udid <connected-phone-UDID> --developer <signed-IPA>`, followed by `apps query --udid <connected-phone-UDID> com.barrelrivals.foundation`. Detect the currently attached phone first. An old USB inventory is not proof of a present device. Installation, launch and sustained performance must each have their own evidence.

## Preserved 0.4 installation and host history

Version 0.4.0/build 4 was verified installed on the user's iPhone 17 Pro on September 19. Its source, export, signed IPA and `Evidence/Reins-iOS-*` records remain historical. It contains Classic and the earlier Reins Lab, unlike the current 0.5 player. The user played and preferred Reins; this is qualitative product feedback, not a complete device acceptance matrix.

The recorded host is a 2017 Intel MacBook Pro, 8 GB RAM, macOS Ventura 13.7.8, Unity 6000.6.0f1 and Xcode 15.2 (15C500b). The recorded phone is iPhone 17 Pro/iOS 26.6.2 (23G90). These facts do not establish current USB availability or App Store eligibility.

For 0.4, the normal Xcode device-install route failed on a developer-image variant mismatch; the standard pymobiledevice3 USB installation service successfully installed the signed app without a jailbreak, firmware change or signing bypass. A prior export under Documents also encountered file coordination and duplicate generated sources. Those older repairs are documented in the preserved native evidence and are not automatically applicable to a fresh export.

The user has no paid Apple Developer membership. This is personal-device development testing; store/TestFlight distribution and a supported publishing toolchain remain separate future work.
