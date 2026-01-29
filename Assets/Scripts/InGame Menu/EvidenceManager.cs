using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EvidenceManager : MonoBehaviour
{
    public GameObject evidencePanel;
    public GameObject evidenceTextPrefab;
    public GameObject clueFoundIcon;
    private HashSet<string> knownEvidence = new();

    private void Start()
    {
        clueFoundIcon.SetActive(false);
        if (SuspectAIManager.GeneratedProfile != null)
        {
            DisplayEvidence(SuspectAIManager.GeneratedProfile.evidence);
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

    public void ShowClueFoundIcon()
    {
        clueFoundIcon.SetActive(true);
    }
}