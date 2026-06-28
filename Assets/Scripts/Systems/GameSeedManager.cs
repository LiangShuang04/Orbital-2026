using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DontDiePlease.Systems
{
    public sealed class GameSeedManager : MonoBehaviour
    {
        public static GameSeedManager Instance { get; private set; }

        [SerializeField] private bool initialiseOnAwake = true;
        [SerializeField] private bool useManualSeed;
        [SerializeField] private int manualSeed = 2026;
        [SerializeField] private bool initialiseUnityRandom = true;
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool logSeed = true;
        [SerializeField] private Text seedDebugText;

        private readonly Dictionary<string, System.Random> streams = new Dictionary<string, System.Random>();

        public int CurrentSeed { get; private set; }
        public bool HasSeed { get; private set; }

        public event Action<int> SeedChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (initialiseOnAwake)
            {
                InitialiseRun();
            }
        }

        public int InitialiseRun()
        {
            var seed = useManualSeed ? manualSeed : GenerateSeed();
            SetSeed(seed);
            return CurrentSeed;
        }

        public void SetSeed(int seed)
        {
            CurrentSeed = seed;
            HasSeed = true;
            streams.Clear();

            if (initialiseUnityRandom)
            {
                UnityEngine.Random.InitState(CurrentSeed);
            }

            RefreshDebugText();

            if (logSeed)
            {
                Debug.Log($"Game seed initialised: {CurrentSeed}");
            }

            SeedChanged?.Invoke(CurrentSeed);
        }

        public System.Random GetRandomStream(string streamName)
        {
            EnsureSeed();

            var key = string.IsNullOrWhiteSpace(streamName) ? "default" : streamName.Trim();

            if (!streams.TryGetValue(key, out var rng))
            {
                rng = CreateRandomStream(key);
                streams[key] = rng;
            }

            return rng;
        }

        public System.Random CreateRandomStream(string streamName)
        {
            EnsureSeed();
            return new System.Random(ToSystemRandomSeed(MixSeed(CurrentSeed, streamName)));
        }

        public int Range(string streamName, int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            return GetRandomStream(streamName).Next(minimumInclusive, maximumExclusive);
        }

        public float Range(string streamName, float minimumInclusive, float maximumInclusive)
        {
            if (maximumInclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            var rng = GetRandomStream(streamName);
            return minimumInclusive + (float)rng.NextDouble() * (maximumInclusive - minimumInclusive);
        }

        private void EnsureSeed()
        {
            if (!HasSeed)
            {
                InitialiseRun();
            }
        }

        private void RefreshDebugText()
        {
            if (seedDebugText != null)
            {
                seedDebugText.text = HasSeed ? $"Seed: {CurrentSeed}" : "Seed: none";
            }
        }

        private static int GenerateSeed()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        private static int MixSeed(int seed, string streamName)
        {
            unchecked
            {
                var hash = 2166136261;
                var text = string.IsNullOrWhiteSpace(streamName) ? "default" : streamName.Trim();

                foreach (var ch in text)
                {
                    hash ^= ch;
                    hash *= 16777619;
                }

                hash ^= (uint)seed;
                hash *= 16777619;
                return (int)hash;
            }
        }

        private static int ToSystemRandomSeed(int seed)
        {
            return seed == int.MinValue ? int.MaxValue : Mathf.Abs(seed);
        }
    }
}
