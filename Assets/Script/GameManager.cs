using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    [SerializeField] private TeleportDataIndex teleportDataIndex;
    public List<Transform> spawnPoint;
    public GameObject player;

    // Position player safely after scene load
    void Start()
    {
        // Fallback: auto-find player by tag if not assigned
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
            {
                player = found;
            }
            else
            {
                Debug.LogError("GameManager: Player reference missing and no GameObject tagged 'Player' found.");
                return;
            }
        }

        if (teleportDataIndex == null)
        {
            Debug.LogError("GameManager: teleportDataIndex reference is missing.");
            return;
        }

        if (spawnPoint == null || spawnPoint.Count == 0)
        {
            Debug.LogError("GameManager: spawnPoint list is empty or not assigned.");
            return;
        }

        var idx = teleportDataIndex.currentIndex;
        if (idx < 0 || idx >= spawnPoint.Count)
        {
            Debug.LogWarning($"GameManager: index {idx} out of range. Clamping to valid range.");
            idx = Mathf.Clamp(idx, 0, spawnPoint.Count - 1);
        }

        var target = spawnPoint[idx];
        if (target == null)
        {
            Debug.LogError($"GameManager: spawnPoint[{idx}] is null.");
            return;
        }

        player.transform.position = target.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
