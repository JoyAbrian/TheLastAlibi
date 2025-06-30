using UnityEngine;
using TMPro;

public class LogManager : MonoBehaviour
{
    public GameObject logPanel;
    public GameObject logTextPrefab;

    public void AddLog(string playerText, string npcText)
    {
        CreateText($"You: {playerText}");
        CreateText($"{SuspectManager.SuspectSingleton.NPCName}: {npcText}");

        StatisticVariables.totalConversation++;
    }

    private void CreateText(string content)
    {
        GameObject logEntry = Instantiate(logTextPrefab, logPanel.transform);
        TMP_Text text = logEntry.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.text = content;
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate();
        }
    }
}