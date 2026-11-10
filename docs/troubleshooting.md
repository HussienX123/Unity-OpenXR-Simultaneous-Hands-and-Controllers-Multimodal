# Troubleshooting

> **Status: Verified on Hardware & Unity Editor.**
> Supports Windows Editor / Standalone (via Meta Quest Link) and Android (Meta Quest). Tested and verified on Meta Quest 3, Meta Quest 2, and Windows Editor via Quest Link on Unity `6000.3.12f1`.

---

## 1. Works on Device (Android Build) but Not in Editor (Quest Link)

### Symptom
On an Android build running directly on a Meta Quest headset, `XR_META_simultaneous_hands_and_controllers` is enabled and simultaneous tracking works, but in the Unity Editor (Play Mode over Meta Quest Link), the Console logs:

```text
[MetaSimultaneousHandsAndControllersFeature] OpenXR extension 'XR_META_simultaneous_hands_and_controllers' is not enabled or not supported by the active OpenXR runtime ('Oculus' v..., Platform=WindowsEditor). Simultaneous hand and controller tracking will remain inactive.
```

or:

```text
[MetaSimultaneousHandsAndControllersFeature] Active OpenXR runtime ('Oculus' v..., Platform=WindowsEditor) reported that simultaneous hands and controllers tracking is not supported (XrResult=Success, supportsSimultaneousHandsAndControllers=0).
```

### Cause & Resolution
- **PC Runtime Configuration:** Simultaneous tracking is verified to work over Meta Quest Link in the Unity Editor. If the PC runtime logs unsupported or not enabled:
  1. Open the **Meta Quest Link** desktop application, go to **Settings > General**, and verify that **OpenXR Runtime** is set to active (Meta Quest Link).
  2. Inside the headset under Link settings, verify **Hand Tracking** over Link is enabled.
  3. In Unity **Edit > Project Settings > XR Plug-in Management > OpenXR (Windows tab)**, set **Play Mode OpenXR Runtime** to **Oculus** (or **System Default**). Note that Unity resets this dropdown in `SessionState` across Editor restarts.
  4. Ensure the Quest Link session is established and active before starting Unity Play Mode.

---

## 2. No `[MetaSimultaneousHandsAndControllersFeature]` Logs Appear at All

### Symptom
Entering Play Mode or running a build produces `[XRDiagnostics]` logs (or no XR logs), and no `[MetaSimultaneousHandsAndControllersFeature]` message appears in the Console.

### Cause & Resolution
1. **OpenXR Loader Not Active for Current Target:**
   - Open **Edit > Project Settings > XR Plug-in Management**.
   - Check the **Windows, Mac, Linux** tab (for Editor Play Mode) and the **Android** tab (for Quest builds).
   - Ensure **Initialize XR on Startup** and **OpenXR** are both checked.
2. **Feature Not Enabled on Current Build Target Tab:**
   - Open **Edit > Project Settings > XR Plug-in Management > OpenXR**.
   - Confirm **Meta Simultaneous Hands and Controllers** is checked under **both** the **Windows, Mac, Linux** tab and the **Android** tab.
3. **Play Mode OpenXR Runtime Reset (Windows Editor):**
   - Unity resets the **Play Mode OpenXR Runtime** dropdown to **System Default** on Editor restart (`PlayModeOpenXRRuntimeSettings.cs`). Re-select **Oculus** (or ensure Quest Link is your Windows default OpenXR runtime) and ensure Quest Link is active before pressing Play.

---

## 3. 3D Controller Visual Moves Into the Center of the Tracked Hand When Set Down

### Symptom
When you put a physical Quest controller down on a desk and use bare-hand tracking, the 3D controller model jumps to the center/wrist of your hand.

### Cause & Resolution
1. **`Hand Interaction Poses` (`HandCommonPosesInteraction`) Enabled:**
   - In **Edit > Project Settings > XR Plug-in Management > OpenXR**, uncheck **Hand Interaction Poses** on both the **Windows, Mac, Linux** and **Android** tabs. When enabled, `HandCommonPosesInteraction.AddAdditiveActions` marks Touch controller `devicePose` and `pointer` actions as `isAdditive = true`, binding them to `XR_EXT_hand_interaction`.
2. **`XRI Left/Position` / `XRI Right/Position` Fallback to `<XRHandDevice>`:**
   - Ensure `ControllerPoseIsolation` is attached to `Left Controller` and `Right Controller` (included in `Assets/Prefabs/XR/XR Origin Hands (XR Rig).prefab` and auto-attached at runtime). It rebinds the controller `TrackedPoseDriver`s exclusively to `<XRControllerWithRumble>` and locks the last valid tracked controller pose when the physical controller loses position tracking.

---

## 4. Hand Visual Disappears Whenever a Controller Is Picked Up

### Symptom
As soon as a controller becomes active on the left or right side, the hand mesh/skeleton on that side disappears.

### Cause & Resolution
- Your `XR Origin` still has an active **`XRInputModalityManager`** component (`com.unity.xr.interaction.toolkit`), which is designed for mutually exclusive hands-or-controllers mode.
- Remove or disable `XRInputModalityManager` on the `XR Origin` GameObject.

---

*Not affiliated with or endorsed by Meta Platforms or Unity Technologies.*
