using UnityEngine;
using System.Collections;

public class DeathZone : MonoBehaviour
{
    [Header("Death Zone Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float respawnDelay = 0f;
    [SerializeField] private DimensionManager dimensionManager;

    void Awake()
    {
        if (dimensionManager == null)
        {
            dimensionManager = FindObjectOfType<DimensionManager>();
        }
        if (dimensionManager == null)
        {
            Debug.LogWarning("DeathZone: DimensionManager not found in scene.");
        }
    }

    // 2D physics
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            HandleDeath();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            HandleDeath();
        }
    }

    // 3D physics
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            HandleDeath();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (dimensionManager == null) return;
        if (respawnDelay > 0f)
        {
            StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            dimensionManager.Respawn();
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        dimensionManager.Respawn();
    }
}
