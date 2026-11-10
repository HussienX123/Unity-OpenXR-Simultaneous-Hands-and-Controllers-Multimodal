# Changelog

All notable changes to this package will be documented in this file.

## [0.1.0-experimental] - 2026-11-10

### Added
- `MetaSimultaneousHandsAndControllersFeature` (`OpenXRFeature`) targeting `BuildTargetGroup.Android` and `BuildTargetGroup.Standalone` to request `XR_META_simultaneous_hands_and_controllers`, query `xrGetSystemProperties`, and invoke `xrResumeSimultaneousHandsAndControllersTrackingMETA` on session start with safe fallback.
- `MetaSimultaneousHandsAndControllersFeatureEditor` custom inspector displaying runtime extension status.
- `SimultaneousHandsAndControllersStatus` component exposing `IsExtensionEnabled`, `SystemReportsSupport`, and `ResumeSucceeded`.
- `ControllerMotionData` component for reading physical controller rotation, forward vector, linear/angular velocity, and speed.
- `ControllerPoseIsolation` component to bind controller `TrackedPoseDriver`s exclusively to `<XRControllerWithRumble>` and preserve the last tracked pose when a controller is set down.
- `Diagnostics` sample (`XRSimultaneousTrackingDiagnostics.cs`) logging platform, active OpenXR runtime name and version, controller/hand tracked states, simultaneous flag, and controller motion once per second.
