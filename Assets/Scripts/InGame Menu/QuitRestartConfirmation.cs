using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitRestartConfirmation : MonoBehaviour
{
    public GameObject settingsPanel;
    public FadeManager fadeManager;

    public void RestartGame()
    {
        fadeManager.FadeToScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        fadeManager.FadeToScene("MenuScene");
    }

    public void Back()
    {
        settingsPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}