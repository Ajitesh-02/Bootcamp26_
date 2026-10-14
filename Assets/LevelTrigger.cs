using UnityEngine;

public class LevelTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the exact name of the Unity Scene you want to load.")]
    public string sceneToLoad;

    [Tooltip("Enter the level number (1, 2, or 3) for restriction checks.")]
    public int levelNumber;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ensure only the player triggers the scene load
        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TryLoadScene(sceneToLoad, levelNumber);
            }
            else
            {
                Debug.LogError("GameManager instance not found in the scene!");
            }
        }
    }
}