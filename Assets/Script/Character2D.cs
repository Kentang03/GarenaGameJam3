using UnityEngine;

public class Character2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("2D Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isGroundedParam = "IsGrounded";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string velYParam = "VelY";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string animSpeedParam = "AnimSpeedMult";
    [SerializeField] private float walkAnimSpeed = 1f;
    [SerializeField] private float runAnimSpeed = 1.5f;
    [SerializeField] private float groundingIgnoreAfterJump = 0.1f; // seconds to ignore ground right after jump
    [SerializeField] private bool freezeJumpUntilGrounded = true; // hold last jump frame until landing
    [SerializeField] private string jumpStateName = "Jump"; // must match Animator state name
    [SerializeField, Range(0.9f, 1f)] private float freezeAtNormalizedTime = 0.98f; // when to freeze near end
    [SerializeField] private bool loopJumpUntilGrounded = true; // keep playing jump while airborne

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
    private float groundingIgnoreTimer;
    private Vector3 camVelocity;
    private float lastX;
    private float lookAheadX;
    private int hashSpeed, hashIsGrounded, hashVelY, hashJump, hashIsRunning, hashAnimSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastX = transform.position.x;

        if (animator == null) TryGetComponent(out animator);
        if (spriteRenderer == null)
        {
            if (!TryGetComponent(out spriteRenderer))
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        if (spriteRenderer != null)
        {
            // Ensure flipX is disabled when using Y rotation flipping
            spriteRenderer.flipX = false;
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator == null)
        {
            Debug.LogWarning("Character2D: Animator not found on this GameObject or children.");
        }

        hashSpeed = Animator.StringToHash(speedParam);
        hashIsGrounded = Animator.StringToHash(isGroundedParam);
        hashVelY = Animator.StringToHash(velYParam);
        hashJump = Animator.StringToHash(jumpTriggerParam);
        hashIsRunning = Animator.StringToHash(isRunningParam);
        hashAnimSpeed = Animator.StringToHash(animSpeedParam);
    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(inputX) > 0.001f)
        {
            var euler = transform.localEulerAngles;
            // Left: 0°, Right: 180°
            if (inputX > 0f)
            {
                euler.y = 0f;
            }
            else if (inputX < 0f)
            {
                euler.y = 180f;
            }
            transform.localEulerAngles = euler;
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            if (animator != null)
            {
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning("Character2D: Animator has no Controller assigned. Jump trigger will have no effect.");
                }
                animator.SetTrigger(hashJump);
                Debug.Log("Jump Triggered");
            }

            groundingIgnoreTimer = groundingIgnoreAfterJump; // temporarily ignore ground so jump state can play
        }

        if (animator != null)
        {
            var speed01 = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / moveSpeed);
            animator.SetFloat(hashSpeed, speed01, 0.1f, Time.deltaTime);
            var clipSpeed = Mathf.Lerp(walkAnimSpeed, runAnimSpeed, speed01);
            animator.SetFloat(hashAnimSpeed, clipSpeed, 0.1f, Time.deltaTime);
            animator.SetBool(hashIsGrounded, isGrounded);
            animator.SetFloat(hashVelY, rb.linearVelocity.y);
            animator.SetBool(hashIsRunning, Mathf.Abs(inputX) > 0.01f && isGrounded);

            // Jump playback control while airborne
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(jumpStateName))
            {
                if (!isGrounded)
                {
                    if (loopJumpUntilGrounded)
                    {
                        // keep jump playing; restart when it finishes
                        if (info.normalizedTime >= 1f)
                        {
                            animator.Play(jumpStateName, 0, 0f);
                        }
                        if (animator.speed == 0f) animator.speed = 1f;
                    }
                    else if (freezeJumpUntilGrounded && info.normalizedTime >= freezeAtNormalizedTime)
                    {
                        if (animator.speed != 0f) animator.speed = 0f;
                    }
                }
                else if (animator.speed == 0f)
                {
                    animator.speed = 1f;
                }
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        // Raw ground check
        var rawGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Decrease timer
        if (groundingIgnoreTimer > 0f)
        {
            groundingIgnoreTimer -= Time.fixedDeltaTime;
        }

        // Apply ignore window: only consider grounded when timer has elapsed
        isGrounded = rawGrounded && groundingIgnoreTimer <= 0f;
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
