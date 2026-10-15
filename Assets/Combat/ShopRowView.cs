using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One row in the Guide's shop (part of the GuideUI prefab).</summary>
public class ShopRowView : MonoBehaviour
{
    public Image background;
    public Sprite normalBackground;
    public Sprite highlightBackground;     // used when the player can afford it
    public Image icon;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public TextMeshProUGUI price;
    public TextMeshProUGUI status;

    public void Set(string titleText, string descText, int cost, string statusText, bool highlight)
    {
        if (title != null) title.text = titleText;
        if (description != null) description.text = descText;
        if (price != null) price.text = cost.ToString();
        if (status != null) status.text = statusText;
        if (background != null)
        {
            Sprite s = highlight ? highlightBackground : normalBackground;
            if (s != null) background.sprite = s;
        }
    }
}
