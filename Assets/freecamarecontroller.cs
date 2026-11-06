
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.15f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Mouse look
        if (Mouse.current != null &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationY += mouseDelta.x * mouseSensitivity;
            rotationX -= mouseDelta.y * mouseSensitivity;

            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.rotation =
                Quaternion.Euler(rotationX, rotationY, 0f);
        }

        // Keyboard movement
        if (Keyboard.current != null)
        {
            Vector3 movement = Vector3.zero;

            if (Keyboard.current.wKey.isPressed)
                movement += transform.forward;

            if (Keyboard.current.sKey.isPressed)
                movement -= transform.forward;

            if (Keyboard.current.aKey.isPressed)
                movement -= transform.right;

            if (Keyboard.current.dKey.isPressed)
                movement += transform.right;

            if (Keyboard.current.eKey.isPressed)
                movement += Vector3.up;

            if (Keyboard.current.qKey.isPressed)
                movement += Vector3.down;

            transform.position +=
                movement.normalized * moveSpeed * Time.deltaTime;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;
        }

        // Click to lock mouse again
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}

