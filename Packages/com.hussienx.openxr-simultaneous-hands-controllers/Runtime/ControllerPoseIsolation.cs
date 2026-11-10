using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using InputSystemCommonUsages = UnityEngine.InputSystem.CommonUsages;
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;

namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    /// <summary>
    /// Isolates a controller hierarchy's <see cref="TrackedPoseDriver"/> so that it only reads from physical
    /// motion controllers (<see cref="XRControllerWithRumble"/>) and never falls back to hand-tracking devices
    /// (<c>XRHandDevice</c> or <c>HandInteraction</c>). When a physical controller is set down and loses
    /// position tracking or enters standby, preserves its last valid tracked local pose in space.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ControllerPoseIsolation : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Which controller hand this hierarchy represents (Left or Right).")]
        private ControllerMotionData.ControllerHand m_Hand = ControllerMotionData.ControllerHand.Left;

        private TrackedPoseDriver m_TrackedPoseDriver;
        private InputAction m_PositionAction;
        private InputAction m_RotationAction;
        private InputAction m_TrackingStateAction;
        private Vector3 m_LastValidLocalPosition;
        private Quaternion m_LastValidLocalRotation = Quaternion.identity;
        private bool m_HasValidPose;
        private bool m_InitialTransformCaptured;
        private Vector3 m_InitialLocalPosition;
        private Quaternion m_InitialLocalRotation = Quaternion.identity;

        /// <summary>
        /// Which controller hand this component isolates (Left or Right).
        /// </summary>
        public ControllerMotionData.ControllerHand hand
        {
            get => m_Hand;
            set
            {
                if (m_Hand != value)
                {
                    m_Hand = value;
                    if (isActiveAndEnabled)
                    {
                        BindControllerOnlyActions();
                    }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachToSceneControllers();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachToSceneControllers();
        }

        private static void AttachToSceneControllers()
        {
            TrackedPoseDriver[] poseDrivers = FindObjectsByType<TrackedPoseDriver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < poseDrivers.Length; i++)
            {
                TrackedPoseDriver driver = poseDrivers[i];
                if (driver == null || driver.GetComponent<ControllerPoseIsolation>() != null)
                {
                    continue;
                }

                if (TryDetectControllerHand(driver, out ControllerMotionData.ControllerHand detectedHand))
                {
                    ControllerPoseIsolation isolation = driver.gameObject.AddComponent<ControllerPoseIsolation>();
                    isolation.m_Hand = detectedHand;
                    if (isolation.isActiveAndEnabled)
                    {
                        isolation.BindControllerOnlyActions();
                    }
                }
            }
        }

        private static bool TryDetectControllerHand(TrackedPoseDriver driver, out ControllerMotionData.ControllerHand detectedHand)
        {
            detectedHand = ControllerMotionData.ControllerHand.Left;
            string objectName = driver.gameObject.name;
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            if (objectName.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (objectName.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                detectedHand = ControllerMotionData.ControllerHand.Left;
                return true;
            }

            if (objectName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                detectedHand = ControllerMotionData.ControllerHand.Right;
                return true;
            }

            return false;
        }

        private void Awake()
        {
            CaptureInitialTransform();
        }

        private void OnEnable()
        {
            CaptureInitialTransform();
            m_TrackedPoseDriver = GetComponent<TrackedPoseDriver>();
            BindControllerOnlyActions();

            InputSystem.onAfterUpdate -= OnAfterInputUpdate;
            InputSystem.onAfterUpdate += OnAfterInputUpdate;
            Application.onBeforeRender -= OnBeforeRenderPoseLock;
            Application.onBeforeRender += OnBeforeRenderPoseLock;

            ApplyControllerPoseLock();
        }

        private void OnDisable()
        {
            InputSystem.onAfterUpdate -= OnAfterInputUpdate;
            Application.onBeforeRender -= OnBeforeRenderPoseLock;
            DisposeActions();
        }

        private void LateUpdate()
        {
            ApplyControllerPoseLock();
        }

        private void OnAfterInputUpdate()
        {
            ApplyControllerPoseLock();
        }

        [BeforeRenderOrder(1000)]
        private void OnBeforeRenderPoseLock()
        {
            ApplyControllerPoseLock();
        }

        private void CaptureInitialTransform()
        {
            if (m_InitialTransformCaptured)
            {
                return;
            }

            m_InitialLocalPosition = transform.localPosition;
            m_InitialLocalRotation = transform.localRotation;
            m_InitialTransformCaptured = true;
        }

        private void BindControllerOnlyActions()
        {
            DisposeActions();

            string handUsage = m_Hand == ControllerMotionData.ControllerHand.Left ? "{LeftHand}" : "{RightHand}";

            m_PositionAction = new InputAction(
                name: $"{m_Hand}MotionControllerPosition",
                type: InputActionType.Value,
                expectedControlType: "Vector3");
            m_PositionAction.AddCompositeBinding("Vector3Fallback")
                .With("first", $"<XRControllerWithRumble>{handUsage}/pointerPosition")
                .With("second", $"<XRControllerWithRumble>{handUsage}/devicePosition");

            m_RotationAction = new InputAction(
                name: $"{m_Hand}MotionControllerRotation",
                type: InputActionType.Value,
                expectedControlType: "Quaternion");
            m_RotationAction.AddCompositeBinding("QuaternionFallback")
                .With("first", $"<XRControllerWithRumble>{handUsage}/pointerRotation")
                .With("second", $"<XRControllerWithRumble>{handUsage}/deviceRotation");

            m_TrackingStateAction = new InputAction(
                name: $"{m_Hand}MotionControllerTrackingState",
                type: InputActionType.Value,
                binding: $"<XRControllerWithRumble>{handUsage}/trackingState",
                expectedControlType: "Integer");

            if (m_TrackedPoseDriver == null)
            {
                m_TrackedPoseDriver = GetComponent<TrackedPoseDriver>();
            }

            if (m_TrackedPoseDriver != null)
            {
                m_TrackedPoseDriver.positionInput = new InputActionProperty(m_PositionAction);
                m_TrackedPoseDriver.rotationInput = new InputActionProperty(m_RotationAction);
                m_TrackedPoseDriver.trackingStateInput = new InputActionProperty(m_TrackingStateAction);
            }

            m_PositionAction.Enable();
            m_RotationAction.Enable();
            m_TrackingStateAction.Enable();
        }

        private void DisposeActions()
        {
            if (m_PositionAction != null)
            {
                m_PositionAction.Disable();
                m_PositionAction.Dispose();
                m_PositionAction = null;
            }

            if (m_RotationAction != null)
            {
                m_RotationAction.Disable();
                m_RotationAction.Dispose();
                m_RotationAction = null;
            }

            if (m_TrackingStateAction != null)
            {
                m_TrackingStateAction.Disable();
                m_TrackingStateAction.Dispose();
                m_TrackingStateAction = null;
            }
        }

        private void ApplyControllerPoseLock()
        {
            if (TrySampleMotionControllerPose(out Vector3 position, out Quaternion rotation, out bool hasValidPosition, out bool hasValidRotation))
            {
                if (hasValidPosition)
                {
                    m_LastValidLocalPosition = position;
                    if (hasValidRotation)
                    {
                        m_LastValidLocalRotation = rotation;
                    }

                    m_HasValidPose = true;
                    transform.SetLocalPositionAndRotation(m_LastValidLocalPosition, m_LastValidLocalRotation);
                    return;
                }
            }

            if (m_HasValidPose)
            {
                transform.SetLocalPositionAndRotation(m_LastValidLocalPosition, m_LastValidLocalRotation);
            }
            else if (m_InitialTransformCaptured)
            {
                transform.SetLocalPositionAndRotation(m_InitialLocalPosition, m_InitialLocalRotation);
            }
        }

        private bool TrySampleMotionControllerPose(
            out Vector3 sampledPosition,
            out Quaternion sampledRotation,
            out bool hasValidPosition,
            out bool hasValidRotation)
        {
            sampledPosition = Vector3.zero;
            sampledRotation = Quaternion.identity;
            hasValidPosition = false;
            hasValidRotation = false;

            InternedString handUsage = m_Hand == ControllerMotionData.ControllerHand.Left
                ? InputSystemCommonUsages.LeftHand
                : InputSystemCommonUsages.RightHand;

            var devices = InputSystem.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is not XRControllerWithRumble controller || !controller.added)
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

                var pointerPose = controller.TryGetChildControl<PoseControl>("pointer");
                var devicePose = controller.TryGetChildControl<PoseControl>("devicePose");

                InputTrackingState pointerState = pointerPose != null
                    ? (InputTrackingState)pointerPose.trackingState.ReadValue()
                    : InputTrackingState.None;

                InputTrackingState deviceState = devicePose != null
                    ? (InputTrackingState)devicePose.trackingState.ReadValue()
                    : (InputTrackingState)controller.trackingState.ReadValue();

                InputTrackingState combinedState = pointerState | deviceState;
                bool isTracked = (pointerPose != null && pointerPose.isTracked.isPressed) ||
                                 (devicePose != null && devicePose.isTracked.isPressed) ||
                                 controller.isTracked.isPressed ||
                                 (combinedState & (InputTrackingState.Position | InputTrackingState.Rotation)) != 0;

                if (!isTracked)
                {
                    continue;
                }

                if (pointerPose != null && (pointerState & InputTrackingState.Position) != 0)
                {
                    sampledPosition = pointerPose.position.ReadValue();
                }
                else if (devicePose != null && (deviceState & InputTrackingState.Position) != 0)
                {
                    sampledPosition = devicePose.position.ReadValue();
                }
                else
                {
                    sampledPosition = controller.devicePosition.ReadValue();
                }

                if (pointerPose != null && (pointerState & InputTrackingState.Rotation) != 0)
                {
                    sampledRotation = pointerPose.rotation.ReadValue();
                }
                else if (devicePose != null && (deviceState & InputTrackingState.Rotation) != 0)
                {
                    sampledRotation = devicePose.rotation.ReadValue();
                }
                else
                {
                    sampledRotation = controller.deviceRotation.ReadValue();
                }

                hasValidPosition = (combinedState & InputTrackingState.Position) != 0 &&
                                   sampledPosition.sqrMagnitude > 1e-6f;
                hasValidRotation = (combinedState & InputTrackingState.Rotation) != 0;
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
    }
}
