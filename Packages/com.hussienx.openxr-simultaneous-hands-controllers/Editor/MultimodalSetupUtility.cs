using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace HussienX.OpenXR.SimultaneousHandsAndControllers.Editor
{
    /// <summary>
    /// Automatically configures OpenXR Plug-in Management and OpenXR Feature Settings for
    /// Simultaneous Hands and Controllers on both Standalone (Quest Link) and Android (Meta Quest),
    /// imports required XRI / XR Hands samples if missing, and provides menu utilities for scene setup.
    /// </summary>
    [InitializeOnLoad]
    public static class MultimodalSetupUtility
    {
        private const string k_OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string k_HandsPackageName = "com.unity.xr.hands";
        private const string k_XRIPackageName = "com.unity.xr.interaction.toolkit";
        private const string k_StarterAssetsSampleName = "Starter Assets";
        private const string k_HandsDemoSampleName = "Hands Interaction Demo";
        private const string k_HandVisualizerSampleName = "HandVisualizer";
        private const string k_DemoScenePath = "Assets/Scenes/MultimodalTest.unity";
        private const string k_DemoRigPrefabPath = "Assets/Prefabs/XR/XR Origin Hands (XR Rig).prefab";
        private const string k_StarterAssetsRigGuid = "f6336ac4ac8b4d34bc5072418cdc62a0";
        private const string k_HandsDemoLeftVisualGuid = "ffd656bf2a3ba3d41b1e4a94b81b7c85";
        private const string k_HandVisualizerScriptGuid = "2e9813c68d7d6f44282ace8bd2d1fd46";

        private static readonly string[] s_CommonEnabledFeatureIds =
        {
            MetaSimultaneousHandsAndControllersFeature.FeatureId,
            "com.unity.openxr.feature.input.handtracking",
            "com.unity.openxr.feature.input.metahandtrackingaim",
            "com.unity.openxr.feature.input.handinteraction",
            "com.unity.openxr.feature.input.oculustouch",
            "com.unity.openxr.feature.input.metaquestpro",
            "com.unity.openxr.feature.input.metaquestplus"
        };

        private static readonly string[] s_AndroidOnlyEnabledFeatureIds =
        {
            "com.unity.openxr.feature.metaquest"
        };

        private static readonly string[] s_DisabledFeatureIds =
        {
            "com.unity.openxr.feature.input.handinteractionposes"
        };

        private static AddRequest s_HandsAddRequest;

        static MultimodalSetupUtility()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode)
            {
                return;
            }

            if (!AreOpenXRFeaturesConfigured(BuildTargetGroup.Standalone) ||
                !AreOpenXRFeaturesConfigured(BuildTargetGroup.Android))
            {
                ConfigureOpenXRSettings(logSummary: true);
            }

            // If the demo rig prefab is present in the project but required sample GUIDs are missing,
            // automatically ensure XR Hands and the 3 required samples are imported.
            if (File.Exists(k_DemoRigPrefabPath) && !AreRequiredSampleAssetsPresent())
            {
                EnsurePackagesAndSamplesInstalled(interactive: false);
            }
        }

        /// <summary>
        /// Configures OpenXR loader and required OpenXR features for both Standalone and Android build targets.
        /// </summary>
        [MenuItem("Tools/OpenXR Multimodal/1. Configure OpenXR Settings (Standalone & Android)", false, 10)]
        public static void ConfigureOpenXRSettingsMenu()
        {
            ConfigureOpenXRSettings(logSummary: true);
        }

        /// <summary>
        /// Installs <c>com.unity.xr.hands</c> (if missing) and imports Starter Assets, Hands Interaction Demo,
        /// and HandVisualizer samples required by the demo XR Rig prefab.
        /// </summary>
        [MenuItem("Tools/OpenXR Multimodal/2. Import Required XRI & XR Hands Samples", false, 20)]
        public static void ImportSamplesMenu()
        {
            EnsurePackagesAndSamplesInstalled(interactive: true);
        }

        /// <summary>
        /// Converts the active scene's XR Origin rig for simultaneous hands and controllers by disabling
        /// <c>XRInputModalityManager</c>, attaching <see cref="ControllerPoseIsolation"/> to Left/Right Controllers,
        /// and creating the <c>XRDiagnostics</c> GameObject.
        /// </summary>
        [MenuItem("Tools/OpenXR Multimodal/3. Convert Active Scene XR Rig for Simultaneous Tracking", false, 30)]
        public static void ConvertActiveSceneRigMenu()
        {
            ConvertActiveSceneRig();
        }

        /// <summary>
        /// Opens the included <c>Assets/Scenes/MultimodalTest.unity</c> demo scene.
        /// </summary>
        [MenuItem("Tools/OpenXR Multimodal/4. Open Multimodal Demo Scene", false, 40)]
        public static void OpenDemoSceneMenu()
        {
            if (!AreRequiredSampleAssetsPresent())
            {
                EnsurePackagesAndSamplesInstalled(interactive: true);
            }

            if (!File.Exists(k_DemoScenePath))
            {
                Debug.LogWarning($"[OpenXR Multimodal] Demo scene not found at '{k_DemoScenePath}'.");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(k_DemoScenePath, OpenSceneMode.Single);
                Debug.Log($"[OpenXR Multimodal] Opened demo scene '{k_DemoScenePath}'.");
            }
        }

        /// <summary>
        /// Exports the distribution .unitypackage to Builds/OpenXR-Simultaneous-Hands-and-Controllers-v0.1.0-experimental.unitypackage.
        /// </summary>
        [MenuItem("Tools/OpenXR Multimodal/6. Export Distribution .unitypackage", false, 60)]
        public static void ExportDistributionPackageMenu()
        {
            ExportDistributionPackage();
        }

        public static string ExportDistributionPackage(string outputPath = "Builds/OpenXR-Simultaneous-Hands-and-Controllers-v0.1.0-experimental.unitypackage")
        {
            var exportPaths = new List<string>
            {
                "Packages/com.hussienx.openxr-simultaneous-hands-controllers",
                k_DemoScenePath,
                k_DemoRigPrefabPath,
                "Assets/Scripts/XR"
            };

            var validPaths = new List<string>();
            foreach (var p in exportPaths)
            {
                if (File.Exists(p) || Directory.Exists(p))
                {
                    validPaths.Add(p);
                }
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.ExportPackage(validPaths.ToArray(), outputPath, ExportPackageOptions.Recurse);
            Debug.Log($"[OpenXR Multimodal] Exported distribution package ({validPaths.Count} items) to '{outputPath}'.");
            return outputPath;
        }

        /// <summary>
        /// Programmatically enables the OpenXR loader and all required OpenXR features for Standalone and Android,
        /// and disables <c>HandCommonPosesInteraction</c> ("Hand Interaction Poses") so controller visuals do not
        /// snap to tracked hands when controllers are placed down.
        /// </summary>
        public static void ConfigureOpenXRSettings(bool logSummary)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[OpenXR Multimodal] Cannot modify OpenXR settings while in Play Mode.");
                return;
            }

            var changes = new List<string>();
            ConfigureBuildTargetGroup(BuildTargetGroup.Standalone, includeAndroidFeatures: false, changes);
            ConfigureBuildTargetGroup(BuildTargetGroup.Android, includeAndroidFeatures: true, changes);

            AssetDatabase.SaveAssets();

            if (logSummary)
            {
                if (changes.Count > 0)
                {
                    Debug.Log(
                        "[OpenXR Multimodal] Configured OpenXR settings for Standalone & Android:\n- " +
                        string.Join("\n- ", changes));
                }
                else
                {
                    Debug.Log(
                        "[OpenXR Multimodal] OpenXR settings for Standalone & Android are already configured " +
                        "(Simultaneous Hands and Controllers enabled, Hand Interaction Poses disabled).");
                }
            }
        }

        private static bool AreOpenXRFeaturesConfigured(BuildTargetGroup group)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null)
            {
                return false;
            }

            var multimodalFeature = FeatureHelpers.GetFeatureWithIdForBuildTarget(
                group,
                MetaSimultaneousHandsAndControllersFeature.FeatureId);
            if (multimodalFeature == null || !multimodalFeature.enabled)
            {
                return false;
            }

            var handPosesFeature = FeatureHelpers.GetFeatureWithIdForBuildTarget(
                group,
                "com.unity.openxr.feature.input.handinteractionposes");
            if (handPosesFeature != null && handPosesFeature.enabled)
            {
                return false;
            }

            return true;
        }

        private static void ConfigureBuildTargetGroup(
            BuildTargetGroup group,
            bool includeAndroidFeatures,
            List<string> changes)
        {
            EnsureOpenXRLoaderAssigned(group, changes);

            FeatureHelpers.RefreshFeatures(group);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null)
            {
                return;
            }

            foreach (string featureId in s_CommonEnabledFeatureIds)
            {
                SetFeatureEnabled(group, settings, featureId, true, changes);
            }

            if (includeAndroidFeatures)
            {
                foreach (string featureId in s_AndroidOnlyEnabledFeatureIds)
                {
                    SetFeatureEnabled(group, settings, featureId, true, changes);
                }
            }

            foreach (string featureId in s_DisabledFeatureIds)
            {
                SetFeatureEnabled(group, settings, featureId, false, changes);
            }
        }

        private static void EnsureOpenXRLoaderAssigned(BuildTargetGroup group, List<string> changes)
        {
            try
            {
                if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(
                        XRGeneralSettings.k_SettingsKey,
                        out var perBuildTarget) ||
                    perBuildTarget == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        perBuildTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                        if (perBuildTarget != null)
                        {
                            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
                        }
                    }
                }

                if (perBuildTarget == null)
                {
                    return;
                }

                if (!perBuildTarget.HasSettingsForBuildTarget(group))
                {
                    perBuildTarget.CreateDefaultSettingsForBuildTarget(group);
                }

                if (!perBuildTarget.HasManagerSettingsForBuildTarget(group))
                {
                    perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                }

                var generalSettings = perBuildTarget.SettingsForBuildTarget(group);
                if (generalSettings != null && !generalSettings.InitManagerOnStart)
                {
                    generalSettings.InitManagerOnStart = true;
                    EditorUtility.SetDirty(generalSettings);
                    changes.Add($"{group}: Set Initialize XR on Startup = true");
                }

                var manager = perBuildTarget.ManagerSettingsForBuildTarget(group);
                if (manager != null && !XRPackageMetadataStore.IsLoaderAssigned(k_OpenXRLoaderTypeName, group))
                {
                    if (XRPackageMetadataStore.AssignLoader(manager, k_OpenXRLoaderTypeName, group))
                    {
                        changes.Add($"{group}: Assigned OpenXR Loader");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenXR Multimodal] Could not verify OpenXR Loader for {group}: {ex.Message}");
            }
        }

        private static void SetFeatureEnabled(
            BuildTargetGroup group,
            OpenXRSettings settings,
            string featureId,
            bool enabledState,
            List<string> changes)
        {
            var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(group, featureId);
            if (feature == null)
            {
                return;
            }

            if (feature.enabled != enabledState)
            {
                feature.enabled = enabledState;
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(settings);
                string action = enabledState ? "Enabled" : "Disabled";
                changes.Add($"{group}: {action} '{feature.name}' ({featureId})");
            }
        }

        private static bool AreRequiredSampleAssetsPresent()
        {
            return !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(k_StarterAssetsRigGuid)) &&
                   !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(k_HandsDemoLeftVisualGuid)) &&
                   !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(k_HandVisualizerScriptGuid));
        }

        private static void EnsurePackagesAndSamplesInstalled(bool interactive)
        {
            if (!IsPackageInstalled(k_HandsPackageName))
            {
                Debug.Log($"[OpenXR Multimodal] Installing required package '{k_HandsPackageName}'...");
                s_HandsAddRequest = Client.Add(k_HandsPackageName);
                EditorApplication.update -= WaitForHandsPackageInstall;
                EditorApplication.update += WaitForHandsPackageInstall;
                return;
            }

            ImportRequiredSamples(interactive);
        }

        private static void WaitForHandsPackageInstall()
        {
            if (s_HandsAddRequest == null || !s_HandsAddRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= WaitForHandsPackageInstall;

            if (s_HandsAddRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[OpenXR Multimodal] Installed '{k_HandsPackageName}'. Importing required samples...");
                ConfigureOpenXRSettings(logSummary: true);
                ImportRequiredSamples(interactive: false);
            }
            else if (s_HandsAddRequest.Error != null)
            {
                Debug.LogError($"[OpenXR Multimodal] Failed to install '{k_HandsPackageName}': {s_HandsAddRequest.Error.message}");
            }
        }

        private static void ImportRequiredSamples(bool interactive)
        {
            var imported = new List<string>();

            if (TryImportSampleIfMissing(k_XRIPackageName, k_StarterAssetsSampleName, k_StarterAssetsRigGuid, interactive))
            {
                imported.Add($"{k_XRIPackageName}: {k_StarterAssetsSampleName}");
            }

            if (TryImportSampleIfMissing(k_XRIPackageName, k_HandsDemoSampleName, k_HandsDemoLeftVisualGuid, interactive))
            {
                imported.Add($"{k_XRIPackageName}: {k_HandsDemoSampleName}");
            }

            if (TryImportSampleIfMissing(k_HandsPackageName, k_HandVisualizerSampleName, k_HandVisualizerScriptGuid, interactive))
            {
                imported.Add($"{k_HandsPackageName}: {k_HandVisualizerSampleName}");
            }

            if (imported.Count > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log("[OpenXR Multimodal] Imported required samples:\n- " + string.Join("\n- ", imported));
            }
            else if (interactive)
            {
                Debug.Log("[OpenXR Multimodal] All required XRI and XR Hands samples are already imported.");
            }
        }

        private static bool TryImportSampleIfMissing(
            string packageName,
            string sampleDisplayName,
            string verificationGuid,
            bool forceIfInteractive)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(verificationGuid);
            if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath) && !forceIfInteractive)
            {
                return false;
            }

            try
            {
                IEnumerable<Sample> samples = Sample.FindByPackage(packageName, string.Empty);
                if (samples == null)
                {
                    return false;
                }

                foreach (Sample sample in samples)
                {
                    if (string.Equals(sample.displayName, sampleDisplayName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!sample.isImported || string.IsNullOrEmpty(existingPath))
                        {
                            return sample.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenXR Multimodal] Could not import sample '{sampleDisplayName}' from '{packageName}': {ex.Message}");
            }

            return false;
        }

        private static bool IsPackageInstalled(string packageName)
        {
            if (Directory.Exists($"Packages/{packageName}"))
            {
                return true;
            }

            string packageCacheDir = "Library/PackageCache";
            if (Directory.Exists(packageCacheDir))
            {
                string[] dirs = Directory.GetDirectories(packageCacheDir, $"{packageName}@*");
                if (dirs.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConvertActiveSceneRig()
        {
            var changes = new List<string>();
            Scene activeScene = SceneManager.GetActiveScene();

            MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb == null)
                {
                    continue;
                }

                Type type = mb.GetType();
                if (string.Equals(type.Name, "XRInputModalityManager", StringComparison.Ordinal))
                {
                    if (mb.enabled)
                    {
                        Undo.RecordObject(mb, "Disable XRInputModalityManager");
                        mb.enabled = false;
                        EditorUtility.SetDirty(mb);
                        changes.Add($"Disabled XRInputModalityManager on '{mb.gameObject.name}'");
                    }

                    // Ensure both hand and controller hierarchy roots stay active
                    Transform rigTransform = mb.transform;
                    foreach (Transform child in rigTransform.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "Left Hand" || child.name == "Right Hand" ||
                            child.name == "Left Controller" || child.name == "Right Controller")
                        {
                            if (!child.gameObject.activeSelf)
                            {
                                Undo.RecordObject(child.gameObject, "Activate Hand/Controller Hierarchy");
                                child.gameObject.SetActive(true);
                                EditorUtility.SetDirty(child.gameObject);
                                changes.Add($"Activated '{child.name}' GameObject");
                            }
                        }
                    }
                }
            }

            // Attach ControllerPoseIsolation to Left Controller and Right Controller
            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Transform t in allTransforms)
            {
                if (t == null)
                {
                    continue;
                }

                bool isLeft = string.Equals(t.name, "Left Controller", StringComparison.OrdinalIgnoreCase);
                bool isRight = string.Equals(t.name, "Right Controller", StringComparison.OrdinalIgnoreCase);
                if (!isLeft && !isRight)
                {
                    continue;
                }

                var isolation = t.GetComponent<ControllerPoseIsolation>();
                if (isolation == null)
                {
                    isolation = Undo.AddComponent<ControllerPoseIsolation>(t.gameObject);
                    changes.Add($"Added ControllerPoseIsolation to '{t.name}'");
                }

                var targetHand = isRight
                    ? ControllerMotionData.ControllerHand.Right
                    : ControllerMotionData.ControllerHand.Left;
                if (isolation.hand != targetHand)
                {
                    Undo.RecordObject(isolation, "Configure ControllerPoseIsolation Hand");
                    isolation.hand = targetHand;
                    EditorUtility.SetDirty(isolation);
                }
            }

            // Ensure XRDiagnostics GameObject exists in the scene
            GameObject diagGo = GameObject.Find("XRDiagnostics");
            if (diagGo == null)
            {
                diagGo = new GameObject("XRDiagnostics");
                Undo.RegisterCreatedObjectUndo(diagGo, "Create XRDiagnostics");
                changes.Add("Created 'XRDiagnostics' GameObject");
            }

            if (diagGo.GetComponent<ControllerMotionData>() == null)
            {
                Undo.AddComponent<ControllerMotionData>(diagGo);
                changes.Add("Added ControllerMotionData to 'XRDiagnostics'");
            }

            if (diagGo.GetComponent<SimultaneousHandsAndControllersStatus>() == null)
            {
                Undo.AddComponent<SimultaneousHandsAndControllersStatus>(diagGo);
                changes.Add("Added SimultaneousHandsAndControllersStatus to 'XRDiagnostics'");
            }

            Type diagType = FindDiagnosticsComponentType();
            if (diagType != null && diagGo.GetComponent(diagType) == null)
            {
                Undo.AddComponent(diagGo, diagType);
                changes.Add($"Added {diagType.Name} to 'XRDiagnostics'");
            }

            if (changes.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log(
                    $"[OpenXR Multimodal] Converted scene '{activeScene.name}' for simultaneous hands and controllers:\n- " +
                    string.Join("\n- ", changes));
            }
            else
            {
                Debug.Log($"[OpenXR Multimodal] Scene '{activeScene.name}' is already configured for simultaneous hands and controllers.");
            }
        }

        private static Type FindDiagnosticsComponentType()
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (string.Equals(type.Name, "XRSimultaneousTrackingDiagnostics", StringComparison.Ordinal))
                {
                    return type;
                }
            }

            return null;
        }
    }
}
