using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitRestartConfirmation : MonoBehaviour
{
    public GameObject settingsPanel;

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void Back()
    {
        settingsPanel.SetActive(true);
        gameObject.SetActive(false);
    }
}