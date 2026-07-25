using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class TextWriter : MonoBehaviour {

    private static TextWriter instance;

    public AudioSource talkingAudioSource;
    public AudioClip[] sounds;
    public UnityEvent OnFinish;
    public List<TextWriterSingle> textWriterSingleList;

    private AudioClip soundWrite;
    private bool SoundEnd;

    private void Awake() {
        instance = this;
        textWriterSingleList = new List<TextWriterSingle>();
    }

    //Receive the request here "AddWriter_Static("TextMeshPro_component", "AudioClip", "if_sound_end_write", "the_text_write", "speed_write", "is_invisible_character", "remove_Writer_Before_Add", "OnComplete_function")"
    public static TextWriterSingle AddWriter_Static(TextMeshProUGUI uiText, AudioClip Writesound, bool EndSound, string textToWrite, float timePerCharacter, bool invisibleCharacters, bool removeWriterBeforeAdd, Action onComplete) {
        TextWriter.instance.soundWrite = Writesound;
        TextWriter.instance.SoundEnd = EndSound;
        if (removeWriterBeforeAdd) {
            instance.RemoveWriter(uiText);
        }
        TextWriter.instance.talkingAudioSource.clip = TextWriter.instance.sounds[0];
        TextWriter.instance.talkingAudioSource.loop = false;
        TextWriter.instance.talkingAudioSource.Play();
        return instance.AddWriter(uiText, textToWrite, timePerCharacter, invisibleCharacters, onComplete);
    }

    private TextWriterSingle AddWriter(TextMeshProUGUI uiText, string textToWrite, float timePerCharacter, bool invisibleCharacters, Action onComplete) {
        TextWriterSingle textWriterSingle = new TextWriterSingle(uiText, textToWrite, timePerCharacter, invisibleCharacters, onComplete);
        textWriterSingleList.Add(textWriterSingle);
        return textWriterSingle;
    }

    public static void RemoveWriter_Static(TextMeshProUGUI uiText) {
        instance.RemoveWriter(uiText);
    }

    private void RemoveWriter(TextMeshProUGUI uiText) {
        for (int i = 0; i < textWriterSingleList.Count; i++) {
            if (textWriterSingleList[i].GetUIText() == uiText) {
                textWriterSingleList.RemoveAt(i);
                i--;
            }
        }
    }

    private void Update() {
        for (int i = 0; i < textWriterSingleList.Count; i++) {
            bool destroyInstance = textWriterSingleList[i].Update();
            if (destroyInstance) {
                textWriterSingleList.RemoveAt(i);
                i--;
            }
        }
    }

    /*
     * Represents a single TextWriter instance
     * */
    public class TextWriterSingle {

        private TextMeshProUGUI uiText;
        private string textToWrite;
        private int characterIndex;
        private float timePerCharacter;
        private float timer;
        private bool invisibleCharacters;
        private Action onComplete;

        public TextWriterSingle(TextMeshProUGUI uiText, string textToWrite, float timePerCharacter, bool invisibleCharacters, Action onComplete) {
            this.uiText = uiText;
            this.textToWrite = textToWrite;
            this.timePerCharacter = timePerCharacter;
            this.invisibleCharacters = invisibleCharacters;
            this.onComplete = onComplete;
            characterIndex = 0;
        }

        // Returns true on complete
        public bool Update() {
            timer -= Time.deltaTime;
            while (timer <= 0f) {
                // Display next character
                timer += timePerCharacter;
                characterIndex++;
                string text = textToWrite.Substring(0, characterIndex);
                if (invisibleCharacters) {
                    text += "<color=#00000000>" + textToWrite.Substring(characterIndex) + "</color>";
                }
                uiText.text = text;

                if (!TextWriter.instance.talkingAudioSource.isPlaying)
                {
                    TextWriter.instance.talkingAudioSource.clip = TextWriter.instance.soundWrite;
                    TextWriter.instance.talkingAudioSource.loop = true;
                    TextWriter.instance.talkingAudioSource.Play();
                }

                if (characterIndex >= textToWrite.Length) {
                    // Entire string displayed
                    if (onComplete != null) onComplete();
                    if (TextWriter.instance.SoundEnd)
                    {
                        TextWriter.instance.talkingAudioSource.clip = TextWriter.instance.sounds[1];
                        TextWriter.instance.talkingAudioSource.loop = false;
                        TextWriter.instance.talkingAudioSource.Play();
                    }
                    else
                    {
                        TextWriter.instance.talkingAudioSource.Stop();
                    }
                    if(TextWriter.instance.OnFinish != null)
                    {
                        TextWriter.instance.OnFinish.Invoke();
                    }
                    return true;
                }
            }

            return false;
        }

        public TextMeshProUGUI GetUIText() {
            return uiText;
        }

        public bool IsActive() {
            return characterIndex < textToWrite.Length;
        }

        public void WriteAllAndDestroy() {
            uiText.text = textToWrite;
            characterIndex = textToWrite.Length;
            if (onComplete != null) onComplete();
            TextWriter.RemoveWriter_Static(uiText);
        }
    }
}
