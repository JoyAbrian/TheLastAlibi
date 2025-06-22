using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;

    public Sprite btn;
    public Sprite btnHover;

    private void Start()
    {
        image = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.sprite = btnHover;
        SoundManager.PlaySound(SoundType.ButtonHover, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.sprite = btn;
    }

    public void PlayClickSound()
    {
        SoundManager.PlaySound(SoundType.ButtonClick, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
    }
}