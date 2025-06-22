using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingMenu : MonoBehaviour
{
    public Slider loadingSlider;
    public SuspectAIManager suspectAIManager;
    public Suspect[] suspects;

    public string nextSceneName = "GameScene";

    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        loadingSlider.value = 0f;

        int index = Random.Range(0, suspects.Length);
        GlobalVariables.CURRENT_SUSPECT = suspects[index];
        GlobalVariables.CURRENT_SUSPECT_NAME = GlobalVariables.CURRENT_SUSPECT.NPCName;
        GlobalVariables.CURRENT_SUSPECT_PERSONALITY = GlobalVariables.CURRENT_SUSPECT.personality;

        float progress = 0f;
        bool isDone = false;

        suspectAIManager.GenerateSuspectProfile(profile =>
        {
            Debug.Log("Suspect generated: " + profile.name);
            isDone = true;
        });

        while (!isDone)
        {
            progress += Time.deltaTime * 0.2f;
            loadingSlider.value = Mathf.Clamp01(progress);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextSceneName);
        loadOp.allowSceneActivation = false;

        while (!loadOp.isDone)
        {
            float loadProgress = Mathf.Clamp01(loadOp.progress / 0.9f);
            loadingSlider.value = loadProgress;

            if (loadProgress >= 1f)
                loadOp.allowSceneActivation = true;

            yield return null;
        }
    }
}