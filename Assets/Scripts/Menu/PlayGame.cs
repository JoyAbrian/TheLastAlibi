using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayGame : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject playGamePanel;

    public FadeManager fadeManager;

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
        GlobalVariables.GAME_DIFFICULTY = difficulty;
        fadeManager.FadeToScene("LoadingScene");
    }
}