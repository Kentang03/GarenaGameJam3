using UnityEngine;

/// <summary>
/// Smooth 2D side-scroller camera follow with look-ahead, vertical dead-zone, and bounds.
/// Attach to the 2D camera (orthographic) and assign the player.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 2f);

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.15f;
    [SerializeField] private float lookAheadDistance = 1.5f;
    [SerializeField] private float lookAheadSmoothing = 5f;
    [SerializeField] private float verticalDeadZone = 0.25f;

    [Header("Bounds")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-Mathf.Infinity, -Mathf.Infinity);
    [SerializeField] private Vector2 maxBounds = new Vector2(Mathf.Infinity, Mathf.Infinity);
    [Tooltip("Ensure full camera view stays within bounds using orthographic size.")]
    [SerializeField] private bool confineViewWithinBounds = true;

    private Camera cam;
    private Vector3 currentVelocity;
    private float lastTargetX;
    private float lookAheadX;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null && !cam.orthographic)
        {
            Debug.LogWarning("CameraFollow2D: Camera is not orthographic. Consider switching to orthographic for 2D.");
        }
        lastTargetX = target != null ? target.position.x : 0f;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        if (target != null)
        {
            lastTargetX = target.position.x;
            lookAheadX = 0f;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            // Fallback: try to find by tag
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
            {
                SetTarget(found.transform);
            }
            else
            {
                return;
            }
        }

        var targetPos = target.position;

        // Horizontal look-ahead based on movement direction
        var deltaX = targetPos.x - lastTargetX;
        lastTargetX = targetPos.x;
        var targetLookAhead = Mathf.Sign(deltaX) * lookAheadDistance;
        lookAheadX = Mathf.Lerp(lookAheadX, targetLookAhead, Time.deltaTime * lookAheadSmoothing);

        // Vertical dead-zone to reduce jitter from small jumps
        var desiredY = transform.position.y;
        if (Mathf.Abs(targetPos.y - desiredY) > verticalDeadZone)
        {
            desiredY = targetPos.y;
        }

        // Desired camera center position
        var desired = new Vector3(targetPos.x + lookAheadX + offset.x, desiredY + offset.y, transform.position.z);

        // Smoothly move towards desired
        var nextPos = Vector3.SmoothDamp(transform.position, desired, ref currentVelocity, positionSmoothTime);

        // Apply bounds
        if (useBounds)
        {
            if (confineViewWithinBounds && cam != null)
            {
                var halfHeight = cam.orthographicSize;
                var halfWidth = halfHeight * cam.aspect;
                nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
                nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
            }
            else
            {
                nextPos.x = Mathf.Clamp(nextPos.x, minBounds.x, maxBounds.x);
                nextPos.y = Mathf.Clamp(nextPos.y, minBounds.y, maxBounds.y);
            }
        }

        transform.position = nextPos;
    }
}
