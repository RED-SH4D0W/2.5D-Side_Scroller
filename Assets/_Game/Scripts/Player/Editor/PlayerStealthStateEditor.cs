using UnityEditor;
using UnityEngine;
using DScrollerGame.Player;

namespace DScrollerGame.Editor.Player
{
    [CustomEditor(typeof(PlayerStealthState))]
    public class PlayerStealthStateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default fields ([SerializeField] variables)
            DrawDefaultInspector();

            var stealth = (PlayerStealthState)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Debug (Read Only)", EditorStyles.boldLabel);

            // Using DisabledScope to make fields read-only
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Current Noise", stealth.CurrentNoise);
                EditorGUILayout.FloatField("Current Radius", stealth.CurrentNoiseRadius);
                EditorGUILayout.FloatField("Current Visibility", stealth.CurrentVisibility);
            }

            // Force repaint while in play mode to see real-time updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
