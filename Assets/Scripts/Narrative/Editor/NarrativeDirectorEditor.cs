using DontDiePlease.Narrative.Runtime;
using UnityEditor;
using UnityEngine;

namespace DontDiePlease.Narrative.Editor
{
    [CustomEditor(typeof(NarrativeDirector))]
    public sealed class NarrativeDirectorEditor : UnityEditor.Editor
    {
        private Vector2 scroll;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var director = (NarrativeDirector)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect and trigger narrative sequences.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Story State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Sequence", director.ActiveSequenceId);
            EditorGUILayout.LabelField("Objective", director.State?.currentObjectiveId ?? string.Empty);
            EditorGUILayout.LabelField("Player Tone", director.State?.playerTone ?? string.Empty);
            EditorGUILayout.LabelField("Signal Progress", $"{director.State?.signalGeneratorProgress ?? 0f:0}%");

            if (GUILayout.Button("Reset Narrative Progress"))
            {
                director.ResetNarrativeProgress();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sequence Debug", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(260f));

            if (director.Database?.sequences != null)
            {
                foreach (var sequence in director.Database.sequences)
                {
                    if (sequence == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(sequence.id, GUILayout.MinWidth(260f));

                        if (GUILayout.Button("Play", GUILayout.Width(54f)))
                        {
                            director.RequestSequence(sequence.id, true);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
