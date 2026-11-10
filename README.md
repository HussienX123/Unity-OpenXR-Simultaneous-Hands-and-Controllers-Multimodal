# Unity OpenXR Simultaneous Hands and Controllers (Multimodal)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Unity: 6000.3.12f1](https://img.shields.io/badge/Unity-6000.3.12f1-black.svg)](https://unity.com/)
[![Status: Experimental](https://img.shields.io/badge/Status-Experimental-orange.svg)](#status)

Enables `XR_META_simultaneous_hands_and_controllers` in Unity using only the built-in OpenXR, XR Hands, and XR Interaction Toolkit packages with no Meta SDK—supporting Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest) builds.

![Hero Screenshot Placeholder](docs/images/PLACEHOLDER-hero.png)

---

## Status

> **Experimental, pending device verification.**
> - **Platform Support:** Supports Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest).
> - **What is proven:** All runtime, editor, and diagnostic scripts compile with zero errors on Unity `6000.3.12f1` against `com.unity.xr.openxr@1.16.1`, `com.unity.xr.hands@1.7.3`, `com.unity.xr.interaction.toolkit@3.3.2`, and `com.unity.inputsystem@1.19.0`. All OpenXR structure layouts and function signatures are verified directly against Unity's installed OpenXR headers.
> - **What is NOT yet proven:** No test on a physical Meta Quest headset or over Meta Quest Link is claimed in this repository yet; compatibility rows below remain `Not yet tested` until device verification is recorded.

---

## What This Does and Why

### Why It Is Needed
1. **Unity's OpenXR packages do not request the extension:** `com.unity.xr.openxr` (`1.16.1`) defines the structure type constants (`1000532001`, `1000532002`, `1000532003`) in `XrStructureType.cs` and the native C definitions in `openxr_reflection_full.h`, but no built-in OpenXR feature requests `XR_META_simultaneous_hands_and_controllers` or calls `xrResumeSimultaneousHandsAndControllersTrackingMETA`.
2. **`XRInputModalityManager` disables hands when a controller is active:** In `com.unity.xr.interaction.toolkit` (`3.3.2`), `XRInputModalityManager` deactivates the hand GameObject hierarchy whenever a controller is tracked on that side.
3. **Default XRI controller bindings fall back to tracked hands:** `XRI Left/Position` and `XRI Right/Position` include `<XRHandDevice>/devicePosition` as a fallback, and `HandCommonPosesInteraction` marks controller `devicePose`/`pointer` actions as `isAdditive = true`, causing a set-down controller visual to snap into the center of your tracked hand unless isolated.

### What This Repository Provides
- **`MetaSimultaneousHandsAndControllersFeature` (`OpenXRFeature`):** Requests `XR_META_simultaneous_hands_and_controllers` on both `BuildTargetGroup.Android` and `BuildTargetGroup.Standalone`, queries system support via `xrGetSystemProperties`, and calls `xrResumeSimultaneousHandsAndControllersTrackingMETA` when the session begins. Fails safely with a single warning if unsupported.
- **`SimultaneousHandsAndControllersStatus`:** Runtime component exposing `IsExtensionEnabled`, `SystemReportsSupport`, and `ResumeSucceeded`.
- **`ControllerMotionData`:** Runtime component exposing controller `rotation`, `forward`, `linearVelocity`, `angularVelocity`, `speed`, `trackingState`, `isTracked`, and `isVelocityValid`.
- **`ControllerPoseIsolation`:** Runtime component that isolates controller `TrackedPoseDriver` bindings to `<XRControllerWithRumble>` and locks the 3D controller visual at its last tracked pose when set down.
- **`XRSimultaneousTrackingDiagnostics` (Sample):** Logs platform, active OpenXR runtime name/version, hand and controller tracked states, simultaneous flag, and controller motion once per second.

---

## Quick Install (Package Manager Git URL)

In Unity **Window > Package Management > Package Manager**, click **+ > Add package from git URL...** and enter:

```text
https://github.com/HussienX123/Unity-OpenXR-Simultaneous-Hands-and-Controllers-Multimodal.git?path=/Packages/com.hussienx.openxr-simultaneous-hands-controllers
```

---

## Setup

### A. Windows Editor / Standalone (via Meta Quest Link)

