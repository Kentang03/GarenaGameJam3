using UnityEngine;

public class Character3D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float rotationSharpness = 12f;
    [SerializeField] private float jumpForce = 6.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Ground")]
    [SerializeField] private float groundedStick = -2f;

    [Header("Camera (Third Person)")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private float camDistance = 6f;
    [SerializeField] private float camHeight = 1.6f;
    [SerializeField] private float yawSensitivity = 240f;
    [SerializeField] private float pitchSensitivity = 160f;
    [SerializeField] private float pitchMin = -30f;
    [SerializeField] private float pitchMax = 65f;
    [SerializeField] private bool lockCursor = true;

    [Header("Camera Anchor")]
    [SerializeField] private Transform cameraAnchor;
    [SerializeField] private Vector3 pivotOffset = Vector3.zero;
    [SerializeField] private bool parentPivotToAnchor = true;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isGroundedParam = "IsGrounded";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string velYParam = "VelY";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string runTriggerParam = "Run";
    [SerializeField] private bool useRunTrigger = true;

    private CharacterController controller;
    private Transform camPivot;
    private float yaw;
    private float pitch;
    private Vector3 velocity;
    private int hashSpeed, hashIsGrounded, hashIsRunning, hashVelY, hashJump, hashRun;
    private bool wasRunning;

    void Awake()
    {
        if (!TryGetComponent(out controller))
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        if (animator == null)
        {
            TryGetComponent(out animator);
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        hashSpeed = Animator.StringToHash(speedParam);
        hashIsGrounded = Animator.StringToHash(isGroundedParam);
        hashVelY = Animator.StringToHash(velYParam);
        hashJump = Animator.StringToHash(jumpTriggerParam);
        hashIsRunning = Animator.StringToHash(isRunningParam);
        hashRun = Animator.StringToHash(runTriggerParam);

        if (followCamera == null)
        {
            var main = Camera.main;
            if (main != null) followCamera = main;
        }

        var pivotGo = new GameObject("CameraPivot");
        camPivot = pivotGo.transform;
        if (cameraAnchor != null && parentPivotToAnchor)
        {
            camPivot.SetParent(cameraAnchor, false);
        }
        else
        {
            camPivot.SetParent(transform, false);
        }
        yaw = transform.eulerAngles.y;
        pitch = 10f;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        var inputX = Input.GetAxisRaw("Horizontal");
        var inputZ = Input.GetAxisRaw("Vertical");

        var camFwd = followCamera != null ? followCamera.transform.forward : Vector3.forward;
        var camRight = followCamera != null ? followCamera.transform.right : Vector3.right;
        camFwd.y = 0f; camRight.y = 0f;
        camFwd.Normalize(); camRight.Normalize();

        var moveDir = (camFwd * inputZ + camRight * inputX);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        var targetSpeed = moveDir.magnitude * moveSpeed;
        var move = moveDir * targetSpeed;
        var isRunningNow = moveDir.sqrMagnitude > 0.0001f && controller.isGrounded;

        if (controller.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = groundedStick;
            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = jumpForce;
                if (animator != null)
                {
                    animator.SetTrigger(hashJump);
                }
            }
        }
        velocity.y += gravity * Time.deltaTime;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSharpness * Time.deltaTime);
        }

        var displacement = new Vector3(move.x, velocity.y, move.z) * Time.deltaTime;
        controller.Move(displacement);

        if (animator != null)
        {
            var speed01 = Mathf.Clamp01(targetSpeed / moveSpeed);
            animator.SetFloat(hashSpeed, speed01, 0.1f, Time.deltaTime);
            animator.SetBool(hashIsGrounded, controller.isGrounded);
            animator.SetBool(hashIsRunning, isRunningNow);
            animator.SetFloat(hashVelY, velocity.y);

            if (useRunTrigger && isRunningNow && !wasRunning)
            {
                animator.SetTrigger(hashRun);
            }
        }

        wasRunning = isRunningNow;
    }

    void LateUpdate()
    {
        if (followCamera == null) return;

        yaw += Input.GetAxis("Mouse X") * yawSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * pitchSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        Vector3 anchorPos = cameraAnchor != null ? cameraAnchor.position : (transform.position + Vector3.up * camHeight);
        camPivot.position = anchorPos + pivotOffset;
        camPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);

        var desiredPos = camPivot.position - camPivot.forward * camDistance;
        followCamera.transform.position = desiredPos;
        followCamera.transform.rotation = Quaternion.LookRotation(camPivot.position - desiredPos, Vector3.up);
    }

    public void SetCameraAnchor(Transform newAnchor, bool parentPivot = true)
    {
        cameraAnchor = newAnchor;
        if (camPivot != null)
        {
            camPivot.SetParent((parentPivot && newAnchor != null) ? newAnchor : transform, false);
        }
    }
}
