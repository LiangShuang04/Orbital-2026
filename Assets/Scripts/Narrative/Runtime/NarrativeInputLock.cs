using System.Collections.Generic;
using Akila.FPSFramework;
using DontDiePlease.Central;
using DontDiePlease.Systems;
using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeInputLock : MonoBehaviour
    {
        private readonly List<Behaviour> disabledControls = new List<Behaviour>();
        private CursorLockMode previousLockMode;
        private bool previousCursorVisible;
        private bool locked;

        public void SetLocked(bool value)
        {
            if (value == locked)
            {
                return;
            }

            if (value)
            {
                Lock();
                return;
            }

            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Lock()
        {
            locked = true;
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            disabledControls.Clear();

            DisableIfActive(FindAnyObjectByType<PlayerMovement>());
            DisableIfActive(FindAnyObjectByType<CameraController>());
            DisableIfActive(FindAnyObjectByType<DontDiePlease.Central.FirstPersonController>());
            DisableIfActive(FindAnyObjectByType<SelectionManager>());
            DisableIfActive(FindAnyObjectByType<CharacterInput>());

            foreach (var itemInput in FindObjectsByType<ItemInput>(FindObjectsInactive.Exclude))
            {
                DisableIfActive(itemInput);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Release()
        {
            if (!locked)
            {
                return;
            }

            foreach (var control in disabledControls)
            {
                if (control != null)
                {
                    control.enabled = true;
                }
            }

            disabledControls.Clear();
            var pauseMenu = FindAnyObjectByType<PauseSettingsMenuController>();
            var paused = pauseMenu != null && pauseMenu.IsPaused;
            Cursor.lockState = paused ? CursorLockMode.None : previousLockMode;
            Cursor.visible = paused || previousCursorVisible;
            locked = false;
        }

        private void DisableIfActive(Behaviour control)
        {
            if (control == null || !control.enabled)
            {
                return;
            }

            control.enabled = false;
            disabledControls.Add(control);
        }
    }
}
