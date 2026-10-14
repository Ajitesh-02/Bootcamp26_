using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the exact name of the Unity Scene you want to load.")]
    public string sceneToLoad;

    [Tooltip("Enter the level number (1-6).")]
    public int levelNumber;

    [Header("Lock (optional)")]
    [Tooltip("Item the player must carry for this trigger to work, e.g. Crystal on a dungeon exit. None = always works.")]
    public CollectibleType requiredItem = CollectibleType.None;
    public string lockedMessage = "You need the Crystal to escape!";
    private float nextMessageTime;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ensure only the player triggers the scene load
        if (collision.CompareTag("Player"))
        {
            // Exit locked until the required item is collected
            if (!PlayerInventory.Has(requiredItem))
            {
                if (Time.time >= nextMessageTime)
                {
                    GameHUD.Instance.ShowMessage(lockedMessage);
                    nextMessageTime = Time.time + 2.5f;
                }
                return;
            }

            if (GameManager.Instance != null)
            {
                // Levels 4, 5, and 6 complete Levels 1, 2, and 3
                if (levelNumber >= 4 && levelNumber <= 6)
                {
                    GameManager.Instance.CompleteLevel(levelNumber - 3);

                    // Load directly without progression restrictions
                    if (!string.IsNullOrEmpty(sceneToLoad))
                    {
                        SceneFader.LoadScene(sceneToLoad);
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
            else if (levelNumber >= 4 && levelNumber <= 6 && !string.IsNullOrEmpty(sceneToLoad))
            {
                // Started this dungeon directly in the editor (no GameManager yet): still allow going back
                SceneFader.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogError("GameManager instance not found in the scene!");
            }
        }
    }
}