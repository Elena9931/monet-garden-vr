using UnityEngine;

namespace MonetGarden
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-100)]
    public class GardenDesktopController : MonoBehaviour
    {
        public Camera viewCamera;
        public float walkSpeed = 0.65f, runSpeed = 1.3f, sensitivity = 2f;
        public float gravity = -9.81f, eyeHeight = 0.82f, maximumFall = 3f;
        public LayerMask groundLayers = ~0;
        public bool useGravity = true;
        public bool InputEnabled { get; private set; }
        public bool Grounded => capsule && capsule.isGrounded;
        public Vector3 SpawnPosition { get; private set; }
        public Quaternion SpawnLook { get; private set; }
        CharacterController capsule;
        float pitch, verticalSpeed;
        bool gravityReady;

        void Awake()
        {
            capsule = GetComponent<CharacterController>();
            if (!viewCamera) viewCamera = GetComponentInChildren<Camera>();
            if (!viewCamera) { Debug.LogError("Monet: player needs a child camera.", this); enabled = false; return; }
            Vector3 eye = viewCamera.transform.position;
            Quaternion look = viewCamera.transform.rotation;
            capsule.enabled = false;
            transform.SetPositionAndRotation(eye - Vector3.up * eyeHeight, Quaternion.Euler(0, look.eulerAngles.y, 0));
            viewCamera.transform.localPosition = Vector3.up * eyeHeight;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0, look.eulerAngles.x), -80, 80);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            capsule.enabled = true;
            SpawnPosition = transform.position;
            SpawnLook = viewCamera.transform.rotation;
            Physics.SyncTransforms();
            // Never enable free fall before finding a real proxy floor under this capsule.
            gravityReady = HasGroundBelow();
            if (useGravity && !gravityReady) Debug.LogWarning("Monet: no floor under spawn. Gravity is suspended until a proxy floor is found.", this);
        }

        bool HasGroundBelow()
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down,
                out _, 0.6f, groundLayers, QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ReleaseCursor();
            if (InputEnabled && Input.GetMouseButtonDown(0)) CaptureCursor();
            if (InputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivity, 0);
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * sensitivity, -80, 80);
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            Vector3 horizontal = Vector3.zero;
            if (InputEnabled)
            {
                horizontal = Vector3.ClampMagnitude(transform.right * Input.GetAxisRaw("Horizontal") +
                    transform.forward * Input.GetAxisRaw("Vertical"), 1);
                horizontal *= Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            }
            if (useGravity && !gravityReady) gravityReady = HasGroundBelow();
            if (useGravity && gravityReady)
            {
                if (capsule.isGrounded && verticalSpeed < 0) verticalSpeed = -1f;
                verticalSpeed = Mathf.Max(verticalSpeed + gravity * Time.deltaTime, -15f);
            }
            else verticalSpeed = 0;
            if (capsule.enabled) capsule.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (transform.position.y < SpawnPosition.y - maximumFall) ResetToSpawn();
        }

        public void SetInput(bool value)
        {
            InputEnabled = value;
            ReleaseCursor(); // Explicit click resumes mouse control; UI clicks never rotate the camera.
        }
        public void CaptureCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        public void ReleaseCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void ResetToSpawn()
        {
            capsule.enabled = false;
            transform.SetPositionAndRotation(SpawnPosition, Quaternion.Euler(0, SpawnLook.eulerAngles.y, 0));
            pitch = Mathf.DeltaAngle(0, SpawnLook.eulerAngles.x);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            verticalSpeed = 0;
            capsule.enabled = true;
            Physics.SyncTransforms();
            gravityReady = HasGroundBelow();
            ReleaseCursor();
        }
        void OnDisable() { ReleaseCursor(); }
        void OnApplicationFocus(bool focus) { if (!focus) ReleaseCursor(); }
    }
}
