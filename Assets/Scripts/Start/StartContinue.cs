using UnityEngine;

public class StartContinue : MonoBehaviour
{
    public FadeManager fadeManager;

    void Update()
    {
        if (Input.anyKeyDown)
        {
            SoundManager.PlaySound(SoundType.Start, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
            fadeManager.FadeToScene("MenuScene");
        }
    }
}