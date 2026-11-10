using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace HussienX.OpenXR.SimultaneousHandsAndControllers
{
    /// <summary>
    /// Custom OpenXR feature that enables <c>XR_META_simultaneous_hands_and_controllers</c> on Android (Meta Quest)
    /// and Standalone (Windows Editor / PC over Meta Quest Link, if supported by the active OpenXR runtime).
    /// Queries system support via <c>xrGetSystemProperties</c> and calls
    /// <c>xrResumeSimultaneousHandsAndControllersTrackingMETA</c> when the OpenXR session begins.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "Meta Simultaneous Hands and Controllers",
        BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
        Company = "HussienX",
        Desc = "Enables XR_META_simultaneous_hands_and_controllers so hand tracking and controllers run simultaneously when supported by the active OpenXR runtime.",
        OpenxrExtensionStrings = ExtensionName,
        Version = "0.1.0-experimental",
        Category = FeatureCategory.Feature,
        FeatureId = FeatureId)]
#endif
    public class MetaSimultaneousHandsAndControllersFeature : OpenXRFeature
    {
        public const string FeatureId = "com.hussienx.openxr.feature.simultaneoushandsandcontrollers";
        public const string ExtensionName = "XR_META_simultaneous_hands_and_controllers";

        private const string LogPrefix = "[MetaSimultaneousHandsAndControllersFeature]";
        private const int MaxSystemNameSize = 256;

        // Verified in openxr_reflection_full.h lines 2839-2842
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrSystemGraphicsProperties
        {
            public uint maxSwapchainImageHeight;
            public uint maxSwapchainImageWidth;
            public uint maxLayerCount;
        }

        // Verified in openxr_reflection_full.h lines 2845-2847
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrSystemTrackingProperties
        {
            public uint orientationTracking;
            public uint positionTracking;
        }

        // 1-byte-aligned 256-byte inline buffer matching char systemName[XR_MAX_SYSTEM_NAME_SIZE]
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = MaxSystemNameSize)]
        internal struct XrSystemNameBuffer
        {
            private byte m_Byte0;
        }

        // Verified in openxr_reflection_full.h lines 2850-2857 & XrStructureType.cs line 41 (SystemProperties = 5)
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrSystemProperties
        {
            public XrStructureType type;
            public IntPtr next;
            public ulong systemId;
            public uint vendorId;
            public XrSystemNameBuffer systemName;
            public XrSystemGraphicsProperties graphicsProperties;
            public XrSystemTrackingProperties trackingProperties;
        }

        // Verified in openxr_reflection_full.h lines 6800-6803 & XrStructureType.cs line 3184 (1000532001)
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrSystemSimultaneousHandsAndControllersPropertiesMETA
        {
            public XrStructureType type;
            public IntPtr next;
            public uint supportsSimultaneousHandsAndControllers;
        }

        // Verified in openxr_reflection_full.h lines 6806-6808 & XrStructureType.cs line 3191 (1000532002)
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrSimultaneousHandsAndControllersTrackingResumeInfoMETA
        {
            public XrStructureType type;
            public IntPtr next;
        }

        // Verified in openxr_reflection_full.h lines 6811-6813 & XrStructureType.cs line 3198 (1000532003)
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrSimultaneousHandsAndControllersTrackingPauseInfoMETA
        {
            public XrStructureType type;
            public IntPtr next;
        }

        // Verified in openxr_reflection_full.h line 12071
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate XrResult PFN_xrGetInstanceProcAddr(
            ulong instance,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            out IntPtr function);

        // Verified in openxr_reflection_full.h line 12100
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate XrResult PFN_xrGetSystemProperties(
            ulong instance,
            ulong systemId,
            ref XrSystemProperties properties);

        // Verified in openxr_reflection_full.h line 12463
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate XrResult PFN_xrResumeSimultaneousHandsAndControllersTrackingMETA(
            ulong session,
            ref XrSimultaneousHandsAndControllersTrackingResumeInfoMETA resumeInfo);

        // Verified in openxr_reflection_full.h line 12464
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate XrResult PFN_xrPauseSimultaneousHandsAndControllersTrackingMETA(
            ulong session,
            ref XrSimultaneousHandsAndControllersTrackingPauseInfoMETA pauseInfo);

        private ulong m_XrInstance;
        private ulong m_XrSystemId;
        private ulong m_XrSession;
        private bool m_ExtensionEnabled;
        private bool m_SystemSupportQueried;
        private bool m_SupportsSimultaneousTracking;
        private bool m_IsSimultaneousTrackingActive;

        private PFN_xrGetSystemProperties m_GetSystemProperties;
        private PFN_xrResumeSimultaneousHandsAndControllersTrackingMETA m_ResumeSimultaneousTracking;
        private PFN_xrPauseSimultaneousHandsAndControllersTrackingMETA m_PauseSimultaneousTracking;

        /// <summary>
        /// True if the active OpenXR runtime enabled <c>XR_META_simultaneous_hands_and_controllers</c>.
        /// </summary>
        public bool IsExtensionEnabled => m_ExtensionEnabled;

        /// <summary>
        /// True if <c>xrGetSystemProperties</c> reported <c>supportsSimultaneousHandsAndControllers != 0</c>.
        /// </summary>
        public bool SupportsSimultaneousTracking => m_SupportsSimultaneousTracking;

        /// <summary>
        /// True if <c>xrResumeSimultaneousHandsAndControllersTrackingMETA</c> succeeded for the current session.
        /// </summary>
        public bool IsSimultaneousTrackingActive => m_IsSimultaneousTrackingActive;

        /// <inheritdoc />
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            ResetState();
            m_XrInstance = xrInstance;

            try
            {
                string runtimeDesc = GetActiveRuntimeDescription();

                if (!OpenXRRuntime.IsExtensionEnabled(ExtensionName))
                {
                    Debug.LogWarning(
                        $"{LogPrefix} OpenXR extension '{ExtensionName}' is not enabled or not supported by the active OpenXR runtime ({runtimeDesc}, Platform={Application.platform}). " +
                        "Simultaneous hand and controller tracking will remain inactive.");
                    return true;
                }

                IntPtr procAddrPtr = xrGetInstanceProcAddr;
                if (procAddrPtr == IntPtr.Zero)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} xrGetInstanceProcAddr pointer is null on active OpenXR runtime ({runtimeDesc}). Cannot resolve '{ExtensionName}' functions.");
                    return true;
                }

                var getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<PFN_xrGetInstanceProcAddr>(procAddrPtr);

                if (!TryResolveFunction(getInstanceProcAddr, xrInstance, "xrGetSystemProperties", out m_GetSystemProperties) ||
                    !TryResolveFunction(getInstanceProcAddr, xrInstance, "xrResumeSimultaneousHandsAndControllersTrackingMETA", out m_ResumeSimultaneousTracking))
                {
                    return true;
                }

                // Pause is optional convenience; resolve it without failing if absent.
                TryResolveFunction(getInstanceProcAddr, xrInstance, "xrPauseSimultaneousHandsAndControllersTrackingMETA", out m_PauseSimultaneousTracking);

                m_ExtensionEnabled = true;
                Debug.Log(
                    $"{LogPrefix} Extension '{ExtensionName}' is enabled on active OpenXR runtime ({runtimeDesc}, Platform={Application.platform}) and functions were resolved for XrInstance={xrInstance}.");

                if (m_XrSystemId != 0)
                {
                    QuerySystemSupport();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} Failed during OnInstanceCreate: {ex.Message}");
                m_ExtensionEnabled = false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override void OnSystemChange(ulong xrSystem)
        {
            m_XrSystemId = xrSystem;
            m_SystemSupportQueried = false;
            m_SupportsSimultaneousTracking = false;

            if (m_ExtensionEnabled && m_XrInstance != 0 && m_XrSystemId != 0)
            {
                QuerySystemSupport();
            }
        }

        /// <inheritdoc />
        protected override void OnSessionBegin(ulong xrSession)
        {
            m_XrSession = xrSession;
            m_IsSimultaneousTrackingActive = false;

            if (!m_ExtensionEnabled)
            {
                return;
            }

            if (!m_SystemSupportQueried && m_XrInstance != 0 && m_XrSystemId != 0)
            {
                QuerySystemSupport();
            }

            if (!m_SupportsSimultaneousTracking)
            {
                return;
            }

            if (m_ResumeSimultaneousTracking == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Skipping xrResumeSimultaneousHandsAndControllersTrackingMETA because function pointer is null.");
                return;
            }

            try
            {
                var resumeInfo = new XrSimultaneousHandsAndControllersTrackingResumeInfoMETA
                {
                    type = XrStructureType.SimultaneousHandsAndControllersTrackingResumeInfoMETA,
                    next = IntPtr.Zero
                };

                XrResult result = m_ResumeSimultaneousTracking(xrSession, ref resumeInfo);
                m_IsSimultaneousTrackingActive = result == XrResult.Success;

                if (result == XrResult.Success)
                {
                    Debug.Log(
                        $"{LogPrefix} xrResumeSimultaneousHandsAndControllersTrackingMETA succeeded (XrResult={result}) for XrSession={xrSession}.");
                }
                else
                {
                    Debug.LogWarning(
                        $"{LogPrefix} xrResumeSimultaneousHandsAndControllersTrackingMETA returned non-success XrResult={result} for XrSession={xrSession}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} Exception calling xrResumeSimultaneousHandsAndControllersTrackingMETA: {ex.Message}");
            }
        }

        /// <inheritdoc />
        protected override void OnSessionEnd(ulong xrSession)
        {
            m_IsSimultaneousTrackingActive = false;
            m_XrSession = 0;
        }

        /// <inheritdoc />
        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            ResetState();
        }

        /// <summary>
        /// Pauses simultaneous hands and controllers tracking for the active session if supported.
        /// </summary>
        public bool PauseSimultaneousTracking()
        {
            if (!m_ExtensionEnabled || m_XrSession == 0 || m_PauseSimultaneousTracking == null)
            {
                return false;
            }

            try
            {
                var pauseInfo = new XrSimultaneousHandsAndControllersTrackingPauseInfoMETA
                {
                    type = XrStructureType.SimultaneousHandsAndControllersTrackingPauseInfoMETA,
                    next = IntPtr.Zero
                };

                XrResult result = m_PauseSimultaneousTracking(m_XrSession, ref pauseInfo);
                Debug.Log($"{LogPrefix} xrPauseSimultaneousHandsAndControllersTrackingMETA returned XrResult={result}.");
                if (result == XrResult.Success)
                {
                    m_IsSimultaneousTrackingActive = false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogPrefix} Exception calling xrPauseSimultaneousHandsAndControllersTrackingMETA: {ex.Message}");
            }

            return false;
        }

        private void QuerySystemSupport()
        {
            if (m_GetSystemProperties == null || m_XrInstance == 0 || m_XrSystemId == 0)
            {
                return;
            }

            m_SystemSupportQueried = true;
            IntPtr simultaneousPropsPtr = IntPtr.Zero;

            try
            {
                var simultaneousProps = new XrSystemSimultaneousHandsAndControllersPropertiesMETA
                {
                    type = XrStructureType.SystemSimultaneousHandsAndControllersPropertiesMETA,
                    next = IntPtr.Zero,
                    supportsSimultaneousHandsAndControllers = 0
                };

                simultaneousPropsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<XrSystemSimultaneousHandsAndControllersPropertiesMETA>());
                Marshal.StructureToPtr(simultaneousProps, simultaneousPropsPtr, false);

                var systemProps = new XrSystemProperties
                {
                    type = XrStructureType.SystemProperties,
                    next = simultaneousPropsPtr
                };

                XrResult result = m_GetSystemProperties(m_XrInstance, m_XrSystemId, ref systemProps);
                simultaneousProps = Marshal.PtrToStructure<XrSystemSimultaneousHandsAndControllersPropertiesMETA>(simultaneousPropsPtr);

                m_SupportsSimultaneousTracking =
                    result == XrResult.Success && simultaneousProps.supportsSimultaneousHandsAndControllers != 0;

                Debug.Log(
                    $"{LogPrefix} xrGetSystemProperties XrResult={result}, " +
                    $"supportsSimultaneousHandsAndControllers={m_SupportsSimultaneousTracking} " +
                    $"(raw={simultaneousProps.supportsSimultaneousHandsAndControllers}, XrSystemId={m_XrSystemId}).");

                if (result != XrResult.Success || !m_SupportsSimultaneousTracking)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} Active OpenXR runtime ({GetActiveRuntimeDescription()}, Platform={Application.platform}) reported that simultaneous hands and controllers tracking is not supported " +
                        $"(XrResult={result}, supportsSimultaneousHandsAndControllers={simultaneousProps.supportsSimultaneousHandsAndControllers}).");
                }
            }
            catch (Exception ex)
            {
                m_SupportsSimultaneousTracking = false;
                Debug.LogWarning($"{LogPrefix} Exception querying xrGetSystemProperties: {ex.Message}");
            }
            finally
            {
                if (simultaneousPropsPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(simultaneousPropsPtr);
                }
            }
        }

        // Verified in OpenXRRuntime.cs lines 14-21 (OpenXRRuntime.name and OpenXRRuntime.version)
        private static string GetActiveRuntimeDescription()
        {
            try
            {
                string runtimeName = OpenXRRuntime.name;
                string runtimeVersion = OpenXRRuntime.version;
                if (!string.IsNullOrEmpty(runtimeName))
                {
                    return !string.IsNullOrEmpty(runtimeVersion)
                        ? $"'{runtimeName}' v{runtimeVersion}"
                        : $"'{runtimeName}'";
                }
            }
            catch
            {
                // Ignore if OpenXR runtime native calls are unavailable.
            }

            return "'Unknown OpenXR Runtime'";
        }

        private static bool TryResolveFunction<TDelegate>(
            PFN_xrGetInstanceProcAddr getInstanceProcAddr,
            ulong xrInstance,
            string functionName,
            out TDelegate resolvedDelegate)
            where TDelegate : class
        {
            resolvedDelegate = null;

            XrResult result = getInstanceProcAddr(xrInstance, functionName, out IntPtr funcPtr);
            if (result != XrResult.Success || funcPtr == IntPtr.Zero)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Failed to resolve OpenXR function '{functionName}' via xrGetInstanceProcAddr (XrResult={result}).");
                return false;
            }

            resolvedDelegate = Marshal.GetDelegateForFunctionPointer<TDelegate>(funcPtr);
            return resolvedDelegate != null;
        }

        private void ResetState()
        {
            m_XrInstance = 0;
            m_XrSystemId = 0;
            m_XrSession = 0;
            m_ExtensionEnabled = false;
            m_SystemSupportQueried = false;
            m_SupportsSimultaneousTracking = false;
            m_IsSimultaneousTrackingActive = false;
            m_GetSystemProperties = null;
            m_ResumeSimultaneousTracking = null;
            m_PauseSimultaneousTracking = null;
        }
    }
}
