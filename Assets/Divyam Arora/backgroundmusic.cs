using UnityEngine;

/// <summary>
/// Menu / game background music.
/// - Starts on the Main Menu and keeps playing (looping) through every scene.
/// - Coming back to the Main Menu does NOT start a second copy.
/// - Mute is handled by the Main Menu's mute/unmute buttons (global volume).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
    [Tooltip("Keep the music playing after leaving the Main Menu.")]
    [SerializeField] private bool keepPlayingAcrossScenes = true;

    private static BackgroundMusic instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        // Already playing from an earlier visit to the menu -> don't start it twice
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (keepPlayingAcrossScenes)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;          // loops the track endlessly
        audioSource.spatialBlend = 0f;    // 2D background sound
        audioSource.volume = volume;
        audioSource.ignoreListenerPause = true;
        if (musicClip != null) audioSource.clip = musicClip;
    }

    private void Start()
    {
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
