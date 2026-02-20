using UnityEditor;
using DScrollerGame.Interaction;

namespace DScrollerGame.Interaction.Editor
{
    [CustomEditor(typeof(BreakableBase), true)]
    public class BreakableBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _currentDamage;
        private SerializedProperty _isBroken;

        private void OnEnable()
        {
            _currentDamage = serializedObject.FindProperty("_currentDamage");
            _isBroken = serializedObject.FindProperty("_isBroken");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all properties except the ones we want to handle specially
            DrawPropertiesExcluding(serializedObject, "_currentDamage", "_isBroken");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_currentDamage);
            EditorGUILayout.PropertyField(_isBroken);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
