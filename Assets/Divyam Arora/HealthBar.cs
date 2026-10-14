using UnityEngine;
using UnityEngine.UI;
using TMPro; // Remove or comment out if using standard UI Text instead of TextMeshPro

public class HealthBar : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthFillImage;     // Drag the red fill image here
    public TextMeshProUGUI healthText; // Drag your TMP Text here (optional)

    /// <summary>
    /// Call this to initialize or update the health bar fill and text.
    /// </summary>
    /// <param name="currentHealth">Current player health</param>
    /// <param name="maxHealth">Maximum player health</param>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        // Calculate health ratio between 0.0 and 1.0
        float fillAmount = (float)currentHealth / maxHealth;

        // Update UI Fill Image
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = fillAmount;
        }

        // Update Health Text (e.g., "100 / 100")
        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
    }
}