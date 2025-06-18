using UnityEngine;

public class Instructions : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject instructionsPanel;

    public void OnInstructionsButtonClick()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    public void OnBackButtonClick()
    {
        mainMenuPanel.SetActive(true);
        instructionsPanel.SetActive(false);
    }
}