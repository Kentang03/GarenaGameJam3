using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows UI hint popups when the player is "stuck" (not moving) for configured durations.
/// - Define multiple hint steps with different times and messages (e.g., 15s, 30s).
/// - Resets when player moves again (optional).
/// Attach this to any GameObject (ideally a scene UI manager) and assign the player Transform.
/// </summary>
public class StuckHintPopup : MonoBehaviour
{
    [Serializable]
    public struct HintStep
    {
        public float stuckSeconds;
        [TextArea]
        public string message;
    }

    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float movementThreshold = 0.2f;
    [SerializeField] private bool resetOnMovement = true;
    [SerializeField] private bool startActive = true;

    [Header("Hint Steps (Ascending Times)")]
    [SerializeField] private List<HintStep> hintSteps = new List<HintStep>
    {
        new HintStep{ stuckSeconds = 15f, message = "Coba lompat atau cari jalan lain."},
        new HintStep{ stuckSeconds = 30f, message = "Butuh bantuan? Lihat petunjuk di sudut layar."}
    };

    [Header("UI")]
    [SerializeField] private CanvasGroup popupGroup;
    [SerializeField] private Text popupText;
    [SerializeField] private float fadeDuration = 0.2f;

    private Vector3 _prevPos;
    private float _stuckTimer;
    private int _currentStepIndex;
    private bool _showing;
    private Coroutine _fadeCo;
    private bool _enabled;

    void Awake()
    {
        if (player == null)
        {
            player = FindPlayerTransform();
        }
        _prevPos = player != null ? player.position : Vector3.zero;
        _enabled = startActive;
        SortStepsAscending();

        if (popupGroup != null)
        {
            popupGroup.alpha = 0f;
            popupGroup.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_enabled || player == null || hintSteps.Count == 0)
            return;

        Vector3 pos = player.position;
        float deltaMag = (pos - _prevPos).magnitude;
        bool isMoving = deltaMag >= movementThreshold;

        if (isMoving)
        {
            _prevPos = pos;
            if (resetOnMovement)
            {
                ResetHints();
            }
            return;
        }

        _stuckTimer += Time.deltaTime;

        // Show first step if not showing yet
        if (!_showing && _stuckTimer >= hintSteps[0].stuckSeconds)
        {
            _currentStepIndex = 0;
            ShowMessage(hintSteps[_currentStepIndex].message);
        }
        // If already showing and the timer passed the next step threshold, update message
        else if (_showing && _currentStepIndex < hintSteps.Count - 1)
        {
            var nextIdx = _currentStepIndex + 1;
            if (_stuckTimer >= hintSteps[nextIdx].stuckSeconds)
            {
                _currentStepIndex = nextIdx;
                ShowMessage(hintSteps[_currentStepIndex].message);
            }
        }
    }

    private void ShowMessage(string msg)
    {
        if (popupText != null)
        {
            popupText.text = msg;
        }

        if (popupGroup != null)
        {
            popupGroup.gameObject.SetActive(true);
            StartFade(1f);
        }
        _showing = true;
    }

    private void HideMessage()
    {
        if (popupGroup != null)
        {
            StartFade(0f, onComplete: () =>
            {
                popupGroup.gameObject.SetActive(false);
            });
        }
        _showing = false;
    }

    private void StartFade(float targetAlpha, Action onComplete = null)
    {
        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
        }
        _fadeCo = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
    {
        if (popupGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float start = popupGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            popupGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }
        popupGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    private void SortStepsAscending()
    {
        hintSteps.Sort((a, b) => a.stuckSeconds.CompareTo(b.stuckSeconds));
    }

    private Transform FindPlayerTransform()
    {
        var go = GameObject.FindWithTag("Player");
        return go != null ? go.transform : null;
    }

    /// <summary>
    /// Resets the stuck timer and hides any active popup.
    /// </summary>
    public void ResetHints()
    {
        _stuckTimer = 0f;
        _currentStepIndex = 0;
        if (_showing) HideMessage();
        _showing = false;
    }

    /// <summary>
    /// Enables or disables hint detection at runtime.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled && _showing)
        {
            HideMessage();
            _stuckTimer = 0f;
            _currentStepIndex = 0;
        }
    }
}