1. Open **Edit > Project Settings > XR Plug-in Management** and select the **Windows, Mac, Linux** tab. Enable **Initialize XR on Startup** and check **OpenXR**.
2. Open **Edit > Project Settings > XR Plug-in Management > OpenXR** (Windows, Mac, Linux tab) and set **Play Mode OpenXR Runtime** to **Oculus** (or **System Default** if Meta Quest Link is the Windows active OpenXR runtime). Note: Unity stores this dropdown in `SessionState` and resets it to **System Default** on Editor restart.
3. Under **OpenXR Feature Groups** (Windows, Mac, Linux tab), enable **Meta Simultaneous Hands and Controllers**, **Hand Tracking Subsystem**, and **Meta Hand Tracking Aim**.
4. Under **Enabled Interaction Profiles** (Windows, Mac, Linux tab), enable **Hand Interaction Profile** and **Oculus Touch Controller Profile** (plus **Meta Quest Touch Pro / Plus** profiles if used). Leave **Hand Interaction Poses** (`HandCommonPosesInteraction`) **disabled**.

![Standalone OpenXR Settings Placeholder](docs/images/PLACEHOLDER-openxr-settings-standalone.png)

### B. Android (Meta Quest Build)

1. Open **Edit > Project Settings > XR Plug-in Management** and select the **Android** tab. Enable **Initialize XR on Startup** and check **OpenXR**.
2. Open **Edit > Project Settings > XR Plug-in Management > OpenXR** (Android tab) and enable **Meta Quest Support**, **Meta Simultaneous Hands and Controllers**, **Hand Tracking Subsystem**, and **Meta Hand Tracking Aim**.
3. Under **Enabled Interaction Profiles** (Android tab), enable **Hand Interaction Profile**, **Oculus Touch Controller Profile**, **Meta Quest Touch Pro Controller Profile**, and **Meta Quest Touch Plus Controller Profile**. Leave **Hand Interaction Poses** (`HandCommonPosesInteraction`) **disabled**.
4. Build and run on the headset (`IL2CPP`, `ARM64`, Minimum API Level `29`+).

![Android OpenXR Settings Placeholder](docs/images/PLACEHOLDER-openxr-settings-android.png)

### C. Shared Scene Steps (Both Platforms)

1. In **Window > Package Management > Package Manager**, import:
   - **XR Interaction Toolkit** > **Starter Assets** and **Hands Interaction Demo**
   - **XR Hands** > **HandVisualizer**
   - **OpenXR Simultaneous Hands and Controllers (Experimental)** > **Diagnostics**
2. On your scene's `XR Origin`, remove or disable **`XRInputModalityManager`** so both `Left/Right Hand` and `Left/Right Controller` hierarchies remain active at the same time (pre-configured in `Assets/Prefabs/XR/XR Origin Hands (XR Rig).prefab` and `Assets/Scenes/MultimodalTest.unity`).
3. Add an empty GameObject with **`XRSimultaneousTrackingDiagnostics`** and **`ControllerMotionData`**.

---

## Expected Log Output

### 1. Supported (Extension Enabled, System Supported, and Session Resumed)

```text
[MetaSimultaneousHandsAndControllersFeature] Extension 'XR_META_simultaneous_hands_and_controllers' is enabled on active OpenXR runtime ('<runtime>' v<version>, Platform=<platform>) and functions were resolved for XrInstance=<instance>.
[MetaSimultaneousHandsAndControllersFeature] xrGetSystemProperties XrResult=Success, supportsSimultaneousHandsAndControllers=True (raw=1, XrSystemId=<systemId>).
[MetaSimultaneousHandsAndControllersFeature] xrResumeSimultaneousHandsAndControllersTrackingMETA succeeded (XrResult=Success) for XrSession=<session>.
[XRDiagnostics] Started on Platform=<platform> | Runtime=<runtime> v<version>
[XRDiagnostics] Platform=<platform> | Runtime=<runtime> v<version> | Controller(L=False, R=True) | Hand(L=True, R=True) | Simultaneous=True (L_Both=False, R_Both=True) | Motion(Right: RotEuler=(0.0, 0.0, 0.0), Speed=0.000 m/s, VelocityValid=True)
```

### 2. Runtime Refused (`supportsSimultaneousHandsAndControllers == 0` or Extension Not Offered)

```text
[MetaSimultaneousHandsAndControllersFeature] OpenXR extension 'XR_META_simultaneous_hands_and_controllers' is not enabled or not supported by the active OpenXR runtime ('<runtime>' v<version>, Platform=<platform>). Simultaneous hand and controller tracking will remain inactive.
```

or if the extension is enabled but `xrGetSystemProperties` reports `supportsSimultaneousHandsAndControllers = 0`:

```text
[MetaSimultaneousHandsAndControllersFeature] xrGetSystemProperties XrResult=Success, supportsSimultaneousHandsAndControllers=False (raw=0, XrSystemId=<systemId>).
[MetaSimultaneousHandsAndControllersFeature] Active OpenXR runtime ('<runtime>' v<version>, Platform=<platform>) reported that simultaneous hands and controllers tracking is not supported (XrResult=Success, supportsSimultaneousHandsAndControllers=0).
```

