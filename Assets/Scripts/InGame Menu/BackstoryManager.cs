using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BackstoryManager : MonoBehaviour
{
    public TextMeshProUGUI backstoryText;

    private void OnEnable()
    {
        RefreshBackstory();
    }

    public void RefreshBackstory()
    {
        if (SuspectAIManager.GeneratedProfile == null)
        {
            backstoryText.text = "Data tersangka belum tersedia. Silakan generate profile terlebih dahulu.";
            return;
        }

        List<string> storyParagraphs = SuspectAIManager.GeneratedProfile.backstory;
        if (storyParagraphs != null && storyParagraphs.Count > 0)
        {
            string fullText = string.Join("\n\n", storyParagraphs);
            backstoryText.text = fullText;
        }
        else
        {
            backstoryText.text = "Tidak ada informasi latar belakang.";
        }
    }
}