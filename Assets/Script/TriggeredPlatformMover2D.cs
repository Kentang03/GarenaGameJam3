using System.Collections;
using UnityEngine;

/// <summary>
/// Cat Mario-style trap platform: when the player approaches or touches it,
/// it moves up/down or switches to Dynamic so the player falls.
/// Configure activation mode, direction, distance, speed, and whether to make
/// the platform pass-through or disable colliders on trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TriggeredPlatformMover2D : MonoBehaviour
{
    [Header("Activation")]
    public string playerTag = "Player";
    public enum ActivationMode { Proximity2D, TriggerEnter2D, CollisionEnter2D }
    public ActivationMode activation = ActivationMode.Proximity2D;
    [Tooltip("For proximity activation: distance from player to trigger.")]
    public float proximityRange = 2f;
    [Tooltip("Optional delay before movement starts.")]
    public float delayBeforeMove = 0f;
    [Tooltip("If true, triggers only once; otherwise can retrigger.")]
    public bool oneShot = true;

    
    
    [Header("Movement")]
    [Tooltip("Used when MoveMode=Custom; normalized automatically if not.")]
    public Vector2 customDirection = Vector2.zero;
    public enum MoveMode { MoveUp, MoveDown, Custom }
    public MoveMode moveMode = MoveMode.MoveDown;
    [Tooltip("Distance to move along direction.")]
    public float moveDistance = 2f;
    [Tooltip("Movement speed in units per second.")]
    public float moveSpeed = 3f;

    public enum TargetMode { DirectionDistance, TargetTransform, TargetPosition }
    [Header("Targeting")]
    
    [Tooltip("Choose how the target is defined: by direction+distance, a target transform, or an explicit position.")]
    public TargetMode targetMode = TargetMode.DirectionDistance;
    [Tooltip("Target transform to move towards (world position). Used when TargetMode=TargetTransform.")]
    public Transform targetTransform;
    [Tooltip("Explicit target position. If 'Is Local' is enabled, this is local to the platform's transform.")]
    public Vector3 targetPosition;
    [Tooltip("Treat 'Target Position' as local to this transform.")]
    public bool targetPositionIsLocal = false;
    [Tooltip("Normalize custom direction when using DirectionDistance.")]
    public bool normalizeCustomDirection = true;
    [Tooltip("Constrain movement to horizontal axis (X) by zeroing vertical component.")]
    public bool constrainHorizontal = false;
    [Tooltip("Constrain movement to vertical axis (Y) by zeroing horizontal component.")]
    public bool constrainVertical = false;

    [Header("Affect Other Object")]
    [Tooltip("If true, when triggered by collision/trigger, also move the colliding object using the same direction/distance/speed.")]
    public bool moveCollidingObject = false;
    [Tooltip("Optional: Specific object to move on trigger. If set, this target will be moved; otherwise the colliding object will be moved.")]
    public Transform otherObjectToMove;

    [Header("Trap Behavior")]
    [Tooltip("Make colliders pass-through on trigger so player falls through.")]
    public bool makePassThroughOnTrigger = true;
    [Tooltip("Disable colliders entirely on trigger (harder fall).")]
    public bool disableCollidersOnTrigger = false;
    [Tooltip("Instead of moving, switch Rigidbody2D to Dynamic so it falls with gravity.")]
    public bool changeBodyToDynamic = false;
    [Tooltip("Gravity scale when switching to Dynamic.")]
    public float dynamicGravityScale = 1f;

    private Rigidbody2D rb;
    private Collider2D[] colliders2D;
    private bool triggered;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Coroutine moveRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colliders2D = GetComponentsInChildren<Collider2D>();
        startPos = transform.position;
    }

    void Update()
    {
        if (triggered && oneShot) return;
        if (activation != ActivationMode.Proximity2D) return;

        var player = FindPlayerByTag();
        if (player == null) return;

        float dist = Vector2.Distance(player.position, transform.position);
        if (dist <= proximityRange)
        {
            TriggerTrap();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activation == ActivationMode.TriggerEnter2D && other.CompareTag(playerTag))
        {
            TriggerTrap(other.transform);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (activation == ActivationMode.CollisionEnter2D && collision.collider.CompareTag(playerTag))
        {
            TriggerTrap(collision.collider.transform);
        }
    }

    private void TriggerTrap(Transform otherTarget = null)
    {
        if (triggered && oneShot) return;
        triggered = true;

        // Pass-through or disable colliders to ensure player falls
        if (colliders2D != null)
        {
            foreach (var c in colliders2D)
            {
                if (disableCollidersOnTrigger) c.enabled = false;
                else if (makePassThroughOnTrigger) c.isTrigger = true;
            }
        }

        // If we are only moving the other object, keep this object static (do not change body type)
        if (changeBodyToDynamic && rb != null && !moveCollidingObject)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = dynamicGravityScale;
            return; // let physics handle falling
        }

        // Compute movement target using selected targeting mode
        Vector2 usedDir;
        targetPos = ComputeTargetPosition(startPos, out usedDir);

        if (!moveCollidingObject)
        {
            // Move this trap/platform itself
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveToTarget());
        }
        else
        {
            // Only move another object; this object acts purely as the trigger
            var target = otherObjectToMove != null ? otherObjectToMove : otherTarget;
            if (target != null)
            {
                // Move the other object by the same delta the platform will travel
                var deltaVec = targetPos - startPos;
                StartCoroutine(MoveOtherTarget(target, deltaVec));
            }
        }
    }

    private IEnumerator MoveToTarget()
    {
        if (delayBeforeMove > 0f) yield return new WaitForSeconds(delayBeforeMove);

        float duration = moveDistance / Mathf.Max(0.001f, moveSpeed);
        float t = 0f;
        Vector3 from = transform.position;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            Vector3 next = Vector3.Lerp(from, targetPos, k);

            if (rb != null && rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.MovePosition(next);
            }
            else
            {
                transform.position = next;
            }
            yield return null;
        }

        // Reset for potential re-trigger if not one-shot
        if (!oneShot)
        {
            triggered = false;
            startPos = transform.position;
        }
    }

    private Transform FindPlayerByTag()
    {
        var playerGO = GameObject.FindWithTag(playerTag);
        return playerGO != null ? playerGO.transform : null;
    }

    private IEnumerator MoveOtherTarget(Transform target, Vector3 deltaVec)
    {
        // Match platform's delay so timing feels identical
        if (delayBeforeMove > 0f) yield return new WaitForSeconds(delayBeforeMove);

        float duration = moveDistance / Mathf.Max(0.001f, moveSpeed);
        float t = 0f;
        Vector3 from = target.position;
        Vector3 otherTargetPos = from + deltaVec;

        var otherRb = target.GetComponent<Rigidbody2D>();
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            Vector3 next = Vector3.Lerp(from, otherTargetPos, k);

            if (otherRb != null && otherRb.bodyType != RigidbodyType2D.Dynamic)
            {
                otherRb.MovePosition(next);
            }
            else
            {
                target.position = next;
            }
            yield return null;
        }
    }

    private Vector3 ComputeTargetPosition(Vector3 from, out Vector2 usedDir)
    {
        usedDir = Vector2.zero;
        switch (targetMode)
        {
            case TargetMode.DirectionDistance:
            {
                Vector2 dir = moveMode == MoveMode.MoveUp ? Vector2.up
                              : moveMode == MoveMode.MoveDown ? Vector2.down
                              : customDirection;
                if (normalizeCustomDirection && dir.sqrMagnitude > 0f) dir = dir.normalized;
                if (constrainHorizontal) dir.y = 0f;
                if (constrainVertical) dir.x = 0f;
                if (dir.sqrMagnitude == 0f) dir = Vector2.down; // safe default
                usedDir = dir;
                return from + (Vector3)(dir * moveDistance);
            }
            case TargetMode.TargetTransform:
            {
                var to = targetTransform != null ? targetTransform.position : from;
                if (constrainHorizontal) to.y = from.y;
                if (constrainVertical) to.x = from.x;
                usedDir = (to - from).sqrMagnitude > 0f ? (to - from).normalized : Vector2.zero;
                return to;
            }
            case TargetMode.TargetPosition:
            default:
            {
                var to = targetPositionIsLocal ? transform.TransformPoint(targetPosition) : targetPosition;
                if (to == Vector3.zero && !targetPositionIsLocal) to = from; // unchanged
                if (constrainHorizontal) to.y = from.y;
                if (constrainVertical) to.x = from.x;
                usedDir = (to - from).sqrMagnitude > 0f ? (to - from).normalized : Vector2.zero;
                return to;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw a line from current position to computed target for easy authoring
        Gizmos.color = Color.cyan;
        Vector3 platformFrom = transform.position;
        Vector2 _;
        Vector3 platformTo = ComputeTargetPosition(platformFrom, out _);

        // Default gizmo: platform movement from -> to
        Vector3 from = platformFrom;
        Vector3 to = platformTo;

        // If moving another object, anchor gizmo from that object's position to its target
        if (moveCollidingObject && otherObjectToMove != null)
        {
            from = otherObjectToMove.position;
            Vector3 delta = platformTo - platformFrom;
            to = from + delta;
        }
        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(from, 0.05f);
        Gizmos.DrawSphere(to, 0.05f);
    }
}
