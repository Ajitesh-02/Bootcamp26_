using UnityEngine;

/// <summary>
/// Final screen (CoordiScene) shown after the Trap Dungeon is cleared.
/// The QUIT button calls Quit(). Enter also quits.
/// </summary>
public class EndScreen : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Quit();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
