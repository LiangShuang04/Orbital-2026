using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Auto-find player body as parent if not assigned
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }

        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Get mouse input (delta — already framerate-independent, don't multiply by Time.deltaTime)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Pitch (look up/down) on the camera itself
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw (look left/right) on the player body so movement direction follows the camera
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            Debug.LogWarning("CameraController: playerBody is not assigned and no parent was found.");
        }
    }
}
