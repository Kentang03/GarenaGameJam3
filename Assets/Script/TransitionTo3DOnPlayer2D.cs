using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// Attach this on a 2D world trigger. When Player enters, plays a Timeline, then switches to 3D mode.
public class TransitionTo3DOnPlayer2D : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool disableTriggerAfterFire = true;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector director; // timeline to play
    [SerializeField] private bool waitForTimelineToFinish = true;
    [Tooltip("If true, switch to 3D at a specific timeline time or frame.")]
    [SerializeField] private bool triggerByTime = false;
    [SerializeField] private float triggerTimeSeconds = 1.0f;
    [Tooltip("Use frame number instead of seconds. Uses TimelineAsset fps if available.")]
    [SerializeField] private bool useFrameNumber = false;
    [SerializeField] private int triggerFrame = 60;

    [Header("Dimension Switch")]
    [SerializeField] private DimensionManager dimensionManager;
    [SerializeField] private int spawnIndex3D = 0; // target spawn index in 3D

    private Collider2D col2d;
    private bool triggered;
    private bool switched;
    private bool monitoringTimeline;
    private float computedTriggerTime;

    void Awake()
    {
        if (dimensionManager == null)
        {
            dimensionManager = FindObjectOfType<DimensionManager>();
        }
        col2d = GetComponent<Collider2D>();
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        triggered = true;

        if (disableTriggerAfterFire && col2d != null)
        {
            col2d.enabled = false;
        }

        if (director != null)
        {
            // compute trigger time if needed
            if (triggerByTime)
            {
                computedTriggerTime = triggerTimeSeconds;
                if (useFrameNumber)
                {
                    var ta = director.playableAsset as TimelineAsset;
                    if (ta != null && ta.editorSettings != null)
                    {
                        var fps = ta.editorSettings.fps;
                        if (fps > 0f)
                        {
                            computedTriggerTime = triggerFrame / fps;
                        }
                    }
                }
            }

            if (waitForTimelineToFinish && !triggerByTime)
            {
                director.stopped += OnTimelineStopped;
                director.Play();
            }
            else if (triggerByTime)
            {
                switched = false;
                monitoringTimeline = true;
                director.Play();
            }
            else
            {
                director.Play();
                SwitchTo3D();
            }
        }
        else
        {
            SwitchTo3D();
        }
    }

    private void OnTimelineStopped(PlayableDirector d)
    {
        d.stopped -= OnTimelineStopped;
        SwitchTo3D();
    }

    private void SwitchTo3D()
    {
        if (dimensionManager == null)
        {
            Debug.LogWarning("TransitionTo3DOnPlayer2D: DimensionManager not found.");
            return;
        }
        dimensionManager.SwitchTo3D(spawnIndex3D);
    }

    void Update()
    {
        if (monitoringTimeline && director != null)
        {
            if (!switched && director.state == PlayState.Playing)
            {
                if (director.time >= computedTriggerTime)
                {
                    switched = true;
                    monitoringTimeline = false;
                    SwitchTo3D();
                }
            }
            else if (director.state != PlayState.Playing)
            {
                monitoringTimeline = false;
            }
        }
    }
}
