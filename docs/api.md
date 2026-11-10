# Public API Reference

> **Status: Experimental, pending device verification.**
> Namespace: `HussienX.OpenXR.SimultaneousHandsAndControllers`
> Assembly: `HussienX.OpenXR.SimultaneousHandsAndControllers.dll`

---

## `MetaSimultaneousHandsAndControllersFeature`

```csharp
namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    public class MetaSimultaneousHandsAndControllersFeature : OpenXRFeature
}
```

Custom `OpenXRFeature` targeting `BuildTargetGroup.Android` and `BuildTargetGroup.Standalone`. Requests the `XR_META_simultaneous_hands_and_controllers` OpenXR extension, queries system support via `xrGetSystemProperties`, and calls `xrResumeSimultaneousHandsAndControllersTrackingMETA` when the OpenXR session begins.

### Constants

| Constant | Value | Description |
| :--- | :--- | :--- |
| `FeatureId` | `"com.hussienx.openxr.feature.simultaneoushandsandcontrollers"` | Unique OpenXR feature identifier. |
| `ExtensionName` | `"XR_META_simultaneous_hands_and_controllers"` | OpenXR extension name requested at instance creation. |

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsExtensionEnabled` | `bool` | `true` if `OpenXRRuntime.IsExtensionEnabled(ExtensionName)` succeeded and native functions were resolved. |
| `SupportsSimultaneousTracking` | `bool` | `true` if `xrGetSystemProperties` returned `XrResult.Success` and `supportsSimultaneousHandsAndControllers != 0`. |
| `IsSimultaneousTrackingActive` | `bool` | `true` if `xrResumeSimultaneousHandsAndControllersTrackingMETA` returned `XrResult.Success` for the current session. |

### Methods

| Method | Return Type | Description |
| :--- | :--- | :--- |
| `PauseSimultaneousTracking()` | `bool` | Calls `xrPauseSimultaneousHandsAndControllersTrackingMETA` for the active session if supported. Returns `true` on `XrResult.Success`. |

---

## `SimultaneousHandsAndControllersStatus`

```csharp
namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    public class SimultaneousHandsAndControllersStatus : MonoBehaviour
}
```

MonoBehaviour component that reads the active `MetaSimultaneousHandsAndControllersFeature` instance from `OpenXRSettings.Instance` and exposes its runtime status properties in the Inspector and via C#.

### Properties & Methods

| Member | Type | Description |
| :--- | :--- | :--- |
| `IsExtensionEnabled` | `bool` | `true` when the active OpenXR runtime enabled `XR_META_simultaneous_hands_and_controllers`. |
| `SystemReportsSupport` | `bool` | `true` when `xrGetSystemProperties` reported `supportsSimultaneousHandsAndControllers != 0`. |
| `ResumeSucceeded` | `bool` | `true` when `xrResumeSimultaneousHandsAndControllersTrackingMETA` succeeded for the active session. |
| `RefreshStatus()` | `void` | Immediately polls `OpenXRSettings.Instance.GetFeature<MetaSimultaneousHandsAndControllersFeature>()`. |

---

## `ControllerMotionData`

```csharp
namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    public class ControllerMotionData : MonoBehaviour
}
```

Reads motion controller rotation, forward vector, linear velocity, angular velocity, speed, and tracking state every frame for a selected hand (`Left` or `Right`). Excludes non-motion controller devices (`HandInteraction`, `HoloLensHand`, `DPad`, `PalmPose`).

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `hand` | `ControllerHand` (`Left`, `Right`) | Which motion controller hand to sample. |
| `poseSource` | `PoseSource` (`PointerAim`, `DeviceGrip`) | Pose control to sample rotation from. |
| `rotation` | `Quaternion` | Controller rotation in tracking space for the current frame. |
| `forward` | `Vector3` | Forward direction vector (`rotation * Vector3.forward`). |
| `linearVelocity` | `Vector3` | Controller linear velocity (`m/s`). `Vector3.zero` when `isVelocityValid` is `false`. |
| `angularVelocity` | `Vector3` | Controller angular velocity (`rad/s`). `Vector3.zero` when `isTracked` is `false`. |
| `speed` | `float` | Magnitude of `linearVelocity` (`m/s`). `0` when `isVelocityValid` is `false`. |
| `trackingState` | `InputTrackingState` | Combined `InputTrackingState` flags reported by the controller. |
| `isTracked` | `bool` | `true` when the motion controller is actively tracked. |
| `isVelocityValid` | `bool` | `true` when tracked and `trackingState` includes `InputTrackingState.Position`. |

---

## `ControllerPoseIsolation`

```csharp
namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    public class ControllerPoseIsolation : MonoBehaviour
}
```

Rebinds a controller hierarchy's `TrackedPoseDriver` exclusively to `<XRControllerWithRumble>` (physical motion controllers with haptics) so it never falls back to `<XRHandDevice>` or `<HandInteraction>`. Preserves the controller's last valid tracked local position and rotation when the physical controller is set down and loses position tracking.

### Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `hand` | `ControllerMotionData.ControllerHand` | Which controller hand (`Left` or `Right`) this hierarchy represents. |

---

## `XRSimultaneousTrackingDiagnostics` (Sample)

```csharp
namespace XRDiagnostics
{
    public class XRSimultaneousTrackingDiagnostics : MonoBehaviour
}
```

Sample diagnostic component (`Samples~/Diagnostics/XRSimultaneousTrackingDiagnostics.cs`) that logs active platform, OpenXR runtime name and version, left/right controller tracked state, left/right hand tracked state, simultaneous tracking flag, and controller motion once per second.

---

*Not affiliated with or endorsed by Meta Platforms or Unity Technologies.*
