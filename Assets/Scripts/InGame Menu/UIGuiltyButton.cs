using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class UIGuiltyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    private Button button;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        outline = GetComponent<Outline>();
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (outline != null) outline.enabled = false;
    }

    private void Update()
    {
        bool canAccuse = CheckUnlockConditions();

        button.interactable = canAccuse;

        canvasGroup.alpha = canAccuse ? 1f : 0.3f;
    }

    private bool CheckUnlockConditions()
    {
        int clues = GlobalVariables.TOTAL_CLUES_FOUND;
        int conversations = GlobalVariables.TOTAL_CONVERSATIONS;
        string mode = GlobalVariables.GAME_DIFFICULTY;

        switch (mode)
        {
            case "Easy":
                return clues >= 5 || conversations >= 10;

            case "Normal":
                return clues >= 7 || conversations >= 15;

            case "Hard":
                return clues >= 7 || conversations >= 15;

            default:
                return clues >= 7 || conversations >= 15;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            outline.enabled = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.enabled = false;
    }
}