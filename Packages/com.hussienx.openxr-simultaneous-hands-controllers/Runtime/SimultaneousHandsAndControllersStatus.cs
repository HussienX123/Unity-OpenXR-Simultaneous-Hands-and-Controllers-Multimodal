using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    /// <summary>
    /// Runtime status component exposing whether <c>XR_META_simultaneous_hands_and_controllers</c>
    /// is enabled, whether the OpenXR system reports support, and whether session resume succeeded.
    /// </summary>
    public class SimultaneousHandsAndControllersStatus : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("True when OpenXRRuntime.IsExtensionEnabled reported XR_META_simultaneous_hands_and_controllers as enabled.")]
        private bool m_ExtensionEnabled;

        [SerializeField]
        [Tooltip("True when xrGetSystemProperties reported supportsSimultaneousHandsAndControllers != 0.")]
        private bool m_SystemReportsSupport;

        [SerializeField]
        [Tooltip("True when xrResumeSimultaneousHandsAndControllersTrackingMETA returned XrResult.Success.")]
        private bool m_ResumeSucceeded;

        /// <summary>
        /// True if the active OpenXR runtime enabled <c>XR_META_simultaneous_hands_and_controllers</c>.
        /// </summary>
        public bool IsExtensionEnabled => m_ExtensionEnabled;

        /// <summary>
        /// True if <c>xrGetSystemProperties</c> reported <c>supportsSimultaneousHandsAndControllers != 0</c>.
        /// </summary>
        public bool SystemReportsSupport => m_SystemReportsSupport;

        /// <summary>
        /// True if <c>xrResumeSimultaneousHandsAndControllersTrackingMETA</c> succeeded for the active session.
        /// </summary>
        public bool ResumeSucceeded => m_ResumeSucceeded;

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void Update()
        {
            RefreshStatus();
        }

        /// <summary>
        /// Queries the active <see cref="MetaSimultaneousHandsAndControllersFeature"/> instance and updates the status properties.
        /// </summary>
        public void RefreshStatus()
        {
            var settings = OpenXRSettings.Instance;
            if (settings == null)
            {
                m_ExtensionEnabled = false;
                m_SystemReportsSupport = false;
                m_ResumeSucceeded = false;
                return;
            }

            var feature = settings.GetFeature<MetaSimultaneousHandsAndControllersFeature>();
            if (feature == null || !feature.enabled)
            {
                m_ExtensionEnabled = false;
                m_SystemReportsSupport = false;
                m_ResumeSucceeded = false;
                return;
            }

            m_ExtensionEnabled = feature.IsExtensionEnabled;
            m_SystemReportsSupport = feature.SupportsSimultaneousTracking;
            m_ResumeSucceeded = feature.IsSimultaneousTrackingActive;
        }
    }
}
