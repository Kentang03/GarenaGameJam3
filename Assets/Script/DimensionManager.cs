using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameDimension { TwoD, ThreeD }

public class DimensionManager : MonoBehaviour
{
    public static event Action<GameDimension> OnDimensionChanged;
    public static GameDimension CurrentDimension { get; private set; }

    [SerializeField] private TeleportDataIndex teleportDataIndex;

    [Header("Player rigs")]
    [SerializeField] private GameObject player2D;
    [SerializeField] private GameObject player3D;

    [Header("Cameras")]
    [SerializeField] private Camera camera2D;
    [SerializeField] private Camera camera3D;

    [Header("Spawns by index")]
    [SerializeField] private List<Transform> spawnPoints2D = new List<Transform>();
    [SerializeField] private List<Transform> spawnPoints3D = new List<Transform>();

    [Header("Defaults & Mapping")]
    [Tooltip("Constant Z used in 2D mode (XY plane)")]
    [SerializeField] private float twoD_Z = 0f;
    [Tooltip("Ground Y height used in 3D mode")]
    [SerializeField] private float threeD_Y = 0f;

    [SerializeField] private GameDimension startMode = GameDimension.TwoD;

    private GameDimension currentMode;

    void Start()
    {
        var idx = teleportDataIndex != null ? teleportDataIndex.currentIndex : 0;
        SetMode(startMode, idx, true);
    }

    public void ToggleMode()
    {
        var next = currentMode == GameDimension.TwoD ? GameDimension.ThreeD : GameDimension.TwoD;
        SetMode(next, teleportDataIndex != null ? teleportDataIndex.currentIndex : 0);
    }

    public void SwitchTo2D(int index)
    {
        SetMode(GameDimension.TwoD, index);
    }

    public void SwitchTo3D(int index)
    {
        SetMode(GameDimension.ThreeD, index);
    }

    public void SetMode(GameDimension mode, int index, bool instant = false)
    {
        if (teleportDataIndex == null)
        {
            Debug.LogError("DimensionManager: TeleportDataIndex reference is missing.");
            return;
        }

        // Clamp index against the target list
        var clampedIndex = ClampIndex(index, mode);
        teleportDataIndex.currentIndex = clampedIndex;

        // Position and activate rigs
        if (mode == GameDimension.TwoD)
        {
            PositionRig(player2D, GetSpawn(spawnPoints2D, clampedIndex), true);
            ActivateRig(player2D, camera2D, true);
            ActivateRig(player3D, camera3D, false);
            // Wire 2D camera via Character2D
            if (player2D != null && camera2D != null)
            {
                var c2d = player2D.GetComponent<Character2D>();
                if (c2d != null)
                {
                    c2d.SetFollowCamera(camera2D);
                }
            }
        }
        else
        {
            PositionRig(player3D, GetSpawn(spawnPoints3D, clampedIndex), false);
            ActivateRig(player3D, camera3D, true);
            ActivateRig(player2D, camera2D, false);
            // Clear 2D follow from Character2D
            if (player2D != null)
            {
                var c2d = player2D.GetComponent<Character2D>();
                if (c2d != null)
                {
                    c2d.SetFollowCamera(null);
                }
            }
        }

        currentMode = mode;
        CurrentDimension = mode;
        OnDimensionChanged?.Invoke(mode);
    }

    private int ClampIndex(int index, GameDimension mode)
    {
        var list = mode == GameDimension.TwoD ? spawnPoints2D : spawnPoints3D;
        if (list == null || list.Count == 0)
        {
            Debug.LogError($"DimensionManager: No spawn points configured for {mode}.");
            return 0;
        }
        if (index < 0 || index >= list.Count)
        {
            Debug.LogWarning($"DimensionManager: index {index} out of range for {mode}. Clamping.");
            return Mathf.Clamp(index, 0, list.Count - 1);
        }
        return index;
    }

    private Transform GetSpawn(List<Transform> list, int index)
    {
        var t = list[index];
        if (t == null)
        {
            Debug.LogError($"DimensionManager: spawnPoints[{index}] is null.");
        }
        return t;
    }

    private void PositionRig(GameObject rig, Transform spawn, bool is2D)
    {
        if (rig == null)
        {
            Debug.LogError("DimensionManager: Rig reference missing.");
            return;
        }

        var tr = rig.transform;
        if (spawn != null)
        {
            tr.position = spawn.position;
        }
        else
        {
            // Map between 2D (XY) and 3D (XZ) if needed
            var p = tr.position;
            if (is2D)
            {
                // Ensure Z constant in 2D mode
                tr.position = new Vector3(p.x, p.y, twoD_Z);
            }
            else
            {
                // Place on ground Y in 3D mode
                tr.position = new Vector3(p.x, threeD_Y, p.z);
            }
        }

        // Reset velocities to avoid carry-over between modes
        var rb2d = rig.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
        var rb3d = rig.GetComponent<Rigidbody>();
        if (rb3d != null)
        {
            rb3d.linearVelocity = Vector3.zero;
            rb3d.angularVelocity = Vector3.zero;
        }
    }

    private void ActivateRig(GameObject rig, Camera cam, bool active)
    {
        if (rig != null) rig.SetActive(active);
        if (cam != null) cam.gameObject.SetActive(active);
    }
}
