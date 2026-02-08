using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
    [System.Serializable]
    public struct SceneTrack
    {
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    public static BackgroundMusicManager Instance;

    [Header("Tracks")]
    [SerializeField] private List<SceneTrack> tracks = new List<SceneTrack>();

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private AudioClip initialClip;
    [SerializeField] private float defaultVolume = 0.8f;
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private bool keepPlayingIfSameClip = true;
    [SerializeField] private bool subscribeSceneLoad = true;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private int activeIndex;
    private AudioClip currentClip;
    private float masterVolume = 1f;
    private float baseActiveVolume = 1f;
    private Coroutine fadeCo;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        ConfigureSource(sourceA);
        ConfigureSource(sourceB);
        activeIndex = 0;

        if (subscribeSceneLoad)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            if (subscribeSceneLoad)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
            Instance = null;
        }
    }

    void Start()
    {
        if (playOnStart)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            var st = GetTrackForScene(sceneName);
            if (st.clip != null) PlayClip(st.clip, st.volume);
            else if (initialClip != null) PlayClip(initialClip, defaultVolume);
        }
    }

    private void ConfigureSource(AudioSource src)
    {
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
    }

    private SceneTrack GetTrackForScene(string sceneName)
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            if (!string.IsNullOrEmpty(tracks[i].sceneName) && tracks[i].sceneName == sceneName)
            {
                return tracks[i];
            }
        }
        return new SceneTrack { sceneName = null, clip = null, volume = defaultVolume };
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var st = GetTrackForScene(scene.name);
        if (st.clip == null) return;
        PlayClip(st.clip, st.volume);
    }

    public void PlayClip(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;
        if (keepPlayingIfSameClip && currentClip == clip)
        {
            SetVolume(volume >= 0f ? volume : defaultVolume);
            return;
        }

        var inactive = activeIndex == 0 ? sourceB : sourceA;
        var active = activeIndex == 0 ? sourceA : sourceB;

        inactive.clip = clip;
        inactive.volume = 0f;
        inactive.Play();

        baseActiveVolume = Mathf.Clamp01(volume >= 0f ? volume : defaultVolume);
        float targetVol = baseActiveVolume * masterVolume;

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Crossfade(active, inactive, targetVol));

        currentClip = clip;
        activeIndex = (activeIndex == 0 ? 1 : 0);
    }

    private System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to, float targetVolume)
    {
        float t = 0f;
        float startFrom = from.volume;
        float startTo = to.volume;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float k = crossfadeDuration <= 0f ? 1f : Mathf.Clamp01(t / crossfadeDuration);
            from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume = Mathf.Lerp(startTo, targetVolume, k);
            yield return null;
        }
        from.volume = 0f;
        from.Stop();
        to.volume = targetVolume;
    }

    public void Stop()
    {
        sourceA.Stop();
        sourceB.Stop();
        sourceA.volume = 0f;
        sourceB.volume = 0f;
        currentClip = null;
    }

    public void SetVolume(float vol)
    {
        baseActiveVolume = Mathf.Clamp01(vol);
        var active = activeIndex == 0 ? sourceA : sourceB;
        if (active.isPlaying)
            active.volume = baseActiveVolume * masterVolume;
    }

    public void SetMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        var active = activeIndex == 0 ? sourceA : sourceB;
        if (active.isPlaying)
            active.volume = baseActiveVolume * masterVolume;
    }

    public void Mute(bool mute)
    {
        SetMasterVolume(mute ? 0f : 1f);
    }

    public void PlayBySceneName(string sceneName)
    {
        var st = GetTrackForScene(sceneName);
        if (st.clip != null) PlayClip(st.clip, st.volume);
    }
}
