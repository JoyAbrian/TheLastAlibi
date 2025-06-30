using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EvidenceManager : MonoBehaviour
{
    public GameObject evidencePanel;
    public GameObject evidenceTextPrefab;

    private HashSet<string> knownEvidence = new();

    private void Start()
    {
        if (SuspectAIManager.GeneratedProfile != null)
        {
            DisplayEvidence(SuspectAIManager.GeneratedProfile.evidence);
        }

        if (FindObjectOfType<SuspectAIManager>()?.interrogationLog != null)
        {
            SyncEvidenceFromLog(FindObjectOfType<SuspectAIManager>().interrogationLog);
        }
    }

    public void DisplayEvidence(List<string> clues)
    {
        if (clues == null) return;

        foreach (string clue in clues)
        {
            AddEvidence(clue);
        }
    }

    public void AddEvidence(string clue)
    {
        if (string.IsNullOrWhiteSpace(clue) || knownEvidence.Contains(clue))
            return;

        knownEvidence.Add(clue);

        GameObject clueGO = Instantiate(evidenceTextPrefab, evidencePanel.transform);
        TMP_Text clueText = clueGO.GetComponent<TMP_Text>();
        if (clueText != null)
        {
            clueText.text = "• " + clue;
            Canvas.ForceUpdateCanvases();
            clueText.ForceMeshUpdate();
        }
    }

    public void SyncEvidenceFromLog(SuspectInterrogationLog log)
    {
        if (log == null) return;

        List<string> clues = log.GetAllClues();
        foreach (string clue in clues)
        {
            AddEvidence(clue);
        }
    }
}