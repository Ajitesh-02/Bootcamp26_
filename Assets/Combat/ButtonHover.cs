using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Grows a UI button slightly while the mouse is over it, shrinks a bit when pressed.</summary>
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.06f;
    public float pressScale = 0.97f;
    public float speed = 14f;

    private float target = 1f;
    private bool over;

    public void OnPointerEnter(PointerEventData e) { over = true; target = hoverScale; }
    public void OnPointerExit(PointerEventData e) { over = false; target = 1f; }
    public void OnPointerDown(PointerEventData e) { target = pressScale; }
    public void OnPointerUp(PointerEventData e) { target = over ? hoverScale : 1f; }

    private void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x, target, Time.unscaledDeltaTime * speed);
        transform.localScale = new Vector3(s, s, 1f);
    }
}
