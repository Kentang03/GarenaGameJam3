using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaser2D : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target to chase. If null, finds GameObject with the Player tag.")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Movement")]
    [Tooltip("Horizontal move speed while chasing.")]
    public float moveSpeed = 3f;
    [Tooltip("Begin chasing when within this horizontal range.")]
    public float detectionRange = 10f;
    [Tooltip("Stop moving when this close on X.")]
    public float stopDistance = 0.3f;
    [Tooltip("If true, only chase when within detectionRange; if false, always chase.")]
    public bool onlyChaseWhenDetected = true;

    [Header("Facing/Flip")]
    [Tooltip("Use Y-axis rotation flip (left=0°, right=180°) consistent with Character2D.")]
    public bool useYRotationFlip = true;

    [Header("Animator (optional)")]
    [Tooltip("Optional Animator to drive Speed (0..1). If null, will search in children.")]
    public Animator animator;
    [Tooltip("Animator float parameter name for speed (0..1).")]
    public string speedParam = "Speed";

    private int hashSpeed;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) hashSpeed = Animator.StringToHash(speedParam);
    }

    void Start()
    {
        if (target == null && !string.IsNullOrEmpty(playerTag))
        {
            var playerGO = GameObject.FindWithTag(playerTag);
            if (playerGO != null) target = playerGO.transform;
        }
    }

    void FixedUpdate()
    {
        float desiredXVel = 0f;

        if (target != null)
        {
            float dx = target.position.x - transform.position.x;
            float absDx = Mathf.Abs(dx);
            bool withinRange = absDx <= detectionRange || !onlyChaseWhenDetected;
            bool needsMove = absDx > stopDistance;

            if (withinRange && needsMove)
            {
                desiredXVel = Mathf.Sign(dx) * moveSpeed;
            }
        }

        // Apply horizontal velocity only; keep vertical velocity from physics.
        rb.linearVelocity = new Vector2(desiredXVel, rb.linearVelocity.y);

        // Flip facing based on movement direction
        if (useYRotationFlip)
        {
            if (desiredXVel < -0.001f)
            {
                var e = transform.localEulerAngles;
                e.y = 0f; // facing left
                transform.localEulerAngles = e;
            }
            else if (desiredXVel > 0.001f)
            {
                var e = transform.localEulerAngles;
                e.y = 180f; // facing right
                transform.localEulerAngles = e;
            }
        }
        else
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (desiredXVel < -0.001f) sr.flipX = false;
                else if (desiredXVel > 0.001f) sr.flipX = true;
            }
        }

        // Animator speed (normalized 0..1)
        if (animator != null)
        {
            float speed01 = Mathf.InverseLerp(0f, moveSpeed, Mathf.Abs(desiredXVel));
            animator.SetFloat(hashSpeed, speed01);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize horizontal detection range
        Gizmos.color = Color.red;
        Vector3 left = transform.position + Vector3.left * detectionRange;
        Vector3 right = transform.position + Vector3.right * detectionRange;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawSphere(left, 0.05f);
        Gizmos.DrawSphere(right, 0.05f);
    }
}
