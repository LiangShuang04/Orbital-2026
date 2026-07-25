using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace DontDiePlease.Tests.EditMode
{
    public sealed class NarrativeEditModeTests
    {
        private const string GuestSaveKey = "DontDiePlease.Narrative.State.guest";
        private const string OtherSaveKey = "DontDiePlease.Narrative.State.other-account";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(GuestSaveKey);
            PlayerPrefs.DeleteKey(OtherSaveKey);
            PlayerPrefs.DeleteKey("settings.mouseSensitivity");
            PlayerPrefs.DeleteKey("settings.masterVolume");
            PlayerPrefs.DeleteKey("settings.fullscreen");
            PlayerPrefs.Save();
        }

        [Test]
        public void NarrativeDatabaseHasUniqueLinkedEnglishSequences()
        {
            var path = Path.Combine(Application.dataPath, "Resources", "Narrative", "narrative_mvp.json");
            var json = File.ReadAllText(path);
            var database = JsonUtility.FromJson<TestDatabase>(json);

            Assert.That(database, Is.Not.Null);
            Assert.That(database.sequences, Is.Not.Null);
            Assert.That(database.sequences.Length, Is.EqualTo(54));
            Assert.That(database.sequences.Select(sequence => sequence.id).Distinct().Count(), Is.EqualTo(database.sequences.Length));

            foreach (var sequence in database.sequences)
            {
                Assert.That(sequence.id, Is.Not.Empty);
                Assert.That(sequence.lines, Is.Not.Null.And.Not.Empty, sequence.id);
                var lineIds = sequence.lines.Select(line => line.id).ToHashSet();

                foreach (var line in sequence.lines)
                {
                    Assert.That(line.id, Is.Not.Empty, sequence.id);
                    Assert.That(ContainsCjk(line.speaker), Is.False, $"{sequence.id}:{line.id}:speaker");
                    Assert.That(ContainsCjk(line.text), Is.False, $"{sequence.id}:{line.id}:text");

                    if (!string.IsNullOrWhiteSpace(line.nextLineId) && line.nextLineId != "END")
                    {
                        Assert.That(lineIds.Contains(line.nextLineId), Is.True, $"{sequence.id}:{line.id}");
                    }

                    foreach (var choice in line.choices ?? Array.Empty<TestChoice>())
                    {
                        Assert.That(ContainsCjk(choice.text), Is.False, $"{sequence.id}:{line.id}:choice");
                        Assert.That(lineIds.Contains(choice.nextLineId), Is.True, $"{sequence.id}:{line.id}:{choice.id}");
                    }
                }
            }
        }

        [Test]
        public void DefenseTimelineUsesProductionDurationAndOrderedMilestones()
        {
            var timeline = CreateRuntimeObject("DontDiePlease.Narrative.Runtime.NarrativeDefenseTimeline");
            Invoke(timeline, "Start");
            Assert.That(GetProperty<float>(timeline, "RemainingSeconds"), Is.EqualTo(180f));

            var first = Invoke(timeline, "Advance", 45f, false);
            Assert.That(GetProperty<bool>(first, "Reached25"), Is.True);
            Assert.That(GetProperty<bool>(first, "Reached60"), Is.False);

            var second = Invoke(timeline, "Advance", 63f, false);
            Assert.That(GetProperty<bool>(second, "Reached25"), Is.False);
            Assert.That(GetProperty<bool>(second, "Reached60"), Is.True);

            var third = Invoke(timeline, "Advance", 54f, false);
            Assert.That(GetProperty<bool>(third, "Reached90"), Is.True);

            var completed = Invoke(timeline, "Advance", 18f, false);
            Assert.That(GetProperty<bool>(completed, "Completed"), Is.True);
            Assert.That(GetProperty<float>(timeline, "RemainingSeconds"), Is.Zero);
        }

        [Test]
        public void DefenseTimelinePausesRestoresAndDoesNotRepeatMilestones()
        {
            var timeline = CreateRuntimeObject("DontDiePlease.Narrative.Runtime.NarrativeDefenseTimeline");
            Invoke(timeline, "Start");
            Invoke(timeline, "Advance", 30f, true);
            Assert.That(GetProperty<float>(timeline, "RemainingSeconds"), Is.EqualTo(180f));

            Invoke(timeline, "Restore", 36f, true, true, false);
            var first = Invoke(timeline, "Advance", 20f, false);
            var second = Invoke(timeline, "Advance", 1f, false);
            Assert.That(GetProperty<bool>(first, "Reached25"), Is.False);
            Assert.That(GetProperty<bool>(first, "Reached60"), Is.False);
            Assert.That(GetProperty<bool>(first, "Reached90"), Is.True);
            Assert.That(GetProperty<bool>(second, "Reached90"), Is.False);
        }

        [Test]
        public async Task NewGameClearsStoryStateWithoutChangingSettingsOrOtherAccounts()
        {
            PlayerPrefs.SetFloat("settings.mouseSensitivity", 1.75f);
            PlayerPrefs.SetFloat("settings.masterVolume", 0.42f);
            PlayerPrefs.SetInt("settings.fullscreen", 1);
            PlayerPrefs.SetString(OtherSaveKey, "other-account-state");
            PlayerPrefs.SetString(GuestSaveKey, CreateCompletedStoryJson());
            PlayerPrefs.Save();

            var adapterType = RuntimeType("DontDiePlease.Narrative.Persistence.NarrativeSaveAdapter");
            var go = new GameObject("NarrativeSaveAdapterTest");
            var adapter = go.AddComponent(adapterType);
            var resetTask = (Task)Invoke(adapter, "ResetForNewGame", 774411);
            await resetTask;
            var state = Invoke(adapter, "LoadLocal");

            Assert.That(GetField<string>(state, "currentObjectiveId"), Is.EqualTo("ACT1_WAKE"));
            Assert.That(GetField<int>(state, "worldSeed"), Is.EqualTo(774411));
            Assert.That(GetField<float>(state, "signalGeneratorProgress"), Is.Zero);
            Assert.That(GetField<bool>(state, "signalDefenseActive"), Is.False);
            Assert.That(GetField<float>(state, "signalDefenseRemainingSeconds"), Is.Zero);
            Assert.That(ListCount(state, "completedSequenceIds"), Is.Zero);
            Assert.That(ListCount(state, "completedObjectiveIds"), Is.Zero);
            Assert.That(ListCount(state, "flags"), Is.Zero);
            Assert.That(GetField<string>(state, "playthroughId"), Is.Not.Empty.And.Not.EqualTo("old-run"));
            Assert.That(PlayerPrefs.GetFloat("settings.mouseSensitivity"), Is.EqualTo(1.75f));
            Assert.That(PlayerPrefs.GetFloat("settings.masterVolume"), Is.EqualTo(0.42f));
            Assert.That(PlayerPrefs.GetInt("settings.fullscreen"), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString(OtherSaveKey), Is.EqualTo("other-account-state"));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void OlderRemotePlaythroughCannotOverwriteNewGameState()
        {
            var stateType = RuntimeType("DontDiePlease.Narrative.Runtime.StoryState");
            var local = Activator.CreateInstance(stateType);
            var remote = Activator.CreateInstance(stateType);
            SetField(local, "playthroughId", "new-run");
            SetField(local, "startedAtUnixMs", 200L);
            SetField(local, "worldSeed", 9001);
            SetField(remote, "playthroughId", "old-run");
            SetField(remote, "startedAtUnixMs", 100L);
            SetField(remote, "worldSeed", 42);
            Invoke(remote, "SetFlag", "story_complete");
            var adapterType = RuntimeType("DontDiePlease.Narrative.Persistence.NarrativeSaveAdapter");
            var merge = adapterType.GetMethod("Merge", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(merge, Is.Not.Null);
            merge.Invoke(null, new[] { local, remote });

            Assert.That(GetField<string>(local, "playthroughId"), Is.EqualTo("new-run"));
            Assert.That(GetField<int>(local, "worldSeed"), Is.EqualTo(9001));
            Assert.That((bool)Invoke(local, "HasFlag", "story_complete"), Is.False);
        }

        private static string CreateCompletedStoryJson()
        {
            var state = CreateRuntimeObject("DontDiePlease.Narrative.Runtime.StoryState");
            SetField(state, "playthroughId", "old-run");
            SetField(state, "startedAtUnixMs", 100L);
            SetField(state, "currentObjectiveId", "FINALE_SIGNAL");
            SetField(state, "signalGeneratorProgress", 100f);
            SetField(state, "signalDefenseActive", true);
            SetField(state, "signalDefenseRemainingSeconds", 12f);
            Invoke(state, "CompleteSequence", "TRG_EPILOGUE");
            Invoke(state, "CompleteObjective", "FINALE_SIGNAL");
            Invoke(state, "SetFlag", "story_complete");
            return JsonUtility.ToJson(state);
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.Any(character =>
                character >= '\u3400' && character <= '\u9fff' ||
                character >= '\u3040' && character <= '\u30ff' ||
                character >= '\uac00' && character <= '\ud7af');
        }

        private static object CreateRuntimeObject(string fullName)
        {
            return Activator.CreateInstance(RuntimeType(fullName));
        }

        private static Type RuntimeType(string fullName)
        {
            return Type.GetType($"{fullName}, Assembly-CSharp", true);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(target, args);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName).GetValue(target);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            return (T)target.GetType().GetField(fieldName).GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName).SetValue(target, value);
        }

        private static int ListCount(object target, string fieldName)
        {
            var list = (System.Collections.ICollection)target.GetType().GetField(fieldName).GetValue(target);
            return list.Count;
        }

        [Serializable]
        private sealed class TestDatabase
        {
            public TestSequence[] sequences;
        }

        [Serializable]
        private sealed class TestSequence
        {
            public string id;
            public TestLine[] lines;
        }

        [Serializable]
        private sealed class TestLine
        {
            public string id;
            public string speaker;
            public string text;
            public string nextLineId;
            public TestChoice[] choices;
        }

        [Serializable]
        private sealed class TestChoice
        {
            public string id;
            public string text;
            public string nextLineId;
        }
    }
}
