# Installation Guide

> **Status: Verified on Hardware & Unity Editor.**
> Supports Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest). Tested and verified on Meta Quest 3, Meta Quest 2, and Windows Editor via Quest Link on Unity `6000.3.12f1`.

## Prerequisites

- **Unity**: `6000.0` or later (verified to compile on `6000.3.12f1`)
- **Packages** (automatically resolved via `package.json` dependencies):
  - `com.unity.xr.openxr` (`1.16.1`)
  - `com.unity.xr.hands` (`1.7.3`)
  - `com.unity.inputsystem` (`1.19.0`)
- **Optional for scene rig setup**:
  - `com.unity.xr.interaction.toolkit` (`3.3.2`)

---

## 1. Install the Package

### Option A: Import `.unitypackage` from GitHub Releases (Includes Demo Scene & XR Rig Prefab)

1. Download **`OpenXR-Simultaneous-Hands-and-Controllers-v0.1.0-experimental.unitypackage`** from [**GitHub Releases**](https://github.com/HussienX123/Unity-OpenXR-Simultaneous-Hands-and-Controllers-Multimodal/releases/tag/v0.1.0-experimental).
2. In your Unity project (with **OpenXR Plugin** and **XR Interaction Toolkit** installed), choose **Assets > Import Package > Custom Package...** and import all files.
3. `MultimodalSetupUtility` automatically runs on import to:
   - Install `com.unity.xr.hands` (if missing) and import the required **Starter Assets**, **Hands Interaction Demo**, and **HandVisualizer** samples.
   - Configure **OpenXR Plug-in Management** and **OpenXR Feature Settings** for both **Standalone (Windows, Mac, Linux)** and **Android** (including disabling **Hand Interaction Poses**).
4. Open **`Assets/Scenes/MultimodalTest.unity`** (or use **Tools > OpenXR Multimodal > 4. Open Multimodal Demo Scene**). You can also convert any existing scene rig using **Tools > OpenXR Multimodal > 3. Convert Active Scene XR Rig for Simultaneous Tracking**.

### Option B: Install via Unity Package Manager (Git URL)

1. In the Unity Editor, open **Window > Package Management > Package Manager**.
2. Click the **+** button in the top-left corner and choose **Add package from git URL...**.
3. Enter the package Git URL:

```text
https://github.com/HussienX123/Unity-OpenXR-Simultaneous-Hands-and-Controllers-Multimodal.git?path=/Packages/com.hussienx.openxr-simultaneous-hands-controllers
```

4. Click **Add**.

---

## 2. Import Required Samples

Because Unity sample assets are distributed through their respective packages rather than committed to git:

1. In **Package Manager**, select **XR Interaction Toolkit** (`3.3.2`) > **Samples** tab:
   - Import **Starter Assets**
   - Import **Hands Interaction Demo**
2. Select **XR Hands** (`1.7.3`) > **Samples** tab:
   - Import **HandVisualizer**
3. Select **OpenXR Simultaneous Hands and Controllers (Experimental)** > **Samples** tab:
   - Import **Diagnostics**

---

## 3. Configure Windows Editor / Standalone (Meta Quest Link)

1. Open **Edit > Project Settings > XR Plug-in Management** and select the **Windows, Mac, Linux** (Standalone) tab.
2. Check **Initialize XR on Startup** and enable **OpenXR** under **Plug-in Providers**.
3. Open **Edit > Project Settings > XR Plug-in Management > OpenXR** and select the **Windows, Mac, Linux** tab:
   - Set **Play Mode OpenXR Runtime** to **Oculus** (or **System Default** if Meta Quest Link is configured as the Windows active OpenXR runtime).
   - **Note:** Unity stores `Play Mode OpenXR Runtime` in memory (`SessionState`) and resets this dropdown to **System Default** whenever the Unity Editor restarts.
   - Under **OpenXR Feature Groups / Features**, enable:
     - **Meta Simultaneous Hands and Controllers**
     - **Hand Tracking Subsystem**
     - **Meta Hand Tracking Aim**
   - Under **Enabled Interaction Profiles**, enable:
     - **Hand Interaction Profile**
     - **Oculus Touch Controller Profile**
     - **Meta Quest Touch Pro Controller Profile** (optional)
     - **Meta Quest Touch Plus Controller Profile** (optional)
   - Keep **Hand Interaction Poses** (`HandCommonPosesInteraction`) **disabled** so OpenXR does not additively bind hand poses onto Touch controller `devicePose`/`pointer` actions.

> **UNVERIFIED (Meta Quest Link App & PC Runtime):** Installed Unity package files do not specify the exact menu names in the Meta Quest Link desktop app or whether the PC OpenXR runtime advertises `XR_META_simultaneous_hands_and_controllers`. Check Meta's official Quest Link documentation to ensure Meta Quest Link is set as the active OpenXR runtime and hand tracking over Link is enabled.

---

## 4. Configure Android (Meta Quest Headset Build)

1. Open **Edit > Project Settings > XR Plug-in Management** and select the **Android** tab.
2. Check **Initialize XR on Startup** and enable **OpenXR** under **Plug-in Providers**.
3. Open **Edit > Project Settings > XR Plug-in Management > OpenXR** and select the **Android** tab:
   - Under **OpenXR Feature Groups / Features**, enable:
     - **Meta Quest Support**
     - **Meta Simultaneous Hands and Controllers**
     - **Hand Tracking Subsystem**
     - **Meta Hand Tracking Aim**
   - Under **Enabled Interaction Profiles**, enable:
     - **Hand Interaction Profile**
     - **Oculus Touch Controller Profile**
     - **Meta Quest Touch Pro Controller Profile**
     - **Meta Quest Touch Plus Controller Profile**
   - Keep **Hand Interaction Poses** (`HandCommonPosesInteraction`) **disabled**.
4. Verify **Edit > Project Settings > Player** (Android tab):
   - **Active Input Handling**: `Input System Package (New)` or `Both`
   - **Scripting Backend**: `IL2CPP`
   - **Target Architectures**: `ARM64`
   - **Minimum API Level**: `29` (Android 10.0) or higher
   - **Graphics APIs**: `Vulkan` (or `OpenGLES3`)
   - **Default Orientation**: `Landscape Left`

---

## 5. Scene Rig Configuration

1. In your scene's `XR Origin` hierarchy, remove or disable the **`XRInputModalityManager`** component so it does not deactivate the `Left Hand` / `Right Hand` GameObjects when controllers are active.
2. Ensure `ControllerPoseIsolation` is attached to `Left Controller` and `Right Controller` (it also auto-attaches at runtime to controller `TrackedPoseDriver` GameObjects) so the 3D controller visual stays in place when you put a controller down and use hand tracking.
3. Add an empty GameObject with `XRSimultaneousTrackingDiagnostics` and `ControllerMotionData` to inspect runtime logs.

---

*Not affiliated with or endorsed by Meta Platforms or Unity Technologies.*
