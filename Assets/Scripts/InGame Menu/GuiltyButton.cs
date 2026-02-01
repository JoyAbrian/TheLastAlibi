using UnityEngine;

public class GuiltyButton : MonoBehaviour
{
    public FadeManager fadeManager;

    public void Guilty(bool isGuilty)
    {
        SoundManager.PlaySound(SoundType.GameStart, volume: GlobalVariables.SOUND_EFFECTS_VOLUME);
        
        StatisticVariables.finalAccusation = isGuilty;
        fadeManager.FadeToScene("StatisticsScene");
    }
}