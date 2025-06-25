using TMPro;
using UnityEngine;

public class SuspectNameBox : MonoBehaviour
{
    public TextMeshProUGUI suspectNameText;

    private void Start()
    {
        suspectNameText.text = SuspectManager.SuspectSingleton.NPCName;
    }
}