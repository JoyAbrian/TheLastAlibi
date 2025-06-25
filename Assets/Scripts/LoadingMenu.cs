using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingMenu : MonoBehaviour
{
    public Slider loadingSlider;
    public SuspectAIManager suspectAIManager;
    public Suspect[] suspects;

    public FadeManager fadeManager;
    public string nextSceneName = "BackstoryScene";

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

        float stallPoint = Random.Range(0.3f, 0.6f);
        float progress = 0f;
        bool isDone = false;

        suspectAIManager.GenerateSuspectProfile(profile =>
        {
            isDone = true;
        });

        while (!isDone)
        {
            if (progress < stallPoint)
            {
                progress += Time.deltaTime * 0.2f;
                loadingSlider.value = Mathf.Clamp01(progress);
            }
            yield return null;
        }

        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f;
            loadingSlider.value = Mathf.Clamp01(progress);
            yield return null;
        }

        yield return new WaitForSeconds(2f);
        fadeManager.FadeToScene(nextSceneName);
    }
}