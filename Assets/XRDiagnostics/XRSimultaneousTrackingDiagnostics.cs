using System.Collections.Generic;
using HussienX.OpenXR.SimultaneousHandsAndControllers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using InputSystemCommonUsages = UnityEngine.InputSystem.CommonUsages;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace XRDiagnostics
{
    /// <summary>
    /// Logs left/right motion controller tracked state, left/right hand tracked state,
    /// whether both controller and hand tracking are active at the same moment, and
    /// controller rotation, speed, and velocity-valid flag once per second.
    /// </summary>
    public class XRSimultaneousTrackingDiagnostics : MonoBehaviour
    {
        private const float LogIntervalSeconds = 1.0f;

        [SerializeField]
        [Tooltip("Optional ControllerMotionData component to log rotation, speed, and isVelocityValid from.")]
        private ControllerMotionData m_ControllerMotion;

        private readonly List<XRHandSubsystem> m_HandSubsystems = new List<XRHandSubsystem>();
        private readonly List<XRInputDevice> m_XRInputDevices = new List<XRInputDevice>();
        private float m_Timer;

        private void Awake()
        {
            ResolveControllerMotionReference();
        }

        private void Start()
        {
            Debug.Log(
                $"[XRDiagnostics] Started on Platform={Application.platform} | Runtime={GetOpenXRRuntimeSummary()}");
        }

        private void Update()
        {
            m_Timer += Time.unscaledDeltaTime;
            if (m_Timer < LogIntervalSeconds)
                return;

            m_Timer -= LogIntervalSeconds;

            if (m_ControllerMotion == null)
            {
                ResolveControllerMotionReference();
            }

            bool leftControllerTracked = IsControllerTracked(
                InputSystemCommonUsages.LeftHand,
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left);

            bool rightControllerTracked = IsControllerTracked(
                InputSystemCommonUsages.RightHand,
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right);

            GetHandTrackedStates(out bool leftHandTracked, out bool rightHandTracked);

            bool anyControllerTracked = leftControllerTracked || rightControllerTracked;
            bool anyHandTracked = leftHandTracked || rightHandTracked;
            bool bothTrackedSimultaneously = anyControllerTracked && anyHandTracked;

            string motionSummary;
            if (m_ControllerMotion != null)
            {
                Vector3 euler = m_ControllerMotion.rotation.eulerAngles;
                motionSummary =
                    $"Motion({m_ControllerMotion.hand}: RotEuler=({euler.x:F1}, {euler.y:F1}, {euler.z:F1}), " +
                    $"Speed={m_ControllerMotion.speed:F3} m/s, VelocityValid={m_ControllerMotion.isVelocityValid})";
            }
            else
            {
                motionSummary = "Motion(None)";
            }

            Debug.Log(
                $"[XRDiagnostics] Platform={Application.platform} | Runtime={GetOpenXRRuntimeSummary()} | " +
                $"Controller(L={leftControllerTracked}, R={rightControllerTracked}) | " +
                $"Hand(L={leftHandTracked}, R={rightHandTracked}) | " +
                $"Simultaneous={bothTrackedSimultaneously} (L_Both={leftControllerTracked && leftHandTracked}, R_Both={rightControllerTracked && rightHandTracked}) | " +
                motionSummary);
        }

        private static string GetOpenXRRuntimeSummary()
        {
            try
            {
                string runtimeName = OpenXRRuntime.name;
                string runtimeVersion = OpenXRRuntime.version;
                if (!string.IsNullOrEmpty(runtimeName))
                {
                    return !string.IsNullOrEmpty(runtimeVersion)
                        ? $"{runtimeName} v{runtimeVersion}"
                        : runtimeName;
                }
            }
            catch
            {
                // Ignore if OpenXR runtime native library is not active.
            }

            return "Unknown/Inactive";
        }

        private void ResolveControllerMotionReference()
        {
            if (m_ControllerMotion != null)
                return;

            m_ControllerMotion = GetComponent<ControllerMotionData>();
            if (m_ControllerMotion == null)
            {
                m_ControllerMotion = FindFirstObjectByType<ControllerMotionData>();
            }
        }

        private bool IsControllerTracked(InternedString handUsage, InputDeviceCharacteristics controllerCharacteristics)
        {
            var devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is not XRController controller || !controller.added)
                    continue;

                if (IsNonMotionControllerDevice(controller))
                    continue;

                if (!controller.usages.Contains(handUsage))
                    continue;

                if (IsDeviceTracked(controller))
                    return true;
            }

            m_XRInputDevices.Clear();
            InputDevices.GetDevices(m_XRInputDevices);
            for (int i = 0; i < m_XRInputDevices.Count; i++)
            {
                XRInputDevice device = m_XRInputDevices[i];
                if (device.characteristics == controllerCharacteristics && IsXRDeviceTracked(device))
                    return true;
            }

            return false;
        }

        private static bool IsNonMotionControllerDevice(XRController device)
        {
#pragma warning disable CS0618
            return device is HandInteractionProfile.HandInteraction ||
                   device is MicrosoftHandInteraction.HoloLensHand ||
                   device is DPadInteraction.DPad ||
                   device is PalmPoseInteraction.PalmPose;
#pragma warning restore CS0618
        }

        private static bool IsDeviceTracked(TrackedDevice device)
        {
            if (device.isTracked.isPressed)
                return true;

            const InputTrackingState positionAndRotation = InputTrackingState.Position | InputTrackingState.Rotation;
            var trackingState = (InputTrackingState)device.trackingState.value;
            return (trackingState & positionAndRotation) == positionAndRotation;
        }

        private static bool IsXRDeviceTracked(XRInputDevice device)
        {
            if (!device.isValid)
                return false;

            if (device.TryGetFeatureValue(XRCommonUsages.isTracked, out bool isTracked) && isTracked)
                return true;

            const InputTrackingState positionAndRotation = InputTrackingState.Position | InputTrackingState.Rotation;
            return device.TryGetFeatureValue(XRCommonUsages.trackingState, out InputTrackingState trackingState) &&
                   (trackingState & positionAndRotation) == positionAndRotation;
        }

        private void GetHandTrackedStates(out bool leftHandTracked, out bool rightHandTracked)
        {
            leftHandTracked = false;
            rightHandTracked = false;

            m_HandSubsystems.Clear();
            SubsystemManager.GetSubsystems(m_HandSubsystems);

            for (int i = 0; i < m_HandSubsystems.Count; i++)
            {
                XRHandSubsystem subsystem = m_HandSubsystems[i];
                if (subsystem != null && subsystem.running)
                {
                    leftHandTracked = subsystem.leftHand.isTracked;
                    rightHandTracked = subsystem.rightHand.isTracked;
                    return;
                }
            }
        }
    }
}
