using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DraggablePlaceable2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Rigidbody2D rb;

    [Header("Drag & Drop")]
    [SerializeField] private bool allowDrag = true;
    [SerializeField] private bool snapToGrid = true;
    [SerializeField] private float gridSize = 0.5f;
    [Tooltip("Layers that invalidate drop; if overlapping, object will still place but you can use this to warn.")]
    [SerializeField] private LayerMask invalidDropMask = 0;

    [Header("Placement Behaviour")]
    [SerializeField] private bool disableColliderWhileDragging = true;
    [SerializeField] private bool enableColliderOnDrop = true;
    [Tooltip("Body type applied when dropped. Dynamic makes it a physical object.")]
    [SerializeField] private RigidbodyType2D bodyTypeOnDrop = RigidbodyType2D.Dynamic;
    [SerializeField] private string groundLayerName = "Default"; // layer for player to stand on

    private bool dragging = false;
    private Vector3 grabOffset;

    void Awake()
    {
        if (sceneCamera == null)
        {
            var main = Camera.main;
            if (main != null) sceneCamera = main;
        }
        if (boxCollider == null) TryGetComponent(out boxCollider);
        if (rb == null) TryGetComponent(out rb);

        // Ensure collider exists
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // If there's a rigidbody, start as kinematic while editing/dragging
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }
    }

    void OnMouseDown()
    {
        if (!allowDrag) return;
        dragging = true;
        var mouseWorld = GetMouseWorld();
        grabOffset = transform.position - mouseWorld;
        if (disableColliderWhileDragging && boxCollider != null)
        {
            boxCollider.enabled = false;
        }
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void OnMouseDrag()
    {
        if (!dragging) return;
        var target = GetMouseWorld() + grabOffset;
        target.z = transform.position.z; // keep Z constant in 2D plane
        transform.position = target;
    }

    void OnMouseUp()
    {
        if (!dragging) return;
        dragging = false;

        if (snapToGrid)
        {
            var p = transform.position;
            p.x = Mathf.Round(p.x / gridSize) * gridSize;
            p.y = Mathf.Round(p.y / gridSize) * gridSize;
            transform.position = p;
        }

        if (enableColliderOnDrop && boxCollider != null)
        {
            boxCollider.enabled = true;
            boxCollider.isTrigger = false; // act as solid platform
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // set dynamic on drop
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            if (rb.gravityScale <= 0f) rb.gravityScale = 1f;
        }

        var groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (groundLayer >= 0)
        {
            gameObject.layer = groundLayer;
        }
    }

    private Vector3 GetMouseWorld()
    {
        var mp = Input.mousePosition;
        if (sceneCamera == null) sceneCamera = Camera.main;
        var world = sceneCamera != null ? sceneCamera.ScreenToWorldPoint(mp) : new Vector3(mp.x, mp.y, 0f);
        return world;
    }
}
