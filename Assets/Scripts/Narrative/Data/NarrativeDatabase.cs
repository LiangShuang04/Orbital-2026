using System;
using UnityEngine;

namespace DontDiePlease.Narrative.Data
{
    [Serializable]
    public sealed class NarrativeDatabase
    {
        public Sequence[] sequences = Array.Empty<Sequence>();
        public Objective[] objectives = Array.Empty<Objective>();

        [Serializable]
        public sealed class Sequence
        {
            public string id = string.Empty;
            public string mode = "Full";
            public int priority = 10;
            public bool oneShot = true;
            public bool lockInput = true;
            public bool skippable = true;
            public float cooldownSeconds;
            public string[] requiredFlags = Array.Empty<string>();
            public string[] blockedFlags = Array.Empty<string>();
            public Line[] lines = Array.Empty<Line>();
            public string setFlag = string.Empty;
            public string completeObjective = string.Empty;
            public string nextObjective = string.Empty;
            public float signalProgress = -1f;
        }

        [Serializable]
        public sealed class Line
        {
            public string id = string.Empty;
            public string speaker = string.Empty;
            [TextArea(2, 6)] public string text = string.Empty;
            public float autoAdvanceSeconds;
            public string nextLineId = string.Empty;
            public Choice[] choices = Array.Empty<Choice>();
        }

        [Serializable]
        public sealed class Choice
        {
            public string id = string.Empty;
            public string text = string.Empty;
            public string nextLineId = string.Empty;
            public string setFlag = string.Empty;
            public string tone = string.Empty;
        }

        [Serializable]
        public sealed class Objective
        {
            public string id = string.Empty;
            public string title = string.Empty;
            [TextArea(2, 5)] public string description = string.Empty;
        }

        public Sequence FindSequence(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            foreach (var sequence in sequences)
            {
                if (sequence != null && string.Equals(sequence.id, id, StringComparison.Ordinal))
                {
                    return sequence;
                }
            }

            return null;
        }

        public Objective FindObjective(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            foreach (var objective in objectives)
            {
                if (objective != null && string.Equals(objective.id, id, StringComparison.Ordinal))
                {
                    return objective;
                }
            }

            return null;
        }
    }
}
