using UnityEditor;
using UnityEngine;

namespace GimmeDOTSGeometry
{
    [CustomEditor(typeof(LineSegmentIntersectionStepper))]
    public class LineSegmentIntersectionStepperEditor : Editor
    {

        private LineSegmentIntersectionStepper stepper;

        private void OnEnable()
        {
            this.stepper = this.target as LineSegmentIntersectionStepper;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Step"))
            {
                this.stepper.Step();
            }

            EditorGUILayout.LabelField("Event Queue");
            var controlRect = EditorGUILayout.GetControlRect(false, 500.0f);
            GUIUtility.DrawTree(controlRect, this.stepper.EventQueue);

            EditorGUILayout.LabelField("Status");
            controlRect = EditorGUILayout.GetControlRect(false, 500.0f);
            GUIUtility.DrawTree(controlRect, this.stepper.Status);
        }
    }
}
