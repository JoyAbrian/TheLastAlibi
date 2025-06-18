using UnityEngine;

public class PlayGame : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject playGamePanel;

    public void OnPlayButtonClick()
    {
        mainMenuPanel.SetActive(false);
        playGamePanel.SetActive(true);
    }

    public void OnBackButtonClick()
    {
        mainMenuPanel.SetActive(true);
        playGamePanel.SetActive(false);
    }

    public void OnDifficultyButtonClick(string difficulty)
    {
        // Start Game
        Debug.Log($"Starting game with difficulty: {difficulty}");
    }
}