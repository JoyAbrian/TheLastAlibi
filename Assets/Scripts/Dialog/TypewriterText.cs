using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterText : MonoBehaviour
{
    public TextMeshProUGUI uiText;
    [TextArea] public string fullText;
    public float typeSpeed = 0.05f;
    public bool isPlaySound = true;
    public bool IsTypingFinished { get; private set; } = false;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool skipRequested = false;

    public void StartTyping(string text)
    {
        fullText = text;
        uiText.text = "";
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        IsTypingFinished = false;
        skipRequested = false;
        uiText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipRequested)
            {
                uiText.text = fullText;
                break;
            }

            uiText.text += fullText[i];
            if (isPlaySound)
            {
                SoundManager.PlaySound(SoundType.Typing, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
            }
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        IsTypingFinished = true;
    }

    public bool IsTyping() => isTyping;

    public void Skip()
    {
        skipRequested = true;
    }
}