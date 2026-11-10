using UnityEditor;
using UnityEngine;

namespace HussienX.OpenXR.SimultaneousHandsAndControllers.Editor
{
    [CustomEditor(typeof(MetaSimultaneousHandsAndControllersFeature))]
    public class MetaSimultaneousHandsAndControllersFeatureEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Requests OpenXR extension XR_META_simultaneous_hands_and_controllers on Android (Meta Quest) and " +
                "Standalone (Windows Editor / PC over Meta Quest Link) and calls xrResumeSimultaneousHandsAndControllersTrackingMETA " +
                "when the session begins if the active runtime supports it. Experimental, pending device verification.",
                MessageType.Info);

            if (Application.isPlaying && target is MetaSimultaneousHandsAndControllersFeature feature)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("Extension Enabled", feature.IsExtensionEnabled);
                    EditorGUILayout.Toggle("System Supported", feature.SupportsSimultaneousTracking);
                    EditorGUILayout.Toggle("Tracking Resumed", feature.IsSimultaneousTrackingActive);
                }
            }
        }
    }
}
