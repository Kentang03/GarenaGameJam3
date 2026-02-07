using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private bool is2D = true;
    [SerializeField] private bool useIndex = true;
    [SerializeField] private int targetIndex = 0;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private DimensionManager dimensionManager;

    void Awake()
    {
        if (dimensionManager == null)
        {
            dimensionManager = FindObjectOfType<DimensionManager>();
        }
        if (dimensionManager == null)
        {
            Debug.LogWarning("TeleportPortal: DimensionManager not found in scene.");
        }
    }

    // 2D triggers
    void OnTriggerEnter2D(Collider2D other)
    {
        if (is2D && other.CompareTag(playerTag))
        {
            Teleport2D();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (is2D && collision.collider.CompareTag(playerTag))
        {
            Teleport2D();
        }
    }

    // 3D triggers
    void OnTriggerEnter(Collider other)
    {
        if (!is2D && other.CompareTag(playerTag))
        {
            Teleport3D();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!is2D && collision.collider.CompareTag(playerTag))
        {
            Teleport3D();
        }
    }

    private void Teleport2D()
    {
        if (dimensionManager == null) return;
        if (useIndex)
        {
            dimensionManager.Teleport2D(targetIndex);
        }
        else
        {
            dimensionManager.Teleport2DTo(targetTransform);
        }
    }

    private void Teleport3D()
    {
        if (dimensionManager == null) return;
        if (useIndex)
        {
            dimensionManager.Teleport3D(targetIndex);
        }
        else
        {
            dimensionManager.Teleport3DTo(targetTransform);
        }
    }
}