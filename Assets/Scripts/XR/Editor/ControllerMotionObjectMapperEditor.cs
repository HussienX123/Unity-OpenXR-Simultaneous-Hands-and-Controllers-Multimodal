using TMPro;
using UnityEditor;
using UnityEngine;

namespace XRDiagnostics.Editor
{
    [CustomEditor(typeof(ControllerMotionObjectMapper))]
    public class ControllerMotionObjectMapperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mapper = (ControllerMotionObjectMapper)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Live Controls & Actions", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Recenter Rotation", GUILayout.Height(30)))
            {
                mapper.Recenter();
            }
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test live rotation mapping, motion status, and recentering (or press 'R' on keyboard).", MessageType.Info);
            }
            else
            {
                string status = mapper.isMoving ? "MOVING" : "STATIC";
                Color statusColor = mapper.isMoving ? Color.green : Color.yellow;

                var prevColor = GUI.contentColor;
                GUI.contentColor = statusColor;
                EditorGUILayout.LabelField($"Live Status: {status}", EditorStyles.boldLabel);
                GUI.contentColor = prevColor;
            }
        }

        [MenuItem("GameObject/XR/Controller Motion Mapped Object (Cube + TMP)", false, 10)]
        [MenuItem("Tools/OpenXR Multimodal/5. Create Motion Mapped 3D Object (Cube + TMP)", false, 50)]
        public static void CreateMotionMappedObject()
        {
            // Create root container
            var root = new GameObject("Controller Motion Mapped Object");
            Undo.RegisterCreatedObjectUndo(root, "Create Controller Motion Mapped Object");
            root.transform.position = new Vector3(0f, 1.2f, 1.0f);

            // Create 3D Cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Create Motion Cube");
            cube.name = "MotionCube";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            // Create TextMeshPro 3D text
            var textGo = new GameObject("MotionStatusText");
            Undo.RegisterCreatedObjectUndo(textGo, "Create Motion Status Text");
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.22f, 0f);

            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = "Status: STATIC";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);

            // Attach Mapper
            var mapper = root.AddComponent<ControllerMotionObjectMapper>();
            mapper.targetObject = cube.transform;
            mapper.statusText = tmp;

            Selection.activeGameObject = root;
            Debug.Log("[OpenXR Multimodal] Created 'Controller Motion Mapped Object' in scene with 3D Cube and TextMeshPro label.");
        }
    }
}
