using BoneTool.Script.Runtime;
using UnityEditor;
using UnityEngine;

namespace BoneTool.Script.Editor
{
    [CustomEditor(typeof(BoneVisualiser))]
    public class BoneVisualiserInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI() {
            var visualiser = target as BoneVisualiser;
            if (!visualiser) return;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("Renderer"));
            //if (EditorGUI.EndChangeCheck()) {
            //    SceneView.RepaintAll();
            //}
            if (GUILayout.Button("Recalculate")) {
                visualiser.PopulateChildren();
                SceneView.RepaintAll();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BoneGizmosSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BoneColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HideRoot"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableConstraint"));

            EditorGUILayout.LabelField(string.Format("Bones :{0}",visualiser.GetChildNodes() == null ? 0 : visualiser.GetChildNodes().Length));

            serializedObject.ApplyModifiedProperties();
        }
    }
}