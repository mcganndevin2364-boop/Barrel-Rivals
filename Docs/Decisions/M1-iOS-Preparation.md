# M1 iPhone preparation

September 16, 2026. The user clarified that the test device is an iPhone 17 Pro running iOS 26.6.2 (user-reported). They have only the current Mac and no paid Apple Developer membership.

Host inspection found MacBookPro14,1, Intel Core i5, 8 GB RAM and macOS Ventura 13.7.8. Full Xcode is absent; xcode-select points to Command Line Tools. Unity 6000.6.0f1 iOS Build Support is installed. The Mac has approximately 19 GiB free after export, so download/expansion space must be checked before attempting an Xcode installation.

`IPhonePracticeBuilder.Export` and `Tools/run-m1.sh ios` export the saved practice scene using device SDK, IL2CPP, Metal, deployment target iOS 15+, version 0.2.0/build 2 and the existing development bundle identifier. Automatic signing is selected, with no invented team or signing credentials. The export is generated content under ignored `Builds/iOS/BarrelRivals-Practice`.

The actual Unity export succeeded with zero reported errors. `Tools/verify-ios-export.py` read the generated project and plist, checked device/ARM64 settings, application ID/version, app/framework targets and IL2CPP output. `Evidence/M1-iOS-Export.json` records the result. No Xcode native compilation, signed app/IPA, device installation or physical iPhone run has occurred. The earlier Android artifact remains separate and cannot run on iOS.

The user's available local route is a provisional Xcode 15.2/Personal Team attempt. Apple lists Xcode 15.2 for Ventura, while Unity 6000.6 recommends Xcode 16+. Documentation does not prove this exact Unity export and phone can build, pair and launch with 15.2, so actual tests are required. Apple's SDK/simulator version columns are not equivalent to its physical-device support column. A newer supported Mac/toolchain is the fallback if the local experiment fails. TestFlight requires paid membership and current upload requirements; no paid service, membership or cloud build has been purchased or provisioned.

Apple's official Xcode download page was opened for the user and requires their sign-in. No account credentials were requested in chat, entered by the agent or stored in this repository. The download/install step remains waiting on that sign-in. Read README-iPhone.md for the route and primary sources. Only the export milestone is verified; the iOS platform acceptance gate remains open.
