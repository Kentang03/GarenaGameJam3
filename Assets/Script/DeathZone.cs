using UnityEngine;
using System.Collections;

public class DeathZone : MonoBehaviour
{
    [Header("Death Zone Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float respawnDelay = 0f;
    [SerializeField] private DimensionManager dimensionManager;

    [Header("Death VFX")]
    [Tooltip("Optional particle prefab to play at the player's position before respawn.")]
    [SerializeField] private ParticleSystem deathParticlePrefab;
    [Tooltip("If true, destroys spawned particle GameObject on respawn. If false, let it finish naturally.")]
    [SerializeField] private bool destroyVfxOnRespawn = true;

    [Header("Camera Shake")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeAmplitude = 0.25f;
    [SerializeField] private bool useUnscaledTime = false;
    private Coroutine shakeRoutine;

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
            HandleDeath(other.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            HandleDeath(collision.collider.gameObject);
        }
    }

    // 3D physics
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            HandleDeath(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            HandleDeath(collision.collider.gameObject);
        }
    }

    private void HandleDeath(GameObject player)
    {
        if (dimensionManager == null) return;
        ParticleSystem spawned = null;
        if (deathParticlePrefab != null && player != null)
        {
            spawned = Instantiate(deathParticlePrefab, player.transform.position, Quaternion.identity);
            // Ensure it plays
            spawned.Play();
        }

        if (enableCameraShake)
        {
            StartCameraShake();
        }

        // Reset moved traps/platforms and objects to their initial positions before respawn
        ResetMovedObjectsGlobal();
        if (respawnDelay > 0f)
        {
            StartCoroutine(RespawnAfterDelay(spawned));
        }
        else
        {
            if (destroyVfxOnRespawn && spawned != null)
            {
                Destroy(spawned.gameObject);
            }
            dimensionManager.Respawn();
        }
    }

    private IEnumerator RespawnAfterDelay(ParticleSystem spawned)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (destroyVfxOnRespawn && spawned != null)
        {
            Destroy(spawned.gameObject);
        }
        dimensionManager.Respawn();
    }

    private void ResetMovedObjectsGlobal()
    {
        var movers = FindObjectsOfType<TriggeredPlatformMover2D>();
        for (int i = 0; i < movers.Length; i++)
        {
            if (movers[i] == null) continue;
            movers[i].ResetToInitial();
        }
    }

    private void StartCameraShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        var cam = GetActiveCamera();
        if (cam == null) return;
        shakeRoutine = StartCoroutine(ShakeCamera(cam));
    }

    private IEnumerator ShakeCamera(Camera cam)
    {
        var tr = cam.transform;
        var original = tr.localPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Vector2 offset2 = Random.insideUnitCircle * shakeAmplitude;
            tr.localPosition = new Vector3(original.x + offset2.x, original.y + offset2.y, original.z);
            yield return null;
        }
        tr.localPosition = original;
        shakeRoutine = null;
    }

    private Camera GetActiveCamera()
    {
        var cam = Camera.main;
        if (cam != null && cam.isActiveAndEnabled) return cam;
        var all = Camera.allCameras;
        if (all != null)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].isActiveAndEnabled)
                {
                    return all[i];
                }
            }
        }
        return null;
    }
}
