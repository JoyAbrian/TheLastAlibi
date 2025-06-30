using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsBackground : MonoBehaviour
{
    [SerializeField] private Image statsBackground;
    [SerializeField] private TextMeshProUGUI guiltyText, statsText;

    [SerializeField] private Sprite trueBackground, falseBackground;

    private void Start()
    {
        if (StatisticVariables.finalAccusation == GlobalVariables.IS_SUSPECT_GUILTY)
        {
            statsBackground.sprite = trueBackground;
            statsText.text = "CORRECT ACCUSATION";
        } 
        else
        {
            statsBackground.sprite = falseBackground;
            statsText.text = "FALSE ACCUSATION";
        }

        if (GlobalVariables.IS_SUSPECT_GUILTY)
        {
            guiltyText.text = "Guilty!";
        }
        else
        {
            guiltyText.text = "Not Guilty!";
        }
    }
}