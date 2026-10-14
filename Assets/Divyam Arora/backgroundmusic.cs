using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (musicClip != null)
        {
            audioSource.clip = musicClip;
            audioSource.loop = true;         // Loops the 2-minute track endlessly
            audioSource.playOnAwake = true;
            audioSource.spatialBlend = 0f;   // Sets to 2D background sound
            audioSource.Play();
        }
    }
}