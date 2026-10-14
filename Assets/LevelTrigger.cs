using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the exact name of the Unity Scene you want to load.")]
    public string sceneToLoad;

    [Tooltip("Enter the level number (1-6).")]
    public int levelNumber;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ensure only the player triggers the scene load
        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                // Levels 4, 5, and 6 complete Levels 1, 2, and 3
                if (levelNumber >= 4 && levelNumber <= 6)
                {
                    GameManager.Instance.CompleteLevel(levelNumber - 3);

                    // Load directly without progression restrictions
                    if (!string.IsNullOrEmpty(sceneToLoad))
                    {
                        SceneManager.LoadScene(sceneToLoad);
                    }
                    else
                    {
                        Debug.LogError("Scene name is empty!");
                    }
                }
                else
                {
                    // Levels 1, 2, and 3 use the existing restriction logic
                    GameManager.Instance.TryLoadScene(
                        sceneToLoad, levelNumber
                    );
                }
            }
            else
            {
                Debug.LogError("GameManager instance not found in the scene!");
            }
        }
    }
}