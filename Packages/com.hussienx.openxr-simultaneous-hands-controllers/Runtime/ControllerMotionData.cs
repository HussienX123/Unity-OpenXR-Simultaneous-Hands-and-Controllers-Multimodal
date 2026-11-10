using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using InputSystemCommonUsages = UnityEngine.InputSystem.CommonUsages;
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    /// <summary>
    /// Reads motion controller rotation, forward direction, linear velocity, angular velocity, speed,
    /// and tracking state every frame for a selectable hand (Left or Right).
    /// </summary>
    public class ControllerMotionData : MonoBehaviour
    {
        public enum ControllerHand
        {
            Left = 0,
            Right = 1
        }

        public enum PoseSource
        {
            /// <summary>
            /// Matches XRI's primary controller pose binding (&lt;XRController&gt;/pointerRotation, /input/aim/pose)
            /// with fallback to grip/devicePose (/input/grip/pose).
            /// </summary>
            PointerAim = 0,

            /// <summary>
            /// Uses the controller grip pose (&lt;XRController&gt;/deviceRotation, /input/grip/pose).
            /// </summary>
            DeviceGrip = 1
        }

        [SerializeField]
        [Tooltip("Which controller hand to read (Left or Right).")]
        private ControllerHand m_Hand = ControllerHand.Right;

        [SerializeField]
        [Tooltip("Pose source on the controller. PointerAim matches the XR Origin controller TrackedPoseDriver primary binding.")]
        private PoseSource m_PoseSource = PoseSource.PointerAim;

        private readonly List<XRInputDevice> m_XRInputDevices = new List<XRInputDevice>();
        private InputAction m_DevicePoseAction;
        private InputAction m_PointerPoseAction;
        private ControllerHand m_BoundHand;

        /// <summary>
        /// Selected controller hand (Left or Right).
        /// </summary>
        public ControllerHand hand
        {
            get => m_Hand;
            set
            {
                if (m_Hand != value)
                {
                    m_Hand = value;
                    if (isActiveAndEnabled)
                    {
                        BindPoseActions();
                    }
                }
            }
        }

        /// <summary>
        /// Selected pose source (PointerAim or DeviceGrip).
        /// </summary>
        public PoseSource poseSource
        {
            get => m_PoseSource;
            set => m_PoseSource = value;
        }

        /// <summary>
        /// Controller rotation in tracking space for the current frame.
        /// </summary>
        public Quaternion rotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// Forward direction vector (rotation * Vector3.forward) for the current frame.
        /// </summary>
        public Vector3 forward { get; private set; } = Vector3.forward;

        /// <summary>
        /// Controller linear velocity (m/s) in tracking space. Zero when <see cref="isVelocityValid"/> is false.
        /// </summary>
        public Vector3 linearVelocity { get; private set; } = Vector3.zero;

        /// <summary>
        /// Controller angular velocity (rad/s) in tracking space. Zero when <see cref="isTracked"/> is false.
        /// </summary>
        public Vector3 angularVelocity { get; private set; } = Vector3.zero;

        /// <summary>
        /// Linear speed (magnitude of <see cref="linearVelocity"/> in m/s). Zero when <see cref="isVelocityValid"/> is false.
        /// </summary>
        public float speed { get; private set; }

        /// <summary>
        /// Current <see cref="InputTrackingState"/> flags reported by the controller.
        /// </summary>
        public InputTrackingState trackingState { get; private set; } = InputTrackingState.None;

        /// <summary>
        /// True when the motion controller is actively tracked.
        /// </summary>
        public bool isTracked { get; private set; }

        /// <summary>
        /// True when the controller is tracked and <see cref="trackingState"/> includes <see cref="InputTrackingState.Position"/>.
        /// False whenever position is not tracked so caller code can ignore invalid velocity.
        /// </summary>
        public bool isVelocityValid { get; private set; }

        private void OnEnable()
        {
            BindPoseActions();
            SampleController();
        }

        private void OnDisable()
        {
            DisposePoseActions();
            ClearMotionData();
        }

        private void Update()
        {
            if (m_BoundHand != m_Hand)
            {
                BindPoseActions();
            }

            SampleController();
        }

        private void BindPoseActions()
        {
            DisposePoseActions();
            m_BoundHand = m_Hand;

            string handUsage = m_Hand == ControllerHand.Left ? "{LeftHand}" : "{RightHand}";

            // Enabling actions bound to devicePose and pointer ensures the OpenXR Input System backend
            // updates the full PoseState (including velocity and angularVelocity) every frame.
            m_DevicePoseAction = new InputAction(
                name: $"{m_Hand}ControllerDevicePose",
                type: InputActionType.Value,
                binding: $"<XRControllerWithRumble>{handUsage}/devicePose",
                expectedControlType: "Pose");

            m_PointerPoseAction = new InputAction(
                name: $"{m_Hand}ControllerPointerPose",
                type: InputActionType.Value,
                binding: $"<XRControllerWithRumble>{handUsage}/pointer",
                expectedControlType: "Pose");

            m_DevicePoseAction.Enable();
            m_PointerPoseAction.Enable();
        }

        private void DisposePoseActions()
        {
            if (m_DevicePoseAction != null)
            {
                m_DevicePoseAction.Disable();
                m_DevicePoseAction.Dispose();
                m_DevicePoseAction = null;
            }

            if (m_PointerPoseAction != null)
            {
                m_PointerPoseAction.Disable();
                m_PointerPoseAction.Dispose();
                m_PointerPoseAction = null;
            }
        }

        private void SampleController()
        {
            InternedString handUsage = m_Hand == ControllerHand.Left
                ? InputSystemCommonUsages.LeftHand
                : InputSystemCommonUsages.RightHand;

            InputDeviceCharacteristics handCharacteristic = m_Hand == ControllerHand.Left
                ? InputDeviceCharacteristics.Left
                : InputDeviceCharacteristics.Right;

            InputDeviceCharacteristics controllerCharacteristics =
                InputDeviceCharacteristics.HeldInHand |
                InputDeviceCharacteristics.TrackedDevice |
                InputDeviceCharacteristics.Controller |
                handCharacteristic;

            if (TrySampleFromInputSystem(handUsage) || TrySampleFromXRInputDevices(controllerCharacteristics))
            {
                return;
            }

            ClearMotionData();
        }

        private bool TrySampleFromInputSystem(InternedString handUsage)
        {
            var devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is not XRController controller || !controller.added)
                {
                    continue;
                }

                if (IsNonMotionControllerDevice(controller))
                {
                    continue;
                }

                if (!controller.usages.Contains(handUsage))
                {
                    continue;
                }

                var devicePose = controller.TryGetChildControl<PoseControl>("devicePose");
                var pointerPose = controller.TryGetChildControl<PoseControl>("pointer");

                InputTrackingState deviceTrackingState = devicePose != null
                    ? (InputTrackingState)devicePose.trackingState.ReadValue()
                    : (InputTrackingState)controller.trackingState.ReadValue();

                InputTrackingState pointerTrackingState = pointerPose != null
                    ? (InputTrackingState)pointerPose.trackingState.ReadValue()
                    : InputTrackingState.None;

                InputTrackingState combinedTrackingState = deviceTrackingState | pointerTrackingState;

                bool deviceIsTracked = (devicePose != null && devicePose.isTracked.isPressed) ||
                                       controller.isTracked.isPressed ||
                                       ((combinedTrackingState & (InputTrackingState.Position | InputTrackingState.Rotation)) != 0);

                if (!deviceIsTracked && combinedTrackingState == InputTrackingState.None)
                {
                    continue;
                }

                // Read rotation from the same pose source as the XR Origin's controller TrackedPoseDriver:
                // XRI Left/Right Rotation uses QuaternionFallback:
                // 1) <XRController>/pointerRotation (pointer/rotation)
                // 2) <XRController>/deviceRotation (devicePose/rotation)
                Quaternion sampledRotation = Quaternion.identity;
                if (m_PoseSource == PoseSource.PointerAim && pointerPose != null &&
                    (pointerPose.isTracked.isPressed || (pointerTrackingState & InputTrackingState.Rotation) != 0))
                {
                    sampledRotation = pointerPose.rotation.ReadValue();
                }
                else if (devicePose != null)
                {
                    sampledRotation = devicePose.rotation.ReadValue();
                }
                else
                {
                    sampledRotation = controller.deviceRotation.ReadValue();
                }

                // Read linear and angular velocity from PoseControl (devicePose primary, pointer fallback)
                Vector3 rawLinearVelocity = Vector3.zero;
                if (devicePose != null && (deviceTrackingState & InputTrackingState.Velocity) != 0)
                {
                    rawLinearVelocity = devicePose.velocity.ReadValue();
                }
                else if (pointerPose != null && (pointerTrackingState & InputTrackingState.Velocity) != 0)
                {
                    rawLinearVelocity = pointerPose.velocity.ReadValue();
                }
                else if (devicePose != null)
                {
                    rawLinearVelocity = devicePose.velocity.ReadValue();
                }

                Vector3 rawAngularVelocity = Vector3.zero;
                if (devicePose != null && (deviceTrackingState & InputTrackingState.AngularVelocity) != 0)
                {
                    rawAngularVelocity = devicePose.angularVelocity.ReadValue();
                }
                else if (pointerPose != null && (pointerTrackingState & InputTrackingState.AngularVelocity) != 0)
                {
                    rawAngularVelocity = pointerPose.angularVelocity.ReadValue();
                }
                else if (devicePose != null)
                {
                    rawAngularVelocity = devicePose.angularVelocity.ReadValue();
                }

                // Treat velocity as invalid whenever trackingState says position is not tracked
                bool hasPositionTracking = (combinedTrackingState & InputTrackingState.Position) != 0;
                bool velocityValid = deviceIsTracked && hasPositionTracking;

                trackingState = combinedTrackingState;
                isTracked = deviceIsTracked;
                rotation = sampledRotation;
                forward = sampledRotation * Vector3.forward;
                isVelocityValid = velocityValid;
                linearVelocity = velocityValid ? rawLinearVelocity : Vector3.zero;
                angularVelocity = deviceIsTracked ? rawAngularVelocity : Vector3.zero;
                speed = velocityValid ? rawLinearVelocity.magnitude : 0f;
                return true;
            }

            return false;
        }

        private bool TrySampleFromXRInputDevices(InputDeviceCharacteristics controllerCharacteristics)
        {
            m_XRInputDevices.Clear();
            InputDevices.GetDevices(m_XRInputDevices);

            for (int i = 0; i < m_XRInputDevices.Count; i++)
            {
                XRInputDevice device = m_XRInputDevices[i];
                if (!device.isValid || device.characteristics != controllerCharacteristics)
                {
                    continue;
                }

                device.TryGetFeatureValue(XRCommonUsages.trackingState, out InputTrackingState state);
                bool tracked = (device.TryGetFeatureValue(XRCommonUsages.isTracked, out bool isTrackedFlag) && isTrackedFlag) ||
                               (state & (InputTrackingState.Position | InputTrackingState.Rotation)) != 0;

                if (!tracked && state == InputTrackingState.None)
                {
                    continue;
                }

                if (!device.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rot))
                {
                    rot = Quaternion.identity;
                }

                bool hasVelocityFeature = device.TryGetFeatureValue(XRCommonUsages.deviceVelocity, out Vector3 vel);
                device.TryGetFeatureValue(XRCommonUsages.deviceAngularVelocity, out Vector3 angVel);

                bool hasPositionTracking = (state & InputTrackingState.Position) != 0;
                bool velocityValid = tracked && hasPositionTracking && hasVelocityFeature;

                trackingState = state;
                isTracked = tracked;
                rotation = rot;
                forward = rot * Vector3.forward;
                isVelocityValid = velocityValid;
                linearVelocity = velocityValid ? vel : Vector3.zero;
                angularVelocity = tracked ? angVel : Vector3.zero;
                speed = velocityValid ? vel.magnitude : 0f;
                return true;
            }

            return false;
        }

        private void ClearMotionData()
        {
            rotation = Quaternion.identity;
            forward = Vector3.forward;
            linearVelocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            speed = 0f;
            trackingState = InputTrackingState.None;
            isTracked = false;
            isVelocityValid = false;
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
    }
}
