using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TextFadeIn : MonoBehaviour
{
    public List<CanvasGroup> textGroups;
    public float fadeDuration = 0.5f;
    public float delayBetween = 0.2f;
    public FadeManager fadeManager;

    private bool skipCurrent = false;
    private bool allShown = false;

    private void Start()
    {
        StartCoroutine(ShowTextsSequentially());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!allShown)
            {
                skipCurrent = true;
            }
            else
            {
                fadeManager.FadeToScene("MenuScene");
            }
        }
    }

    private IEnumerator ShowTextsSequentially()
    {
        foreach (var group in textGroups)
            group.alpha = 0;

        foreach (var group in textGroups)
        {
            float timer = 0f;
            SoundManager.PlaySound(SoundType.Swish, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
            skipCurrent = false;

            while (timer < fadeDuration)
            {
                if (skipCurrent)
                {
                    group.alpha = 1f;
                    break;
                }

                timer += Time.deltaTime;
                group.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }

            group.alpha = 1f;
            yield return new WaitForSeconds(delayBetween);
        }

        allShown = true;
    }
}