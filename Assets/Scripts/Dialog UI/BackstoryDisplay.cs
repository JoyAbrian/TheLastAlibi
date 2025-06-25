using System.Collections.Generic;
using UnityEngine;

public class BackstoryDisplay : MonoBehaviour
{
    public TypewriterText typewriterText;
    public FadeManager fadeManager;

    private List<string> paragraphs;
    private int currentIndex = 0;

    void Start()
    {
        paragraphs = SuspectAIManager.GeneratedProfile?.backstory ?? new List<string> { "No backstory loaded." };
        currentIndex = 0;

        StartCurrentParagraph();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            if (typewriterText.IsTyping())
            {
                typewriterText.Skip();
            }
            else
            {
                ShowNextParagraph();
            }
        }
    }

    void StartCurrentParagraph()
    {
        if (currentIndex < paragraphs.Count)
        {
            typewriterText.StartTyping(paragraphs[currentIndex]);
        }
        else
        {
            Debug.Log("Backstory finished!");
            SoundManager.PlaySound(SoundType.GameStart, GlobalVariables.SOUND_EFFECTS_VOLUME);
            fadeManager.FadeToScene("GameScene");
        }
    }

    void ShowNextParagraph()
    {
        currentIndex++;
        StartCurrentParagraph();
    }
}