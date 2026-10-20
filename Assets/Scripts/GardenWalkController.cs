
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GardenWalkController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float runSpeed = 4f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private Transform playerCamera;

    private float pitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Camera cam = GetComponentInChildren<Camera>();

        if (cam == null)
        {
            Debug.LogError("Player camera not found!");
            enabled = false;
            return;
        }

        playerCamera = cam.transform;

        // Save the original camera view
        Vector3 originalPosition = playerCamera.position;
        Vector3 originalRotation = playerCamera.eulerAngles;

        // Align the player's body with the camera direction
        transform.rotation = Quaternion.Euler(
            0f,
            originalRotation.y,
            0f
        );

        pitch = Mathf.DeltaAngle(0f, originalRotation.x);
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        // Camera handles vertical looking only
        playerCamera.localRotation = Quaternion.Euler(
            pitch, 0f, 0f
        );

        // Center the camera inside the player collider
        Vector3 localPos = playerCamera.localPosition;
        localPos.x = 0f;
        localPos.z = 0f;
        playerCamera.localPosition = localPos;

        // Preserve the original starting camera position
        transform.position =
            originalPosition - transform.rotation * localPos;
    }

    void Update()
    {
        HandleMouse();
        HandleMovement();
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Rotate player body left and right
            transform.Rotate(
                Vector3.up * mouseX * mouseSensitivity
            );

            // Rotate camera up and down
            pitch -= mouseY * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            playerCamera.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement =
            transform.right * horizontal +
            transform.forward * vertical;

        movement = Vector3.ClampMagnitude(movement, 1f);

        float speed = Input.GetKey(KeyCode.LeftShift)
            ? runSpeed
            : moveSpeed;

        controller.Move(
            movement * speed * Time.deltaTime
        );
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
