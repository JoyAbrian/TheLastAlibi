using TMPro;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cluesText, minutesPlayedText, conversationText, accuracyText;

    private void Start()
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        cluesText.text = CreateTOCLine("Clues Found", GlobalVariables.TOTAL_CLUES_FOUND.ToString());
        minutesPlayedText.text = CreateTOCLine("Minutes Played", ConvertTime(StatisticVariables.totalTimeSpend.ToString()));
        conversationText.text = CreateTOCLine("Conversations", GlobalVariables.TOTAL_CONVERSATIONS.ToString());
        accuracyText.text = CreateTOCLine("Accuracy", $"{CalculateAccuracy()}%");
    }

    public string CreateTOCLine(string title, string value, int totalLength = 40)
    {
        int dotsCount = totalLength - title.Length - value.Length;
        dotsCount = Mathf.Max(2, dotsCount);
        return $"{title} {new string('.', dotsCount)} {value}";
    }

    public string ConvertTime(string totalTimes)
    {
        if (!int.TryParse(totalTimes, out int totalSeconds))
            return "00m00s";

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"{minutes:D2}m{seconds:D2}s";
    }

    public int CalculateAccuracy()
    {
        int score = 100;
        if (StatisticVariables.finalAccusation != GlobalVariables.IS_SUSPECT_GUILTY)
        {
            score -= 80;
        }

        score -= StatisticVariables.totalTimeSpend / 60 * 2;
        score -= StatisticVariables.totalConversation * 1;
        score += StatisticVariables.totalCluesFound * 5;

        return Mathf.Clamp(score, 0, 100);
    }
}