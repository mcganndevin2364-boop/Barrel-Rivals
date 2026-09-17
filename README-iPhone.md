# Barrel Rivals — iPhone test setup

The Android APK does not run on an iPhone. The iPhone path is Unity export → Xcode native build → Apple development signing → install/run on the connected phone. An Xcode project is source for that process, not an installable app.

**Verified so far:** Unity Xcode export succeeded with zero reported errors; the export inspector passed. Native compilation, signing and the phone installation remain unfinished. Apple sign-in is needed to download Xcode.

## Current device and Mac

- User-reported phone: iPhone 17 Pro, iOS 26.6.2. Device connection and OS have not been independently verified.
- Available Mac: MacBookPro14,1, Intel, 8 GB RAM, macOS Ventura 13.7.8. Full Xcode is not installed; Command Line Tools are present.
- Unity 6000.6.0f1 and its iOS Build Support module are installed.
- The user has no paid Apple Developer membership and no newer Mac available.

Apple identifies this Mac as the 2017 model with Ventura as its latest compatible system. Xcode 15.2 is the last Xcode release listed for Ventura; Xcode 16 requires a newer macOS. Unity 6000.6 recommends Xcode 16+, rather than stating a hard minimum. A local Xcode 15.2 build is therefore an experiment: native compilation, phone pairing, signing and launch must all be tested before claiming compatibility. The iOS SDK version alone is not a complete statement of physical-device support. [Apple Mac identification](https://support.apple.com/en-us/108052), [Xcode compatibility](https://developer.apple.com/xcode/system-requirements), [Unity iOS requirements](https://docs.unity3d.com/6000.6/Documentation/Manual/ios-requirements-and-compatibility.html).

## Export the current practice scene

With other Editors for this project closed, run from this repository:

```bash
Tools/run-m1.sh ios
python3 Tools/verify-ios-export.py
```

The menu equivalent is **Barrel Rivals → Build iPhone Practice (Xcode Export)**. Output belongs in `Builds/iOS/BarrelRivals-Practice/Unity-iPhone.xcodeproj`. The build uses the saved `Arena_Practice` scene, IL2CPP, Metal, device SDK and iOS 15+ deployment target. App version is 0.2.0/build 2. The existing development bundle ID is retained and automatic signing is enabled without inventing an Apple team.

Generated Xcode content stays under ignored `Builds/`; do not commit certificates, provisioning profiles or account credentials. The export inspector writes `Evidence/M1-iOS-Export.json` and explicitly distinguishes export checks from native compilation, signing and a phone run. Regenerating the export can replace manual changes inside the generated project; keep lasting Unity changes in source/build tooling.

## Free personal-device attempt

1. Sign in directly to [Apple's Xcode 15.2 download listing](https://developer.apple.com/download/all/?q=Xcode%2015.2). Download from Apple, confirm there is enough free storage for the archive and expanded app, and install full Xcode. The regular App Store listing offers newer Xcode that this Mac cannot run.
2. Complete Xcode's first launch, license/setup steps and Apple Account sign-in. A free Personal Team can sign an app for personal device testing; free provisioning expires after seven days. [Apple account options](https://developer.apple.com/help/account/basics/about-your-developer-account).
3. Open the exported `Unity-iPhone.xcodeproj`, select the **Unity-iPhone** scheme, connect/unlock the phone by USB and complete the phone's Trust prompts. Enable Developer Mode on the phone when required. [Apple device-run guide](https://help.apple.com/xcode/mac/current/en.lproj/dev5a825a1ca.html).
4. Select your Personal Team in Signing & Capabilities with automatic signing. If Apple reports that the development bundle ID is unavailable, choose a unique development ID in Unity and regenerate rather than assuming ownership of that ID.
5. Select the actual iPhone as the run destination and build/run. Preserve the first precise compiler, signing or device-preparation error if this older toolchain cannot complete the process. A successful Unity export alone does not pass this step.
6. On the phone, test cold launch, launch audio timing, drawing, exit taps, retry, background/foreground behavior and screen safe areas. Record observations before changing tuning.

The app currently uses prototype art and stops after one barrel. No multiplayer, purchases or ranked rewards are enabled.

## If the older local toolchain cannot run it

Use a compatible newer Mac/build host with an appropriate Xcode version. TestFlight also needs paid Apple Developer Program membership and current App Store Connect upload requirements; since April 28, 2026, those require Xcode 26+ and the iOS 26 SDK+. That route has not been purchased or provisioned. A browser-only gameplay preview is a separate optional build, not a signed iPhone app. [Apple membership](https://developer.apple.com/programs/), [upload requirements](https://developer.apple.com/news/upcoming-requirements/).