### 3. Feature Not Enabled in OpenXR Settings

No `[MetaSimultaneousHandsAndControllersFeature]` log lines are emitted, and `[XRDiagnostics]` logs:

```text
[XRDiagnostics] Started on Platform=<platform> | Runtime=<runtime> v<version>
[XRDiagnostics] Platform=<platform> | Runtime=<runtime> v<version> | Controller(L=False, R=True) | Hand(L=False, R=False) | Simultaneous=False (L_Both=False, R_Both=False) | Motion(...)
```

![Console Logs Placeholder](docs/images/PLACEHOLDER-console-logs.png)

---

## Compatibility Table

| Target / Device / Unity Version | Status | Notes |
| :--- | :--- | :--- |
| **Unity `6000.3.12f1`** | **Compiles** | Zero compiler errors across runtime, editor, and sample assemblies |
| **Editor (Windows) via Quest Link** | Not yet tested | PC OpenXR runtime support for `XR_META_simultaneous_hands_and_controllers` is **UNVERIFIED** |
| **Android build on Quest** | Not yet tested | Standalone APK on Meta Quest |
| **Meta Quest 2** | Not yet tested | Pending device verification |
| **Meta Quest 3** | Not yet tested | Pending device verification |
| **Meta Quest 3S** | Not yet tested | Pending device verification |
| **Meta Quest Pro** | Not yet tested | Pending device verification |
| **Unity `6000.0` LTS** | Not yet tested | Minimum declared `unity` version in `package.json` |

---

## Known Limitations and UNVERIFIED Items

- **PC OpenXR Runtime Support (UNVERIFIED):** Unity's installed OpenXR packages do not state whether the Meta Quest Link PC OpenXR runtime advertises `XR_META_simultaneous_hands_and_controllers` on Windows. If Play Mode over Link logs that the extension is not enabled or not supported, it can be a PC runtime limitation; an Android build on the headset is the definitive test.
- **Meta Quest Link App Menu Names (UNVERIFIED):** Exact menu paths and toggle names in the Meta Quest Link desktop application are not present in Unity's installed package files. Check Meta's official Quest Link documentation.
- **Android Manifest Requirements for `XR_META_simultaneous_hands_and_controllers` (UNVERIFIED):** `com.unity.xr.hands` documents `<uses-permission android:name="com.oculus.permission.HAND_TRACKING" />` and `<uses-feature android:name="oculus.software.handtracking" android:required="false" />`, which `MetaHandTrackingAim` injects automatically on Android builds. Whether `XR_META_simultaneous_hands_and_controllers` requires any additional `<meta-data>` tag in `AndroidManifest.xml` is not specified in Unity's installed OpenXR files; consult Meta's OpenXR specification if the Android runtime refuses the extension.
- **Quest OS Version Requirements (UNVERIFIED):** Minimum Horizon OS / Quest system software version required for `XR_META_simultaneous_hands_and_controllers` is not specified in the installed Unity packages.

---

## Documentation

- [Installation Guide](docs/installation.md)
- [How It Works (with Mermaid Flow Diagram)](docs/how-it-works.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Public API Reference](docs/api.md)

---

## FAQ

### Works on device but not in Editor?
When testing in the Unity Editor over Meta Quest Link, Unity runs against the **Windows PC OpenXR runtime**, not the headset's Android OpenXR runtime. PC runtime support for `XR_META_simultaneous_hands_and_controllers` is **UNVERIFIED** in Unity's installed OpenXR files. If the PC runtime does not advertise the extension, `MetaSimultaneousHandsAndControllersFeature` logs one warning and continues safely; build and run an Android APK on the headset for the definitive test.

### Why does the 3D controller visual jump into my hand when I put the controller down?
By default, `XRI Left/Position` and `XRI Right/Position` fall back to `<XRHandDevice>/devicePosition` (the wrist of your tracked hand) and `HandCommonPosesInteraction` marks Touch controller `devicePose`/`pointer` actions as `isAdditive = true`. Disable **Hand Interaction Poses** in OpenXR settings and keep `ControllerPoseIsolation` on the controller GameObjects so they only bind to `<XRControllerWithRumble>` and stay locked at their last valid tracked position when set down.

---

## Roadmap

- [ ] Verify on Editor over Link
- [ ] Verify on Quest build
- [ ] Controller rotation and velocity helpers

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). When reporting device test results or issues, please include your headset model, Quest OS version, Unity version, package version, platform tested, active OpenXR runtime name, and console logs.

---

## License

Released under the [MIT License](LICENSE).

Not affiliated with or endorsed by Meta Platforms or Unity Technologies.
