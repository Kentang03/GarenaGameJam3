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

        // Compute movement target
        Vector2 dir = moveMode == MoveMode.MoveUp ? Vector2.up
                      : moveMode == MoveMode.MoveDown ? Vector2.down
                      : (customDirection.sqrMagnitude > 0f ? customDirection.normalized : Vector2.down);
        targetPos = startPos + (Vector3)(dir * moveDistance);

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
                StartCoroutine(MoveOtherTarget(target, dir));
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

    private IEnumerator MoveOtherTarget(Transform target, Vector2 dir)
    {
        float duration = moveDistance / Mathf.Max(0.001f, moveSpeed);
        float t = 0f;
        Vector3 from = target.position;
        Vector3 otherTargetPos = from + (Vector3)(dir * moveDistance);

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
}
