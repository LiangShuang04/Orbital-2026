using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace DontDiePlease.Central
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float crouchSpeed = 2.4f;
        [SerializeField] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float pitchClamp = 85f;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.18f;
        [SerializeField] private float crouchBlendSpeed = 10f;
        [SerializeField] private float headBobAmount = 0.035f;
        [SerializeField] private float headBobRate = 9.5f;
        [SerializeField] private float sprintFov = 68f;
        [SerializeField] private float normalFov = 60f;
        [SerializeField] private float fovBlendSpeed = 8f;

        private CharacterController controller;
        private Camera playerCamera;
        private Vector3 cameraDefaultLocalPosition;
        private float pitch;
        private float verticalVelocity;
        private float headBobTime;
        private bool cursorLocked = true;

        public bool HasMovementInput => ReadMoveInput().sqrMagnitude > 0.01f;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (cameraPivot == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
                cameraPivot = playerCamera != null ? playerCamera.transform : transform;
            }
            else
            {
                playerCamera = cameraPivot.GetComponent<Camera>();
            }

            cameraDefaultLocalPosition = cameraPivot.localPosition;
            normalFov = playerCamera != null ? playerCamera.fieldOfView : normalFov;
            LockCursor(true);
        }

        private void OnEnable()
        {
            LockCursor(true);
        }

        private void Update()
        {
            if (WasCursorTogglePressed())
            {
                LockCursor(!cursorLocked);
            }

            Look();
            Move();
            UpdateCameraPolish();
        }

        public void SetActiveControl(bool active)
        {
            enabled = active;

            if (active)
            {
                LockCursor(true);
            }
        }

        public void ConfigureCameraPivot(Transform value)
        {
            cameraPivot = value;
        }

        private void Look()
        {
            if (!cursorLocked)
                return;

            var look = ReadLookInput() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -pitchClamp, pitchClamp);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void Move()
        {
            var input = ReadMoveInput();
            var move = transform.right * input.x + transform.forward * input.y;

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            var height = IsCrouching() ? crouchingHeight : standingHeight;
            controller.height = Mathf.Lerp(controller.height, height, crouchBlendSpeed * Time.deltaTime);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

            var speed = IsCrouching() ? crouchSpeed : IsSprinting() ? sprintSpeed : walkSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (controller.isGrounded && WasJumpPressed() && !IsCrouching())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private void UpdateCameraPolish()
        {
            if (playerCamera != null)
            {
                var targetFov = IsSprinting() && HasMovementInput && !IsCrouching() ? sprintFov : normalFov;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovBlendSpeed * Time.deltaTime);
            }

            var groundedMovement = controller.isGrounded && HasMovementInput;

            if (groundedMovement)
            {
                var bobSpeed = IsSprinting() ? headBobRate * 1.35f : headBobRate;
                headBobTime += Time.deltaTime * bobSpeed;
                var bob = Mathf.Sin(headBobTime) * headBobAmount;
                cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, cameraDefaultLocalPosition + Vector3.up * bob, 12f * Time.deltaTime);
                return;
            }

            headBobTime = 0f;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, cameraDefaultLocalPosition, 10f * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current == null)
                return Vector2.zero;

            var x = ReadKey(Keyboard.current.dKey, Keyboard.current.rightArrowKey) - ReadKey(Keyboard.current.aKey, Keyboard.current.leftArrowKey);
            var y = ReadKey(Keyboard.current.wKey, Keyboard.current.upArrowKey) - ReadKey(Keyboard.current.sKey, Keyboard.current.downArrowKey);
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        private Vector2 ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.08f : Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        private bool WasJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetButtonDown("Jump");
#endif
        }

        private bool IsSprinting()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        private bool IsCrouching()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftControl);
#endif
        }

        private bool WasCursorTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private float ReadKey(KeyControl primary, KeyControl secondary)
        {
            return (primary != null && primary.isPressed) || (secondary != null && secondary.isPressed) ? 1f : 0f;
        }
#endif

        private void LockCursor(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
