using UnityEngine;

public class Character2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("2D Camera Follow")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private Vector2 cameraOffset = new Vector2(0f, 2f);
    [SerializeField] private float cameraSmoothTime = 0.15f;
    [SerializeField] private float lookAheadDistance = 1.5f;
    [SerializeField] private float lookAheadSmoothing = 5f;
    [SerializeField] private float verticalDeadZone = 0.25f;
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-Mathf.Infinity, -Mathf.Infinity);
    [SerializeField] private Vector2 maxBounds = new Vector2(Mathf.Infinity, Mathf.Infinity);

    private Rigidbody2D rb;
    private float inputX;
    private bool isGrounded;
    private Vector3 camVelocity;
    private float lastX;
    private float lookAheadX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastX = transform.position.x;
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void LateUpdate()
    {
        if (followCamera == null)
        {
            var main = Camera.main;
            if (main != null && main.orthographic)
            {
                followCamera = main;
            }
            else
            {
                return;
            }
        }

        var cam = followCamera;
        var camTr = cam.transform;
        var targetPos = transform.position;

        // Horizontal look-ahead
        var deltaX = targetPos.x - lastX;
        lastX = targetPos.x;
        var targetLookAhead = Mathf.Sign(deltaX) * lookAheadDistance;
        lookAheadX = Mathf.Lerp(lookAheadX, targetLookAhead, Time.deltaTime * lookAheadSmoothing);

        // Vertical dead-zone
        var desiredY = camTr.position.y;
        if (Mathf.Abs(targetPos.y - desiredY) > verticalDeadZone)
        {
            desiredY = targetPos.y;
        }

        var desired = new Vector3(targetPos.x + lookAheadX + cameraOffset.x,
                                  desiredY + cameraOffset.y,
                                  camTr.position.z);

        var nextPos = Vector3.SmoothDamp(camTr.position, desired, ref camVelocity, cameraSmoothTime);

        if (useBounds)
        {
            var halfHeight = cam.orthographicSize;
            var halfWidth = halfHeight * cam.aspect;
            nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
            nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
        }

        camTr.position = nextPos;
    }

    public void SetFollowCamera(Camera cam)
    {
        followCamera = cam;
    }
}
