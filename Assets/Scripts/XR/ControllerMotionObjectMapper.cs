using HussienX.OpenXR.SimultaneousHandsAndControllers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace XRDiagnostics
{
    /// <summary>
    /// Maps rotation from <see cref="ControllerMotionData"/> onto a target 3D Object and updates
    /// a TextMeshPro label with the current status (<c>MOVING</c> or <c>STATIC</c>).
    /// Provides a <see cref="Recenter"/> function (callable via UI Button, controller input action,
    /// keyboard shortcut, or script) that resets the 3D object's rotation and adds subsequent
    /// controller rotation relative to the recentered orientation.
    /// </summary>
    [AddComponentMenu("XR/XR Diagnostics/Controller Motion Object Mapper")]
    public class ControllerMotionObjectMapper : MonoBehaviour
    {
        public enum ResetMode
        {
            [Tooltip("Reset to (0, 0, 0) Quaternion.identity.")]
            Identity = 0,

            [Tooltip("Reset to the 3D object's initial scene rotation on Awake.")]
            InitialSceneRotation = 1,

            [Tooltip("Reset to a custom Euler angle specified in CustomResetEuler.")]
            CustomEuler = 2
        }

        [Header("References")]
        [SerializeField]
        [Tooltip("Source ControllerMotionData component. If empty, searches the scene automatically.")]
        private ControllerMotionData m_ControllerMotion;

        [SerializeField]
        [Tooltip("The 3D object Transform to rotate. Defaults to this GameObject if empty.")]
        private Transform m_TargetObject;

        [SerializeField]
        [Tooltip("TextMeshPro component (3D TextMeshPro or UI TextMeshProUGUI) that displays MOVING / STATIC status.")]
        private TMP_Text m_StatusText;

        [SerializeField]
        [Tooltip("Optional UI Button that triggers Recenter() when clicked.")]
        private Button m_RecenterButton;

        [Header("Recenter Settings")]
        [SerializeField]
        [Tooltip("How the 3D object rotation is reset upon calling Recenter().")]
        private ResetMode m_ResetMode = ResetMode.Identity;

        [SerializeField]
        [Tooltip("Custom Euler rotation used when ResetMode is set to CustomEuler.")]
        private Vector3 m_CustomResetEuler = Vector3.zero;

        [SerializeField]
        [Tooltip("Optional controller InputAction to trigger Recenter (e.g. <XRController>{RightHand}/primaryButton).")]
        private InputActionProperty m_RecenterAction;

        [SerializeField]
        [Tooltip("Keyboard shortcut to trigger Recenter() in Editor Play Mode (default: R). Set to None to disable.")]
        private Key m_RecenterKey = Key.R;

        [Header("Motion Detection Thresholds")]
        [SerializeField]
        [Tooltip("Linear speed threshold (m/s) above which the controller is considered MOVING.")]
        private float m_LinearSpeedThreshold = 0.04f;

        [SerializeField]
        [Tooltip("Angular speed threshold (degrees/s) above which the controller is considered MOVING.")]
        private float m_AngularSpeedThresholdDeg = 5.0f;

        [SerializeField]
        [Tooltip("If true, shows speed metrics and object Euler angles below the status line.")]
        private bool m_ShowDetailedMetrics = true;

        private Quaternion m_BaseTargetRotation = Quaternion.identity;
        private Quaternion m_ReferenceControllerRotation = Quaternion.identity;
        private Quaternion m_PreviousControllerRotation = Quaternion.identity;
        private bool m_HasReferenceRotation;
        private bool m_HasPreviousRotation;

        /// <summary>
        /// True when the motion controller is tracked and its linear or angular speed exceeds the motion thresholds.
        /// </summary>
        public bool isMoving { get; private set; }

        /// <summary>
        /// Relative rotation applied from the controller since the last <see cref="Recenter"/>.
        /// </summary>
        public Quaternion relativeControllerRotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// Target 3D object being rotated.
        /// </summary>
        public Transform targetObject
        {
            get => m_TargetObject;
            set => m_TargetObject = value;
        }

        /// <summary>
        /// TextMeshPro component displaying the status.
        /// </summary>
        public TMP_Text statusText
        {
            get => m_StatusText;
            set => m_StatusText = value;
        }

        private void Awake()
        {
            if (m_TargetObject == null)
            {
                m_TargetObject = transform;
            }

            ComputeBaseRotation();

            if (m_ControllerMotion == null)
            {
                m_ControllerMotion = FindFirstObjectByType<ControllerMotionData>();
            }
        }

        private void OnEnable()
        {
            if (m_RecenterButton != null)
            {
                m_RecenterButton.onClick.AddListener(Recenter);
            }

            if (m_RecenterAction.action != null)
            {
                m_RecenterAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (m_RecenterButton != null)
            {
                m_RecenterButton.onClick.RemoveListener(Recenter);
            }

            if (m_RecenterAction.action != null)
            {
                m_RecenterAction.action.Disable();
            }
        }

        private void Update()
        {
            CheckRecenterInputs();

            if (m_ControllerMotion == null)
            {
                UpdateStatusText(tracked: false, moving: false, linearSpeed: 0f, angularSpeedDeg: 0f);
                return;
            }

            if (!m_ControllerMotion.isTracked)
            {
                isMoving = false;
                m_HasPreviousRotation = false;
                UpdateStatusText(tracked: false, moving: false, linearSpeed: 0f, angularSpeedDeg: 0f);
                return;
            }

            Quaternion currentControllerRotation = m_ControllerMotion.rotation;

            // Capture reference orientation on first valid frame
            if (!m_HasReferenceRotation)
            {
                m_ReferenceControllerRotation = currentControllerRotation;
                m_HasReferenceRotation = true;
            }

            // Compute relative rotation delta since recenter and add it to the base target rotation
            relativeControllerRotation = currentControllerRotation * Quaternion.Inverse(m_ReferenceControllerRotation);
            if (m_TargetObject != null)
            {
                m_TargetObject.rotation = relativeControllerRotation * m_BaseTargetRotation;
            }

            // Determine if moving or static from linear and angular speeds
            float linearSpeed = m_ControllerMotion.isVelocityValid ? m_ControllerMotion.speed : 0f;
            float reportedAngularSpeedDeg = m_ControllerMotion.angularVelocity.magnitude * Mathf.Rad2Deg;

            float deltaRotationSpeedDeg = 0f;
            if (m_HasPreviousRotation && Time.deltaTime > 0.0001f)
            {
                deltaRotationSpeedDeg = Quaternion.Angle(m_PreviousControllerRotation, currentControllerRotation) / Time.deltaTime;
            }

            m_PreviousControllerRotation = currentControllerRotation;
            m_HasPreviousRotation = true;

            float angularSpeedDeg = Mathf.Max(reportedAngularSpeedDeg, deltaRotationSpeedDeg);
            isMoving = (linearSpeed >= m_LinearSpeedThreshold) || (angularSpeedDeg >= m_AngularSpeedThresholdDeg);

            UpdateStatusText(tracked: true, moving: isMoving, linearSpeed: linearSpeed, angularSpeedDeg: angularSpeedDeg);
        }

        private void CheckRecenterInputs()
        {
            if (m_RecenterKey != Key.None &&
                Keyboard.current != null &&
                Keyboard.current[m_RecenterKey].wasPressedThisFrame)
            {
                Recenter();
                return;
            }

            if (m_RecenterAction.action != null && m_RecenterAction.action.WasPressedThisFrame())
            {
                Recenter();
            }
        }

        /// <summary>
        /// Resets the 3D object rotation to its base rotation and recalibrates the controller reference
        /// orientation so all subsequent rotation from the controller is added from this point.
        /// Can be wired directly to a UI Button's <c>OnClick</c> event.
        /// </summary>
        [ContextMenu("Recenter Rotation")]
        public void Recenter()
        {
            ComputeBaseRotation();

            if (m_TargetObject != null)
            {
                m_TargetObject.rotation = m_BaseTargetRotation;
            }

            if (m_ControllerMotion != null && m_ControllerMotion.isTracked)
            {
                m_ReferenceControllerRotation = m_ControllerMotion.rotation;
                m_HasReferenceRotation = true;
            }
            else
            {
                m_HasReferenceRotation = false;
            }

            relativeControllerRotation = Quaternion.identity;
        }

        private void ComputeBaseRotation()
        {
            switch (m_ResetMode)
            {
                case ResetMode.Identity:
                    m_BaseTargetRotation = Quaternion.identity;
                    break;
                case ResetMode.InitialSceneRotation:
                    if (m_TargetObject != null)
                        m_BaseTargetRotation = m_TargetObject.rotation;
                    break;
                case ResetMode.CustomEuler:
                    m_BaseTargetRotation = Quaternion.Euler(m_CustomResetEuler);
                    break;
            }
        }

        private void UpdateStatusText(bool tracked, bool moving, float linearSpeed, float angularSpeedDeg)
        {
            if (m_StatusText == null)
            {
                return;
            }

            if (!tracked)
            {
                m_StatusText.text = "<color=#FF6B6B><b>Status: UNTRACKED</b></color>";
                return;
            }

            string stateLabel = moving
                ? "<color=#4CAF50><b>Status: MOVING</b></color>"
                : "<color=#FFD54F><b>Status: STATIC</b></color>";

            if (!m_ShowDetailedMetrics)
            {
                m_StatusText.text = stateLabel;
                return;
            }

            Vector3 euler = m_TargetObject != null ? m_TargetObject.eulerAngles : relativeControllerRotation.eulerAngles;
            m_StatusText.text =
                $"{stateLabel}\n" +
                $"<size=75%>Linear: {linearSpeed:F2} m/s | Angular: {angularSpeedDeg:F1} deg/s\n" +
                $"Rotation: ({euler.x:F1}\u00b0, {euler.y:F1}\u00b0, {euler.z:F1}\u00b0)</size>";
        }
    }
}
