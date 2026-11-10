# OpenXR Simultaneous Hands and Controllers Documentation

> **Experimental, pending device verification.**
> Supports Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest). Compiles on Unity `6000.3.12f1`; not yet verified on a physical headset or over Meta Quest Link.

## Overview

Unity's `com.unity.xr.openxr` (`1.16.1`) and `com.unity.xr.hands` (`1.7.3`) packages include the OpenXR structure type constants (`SystemSimultaneousHandsAndControllersPropertiesMETA = 1000532001`, `SimultaneousHandsAndControllersTrackingResumeInfoMETA = 1000532002`, `SimultaneousHandsAndControllersTrackingPauseInfoMETA = 1000532003`) in `XrStructureType.cs`, but no built-in OpenXR feature requests `XR_META_simultaneous_hands_and_controllers` or calls `xrResumeSimultaneousHandsAndControllersTrackingMETA`.

Additionally, `XRInputModalityManager` in `com.unity.xr.interaction.toolkit` (`3.3.2`) deactivates the hand GameObject hierarchy whenever a motion controller is tracked on the same hand.

This package provides:
1. **`MetaSimultaneousHandsAndControllersFeature`**: Requests `XR_META_simultaneous_hands_and_controllers` on Android and Standalone, resolves `xrGetSystemProperties`, `xrResumeSimultaneousHandsAndControllersTrackingMETA`, and `xrPauseSimultaneousHandsAndControllersTrackingMETA` via `xrGetInstanceProcAddr`, queries system support, and resumes simultaneous tracking in `OnSessionBegin`. Fails safely with a single warning if the active runtime does not offer or support the extension.
2. **`SimultaneousHandsAndControllersStatus`**: Component exposing `IsExtensionEnabled`, `SystemReportsSupport`, and `ResumeSucceeded`.
3. **`ControllerMotionData`**: Component reading controller rotation, forward vector, linear velocity, angular velocity, speed, and `isVelocityValid`.
4. **`ControllerPoseIsolation`**: Component isolating controller `TrackedPoseDriver` bindings to `<XRControllerWithRumble>` and locking the last valid tracked controller pose when the controller is set down.

## Setup Summary

### Windows Editor / Standalone (via Meta Quest Link)
1. Enable **OpenXR** under **Edit > Project Settings > XR Plug-in Management** (Windows, Mac, Linux tab).
2. Under **XR Plug-in Management > OpenXR** (Windows, Mac, Linux tab):
   - Set **Play Mode OpenXR Runtime** to **Oculus** (or **System Default** if Meta Quest Link is set as the Windows active OpenXR runtime). Note: Unity resets `Play Mode OpenXR Runtime` when the Editor restarts.
   - Enable **Meta Simultaneous Hands and Controllers**, **Hand Tracking Subsystem**, **Meta Hand Tracking Aim**, **Hand Interaction Profile**, and **Oculus Touch Controller Profile** (plus **Meta Quest Touch Pro / Plus** profiles if used).
   - Keep **Hand Interaction Poses** (`HandCommonPosesInteraction`) disabled so hand poses are not additively bound to controller pose actions.
3. **UNVERIFIED:** PC OpenXR runtime support for `XR_META_simultaneous_hands_and_controllers` over Meta Quest Link is not documented in Unity's installed package files. Consult Meta's developer documentation for Quest Link app settings. If the Editor reports the extension as unsupported, test via an Android build on the headset.

### Android (Meta Quest)
1. Enable **OpenXR** under **Edit > Project Settings > XR Plug-in Management** (Android tab).
2. Under **XR Plug-in Management > OpenXR** (Android tab), enable **Meta Quest Support**, **Meta Simultaneous Hands and Controllers**, **Hand Tracking Subsystem**, **Meta Hand Tracking Aim**, **Hand Interaction Profile**, and **Oculus Touch Controller Profile** (plus **Meta Quest Touch Pro / Plus** profiles).

### Scene Rig Setup
1. Remove or disable `XRInputModalityManager` on your `XR Origin` so both `Left/Right Hand` and `Left/Right Controller` hierarchies remain active simultaneously.
2. Import the **Diagnostics** sample from Package Manager and attach `XRSimultaneousTrackingDiagnostics` to a scene GameObject.

---

*Not affiliated with or endorsed by Meta Platforms or Unity Technologies.*
