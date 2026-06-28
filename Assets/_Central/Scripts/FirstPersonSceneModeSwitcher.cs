using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DontDiePlease.Central
{
    public sealed class FirstPersonSceneModeSwitcher : MonoBehaviour
    {
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener playerAudioListener;
        [SerializeField] private Camera freeCamera;
        [SerializeField] private AudioListener freeCameraAudioListener;
        [SerializeField] private MonoBehaviour freeCameraController;
        [SerializeField] private KeyCode legacyToggleKey = KeyCode.F;

        private bool usingFreeCamera;

        private void Awake()
        {
            ApplyFirstPersonMode();
        }

        private void Update()
        {
            if (!WasTogglePressed())
                return;

            if (usingFreeCamera)
                ApplyFirstPersonMode();
            else
                ApplyFreeCameraMode();
        }

        public void Configure(FirstPersonController fpsController, Camera fpsCamera, AudioListener fpsAudio, Camera editorCamera, AudioListener editorAudio, MonoBehaviour editorCameraController)
        {
            playerController = fpsController;
            playerCamera = fpsCamera;
            playerAudioListener = fpsAudio;
            freeCamera = editorCamera;
            freeCameraAudioListener = editorAudio;
            freeCameraController = editorCameraController;
        }

        private void ApplyFirstPersonMode()
        {
            usingFreeCamera = false;

            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.tag = "MainCamera";
            }

            if (playerAudioListener != null)
                playerAudioListener.enabled = true;

            if (playerController != null)
                playerController.SetActiveControl(true);

            if (freeCamera != null)
            {
                freeCamera.enabled = false;
                freeCamera.tag = "Untagged";
            }

            if (freeCameraAudioListener != null)
                freeCameraAudioListener.enabled = false;

            if (freeCameraController != null)
                freeCameraController.enabled = false;
        }

        private void ApplyFreeCameraMode()
        {
            usingFreeCamera = true;

            if (playerController != null)
                playerController.SetActiveControl(false);

            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                playerCamera.tag = "Untagged";
            }

            if (playerAudioListener != null)
                playerAudioListener.enabled = false;

            if (freeCamera != null)
            {
                freeCamera.enabled = true;
                freeCamera.tag = "MainCamera";
            }

            if (freeCameraAudioListener != null)
                freeCameraAudioListener.enabled = true;

            if (freeCameraController != null)
                freeCameraController.enabled = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(legacyToggleKey);
#endif
        }
    }
}
