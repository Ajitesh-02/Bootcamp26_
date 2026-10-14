using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton instance so other scripts can easily access it
    public static GameManager Instance;

    [Header("Level Progress Tracking")]
    [Tooltip("Tracks if Level 1 has been completed.")]
    public bool isLevel1Complete = false;

    [Tooltip("Tracks if Level 2 has been completed.")]
    public bool isLevel2Complete = false;

    [Tooltip("Tracks if Level 3 has been completed.")]
    public bool isLevel3Complete = false;

    void Awake()
    {
        // Simple Singleton setup (You can expand this later when adding DontDestroyOnLoad)
        if (Instance == null)
        {
            Instance = this;
            // Uncomment the line below later when you are ready to make it persist across scenes:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this method from your level completion triggers/goals.
    /// </summary>
    public void CompleteLevel(int levelNumber)
    {
        if (levelNumber == 1) isLevel1Complete = true;
        else if (levelNumber == 2) isLevel2Complete = true;
        else if (levelNumber == 3) isLevel3Complete = true;

        Debug.Log("Level " + levelNumber + " completed!");
    }

    /// <summary>
    /// Attempts to load a scene based on the level rules.
    /// </summary>
    public void TryLoadScene(string sceneName, int targetLevel)
    {
        // Check progression locks before loading
        if (targetLevel == 2 && !isLevel1Complete)
        {
            Debug.LogWarning("Level 2 is locked! You must complete Level 1 first.");
            return;
        }

        if (targetLevel == 3 && (!isLevel1Complete || !isLevel2Complete))
        {
            Debug.LogWarning("Level 3 is locked! You must complete both Level 1 and Level 2 first.");
            return;
        }

        // If checks pass, load the scene
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is empty! Please assign a valid scene name in the inspector.");
        }
    }
}