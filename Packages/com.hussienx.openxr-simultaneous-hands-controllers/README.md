# OpenXR Simultaneous Hands and Controllers (`com.hussienx.openxr-simultaneous-hands-controllers`)

> **Status: Experimental, pending device verification.**
> Supports Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest). Compiles on Unity `6000.3.12f1`; not yet verified on a physical headset or over Meta Quest Link.

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

| Target / Version | Status |
| :--- | :--- |
| Unity `6000.3.12f1` | Compiles |
| Editor (Windows) via Quest Link | Not yet tested |
| Android build on Quest | Not yet tested |

> **UNVERIFIED:** Installed Unity package files do not specify whether the Meta Quest Link PC OpenXR runtime advertises `XR_META_simultaneous_hands_and_controllers`. If the Editor reports the extension as unsupported, an Android build on the headset is the definitive test.

---

*Not affiliated with or endorsed by Meta Platforms or Unity Technologies.*
