using UnityEngine;

public class SuspectManager : MonoBehaviour
{
    public Suspect[] suspects;

    public static Suspect SuspectSingleton;

    private void Awake()
    {
        foreach (Suspect suspect in suspects)
        {
            if (suspect.NPCName == SuspectAIManager.GeneratedProfile.name)
            {
                SuspectSingleton = suspect;
                break;
            }
        }
    }
}