using UnityEditor;
using UnityEngine;
using DScrollerGame.Player;

namespace DScrollerGame.Editor.Player
{
    [CustomEditor(typeof(PlayerPhysicalState))]
    public class PlayerPhysicalStateEditor : UnityEditor.Editor
    {
        private static readonly Color StaminaBar = new Color(0.2f, 0.75f, 0.3f);
        private static readonly Color WeightBar  = new Color(0.9f, 0.65f, 0.15f);

        public override void OnInspectorGUI()
        {
            // Draw normal serialized inspector (your [Header], [Tooltip], etc.)
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime (Read Only)", EditorStyles.boldLabel);

            var state = (PlayerPhysicalState)target;

            using (new EditorGUI.DisabledScope(true))
            {
                DrawProgressBar(
                    label: "Stamina",
                    value01: state.NormalizedStamina,
                    text: $"{state.CurrentStamina:0.0} / {GetMaxStamina(state):0.0}",
                    barColor: StaminaBar);

                EditorGUILayout.Space(4);

                DrawProgressBar(
                    label: "Carry Weight",
                    value01: state.WeightPercent,
                    text: $"{GetCurrentCarryWeight(state):0.0} / {GetMaxCarryWeight(state):0.0}",
                    barColor: WeightBar);

                EditorGUILayout.Space(8);

                // Your properties from the snippet
                EditorGUILayout.Toggle("Has Stamina For Sprint", state.HasStaminaForSprint);
                EditorGUILayout.Toggle("Has Stamina For Jump", state.HasStaminaForJump);
                EditorGUILayout.Toggle("Is Exhausted", state.IsExhausted);
                EditorGUILayout.Toggle("Is Overburdened", state.IsOverburdened);

                EditorGUILayout.Space(6);

                EditorGUILayout.FloatField("Movement Multiplier", state.MovementMultiplier);
                EditorGUILayout.FloatField("Jump Multiplier", state.JumpMultiplier);
                EditorGUILayout.FloatField("Stamina Drain Multiplier", state.StaminaDrainMultiplier);
            }
        }

        private static void DrawProgressBar(string label, float value01, string text, Color barColor)
        {
            value01 = Mathf.Clamp01(value01);

            var rect = EditorGUILayout.GetControlRect(false, 18f);
            EditorGUI.LabelField(rect, label);

            rect.xMin += EditorGUIUtility.labelWidth;

            // Background
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.18f));

            // Fill
            var fill = rect;
            fill.width *= value01;
            EditorGUI.DrawRect(fill, barColor);

            // Text
            EditorGUI.LabelField(rect, text, EditorStyles.centeredGreyMiniLabel);
        }

        // --- Optional: show the raw numbers even if the class keeps them private ---
        // We read private serialized fields via SerializedObject (safe & Unity-friendly).

        private float GetMaxStamina(PlayerPhysicalState state)
        {
            var so = new SerializedObject(state);
            var p = so.FindProperty("_maxStamina");
            return p != null ? p.floatValue : 0f;
        }

        private float GetMaxCarryWeight(PlayerPhysicalState state)
        {
            var so = new SerializedObject(state);
            var p = so.FindProperty("_maxCarryWeight");
            return p != null ? p.floatValue : 0f;
        }

        private float GetCurrentCarryWeight(PlayerPhysicalState state)
        {
            // Not serialized in your file right now, so it won't show up unless you serialize it.
            // Returning an approximation using percent * max (still useful visually).
            return state.WeightPercent * GetMaxCarryWeight(state);
        }
    }
}