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
        HideClueFoundIcon();
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

    public bool AddEvidence(string clue)
    {
        if (string.IsNullOrWhiteSpace(clue)) return false;
        if (knownEvidence.Contains(clue)) return false;

        knownEvidence.Add(clue);

        GlobalVariables.TOTAL_CLUES_FOUND++;

        if (!GlobalVariables.FOUND_EVIDENCE.Contains(clue))
        {
            GlobalVariables.FOUND_EVIDENCE.Add(clue);
        }

        if (evidenceTextPrefab != null && evidencePanel != null)
        {
            GameObject clueGO = Instantiate(evidenceTextPrefab, evidencePanel.transform);
            TMP_Text clueText = clueGO.GetComponent<TMP_Text>();

            if (clueText != null)
            {
                clueText.text = $"• {clue}";
            }

            Canvas.ForceUpdateCanvases();
        }
        else
        {
            Debug.LogError("❌ EvidenceManager: Prefab atau Panel belum di-assign di Inspector!");
        }

        return true;
    }

    public void ShowClueFoundIcon()
    {
        clueFoundIcon.SetActive(true);
    }

    public void HideClueFoundIcon()
    {
        clueFoundIcon.SetActive(false);
    }
}