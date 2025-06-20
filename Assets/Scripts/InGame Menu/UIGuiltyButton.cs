using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIGuiltyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;

    private void Start()
    {
        outline = GetComponent<Outline>();
        outline.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.enabled = false;
    }
}