using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeDefenseTimeline
    {
        public const float DurationSeconds = 180f;
        private bool sent25;
        private bool sent60;
        private bool sent90;

        public float RemainingSeconds { get; private set; }
        public float Progress => Mathf.Clamp01(1f - RemainingSeconds / DurationSeconds);

        public void Start()
        {
            RemainingSeconds = DurationSeconds;
            sent25 = false;
            sent60 = false;
            sent90 = false;
        }

        public void Restore(float remainingSeconds, bool reached25, bool reached60, bool reached90)
        {
            RemainingSeconds = Mathf.Clamp(remainingSeconds, 0f, DurationSeconds);
            sent25 = reached25;
            sent60 = reached60;
            sent90 = reached90;
        }

        public NarrativeDefenseTick Advance(float deltaTime, bool paused)
        {
            if (!paused)
            {
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaTime));
            }

            var reached25 = !sent25 && Progress >= 0.25f;
            var reached60 = !sent60 && Progress >= 0.6f;
            var reached90 = !sent90 && Progress >= 0.9f;
            sent25 |= reached25;
            sent60 |= reached60;
            sent90 |= reached90;
            return new NarrativeDefenseTick(reached25, reached60, reached90, RemainingSeconds <= 0f);
        }
    }

    public readonly struct NarrativeDefenseTick
    {
        public NarrativeDefenseTick(bool reached25, bool reached60, bool reached90, bool completed)
        {
            Reached25 = reached25;
            Reached60 = reached60;
            Reached90 = reached90;
            Completed = completed;
        }

        public bool Reached25 { get; }
        public bool Reached60 { get; }
        public bool Reached90 { get; }
        public bool Completed { get; }
    }
}
