using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadToScene : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool loadOnKey = false;
    [SerializeField] private int teleportIndex;
    [SerializeField] private KeyCode triggerKey = KeyCode.Space;
    [SerializeField] private TeleportDataIndex teleportDataIndex;
    [SerializeField] private bool isTrigger;
    private bool isLoading;

    [Header("Single Scene Mode")]
    [SerializeField] private bool useSingleScene = false;
    [SerializeField] private GameDimension targetDimension = GameDimension.TwoD;
    [SerializeField] private DimensionManager dimensionManager;
    [SerializeField] private GameDimension doorDimension = GameDimension.TwoD;

    private void OnEnable()
    {
        DimensionManager.OnDimensionChanged += HandleDimensionChanged;
    }

    private void OnDisable()
    {
        DimensionManager.OnDimensionChanged -= HandleDimensionChanged;
        // Reset stale states when disabled (e.g., scene change or object toggle)
        isTrigger = false;
        isLoading = false;
    }

    private void HandleDimensionChanged(GameDimension mode)
    {
        // Prevent stale trigger from previous dimension causing unintended loads
        isTrigger = false;
        isLoading = false;
    }

    void Start()
    {
        if (loadOnStart)
        {
            Load();
        }
    }

    void Update()
    {
        // In single-scene mode, only respond when the door belongs to the active dimension
        if (useSingleScene)
        {
            if (DimensionManager.CurrentDimension != doorDimension)
            {
                return;
            }
        }

        if (loadOnKey && Input.GetKeyDown(triggerKey) && isTrigger && !isLoading)
        {
            Load();
            
        }
    }

    public void Load()
    {
        if (isLoading)
        {
            return;
        }

        if (teleportDataIndex == null)
        {
            Debug.LogError("LoadToScene: teleportDataIndex reference is missing.");
            return;
        }

        // Single-scene dimension switch
        if (useSingleScene)
        {
            teleportDataIndex.currentIndex = teleportIndex;
            if (dimensionManager == null)
            {
                Debug.LogError("LoadToScene: DimensionManager is not assigned for single-scene mode.");
                return;
            }
            
            dimensionManager.SetMode(targetDimension, teleportIndex);

            return;
        }

        // Multi-scene load
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LoadToScene: sceneName is empty.");
            return;
        }

        isLoading = true;
        teleportDataIndex.currentIndex = teleportIndex;
        var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (async == null)
        {
            Debug.LogError($"LoadToScene: failed to start async load for scene '{sceneName}'.");
            isLoading = false;
        }
    }

    public void ReloadCurrent()
    {
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    // Load scene when this object touches a GameObject tagged "Player"
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isTrigger = false;
        }
    }

    // Also support non-trigger collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isTrigger = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isTrigger = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTrigger = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTrigger = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("Player"))
        {
            isTrigger = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isTrigger = false;
        }
    }
}
