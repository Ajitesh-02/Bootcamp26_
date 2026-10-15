using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Audio Buttons")]
    [SerializeField] private GameObject muteButton;   // Button to click when you want to mute
    [SerializeField] private GameObject unmuteButton; // Button to click when you want to unmute

    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject instructionsPanel;

    private bool isMuted = false;

    public const string MutePrefKey = "IsMuted";

    /// <summary>Applies the saved mute setting as soon as the game starts, in ANY scene.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedMuteOnStartup()
    {
        // Audio is always on (mute buttons removed)
        PlayerPrefs.DeleteKey(MutePrefKey);
        AudioListener.pause = false;
        AudioListener.volume = 1f;
    }

    private void Start()
    {
        // Sound is ON the first time the game runs; afterwards the player's choice is remembered
        isMuted = false;   // audio always on

        // Buttons not assigned in the Inspector? find them by name ("mute" / "unmute")
        if (muteButton == null || unmuteButton == null) FindAudioButtons();

        // Apply initial state (muted at start)
        ApplyAudioState();

        // Ensure sub-panels are closed at start
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
    }

    // Call this when clicking either audio button
    public void ToggleMute()
    {
        // Mute was removed - audio always stays on. Kept so old button links don't break.
        ApplyAudioState();
    }

    private void ApplyAudioState()
    {
        // Mute = global volume 0. (AudioListener.pause is NOT used: it freezes every sound in the
        // game and sounds started while paused never play.)
        AudioListener.pause = false;
        AudioListener.volume = isMuted ? 0f : 1f;

        // Mute / unmute buttons are removed from the menu: keep them hidden
        if (muteButton != null) muteButton.SetActive(false);
        if (unmuteButton != null) unmuteButton.SetActive(false);
    }

    private void FindAudioButtons()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (Transform t in c.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (muteButton == null && n == "mute") muteButton = t.gameObject;
                if (unmuteButton == null && n == "unmute") unmuteButton = t.gameObject;
            }
        }
    }

    // Options & Instructions Panel Navigation
    public void OpenOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void OpenInstructions()
    {
        if (instructionsPanel != null) instructionsPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void CloseInstructions()
    {
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    // Scene Loading & Application Quit
    public void PlayGame()
    {
        SceneFader.LoadScene("NandiniStartScene");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}