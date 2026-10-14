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

    private bool isMuted = true; // Set default variable state to true

    private void Start()
    {
        // Default to 1 (MUTED) on first run
        isMuted = PlayerPrefs.GetInt("IsMuted", 1) == 1;

        // Apply initial state (muted at start)
        ApplyAudioState();

        // Ensure sub-panels are closed at start
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
    }

    // Call this when clicking either audio button
    public void ToggleMute()
    {
        isMuted = !isMuted;

        // Save the updated state
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioState();
    }

    private void ApplyAudioState()
    {
        // Control global Unity audio
        AudioListener.pause = isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;

        // Toggle UI button visibility
        if (muteButton != null && unmuteButton != null)
        {
            // When MUTED: show the Unmute button so the player can turn sound back ON
            unmuteButton.SetActive(isMuted);

            // When NOT muted: show the Mute button so the player can turn sound OFF
            muteButton.SetActive(!isMuted);
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