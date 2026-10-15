using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// References into the Guide's UI prefab (Assets/Combat/Resources/UI/GuideUI.prefab).
/// The layout, fonts and art are edited in the Editor; GuideNPC only fills in text.
/// </summary>
public class GuideUIView : MonoBehaviour
{
    [Header("[E] Talk prompt (follows the Guide on screen)")]
    public RectTransform prompt;

    [Header("Dialogue box")]
    public GameObject dialogBox;
    public TextMeshProUGUI dialogName;
    public TextMeshProUGUI dialogText;
    public TextMeshProUGUI dialogHint;
    public Image dialogPortrait;

    [Header("Shop box")]
    public GameObject shopBox;
    public TextMeshProUGUI shopName;
    public TextMeshProUGUI shopSay;
    public TextMeshProUGUI shopCoins;
    public Image shopPortrait;
    public ShopRowView[] rows;
}
