# OpenXR Simultaneous Hands and Controllers (`com.hussienx.openxr-simultaneous-hands-controllers`)

> **Status: Verified on Hardware & Unity Editor.**
> Supports Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest). Tested and verified on Meta Quest 3, Meta Quest 2, and Unity Editor (via Quest Link) on Unity `6000.3.12f1`.

Enables the `XR_META_simultaneous_hands_and_controllers` OpenXR extension through Unity's OpenXR + XR Hands + XR Interaction Toolkit stack with no Meta SDK required.

## Installation (Package Manager Git URL)

In Unity **Window > Package Management > Package Manager**, click **+ > Add package from git URL...** and enter:

```text
https://github.com/HussienX123/Unity-OpenXR-Simultaneous-Hands-and-Controllers-Multimodal.git?path=/Packages/com.hussienx.openxr-simultaneous-hands-controllers
```

## Included Components

- `MetaSimultaneousHandsAndControllersFeature` (`OpenXRFeature` for `BuildTargetGroup.Android` and `BuildTargetGroup.Standalone`)
- `SimultaneousHandsAndControllersStatus` (runtime status component exposing `IsExtensionEnabled`, `SystemReportsSupport`, and `ResumeSucceeded`)
- `ControllerMotionData` (reads motion controller rotation, forward vector, linear velocity, angular velocity, speed, and `isVelocityValid`)
- `ControllerPoseIsolation` (isolates controller `TrackedPoseDriver` bindings to `<XRControllerWithRumble>` and locks the last valid pose when a controller is set down)
- **Diagnostics Sample** (`Samples~/Diagnostics/XRSimultaneousTrackingDiagnostics.cs`)

## Compatibility

| Target / Version | Status | Notes |
| :--- | :--- | :--- |
| Unity `6000.3.12f1` | Verified (Compiles & Runs) | Verified with zero compiler errors |
| Editor (Windows) via Quest Link | Verified / Supported | Tested & verified in Play Mode via Meta Quest Link |
| Meta Quest 3 | Verified / Supported | Tested & verified on hardware |
| Meta Quest 2 | Verified / Supported | Tested & verified on hardware |
| Android build on Quest | Verified / Supported | Standalone APK tested & verified on Meta Quest |
| Meta Quest 3S | Expected Supported | Shares Snapdragon XR2 Gen 2 platform |
| Meta Quest Pro | Expected Supported | Hardware multimodal supported by Horizon OS |

---

*Not affiliated with or endorsed by Meta Platforms or Unity Technologies.*
